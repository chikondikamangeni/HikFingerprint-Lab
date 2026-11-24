# HikFingerprint Lab

**A Windows desktop fingerprint-enrollment and verification experiment for the Hikvision DS-K1F820-F.**

![.NET 8](https://img.shields.io/badge/.NET-8-512BD4) ![Platform](https://img.shields.io/badge/platform-Windows%20x64-0078D4) ![Status](https://img.shields.io/badge/status-experimental-orange) ![License](https://img.shields.io/badge/license-MIT-blue)

HikFingerprint Lab is an open-source **C# / .NET 8 Windows Forms** proof of concept. It calls the fingerprint SDK from a locally installed copy of Hikvision iVMS-4200 to capture, enroll and compare fingerprint templates. The app can optionally persist a locally encrypted enrollment using Windows **DPAPI CurrentUser**.

> [!CAUTION]
> **Not Windows Hello, not a WBDI driver, and not a Windows login or access-control product.** Do not depend on it for real-world authentication. The SDK's spoof resistance, failure rates, provenance and licensing requirements have not been independently validated. A positive or negative result in a few manual tests is not a security certification.

## Interface

The v0.2.0 candidate adds a compact dashboard, decorative scanning animation, separate session metrics, SDK quality reporting, and collapsible diagnostics. The graphic is illustrative and does not display actual fingerprint images.

## Features

| Feature | Status |
| --- | --- |
| Connect to DS-K1F820-F through vendor SDK | Demonstrated on one Windows 11 machine |
| Enroll a fingerprint and check SDK quality | Implemented |
| Verify against an enrolled template | Implemented (SDK result 0 = match; 6 = no match in tests) |
| Optional encrypted enrollment across restarts | Implemented, user-confirmed in testing |
| Clear enrollment | Implemented (best-effort deletion) |
| Session statistics and 30-second demonstration cooldown | Implemented |
| Windows Hello or lock-screen unlocking | **Not supported** |

## Requirements

- Windows 10/11 x64 (tested on Windows 11), with **.NET 8 SDK** for building and Windows Desktop runtime for running.
- Hikvision **DS-K1F820-F** fingerprint enrollment scanner.
- A legitimate iVMS-4200 installation exposing the compatible SDK at:

  ```text
  C:\Program Files (x86)\iVMS-4200 Site\iVMS-4200 Client\Client\FPModule_SDK_x64.dll
  ```

- Close iVMS-4200 before using the scanner from this app. The vendor SDK and its dependencies **are not included** in this repository.

> [!NOTE]
> The current interop source deliberately uses the tested absolute path above. Other iVMS installation locations require updating `FingerprintSdk.DllPath` and its P/Invoke DLL path before building. Compatibility with other SDK versions or scanners is not guaranteed.

## Quick start

Open **64-bit PowerShell**:

```powershell
# From the cloned repository directory
dotnet restore .\HikFingerprintLabV2.csproj
dotnet run --project .\HikFingerprintLabV2.csproj --configuration Release
```

No Visual Studio installation is required if the .NET 8 SDK is already installed. A GitHub Actions Windows build checks that the **source compiles without bundling any Hikvision DLLs**, but cannot test a physical scanner.

### Try it

1. Disconnect iVMS-4200 from the device (fully close the program).
2. Check **Remember enrollment for this Windows user** *before* enrolling if you want persistence.
3. Select **Enroll / Replace**, follow the sensor prompts, and wait for success.
4. Select **Verify Finger** with the enrolled finger (the SDK may return `MATCH (0)`).
5. Try a different finger (the SDK may return `NO MATCH (6)`).
6. Close and reopen the app to test saved enrollment; use **Clear All Enrollment** to remove it.

See [testing guidance](docs/TEST-PLAN.md) before drawing conclusions from successful scans.

## Security and privacy

- Fingerprint templates are **biometric data**. Only enroll fingerprints you are authorized to collect.
- Enrollment is **memory-only by default** unless you opt into persistent storage. When enabled, the file is written to `%LOCALAPPDATA%\HikFingerprintLabV2\enrollment.dpapi` and protected with DPAPI `CurrentUser`.
- Other processes running as the same user (or with sufficient access) can potentially decrypt or extract biometric data. Deletion and memory clearing are **best-effort**, not guaranteed secure erasure.
- No cloud service, network authentication, Windows credentials, account password collection or telemetry is implemented.
- The 30-second cooldown resets when the app restarts and is **not an authentication security control**.
- There was an anomalous earlier different-finger match during development; subsequent controlled tests rejected different fingers. Results are **not evidence of production-grade accuracy**.
- Do **not** upload enrollment files, captured templates, debug memory dumps, or the proprietary SDK to GitHub.

More information: [Security policy](SECURITY.md) · [Architecture](docs/ARCHITECTURE.md) · [SDK setup](docs/HARDWARE-SETUP.md).

## Build / project layout

```text
HikFingerprint-Lab/
├── HikFingerprintLabV2.csproj
├── Program.cs
├── MainForm.cs
├── FingerprintSdk.cs
├── FingerprintVerificationService.cs
├── EnrollmentStore.cs
├── docs/
│   ├── ARCHITECTURE.md
│   ├── HARDWARE-SETUP.md
│   └── TEST-PLAN.md
├── scripts/
│   └── check-publication.ps1
├── .github/workflows/build.yml
├── CONTRIBUTING.md
├── SECURITY.md
├── CHANGELOG.md
└── LICENSE
```

## Limitations

This project was exercised with `FPModuleSDK_Win_x64_V2.2.1_Build200227`. Only one scanner/SDK combination has been tried; vendor SDK exports are not a documented stable public API here. Native calls are potentially process-crashing if signatures differ. There is no Windows Hello integration and no published false acceptance/rejection rate, hardware-backed template protection, liveness verification or formal security audit.

## License and trademarks

The original source code in this repository is offered under [MIT](LICENSE). **No rights are granted to redistribute Hikvision libraries or to use its trademarks beyond identification.** Hikvision and iVMS-4200 belong to their respective owners; this is an independent community experiment, not an official Hikvision project.

Contributions welcome — start with [CONTRIBUTING.md](CONTRIBUTING.md).
