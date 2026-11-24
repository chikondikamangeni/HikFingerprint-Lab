# Changelog

## [0.2.0] — draft

- Redesigned dashboard with decorative scan animation and live session metrics.
- Collapsible diagnostic log and custom application icon.
- Existing fingerprint SDK, verification service and saved enrollment format unchanged.
- Windows build and physical scanner regression tests required before release.

## [0.1.0] — 2026-11-24 (release candidate)

- Windows Forms interface for fingerprint enrollment and SDK verification.
- Demonstrated integration with DS-K1F820-F and FPModule SDK v2.2.1 (x64).
- Optional DPAPI CurrentUser encrypted enrollment that survives application restarts.
- Memory-only enrollment by default; clear-enrollment action.
- Session counters and demonstration cooldown after repeated non-matches.
- Initial public documentation, security policy, publication scan and Windows GitHub Actions build.

**Known limitation:** This is not Windows Hello or a Windows sign-in provider. Real-world biometric security was not validated.
