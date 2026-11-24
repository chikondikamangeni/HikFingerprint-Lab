# Hardware and SDK setup

1. Install a legitimate, compatible Hikvision iVMS-4200 package on Windows x64.
2. Check that `FPModule_SDK_x64.dll` exists at the path documented in the [README](../README.md). SDK release observed in testing: `FPModuleSDK_Win_x64_V2.2.1_Build200227`.
3. Connect the DS-K1F820-F directly to USB. Fully close iVMS-4200 before starting this app.
4. Run `dotnet run --project .\HikFingerprintLabV2.csproj --configuration Release` from the project root.

## Troubleshooting

| Symptom | What to check |
| --- | --- |
| SDK DLL not found | iVMS install path and DLL version; do not download random DLLs from untrusted sites |
| OpenDevice nonzero | iVMS still running, USB cable, unsupported SDK combination |
| Enrollment result 3 | Capture/enrollment failure; make sure sensor is clear and follow the on-screen prompts |
| Repeated no matches | Verify enrolled finger and persisted template; use controlled same/different-finger tests |
| Unexpected matches | Stop using matching as a security measure and collect non-sensitive reproduction steps |
| DPAPI decryption failure | Windows user account changed or profile migrated; re-enroll only when appropriate |

## Vendor dependencies

This repository intentionally omits all Hikvision SDK binaries and any license materials. Use the vendor's authorized distribution and comply with its SDK license. This project has only been physically exercised on one model/version combination.
