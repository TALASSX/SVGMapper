using System.Collections.Generic;
using SVGMapper.Models;
using SVGMapper.Views;

namespace SVGMapper.Services
{
    public class DeleteRoomsAction : IUndoableAction
    {
        private readonly FloorPlanView _view;
        private readonly List<Room> _rooms;

        public string Description { get; }

        public DeleteRoomsAction(FloorPlanView view, List<Room> rooms, string description = "Delete Rooms")
        {
            _view = view;
            _rooms = rooms;
            Description = description;
        }

        public void Execute()
        {
            foreach (var r in _rooms) _view.RemoveRoomInternal(r);
        }

        public void Undo()
        {
            foreach (var r in _rooms) _view.AddRoomInternal(r);
        }
    }
}