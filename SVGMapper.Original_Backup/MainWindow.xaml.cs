using System.Windows;
using System.Windows.Input;
using SVGMapper.Services;

namespace SVGMapper
{
    public partial class MainWindow : Window
    {
        private readonly UndoService _undoService = new UndoService();
        private readonly SelectionService _selectionService = new SelectionService();

        public MainWindow()
        {
            InitializeComponent();
            _undoService.StateChanged += () => CommandManager.InvalidateRequerySuggested();

            // Wire selection service to the inspector
            InspectorView.DataContext = _selectionService;

            // Wire selection service to the seating view
            SeatingView.SelectionService = _selectionService;
            SeatingView.UndoService = _undoService;

            // Wire selection & undo services to the floor view
            FloorView.SelectionService = _selectionService;
            FloorView.UndoService = _undoService;

            // Copy/paste service
            _copyPasteService = new Services.CopyPasteService();

            // Command bindings for copy/paste/duplicate
            CommandBindings.Add(new CommandBinding(Commands.Copy, Copy_Executed, Copy_CanExecute));
            CommandBindings.Add(new CommandBinding(Commands.Paste, Paste_Executed, Paste_CanExecute));
            CommandBindings.Add(new CommandBinding(Commands.Duplicate, Duplicate_Executed, Duplicate_CanExecute));
            CommandBindings.Add(new CommandBinding(Commands.ZoomIn, ZoomIn_Executed));
            CommandBindings.Add(new CommandBinding(Commands.ZoomOut, ZoomOut_Executed));
        }

        private void Undo_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _undoService.CanUndo;
        }

        private void Redo_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _undoService.CanRedo;
        }

        private void Undo_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            _undoService.Undo();
        }

        private void Redo_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            _undoService.Redo();
        }

        // Expose the UndoService so tools can access it (in a real app you'd use DI or a central App.Services locator)
        public UndoService UndoService => _undoService;
        private CopyPasteService? _copyPasteService = null;

        private void Copy_CanExecute(object sender, System.Windows.Input.CanExecuteRoutedEventArgs e)
        {
            // Enable if any selection exists in either view
            var can = (_selectionService.SelectedItems?.Count ?? 0) > 0;
            e.CanExecute = can;
        }

        private void Copy_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs? e)
        {
            // Copy seats if seating selection exists, otherwise rooms
            var seats = new System.Collections.Generic.List<Models.Seat>();
            var rooms = new System.Collections.Generic.List<Models.Room>();

            foreach (var obj in _selectionService.SelectedItems)
            {
                if (obj is Models.Seat s) seats.Add(s);
                if (obj is Models.Room r) rooms.Add(r);
            }

            if (_copyPasteService != null)
            {
                if (seats.Count > 0) _copyPasteService.CopySeats(seats);
                if (rooms.Count > 0) _copyPasteService.CopyRooms(rooms);
            }
        }

        private void Paste_CanExecute(object sender, System.Windows.Input.CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _copyPasteService != null && (_copyPasteService.HasSeats || _copyPasteService.HasRooms);
        }

        private void Paste_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs? e)
        {
            if (_copyPasteService == null) return;
            if (_copyPasteService.HasSeats)
            {
                var seats = _copyPasteService.PasteSeats(10, 10);
                if (seats.Count > 0 && SeatingView.UndoService != null)
                {
                    SeatingView.UndoService.Do(new AddSeatsAction(SeatingView, seats));
                }
            }

            if (_copyPasteService.HasRooms)
            {
                var rooms = _copyPasteService.PasteRooms(10, 10);
                foreach (var r in rooms)
                {
                    if (FloorView.UndoService != null)
                    {
                        FloorView.UndoService.Do(new AddRoomAction(FloorView, r));
                    }
                }
            }
        }

        private void Duplicate_CanExecute(object sender, System.Windows.Input.CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (_selectionService.SelectedItems?.Count ?? 0) > 0;
        }

        private void Duplicate_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs? e)
        {
            // Duplicate selection by copying then pasting with small offset
            Copy_Executed(sender, null);
            Paste_Executed(sender, null);
        }
        private void ZoomIn_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            // Zoom the currently active tab's canvas
            var idx = MainTab.SelectedIndex;
            if (idx == 0) FloorView?.ZoomCanvas?.ZoomInPublic();
            else if (idx == 1) SeatingView?.ZoomCanvas?.ZoomInPublic();
        }

        private void ZoomOut_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            var idx = MainTab.SelectedIndex;
            if (idx == 0) FloorView?.ZoomCanvas?.ZoomOutPublic();
            else if (idx == 1) SeatingView?.ZoomCanvas?.ZoomOutPublic();
        }
        private void ImportBackground_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "Image & SVG files (*.png;*.jpg;*.jpeg;*.svg)|*.png;*.jpg;*.jpeg;*.svg|All files|*.*" };
            if (dlg.ShowDialog() == true)
            {
                var ext = System.IO.Path.GetExtension(dlg.FileName).ToLowerInvariant();
                if (ext == ".svg")
                {
                    var svg = Services.SvgImportService.LoadSvgAsString(dlg.FileName);
                    FloorView.LoadBackgroundSvg(svg, System.IO.Path.GetFileName(dlg.FileName));
                }
                else
                {
                    // Raster image
                    FloorView.LoadBackgroundImage(dlg.FileName);
                }
            }
        }

        private void ExportPlan_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "SVG files (*.svg)|*.svg", FileName = "plan.svg" };
            if (dlg.ShowDialog() == true)
            {
                var rooms = FloorView.GetRooms();
                var seats = SeatingView.GetSeats();
                var bgSvg = FloorView.GetBackgroundSvg();
                var bgImage = FloorView.GetBackgroundImagePath();
                Services.SvgExportService.Export(dlg.FileName, rooms, seats, bgSvg, bgImage);
            }
        }

        private void ExportSelection_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "SVG files (*.svg)|*.svg", FileName = "selection.svg" };
            if (dlg.ShowDialog() == true)
            {
                var selectedRooms = new System.Collections.Generic.List<Models.Room>();
                var selectedSeats = new System.Collections.Generic.List<Models.Seat>();

                foreach (var obj in _selectionService.SelectedItems)
                {
                    if (obj is Models.Room r) selectedRooms.Add(r);
                    if (obj is Models.Seat s) selectedSeats.Add(s);
                }

                Services.SvgExportService.Export(dlg.FileName, selectedRooms, selectedSeats);
            }
        }

        private void ExportRooms_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "SVG files (*.svg)|*.svg", FileName = "rooms.svg" };
            if (dlg.ShowDialog() == true)
            {
                var rooms = FloorView.GetRooms();
                Services.SvgExportService.Export(dlg.FileName, rooms, System.Array.Empty<Models.Seat>());
            }
        }

        private void ExportSeats_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "SVG files (*.svg)|*.svg", FileName = "seats.svg" };
            if (dlg.ShowDialog() == true)
            {
                var seats = SeatingView.GetSeats();
                Services.SvgExportService.Export(dlg.FileName, System.Array.Empty<Models.Room>(), seats);
            }
        }
    }
}