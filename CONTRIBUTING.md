# Contributing

Thanks for helping improve HikFingerprint Lab.

1. Propose a bug fix, documentation improvement or small self-contained change in an issue.
2. Keep all proprietary Hikvision DLLs, enrollment templates, captured images, personal logs and hardware identifiers out of commits.
3. Build with .NET 8 on Windows: `dotnet build .\HikFingerprintLabV2.csproj -c Release`.
4. Describe the scanner model, Windows build, SDK version, test steps and return codes in PRs **without** submitting biometric data.
5. Do not claim Windows Hello support or production security without independent validation.

Please keep the default app non-invasive: no sign-in provider registration, driver installation, forced elevated privileges, TPM operations or Windows account changes.

The bundled `.github/workflows/build.yml` compiles the app on GitHub-hosted Windows runners; it does not connect to physical hardware and cannot establish biometric accuracy.
