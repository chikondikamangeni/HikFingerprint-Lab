using System;
using System.IO;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace HikFingerprintLab;

/// <summary>
/// Experimental interop with the FPModule SDK already installed by iVMS-4200.
/// The declarations for these specific functions were validated in Windows PowerShell
/// against one FPModule_SDK_x64.dll v2.2.1.0 on a DS-K1F820-F.
/// This is not a WBDI driver, Windows Hello integration, or secure sign-in product.
/// </summary>
internal static class FingerprintSdk
{
    internal const string DllName = "FPModule_SDK_x64.dll";
    private static string? _selectedPath;
    private static IntPtr _module;
    private static readonly string[] RequiredExports =
    {
        "FPModule_OpenDevice", "FPModule_CloseDevice", "FPModule_SetTimeout",
        "FPModule_SetCollectTimes", "FPModule_DetectFinger", "FPModule_FpEnroll",
        "FPModule_GetQuality", "FPModule_MatchTemplate"
    };

    static FingerprintSdk()
    {
        NativeLibrary.SetDllImportResolver(typeof(FingerprintSdk).Assembly, ResolveImport);
    }

    internal static string? SdkPath => _selectedPath ?? FindInstalledSdk();
    internal static bool CanChangeLocation => _module == IntPtr.Zero;

