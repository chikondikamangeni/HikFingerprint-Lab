# HikFingerprint Lab v0.1.0 — first public release candidate

HikFingerprint Lab is an experimental C#/.NET 8 Windows Forms application demonstrating fingerprint capture, enrollment and template comparison with a **Hikvision DS-K1F820-F** scanner through a locally installed Hikvision FPModule SDK.

### Includes

- An interactive enrollment and verification interface
- Optional DPAPI CurrentUser encrypted local enrollment
- In-memory template handling, quality/status logs and clear enrollment
- Demonstration cooldown after repeated non-matches
- Source code, setup notes, security warnings and GitHub Actions compile checks

### Requirements

Windows x64, .NET 8, DS-K1F820-F, and a compatible independently licensed iVMS-4200 installation containing `FPModule_SDK_x64.dll`. Vendor DLLs are not bundled.

### Important limitation

**This does not provide Windows Hello, Windows unlocking or production-grade biometric authentication.** Matching and hardware compatibility have only been demonstrated in limited tests on a single device/SDK combination. Please review the [README](README.md) and [security policy](SECURITY.md).
