SVGMapper
=========

Small WPF app for mapping SVG floor plans and seating.

Files to know
- `release/SVGMapper.Minimal.exe` — runnable application (release build)

How to run
- Double-click `release/SVGMapper.Minimal.exe` on Windows.
- Or run from PowerShell:

```powershell
& .\release\SVGMapper.Minimal.exe
```

Installation (via MSI installer)
- Download the latest MSI from [GitHub Releases](https://github.com/TALASSX/SVGMapper/releases).
- Double-click the MSI file and follow the installer prompts.
- Or install from command line (elevated PowerShell):

```powershell
msiexec /i "path\to\SVGMapper.Installer.msi"
```

Trust verification for MSI
- The MSI is unsigned, so verify before installing:
  - Compute SHA256: `Get-FileHash "path\to\SVGMapper.Installer.msi" -Algorithm SHA256`
  - Expected for v1.0.7: `91DCD3CFEA2F642C5B7016966E6D1C3F5763D17215B41D5E49E86CA306BCE421`
  - Scan with Windows Defender: `Start-MpScan -ScanType Quick`
  - Upload to VirusTotal for additional checks.
- See `RELEASE_VERIFICATION_v1.0.7.md` for full verification details.

Build from source
- Open `SVGMapper.Minimal.sln` or the project in Visual Studio (Windows) and build.
- Target: .NET (WPF). Ensure the appropriate .NET Desktop Runtime is installed.

Notes
- The `release/` folder contains the packaged `.exe` and runtime DLLs required to run the app.

Contributing
- Make changes on a branch, open PRs against `main`.

License
- Add your license here.
