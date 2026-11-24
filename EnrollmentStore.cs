using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace HikFingerprintLab;

/// <summary>
/// Optional local, per-Windows-user encrypted enrollment for LAB use only.
/// Uses Windows DPAPI CurrentUser (CryptProtectData without LOCAL_MACHINE flag).
/// IMPORTANT: Other programs running under the same Windows account can usually
/// decrypt the enrollment. This is NOT adequate protection for Windows login.
/// </summary>
internal static class EnrollmentStore
{
    private static readonly byte[] Magic = Encoding.ASCII.GetBytes("HFL2");
    private const int TemplateLength = 512;
    private const uint UiForbidden = 0x1;

    internal static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "HikFingerprintLabV2", "enrollment.dpapi");

    internal static bool Exists => File.Exists(FilePath);

    internal static void Save(byte[] template)
    {
        if (template.Length != TemplateLength)
            throw new ArgumentException("Invalid fingerprint template size.", nameof(template));

        byte[] protectedBytes = ProtectOrUnprotect(template, decrypt: false);
        string? tempFile = null;
        try
        {
            string path = FilePath;
            string folder = Path.GetDirectoryName(path)!;
            Directory.CreateDirectory(folder);

            // An unpredictable temporary name avoids overwriting another writer's temp file.
            tempFile = Path.Combine(folder, ".enrollment-" + Guid.NewGuid().ToString("N") + ".tmp");
            using (var fs = new FileStream(tempFile, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                fs.Write(Magic);
                fs.Write(protectedBytes);
                fs.Flush(flushToDisk: true);
            }
            File.Move(tempFile, path, overwrite: true);
            tempFile = null;
        }
        finally
        {
            Array.Clear(protectedBytes, 0, protectedBytes.Length);
            if (tempFile is not null && File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    internal static byte[] Load()
    {
        byte[] encrypted = File.ReadAllBytes(FilePath);
        try
        {
            if (encrypted.Length < Magic.Length + 1 ||
                !encrypted.AsSpan(0, Magic.Length).SequenceEqual(Magic))
                throw new InvalidDataException("Unrecognized enrollment file format.");

            byte[] ciphertext = encrypted.AsSpan(Magic.Length).ToArray();
            try
            {
                byte[] template = ProtectOrUnprotect(ciphertext, decrypt: true);
                if (template.Length != TemplateLength)
                {
                    Array.Clear(template, 0, template.Length);
                    throw new InvalidDataException("Unexpected decrypted template size.");
                }
                return template;
            }
            finally
            {
                Array.Clear(ciphertext, 0, ciphertext.Length);
            }
        }
        finally
        {
            Array.Clear(encrypted, 0, encrypted.Length);
        }
    }

    internal static void Delete()
    {
        // Deletes the file, but does not guarantee physical erasure from SSDs/backups.
        if (File.Exists(FilePath))
            File.Delete(FilePath);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DataBlob
    {
        public int Size;
        public IntPtr Data;
    }

    [DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptProtectData(ref DataBlob input, string? description,
        IntPtr entropy, IntPtr reserved, IntPtr prompt, uint flags, out DataBlob output);

    [DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptUnprotectData(ref DataBlob input, IntPtr description,
        IntPtr entropy, IntPtr reserved, IntPtr prompt, uint flags, out DataBlob output);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr LocalFree(IntPtr ptr);

    private static byte[] ProtectOrUnprotect(byte[] source, bool decrypt)
    {
        IntPtr inputPointer = Marshal.AllocHGlobal(source.Length);
        DataBlob output = default;
        try
        {
            Marshal.Copy(source, 0, inputPointer, source.Length);
            DataBlob input = new() { Size = source.Length, Data = inputPointer };
            bool success = decrypt
                ? CryptUnprotectData(ref input, IntPtr.Zero, IntPtr.Zero,
                    IntPtr.Zero, IntPtr.Zero, UiForbidden, out output)
                : CryptProtectData(ref input, "HikFingerprint Lab V2 enrollment",
                    IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, UiForbidden, out output);

            if (!success)
                throw new Win32Exception(Marshal.GetLastWin32Error(),
                    decrypt ? "Cannot decrypt enrollment for this Windows user." : "Could not encrypt enrollment.");
            if (output.Size <= 0 || output.Size > 16 * 1024 || output.Data == IntPtr.Zero)
                throw new InvalidDataException("Unexpected Windows DPAPI output.");

            byte[] result = new byte[output.Size];
            Marshal.Copy(output.Data, result, 0, result.Length);
            return result;
        }
        finally
        {
            // Best-effort clearing of native buffers (cannot control all SDK copies).
            if (inputPointer != IntPtr.Zero)
            {
                Marshal.Copy(new byte[source.Length], 0, inputPointer, source.Length);
                Marshal.FreeHGlobal(inputPointer);
            }
            if (output.Data != IntPtr.Zero)
            {
                if (output.Size > 0 && output.Size <= 16 * 1024)
                    Marshal.Copy(new byte[output.Size], 0, output.Data, output.Size);
                _ = LocalFree(output.Data);
            }
        }
    }
}
