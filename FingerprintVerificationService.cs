using System;
using System.IO;

namespace HikFingerprintLab;

/// <summary>
/// Application-layer verification service, NOT a Windows service or sign-in provider.
/// Owns enrollment lifecycle, test counters and a session-only cooldown.
/// Native SDK access is serialized by the UI (one operation at a time).
/// </summary>
internal sealed class FingerprintVerificationService : IDisposable
{
    private byte[]? _memoryEnrollment;
    private int _consecutiveNonMatches;
    private DateTimeOffset _cooldownUntil;

    internal bool HasEnrollment => _memoryEnrollment is not null || EnrollmentStore.Exists;
    internal bool HasSavedEnrollment => EnrollmentStore.Exists;
    internal int Matches { get; private set; }
    internal int NonMatches { get; private set; }
    internal int UnexpectedResults { get; private set; }
    internal TimeSpan CooldownRemaining => _cooldownUntil > DateTimeOffset.UtcNow
        ? _cooldownUntil - DateTimeOffset.UtcNow : TimeSpan.Zero;

    internal int Enroll(bool rememberOnThisPc, Action<string> report)
    {
        // The SDK call can take up to 60 seconds. Enrollment replaces the old
        // reference only AFTER the capture and the optional save succeed.
        var result = FingerprintSdk.EnrollFingerprint(report);
        byte[] data = result.Template;
        try
        {
            if (rememberOnThisPc)
            {
                EnrollmentStore.Save(data);
                ClearMemoryEnrollment();
            }
            else
            {
                // Avoid silently restoring an older saved enrollment on next launch.
                EnrollmentStore.Delete();
                ClearMemoryEnrollment();
                _memoryEnrollment = data;
                data = Array.Empty<byte>(); // ownership transferred
            }
            _consecutiveNonMatches = 0;
            _cooldownUntil = default;
            return result.Quality;
        }
        finally
        {
            Array.Clear(data, 0, data.Length);
        }
    }

    internal FingerprintSdk.VerificationResult Verify(Action<string> report)
    {
        if (!HasEnrollment)
            throw new InvalidOperationException("No enrollment available.");
        if (CooldownRemaining > TimeSpan.Zero)
            throw new InvalidOperationException("Test cooldown active. Try again later.");

        byte[] reference = HasSavedEnrollment
            ? EnrollmentStore.Load()
            : (byte[])_memoryEnrollment!.Clone();

        try
        {
            var outcome = FingerprintSdk.VerifyFingerprint(reference, report);
            if (outcome.MatchCode == 0)
            {
                Matches++;
                _consecutiveNonMatches = 0;
            }
            else if (outcome.MatchCode == 6)
            {
                NonMatches++;
                _consecutiveNonMatches++;
                if (_consecutiveNonMatches >= 5)
                {
                    _cooldownUntil = DateTimeOffset.UtcNow.AddSeconds(30);
                    _consecutiveNonMatches = 0;
                }
            }
            else
            {
                UnexpectedResults++;
            }
            return outcome;
        }
        finally
        {
            Array.Clear(reference, 0, reference.Length);
        }
    }

    internal void ClearAll()
    {
        // If deletion fails, do not say enrollment was cleared.
        EnrollmentStore.Delete();
        ClearMemoryEnrollment();
        _consecutiveNonMatches = 0;
        _cooldownUntil = default;
    }

    private void ClearMemoryEnrollment()
    {
        if (_memoryEnrollment is null) return;
        Array.Clear(_memoryEnrollment, 0, _memoryEnrollment.Length);
        _memoryEnrollment = null;
    }

    public void Dispose() => ClearMemoryEnrollment();
}
