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

## How to use — Draw polygons and save rooms ✅

This section explains how to draw polygon rooms on the app screen and save/export them.

1. Import a background image
   - Click **Import Image** and choose a PNG/JPG. The canvas will resize to the image.

2. Enable drawing
   - Click **Draw Polygon** to enable the polygon tool.

3. Draw a polygon (add vertices)
   - **Click** on the image to add vertices (points are added in image pixel coordinates).
   - A red dashed preview line follows your cursor and a start marker appears at the first vertex.
   - Grid snapping is enabled by default; toggle **Show Grid** or change the **Grid** size to adjust snapping granularity.

4. Edit during drawing
   - Press **Backspace** to remove the last draft point.
   - Press **Esc** to cancel the current draft.
   - Use **Undo** or **Ctrl+Z** to revoke the last draft point or undo actions.

5. Finish and save a room
   - **Double-click** near the starting point (within the start marker) to finish the polygon.
   - You will be prompted to enter a room name; confirming will add the room to the Rooms list (right-hand inspector).

6. Inspect, rename, and delete rooms
   - Select a room in the **Rooms** list or click a polygon on the canvas to select it.
   - Double-click a room in the list (or use the right-click menu) to **Rename** or **Delete** it.
   - Press **Delete** to remove the selected room; if a vertex is selected, **Delete** removes that vertex (polygons must keep at least 3 points).

7. Edit polygon vertices
   - When a room is selected, draggable vertex handles appear; **drag** a handle to move a vertex (grid snapping applies).
   - Right-click a vertex to remove it (must leave at least 3 points).

8. Export
   - Click **Export SVG** to save the current document's rooms and seats as an SVG file.

> Tips: use the grid and snapping for accurate alignment, and use the inspector on the right to view or edit room details (Name, Field Number, Id).

---

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