    private static string? FindInstalledSdk()
    {
        foreach (string root in new[] {
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) })
        {
            if (string.IsNullOrWhiteSpace(root)) continue;
            foreach (string product in new[] { "iVMS-4200 Site", "iVMS-4200" })
            {
                string candidate = Path.Combine(root, product, "iVMS-4200 Client", "Client", DllName);
                if (File.Exists(candidate)) return candidate;
            }
        }
        return null;
    }

    internal static void SelectSdk(string path)
    {
        if (!CanChangeLocation) throw new InvalidOperationException("Restart the app before changing the loaded SDK.");
        string full = Path.GetFullPath(path);
        string root = Path.GetPathRoot(full) ?? "";
        if (root.Length != 3 || root[1] != ':' || new DriveInfo(root).DriveType != DriveType.Fixed)
            throw new InvalidOperationException("Select the SDK from a local fixed drive.");
        if (!string.Equals(Path.GetFileName(full), DllName, StringComparison.OrdinalIgnoreCase) || !File.Exists(full))
            throw new FileNotFoundException("Select the existing FPModule_SDK_x64.dll from a trusted Hikvision installation.");
        _selectedPath = full;
    }

    internal static string DescribeSdk()
    {
        string? path = SdkPath;
        if (path is null) return "SDK not found. Use SDK location to select a trusted installation.";
        return $"SDK: {path} | File version: {FileVersionInfo.GetVersionInfo(path).FileVersion ?? "unknown"}";
    }

    private static IntPtr ResolveImport(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (!string.Equals(libraryName, DllName, StringComparison.Ordinal)) return IntPtr.Zero;
        if (_module == IntPtr.Zero) throw new DllNotFoundException("SDK has not been explicitly loaded.");
        return _module;
    }

    private static void EnsureLibraryLoaded()
    {
        if (_module != IntPtr.Zero) return;
        string path = SdkPath ?? throw new FileNotFoundException("Hikvision SDK not found. Use SDK location to select it.");
        // Explicit absolute path; dependencies searched in the SDK folder and System32.
        // LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR | LOAD_LIBRARY_SEARCH_SYSTEM32
        IntPtr module = LoadLibraryExW(path, IntPtr.Zero, 0x00000100 | 0x00000800);
        if (module == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not load the SDK or its dependencies.");
        try
        {
            foreach (string name in RequiredExports)
                if (!NativeLibrary.TryGetExport(module, name, out _))
                    throw new EntryPointNotFoundException($"SDK does not export {name}.");
            _selectedPath = path;
            _module = module; // Keep the module loaded until process exit; P/Invoke caches addresses.
        }
        catch
        {
            NativeLibrary.Free(module);
            throw;
        }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    private static extern IntPtr LoadLibraryExW(string path, IntPtr file, uint flags);

    internal static void TestConnection(Action<string> report) =>
        WithDevice(report, () => true);

    private const int TemplateSize = 512;
    private const int SecurityLevel = 4;

    [DllImport(DllName, EntryPoint = "FPModule_OpenDevice", CallingConvention = CallingConvention.StdCall)]
    private static extern int OpenDevice();

    [DllImport(DllName, EntryPoint = "FPModule_CloseDevice", CallingConvention = CallingConvention.StdCall)]
    private static extern int CloseDevice();

    [DllImport(DllName, EntryPoint = "FPModule_SetTimeout", CallingConvention = CallingConvention.StdCall)]
    private static extern int SetTimeout(int seconds);

    [DllImport(DllName, EntryPoint = "FPModule_SetCollectTimes", CallingConvention = CallingConvention.StdCall)]
    private static extern int SetCollectTimes(int count);

    [DllImport(DllName, EntryPoint = "FPModule_DetectFinger", CallingConvention = CallingConvention.StdCall)]
    private static extern int DetectFinger(ref int status);

    [DllImport(DllName, EntryPoint = "FPModule_FpEnroll", CallingConvention = CallingConvention.StdCall)]
    private static extern int Enroll([Out] byte[] templateData);

    [DllImport(DllName, EntryPoint = "FPModule_GetQuality", CallingConvention = CallingConvention.StdCall)]
    private static extern int GetQuality([In] byte[] templateData);

    [DllImport(DllName, EntryPoint = "FPModule_MatchTemplate", CallingConvention = CallingConvention.StdCall)]
    private static extern int MatchTemplate(
        [In] byte[] templateA, [In] byte[] templateB, int securityLevel);

    internal sealed record EnrollmentResult(byte[] Template, int Quality);
    internal sealed record VerificationResult(int MatchCode, int Quality);

    internal static EnrollmentResult EnrollFingerprint(Action<string> report) =>
        WithDevice(report, () => Capture(report));

    internal static VerificationResult VerifyFingerprint(byte[] referenceTemplate, Action<string> report)
    {
        if (referenceTemplate.Length != TemplateSize)
            throw new ArgumentException("Reference template has unexpected size.", nameof(referenceTemplate));

        return WithDevice(report, () =>
        {
            var captured = Capture(report);
            try
            {
                report("Comparing the newly captured template...");
                int code = MatchTemplate(referenceTemplate, captured.Template, SecurityLevel);
                return new VerificationResult(code, captured.Quality);
            }
            finally
            {
                Array.Clear(captured.Template, 0, captured.Template.Length);
            }
        });
    }

    private static T WithDevice<T>(Action<string> report, Func<T> action)
    {
        if (!Environment.Is64BitProcess)
            throw new InvalidOperationException("This prototype must run as a 64-bit process.");

        EnsureLibraryLoaded();
        bool opened = false;

        try
        {
            report("Opening scanner...");
            RequireOk(OpenDevice(), "OpenDevice");
            opened = true;
            RequireOk(SetTimeout(60), "SetTimeout");
            RequireOk(SetCollectTimes(1), "SetCollectTimes");
            return action();
        }
        finally
        {
            if (opened)
            {
                try { _ = CloseDevice(); } catch { /* keep the original error if one occurred */ }
            }
        }
    }

    private static EnrollmentResult Capture(Action<string> report)
    {
        byte[] template = new byte[TemplateSize];
        try
        {
            report("Lift your finger off the scanner...");
            WaitForFingerRemoval();

            report("Place your finger on the scanner now. One capture is required.");
            Thread.Sleep(350); // gives UI a moment to show the instruction
            RequireOk(Enroll(template), "FpEnroll");

            int nonZero = 0;
            foreach (byte b in template)
                if (b != 0) nonZero++;
            if (nonZero == 0)
                throw new InvalidOperationException("The SDK returned an empty template.");

            int quality = GetQuality(template);
            return new EnrollmentResult(template, quality);
        }
        catch
        {
            Array.Clear(template, 0, template.Length);
            throw;
        }
    }

    private static void WaitForFingerRemoval()
    {
        int clearCount = 0;
        DateTime deadline = DateTime.UtcNow.AddSeconds(20);

        while (DateTime.UtcNow < deadline)
        {
            int status = -1;
            RequireOk(DetectFinger(ref status), "DetectFinger");
            if (status == 0)
            {
                clearCount++;
                if (clearCount >= 3)
                    return;
            }
            else
            {
                clearCount = 0;
            }
            Thread.Sleep(300);
        }
        throw new InvalidOperationException("Scanner did not confirm finger removal within 20 seconds.");
    }

    private static void RequireOk(int code, string function)
    {
        if (code != 0)
            throw new InvalidOperationException($"{function} returned SDK code {code}.");
    }
}
