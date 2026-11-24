# Security policy

## Scope

HikFingerprint Lab is an **experimental fingerprint demonstration**, not an authentication product. Do not use it as a Windows login provider, access-control gatekeeper, payment authorization system, or other security-critical control.

## Reporting

If a security issue might expose biometric data or allow false acceptance, avoid sharing actual fingerprint templates, raw fingerprint images, keys, memory dumps or device identifiers in public issues. Once the repository is public, use **GitHub private vulnerability reporting / Security Advisories** if enabled. If that is not available, open an issue describing the general problem **without sensitive details** and ask for a private reporting channel.

## Known limitations

- DPAPI `CurrentUser` protects saved templates at rest, but same-user applications or highly privileged processes may decrypt them.
- A malicious or compromised native SDK can access process memory; the SDK's DLL signing and provenance must be assessed independently.
- The SDK does not provide a demonstrated Windows Hello / WBDI trust path here.
- Fingerprint capture and matching have not been formally evaluated for spoofing, replay resistance, liveness, false-acceptance rates, or false-rejection rates.
- In-memory wiping and file deletion are best-effort only.
- Session-only cooldown is a UI test aid, not an attack-resistant rate limit.
- The application is designed for a local interactive Windows session and does not implement an OS authentication boundary.

## User data

Never attach enrollment.dpapi, raw captures, templates, Windows passwords, access tokens, device secrets or crash dumps to public issues or pull requests.
