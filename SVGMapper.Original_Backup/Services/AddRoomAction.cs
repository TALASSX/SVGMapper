using System.Collections.Generic;
using System.Windows;
using SVGMapper.Models;
using SVGMapper.Views;

namespace SVGMapper.Services
{
    public class AddRoomAction : IUndoableAction
    {
        private readonly FloorPlanView _view;
        private readonly Room _room;
        public string Description { get; }

        public AddRoomAction(FloorPlanView view, Room room, string description = "Add Room")
        {
            _view = view;
            _room = room;
            Description = description;
        }

        public void Execute() => _view.AddRoomInternal(_room);
        public void Undo() => _view.RemoveRoomInternal(_room);
    }
}