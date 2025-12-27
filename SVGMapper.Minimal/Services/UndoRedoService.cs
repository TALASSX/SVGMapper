using System;
using System.Collections.Generic;

namespace SVGMapper.Minimal.Services
{
    public class UndoRedoService
    {
        private readonly Stack<UndoRedoEntry> _undo = new();
        private readonly Stack<UndoRedoEntry> _redo = new();

        private sealed record UndoRedoEntry(Action DoAction, Action UndoAction);

        public void Execute(Action doAction, Action undoAction)
        {
            doAction();
            _undo.Push(new UndoRedoEntry(doAction, undoAction));
            _redo.Clear();
        }

        public void Undo()
        {
            if (_undo.Count == 0) return;
            var entry = _undo.Pop();
            try { entry.UndoAction(); }
            catch { /* swallow to avoid breaking host; consider logging */ }
            _redo.Push(entry);
        }

        public void Redo()
        {
            if (_redo.Count == 0) return;
            var entry = _redo.Pop();
            try { entry.DoAction(); }
            catch { /* swallow to avoid breaking host; consider logging */ }
            _undo.Push(entry);
        }
    }
}