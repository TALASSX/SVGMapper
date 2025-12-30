[Version]
Class=IEXPRESS
[Options]
PackagePurpose=InstallApp
ShowInstallProgramWindow=0
HideExtractAnimation=1
UseLongFileName=1
InsideCompressed=0
RebootMode=N
OverwritePrompt=Never
[Strings]
SetupCommand=cmd /c powershell -NoProfile -ExecutionPolicy Bypass -Command "Expand-Archive -Force 'SVGMapper.Minimal.zip' "C:\Users\rocks\AppData\Local\Temp\\SVGMapper"; Start-Process "C:\Users\rocks\AppData\Local\Temp\\SVGMapper\\SVGMapper.Minimal.exe""
