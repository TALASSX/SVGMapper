using System;
using System.Collections.Generic;
using System.Linq;

namespace SVGMapper.Services
{
    public class UndoService
    {
        private readonly Stack<IUndoableAction> _undoStack = new();
        private readonly Stack<IUndoableAction> _redoStack = new();

        public event Action? StateChanged;

        public int MaxHistory { get; set; } = 100;

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        public void Do(IUndoableAction action)
        {
            action.Execute();
            _undoStack.Push(action);
            _redoStack.Clear();
            TrimHistory();
            StateChanged?.Invoke();
        }

        public void Undo()
        {
            if (!CanUndo) return;
            var act = _undoStack.Pop();
            act.Undo();
            _redoStack.Push(act);
            StateChanged?.Invoke();
        }

        public void Redo()
        {
            if (!CanRedo) return;
            var act = _redoStack.Pop();
            act.Execute();
            _undoStack.Push(act);
            StateChanged?.Invoke();
        }

        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            StateChanged?.Invoke();
        }

        private void TrimHistory()
        {
            if (_undoStack.Count <= MaxHistory) return;
            var arr = _undoStack.ToArray(); // LIFO array: arr[0] is top
            Array.Resize(ref arr, MaxHistory);
            _undoStack.Clear();
            // Rebuild stack so top remains arr[0]
            for (int i = arr.Length - 1; i >= 0; i--)
                _undoStack.Push(arr[i]);
        }
    }
}