using System;

namespace SVGMapper.Services
{
    /// <summary>
    /// Represents an action that can be executed and undone.
    /// </summary>
    public interface IUndoableAction
    {
        void Execute();
        void Undo();
        string Description { get; }
    }
}