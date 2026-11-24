# Manual test plan (not a security certification)

The project currently has **no automated hardware tests**. Use this checklist on an authorized scanner with consenting participants.

1. Enroll one finger with memory-only storage; verify with the same finger.
2. Verify with a clearly different finger; check that the SDK returns `NO MATCH (6)`.
3. Repeat the scans under controlled placement and finger-lift conditions; record only results and quality numbers, **never actual templates**.
4. Opt into saved enrollment, close and reopen the application, and verify again without re-enrolling.
5. Trigger five consecutive non-matches and check that the demonstration cooldown appears; confirm it is reset on restart and **not a real authentication control**.
6. Clear enrollment and restart the application; verify the application no longer recognizes a saved enrollment.
7. Attempt unplugged-scanner and iVMS-in-use conditions; confirm error messages are understandable and no Windows login settings change.

Example report (do not include biometric data):

```text
Windows version: [your version]
Scanner: DS-K1F820-F
FPModule SDK: [version]
Expected same-finger matches: [N]
Observed matches: [N]
Expected different-finger rejections: [N]
Observed rejections: [N]
Unexpected matches or errors: [describe]
App restarted for DPAPI test: yes/no
```

### Known testing caveat

An early single-collection experiment unexpectedly matched templates from different fingers. A subsequent finger-removal-controlled experiment and later application runs produced expected rejections. This underscores why this repository makes **no production security claims**.

Controlled laboratory testing cannot establish biometric matching performance, spoof resistance, anti-replay properties, or secure Windows sign-in compatibility.
