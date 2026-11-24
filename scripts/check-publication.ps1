# Checks for high-risk files in the source tree. This is not a full secrets scanner.
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$forbiddenExtensions = @('.dpapi', '.pfx', '.p12', '.pem', '.key', '.fpt', '.fingerprint', '.dmp', '.mdmp')
$vendorNames = @('FPModule_SDK.dll', 'FPModule_SDK_x64.dll', 'HCUSBSDK.dll', 'HCNetSDK.dll', 'libusb-1.0.dll')
$bad = @(Get-ChildItem -Path $root -Recurse -File | Where-Object {
    $relative = $_.FullName.Substring($root.Length).TrimStart('\','/')
    $segments = $relative -split '[\/]'
    if ($segments -contains '.git' -or $segments -contains 'bin' -or $segments -contains 'obj') { return $false }
    $_.Extension -in $forbiddenExtensions -or $_.Name -in $vendorNames -or $_.Name -eq 'enrollment.dpapi'
})
if ($bad.Count -gt 0) {
    Write-Host 'Files that must NOT be published:' -ForegroundColor Red
    $bad.FullName | ForEach-Object { Write-Host $_ }
    throw 'Publication check failed.'
}
Write-Host 'No blocked SDK binaries, enrollment files, or key files found.' -ForegroundColor Green
