# Publish the first public repository

This folder is a **source-only release candidate**, not an already-published GitHub repository. Review the source and the [security policy](SECURITY.md) before publishing it.

## Option A — GitHub website + PowerShell

1. Sign in to the **personal** GitHub account `chikondikamangeni`.
2. Open https://github.com/new and set repository name to `HikFingerprint-Lab`.
3. Set visibility to **Public** and leave GitHub's README, license, and `.gitignore` initialization options unchecked (these files are already in this folder).
4. From a PowerShell window **inside this unpacked folder**, run:

```powershell
./scripts/check-publication.ps1
git init -b main
git add .
git commit -m "Initial public source release v0.1.0"
git remote add origin https://github.com/chikondikamangeni/HikFingerprint-Lab.git
git push -u origin main
```

GitHub credentials may be requested by Git Credential Manager. ChatGPT's connected GitHub account does **not** automatically sign in Git on your computer. If you already initialized a local repo, skip `git init` and check existing remotes before adding a new one.

## Option B — GitHub CLI, if already installed and authenticated

```powershell
./scripts/check-publication.ps1
git init -b main
git add .
git commit -m "Initial public source release v0.1.0"
gh repo create chikondikamangeni/HikFingerprint-Lab --public --source . --remote origin --push
```

Do not use this option unless `gh auth status` confirms you are signed into the intended personal account.

## After pushing

- Open **Actions** and confirm the Windows build workflow passes.
- Review the README and documentation on GitHub.
- When satisfied, create a `v0.1.0` GitHub release using `RELEASE_NOTES_v0.1.0.md`. Label it **pre-release**, because the SDK has not undergone a security assessment and compilation has not been independently verified in the packaging environment.
- Do not attach proprietary SDK DLLs, fingerprint templates, encrypted enrollment files or raw biometric captures to releases.
