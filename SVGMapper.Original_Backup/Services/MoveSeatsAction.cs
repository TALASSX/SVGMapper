using System.Collections.Generic;
using SVGMapper.Models;
using System.Windows;

namespace SVGMapper.Services
{
    public class MoveSeatsAction : IUndoableAction
    {
        private readonly List<(Seat seat, Point from, Point to)> _changes;
        public string Description { get; }

        public MoveSeatsAction(List<(Seat seat, Point from, Point to)> changes, string description = "Move Seats")
        {
            _changes = changes;
            Description = description;
        }

        public void Execute()
        {
            foreach (var (seat, from, to) in _changes)
                seat.Position = to;
        }

        public void Undo()
        {
            foreach (var (seat, from, to) in _changes)
                seat.Position = from;
        }
    }
}