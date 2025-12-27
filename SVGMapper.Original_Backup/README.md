# SVGMapper — Floor & Seating Planner (WPF)

**Overview**

SVGMapper is a Windows desktop app (WPF, C#) for designing floor plans and seating layouts and exporting them as clean SVG files for reuse. It contains two main designer pages: Floor Plan Designer and Seating Plan Designer, sharing a zoomable/pannable canvas and common services (SVG import/export, selection, undo/redo).

---

## 🔹 Pages / Modes

### Floor Plan Designer
- Upload an image or SVG as background (blueprint)
- Draw closed polygons to define rooms
- Name/number rooms
- Export rooms as SVG

### Seating Plan Designer
- Optionally load a floor plan as a background
- Place seats, rows, and tables
- Assign seat numbers and row names
- Export seating layout as SVG

---

## 🧭 Navigation (UI Options)
- **TabControl**: `Floor Plan` | `Seating Plan`
- **Left Sidebar**: list of pages

Both options reuse the same canvas logic and zoom/pan controls.

---

## 🔧 Core Canvas & Tools

- **ZoomCanvas** control: zoom in/out, mouse-wheel zoom, pan
- **Grid & Snap**: transparent, configurable grid overlay (size, opacity, color) with a toggle; optional snap-to-grid support to help precise placement of points and seats
- **PolygonTool**: create rooms by clicking points and closing the polygon
- **SeatTool**: create seat shapes, drag, edit, delete
- **RowTool**: click-start/click-end to auto-generate seats between two points
- **TableTool** (optional): place table + seats around it

**Grid Notes:** The grid is drawn in world coordinates and will show lines at multiples of the configured grid size (default 25px). Use the grid toggle in the `ZoomCanvas` toolbar to show/hide it and tune `GridSize` and `GridOpacity` in code or via a future Settings panel.
---

## 📐 Data Models (C#)

Room:
```csharp
public class Room {
    public string Name { get; set; }
    public List<Point> Points { get; set; } = new();
}
```

Seat & Row:
```csharp
public class Seat {
    public string SeatNumber { get; set; }
    public Point Position { get; set; }
    public string Row { get; set; }
}

public class SeatingRow {
    public string RowName { get; set; }
    public List<Seat> Seats { get; set; } = new();
}
```

---

## 🗂 Project Folder Structure
```
/Views
  FloorPlanView.xaml
  SeatingPlanView.xaml
/Models
  Room.cs
  Seat.cs
  SeatingRow.cs
/Controls
  ZoomCanvas.xaml
/Services
  SvgImportService.cs
  SvgExportService.cs
/Tools
  PolygonTool.cs
  SeatTool.cs
/Resources
  (icons, styles)
```

---

## 🟢 SVG Export
- Everything exported is pure SVG shapes and text so it can be reused anywhere.
- Export includes room polygons, seat circles/rects with text labels, and optional background SVG.
- Export groups shapes into logical layers (`<g id="rooms">`, `<g id="seats">`) so downstream tools can treat layers separately.
- New export options:
  - Export whole plan (rooms + seats)
  - Export selection only (File → Export Selection as SVG...)
  - Export by layer (File → Export Rooms as SVG... / Export Seats as SVG...)

Example snippet:
```xml
<svg xmlns="http://www.w3.org/2000/svg">
  <g id="rooms">
    <polygon points="10,10 200,10 200,150" fill="none" stroke="red"/>
  </g>
  <g id="seats">
    <circle cx="100" cy="100" r="10" fill="blue"/>
    <text x="100" y="130">A1</text>
  </g>
</svg>
```

---

## ✅ UX Flow
1. Design floor plan (draw rooms or import SVG)
2. Save or export floor plan
3. Switch to Seating Plan, load floor plan as background
4. Place seats/rows/tables and assign numbers
5. Export final SVG

---

## 🛠 Implementation Plan (Step-by-step)
1. Scaffold UI: `MainWindow`, `FloorPlanView`, `SeatingPlanView`, `ZoomCanvas` ✅
2. Implement core canvas features: **Grid**, **Snap-to-grid**, **Rulers**, and zoom/pan behavior
3. Implement tools & selection: `PolygonTool` (floor plan) and `SelectionTool` (multi-select, drag, align, distribute) — **Selection & Inspector (basic) implemented**
4. Add **Undo / Redo** system (global command stack) and keyboard shortcuts (Ctrl+Z / Ctrl+Y) ✅ (basic service implemented)
5. Implement `SeatTool` and `RowTool` (drag, edit, numbering, row autogeneration) ✅ (basic RowTool + preview + undo implemented)
6. Implement `PolygonTool` (floor plan polygon drawing) ✅ (basic polygon drawing + undo + inspector binding)
7. Implement vertex editing for polygons (drag to move vertices) ✅ (drag handles + undo support)
6. SVG import and export services (background floorplan support, export by selection/layer)
7. Persistence (save/load project JSON), project templates
8. Tests, polishing, inspector panel, layers, performance tuning

---

## 🧩 Additional Recommended Features (Prioritized)

### High priority — must-have ✅
1. **Undo / Redo (global command stack)** — Fast recovery from mistakes; essential for a design workflow.
2. **Selection & Properties Panel (Inspector)** — Edit room/seat/table attributes (name, number, color, status) via a dedicated side panel. ✅ (basic inspector implemented)
3. **Multi-select / Group / Align / Distribute** — Speeds layout tasks and ensures consistent rows and table placement.
4. **Save / Load Projects (JSON)** — Persist work, share projects, and support versioning.
5. **Grid + Snap-to-Grid + Object Snapping** — Precise placement and consistent spacing; configurable grid size and snap tolerance.

### Medium priority — strong enhancers ✨
- Properties templates & object library (seat/table presets)
- Row autogeneration & advanced seat-numbering patterns (1..n, A1,A2…)
- Rulers & measurement tool / scale settings (real-world units)
- Layer system (rooms, seats, annotations) with hide/lock
- Copy/Paste, Duplicate, Nudge (keyboard arrow), and rich keyboard shortcuts
- Export variations: SVG, PDF, PNG; export selection or by layer

### Low priority / advanced features ⭐
- CSV import/export for seats / assignments
- Seat status management & simple reservation integration
- Advanced layout tools: automatic seat packing, collision detection
- Collaboration & version history (cloud sync)
- Plugins / scripting API for advanced automation

### UX & Accessibility improvements ♿
- Keyboard-first flow (hotkeys for tools, undo/redo, copy/paste/duplicate, nudging) — implemented: Undo/Redo, Copy/Paste (Ctrl+C/Ctrl+V), Duplicate (Ctrl+D), Arrow nudges (+ Shift for larger nudges), ESC cancels tools, DEL deletes selection (undoable)
- Tooltips, onboarding tutorial, and sample templates
- High-contrast theme and screen reader support
- Autosave and undo checkpointing

### Performance & robustness 🔧
- Virtualize rendering for very large seat counts (render only viewport)
- Batch updates for undo/redo and SVG export to keep UI responsive
- Validation (overlapping seats, invalid numbering) and user warnings

### Small recommended defaults
- Grid size: 25 px (configurable)
- Snap tolerance: 8 px (configurable)
- Snap-to-grid: implemented as a toggle on the canvas ("Snap") — snaps during drag operations
- Export: SVG export implemented (File → Export Plan as SVG). For full SVG background rendering the app attempts to use **SharpVectors** (WPF renderer). To enable it, add the NuGet package:

  dotnet add package SharpVectors.Wpf

  If SharpVectors isn't present the app falls back to a lightweight placeholder label on the canvas.
- Undo levels: at least 50 actions
- Keyboard: Arrow keys = nudge (1 px), Shift+Arrow = larger nudge (10 px)

### Where to add these features (mapping to structure)
- `/Controls`: update `ZoomCanvas` (grid, snapping, rulers)
- `/Tools`: expand `SelectionTool`, `RowTool`, `AlignService`
- `/Services`: add `UndoService`, `ProjectPersistenceService`, `ExportService`
- `/Views`: add `InspectorView`, `LayersView`, toolbar/menus for quick access

---

## ▶️ How to run (quick start)
- Recommended: .NET 6 or later (prefer .NET 8)
- Create project: `dotnet new wpf -n SVGMapper`
- Run: `dotnet run` inside project folder

---

## 📌 Notes & Decisions
- Use MVVM for production grade maintainability (ViewModels + Commands). For quick prototypes, code-behind is acceptable.
- Maintain a single shared `ZoomCanvas` control to avoid duplication.

---

## 🎯 Next Steps
Choose one to start:
- Design UI wireframes / produce XAML for `MainWindow` and `ZoomCanvas`
- Implement `PolygonTool` (floor plan)
- Implement `SeatTool` and `RowTool` (seating layout)
- Add `SvgExportService` to produce final SVGs

Let me know which step you want first and I’ll implement it.

---

_This README was generated by GitHub Copilot (Raptor mini - Preview)._
