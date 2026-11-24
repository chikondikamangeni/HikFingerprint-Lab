# Changelog

## [0.1.0] — 2026-11-24 (release candidate)

- Windows Forms interface for fingerprint enrollment and SDK verification.
- Demonstrated integration with DS-K1F820-F and FPModule SDK v2.2.1 (x64).
- Optional DPAPI CurrentUser encrypted enrollment that survives application restarts.
- Memory-only enrollment by default; clear-enrollment action.
- Session counters and demonstration cooldown after repeated non-matches.
- Initial public documentation, security policy, publication scan and Windows GitHub Actions build.

**Known limitation:** This is not Windows Hello or a Windows sign-in provider. Real-world biometric security was not validated.
