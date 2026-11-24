# Architecture

```text
Windows Forms UI (MainForm)
       |
       v
FingerprintVerificationService  <---->  EnrollmentStore
       |                                  | DPAPI CurrentUser
       v                                  v
FingerprintSdk (P/Invoke)             enrollment.dpapi
       |
       v
Local FPModule_SDK_x64.dll (iVMS installation, NOT bundled)
       |
       v
Hikvision DS-K1F820-F via USB
```

## Data flow

- Enrollment: capture a 512-byte SDK template; retain it only in memory or opt in to a DPAPI-protected per-user file.
- Verification: decrypt/clone reference in memory, capture a fresh template, call `FPModule_MatchTemplate` with security level `4`, and clear managed buffers on completion (best effort).
- Result codes seen in testing: `0` for a match and `6` for no match. Any other code is reported as unexpected.
- Device session: open device, configure a 60-second timeout and one collection, check finger removal, capture, close device.
- `SetDllDirectory` modifies process-wide native DLL resolution for the SDK session; avoid concurrent native SDK calls or untrusted DLL directories.

## Important boundaries

The verification service is a C# application component, **not** a Windows service. It has no Windows logon credential provider, biometric driver, credential manager integration, anti-spoofing assurance or security enforcement role. It does not unlock Windows.

The current P/Invoke absolute path is deliberate to retain the behavior of the originally tested machine; contributions supporting configurable paths must preserve dependency loading safety.
