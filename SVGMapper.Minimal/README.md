SVGMapper Minimal

A minimal WPF app for drawing polygon rooms, grid/snap, seating layout, basic inspector, undo/redo, and SVG export.

## Development

- Build:

```powershell
dotnet build SVGMapper.Minimal.csproj
```

- Run (dev):

```powershell
dotnet run --project SVGMapper.Minimal.csproj
```

## Distribution / Run the release EXE

- The `release` folder contains the self-contained `SVGMapper.Minimal.exe` and required native DLLs. To install for others, distribute the entire `release` folder.

- Run by double-clicking `SVGMapper.Minimal.exe` or from PowerShell:

```powershell
Start-Process -FilePath 'C:\path\to\release\SVGMapper.Minimal.exe'
```

## Packaging

- Create a ZIP of the `release` folder to share:

```powershell
Compress-Archive -Path 'release\*' -DestinationPath 'SVGMapper-win-x64.zip' -Force
```

## Troubleshooting

- If the app does not open after extraction, right-click the EXE → Properties → check "Unblock", or run:

```powershell
Unblock-File -Path 'C:\path\to\SVGMapper.Minimal.exe'
```

- If antivirus flags the binary, mark it as safe or add an exception for the folder.

## Contact

- Repository: https://github.com/rcksai125/SVGMapper
