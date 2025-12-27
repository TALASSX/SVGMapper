using System.Windows.Input;

namespace SVGMapper
{
    public static class Commands
    {
        public static readonly RoutedUICommand Undo = new RoutedUICommand("Undo", "Undo", typeof(Commands), new InputGestureCollection { new KeyGesture(Key.Z, ModifierKeys.Control) });
        public static readonly RoutedUICommand Redo = new RoutedUICommand("Redo", "Redo", typeof(Commands), new InputGestureCollection { new KeyGesture(Key.Y, ModifierKeys.Control) });
        public static readonly RoutedUICommand Copy = new RoutedUICommand("Copy", "Copy", typeof(Commands), new InputGestureCollection { new KeyGesture(Key.C, ModifierKeys.Control) });
        public static readonly RoutedUICommand Paste = new RoutedUICommand("Paste", "Paste", typeof(Commands), new InputGestureCollection { new KeyGesture(Key.V, ModifierKeys.Control) });
        public static readonly RoutedUICommand Duplicate = new RoutedUICommand("Duplicate", "Duplicate", typeof(Commands), new InputGestureCollection { new KeyGesture(Key.D, ModifierKeys.Control) });
        public static readonly RoutedUICommand ZoomIn = new RoutedUICommand("Zoom In", "ZoomIn", typeof(Commands), new InputGestureCollection { new KeyGesture(Key.OemPlus, ModifierKeys.Control) });
        public static readonly RoutedUICommand ZoomOut = new RoutedUICommand("Zoom Out", "ZoomOut", typeof(Commands), new InputGestureCollection { new KeyGesture(Key.OemMinus, ModifierKeys.Control) });
    }
}