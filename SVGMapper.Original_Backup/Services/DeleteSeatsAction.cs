using System.Collections.Generic;
using SVGMapper.Models;
using SVGMapper.Views;

namespace SVGMapper.Services
{
    public class DeleteSeatsAction : IUndoableAction
    {
        private readonly SeatingPlanView _view;
        private readonly List<Seat> _seats;

        public string Description { get; }

        public DeleteSeatsAction(SeatingPlanView view, List<Seat> seats, string description = "Delete Seats")
        {
            _view = view;
            _seats = seats;
            Description = description;
        }

        public void Execute()
        {
            foreach (var s in _seats) _view.RemoveSeatInternal(s);
        }

        public void Undo()
        {
            foreach (var s in _seats) _view.AddSeatInternal(s);
        }
    }
}