SVGMapper
=========

Small WPF app for mapping SVG floor plans and seating.

Files to know
- `release/SVGMapper.Minimal.exe` — runnable application (release build)

How to run
- If you downloaded the `SVGMapper` folder (or ZIP), extract it, open the folder, then open the `release` folder and **double-click** `SVGMapper.Minimal.exe` to launch the application.
- Double-click `release/SVGMapper.Minimal.exe` on Windows.
- Or run from PowerShell:

```powershell
& .\release\SVGMapper.Minimal.exe
```

Quick notes
- If Windows blocks the app, right‑click the EXE → Properties → **Unblock**, or run:

```powershell
Unblock-File "path\to\SVGMapper.Minimal.exe"
```
- Verify the file's SHA256 before running (see release page and `CHANGELOG.md`).

Installation (via MSI installer)
- Download the latest MSI from [GitHub Releases](https://github.com/TALASSX/SVGMapper/releases).
- Double-click the MSI file and follow the installer prompts.
- Or install from command line (elevated PowerShell):

```powershell
msiexec /i "path\to\SVGMapper.Installer.msi"
```

**Recommended: Portable ZIP (no warnings)**
- Download `SVGMapper.Minimal.portable.zip` from [GitHub Releases](https://github.com/TALASSX/SVGMapper/releases).
- Extract the ZIP.
- Run `SVGMapper.Minimal.exe` directly (no installation needed).

Trust verification for MSI
- The MSI is unsigned, so verify before installing:
  - Compute SHA256: `Get-FileHash "path\to\SVGMapper.Installer.msi" -Algorithm SHA256`
  - Expected for v1.0.7: `91DCD3CFEA2F642C5B7016966E6D1C3F5763D17215B41D5E49E86CA306BCE421`
  - Scan with Windows Defender: `Start-MpScan -ScanType Quick`
  - Upload to VirusTotal for additional checks.
- See `RELEASE_VERIFICATION_v1.0.7.md` for full verification details.

Packaging (MSIX)
- MSIX packaging and validation are not performed in CI to avoid build failures due to strict manifest rules.
- To produce an MSIX manually (recommended):
  - Use Visual Studio: create a MSIX Packaging Project and add `installer\AppxManifest.xml` and your app files; build and sign the package.
  - Or use the MSIX Packaging Tool (Microsoft Store) to convert the MSI to MSIX, then sign the output.
  - Signing: use a code-signing certificate (PFX) or a test cert for local distribution. Use `signtool sign /fd SHA256 /f yourcert.pfx /p password /t http://timestamp.digicert.com path\to\SVGMapper.msix`.

Build from source
- Open `SVGMapper.Minimal.sln` or the project in Visual Studio (Windows) and build.
- Target: .NET (WPF). Ensure the appropriate .NET Desktop Runtime is installed.

Notes
- The `release/` folder contains the packaged `.exe` and runtime DLLs required to run the app.

Contributing
- Make changes on a branch, open PRs against `main`.

License
- MIT License (see LICENSE file).
