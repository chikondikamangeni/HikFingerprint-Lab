using System;
using System.IO;
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
    internal const string DllPath =
        @"C:\Program Files (x86)\iVMS-4200 Site\iVMS-4200 Client\Client\FPModule_SDK_x64.dll";

    private const int TemplateSize = 512;
    private const int SecurityLevel = 4;

    [DllImport(DllPath, EntryPoint = "FPModule_OpenDevice", CallingConvention = CallingConvention.StdCall)]
    private static extern int OpenDevice();

    [DllImport(DllPath, EntryPoint = "FPModule_CloseDevice", CallingConvention = CallingConvention.StdCall)]
    private static extern int CloseDevice();

    [DllImport(DllPath, EntryPoint = "FPModule_SetTimeout", CallingConvention = CallingConvention.StdCall)]
    private static extern int SetTimeout(int seconds);

    [DllImport(DllPath, EntryPoint = "FPModule_SetCollectTimes", CallingConvention = CallingConvention.StdCall)]
    private static extern int SetCollectTimes(int count);

    [DllImport(DllPath, EntryPoint = "FPModule_DetectFinger", CallingConvention = CallingConvention.StdCall)]
    private static extern int DetectFinger(ref int status);

    [DllImport(DllPath, EntryPoint = "FPModule_FpEnroll", CallingConvention = CallingConvention.StdCall)]
    private static extern int Enroll([Out] byte[] templateData);

    [DllImport(DllPath, EntryPoint = "FPModule_GetQuality", CallingConvention = CallingConvention.StdCall)]
    private static extern int GetQuality([In] byte[] templateData);

    [DllImport(DllPath, EntryPoint = "FPModule_MatchTemplate", CallingConvention = CallingConvention.StdCall)]
    private static extern int MatchTemplate(
        [In] byte[] templateA, [In] byte[] templateB, int securityLevel);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetDllDirectory(string? path);

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

        if (!File.Exists(DllPath))
            throw new FileNotFoundException(
                "Hikvision SDK not found. Install/retain the existing iVMS-4200 installation.", DllPath);

        var folder = Path.GetDirectoryName(DllPath)!;
        bool opened = false;

        if (!SetDllDirectory(folder))
            throw new InvalidOperationException("Could not configure the Hikvision SDK library directory.");

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
            _ = SetDllDirectory(null);
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
