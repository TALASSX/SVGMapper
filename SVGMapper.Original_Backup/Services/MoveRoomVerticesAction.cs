using System.Collections.Generic;
using System.Windows;
using SVGMapper.Models;
using SVGMapper.Views;

namespace SVGMapper.Services
{
    public class MoveRoomVerticesAction : IUndoableAction
    {
        private readonly Room _room;
        private readonly List<Point> _from;
        private readonly List<Point> _to;

        public string Description { get; }

        public MoveRoomVerticesAction(Room room, List<Point> from, List<Point> to, string description = "Move Room Vertices")
        {
            _room = room;
            _from = from;
            _to = to;
            Description = description;
        }

        public void Execute() => _room.SetPoints(new List<Point>(_to));
        public void Undo() => _room.SetPoints(new List<Point>(_from));
    }
}