using System.Collections.Generic;
using System.Linq;
using SVGMapper.Models;
using System.Windows;

namespace SVGMapper.Services
{
    public class CopyPasteService
    {
        private List<Seat> _copiedSeats = new();
        private List<Room> _copiedRooms = new();

        public bool HasSeats => _copiedSeats.Count > 0;
        public bool HasRooms => _copiedRooms.Count > 0;

        public void CopySeats(IEnumerable<Seat> seats)
        {
            _copiedSeats = seats.Select(s => new Seat { SeatNumber = s.SeatNumber, Position = new Point(s.Position.X, s.Position.Y), Row = s.Row }).ToList();
        }

        public void CopyRooms(IEnumerable<Room> rooms)
        {
            _copiedRooms = rooms.Select(r => new Room { Name = r.Name, Points = r.Points.Select(p => new Point(p.X, p.Y)).ToList() }).ToList();
        }

        public List<Seat> PasteSeats(double offsetX = 10, double offsetY = 10)
        {
            var list = _copiedSeats.Select(s => new Seat { SeatNumber = s.SeatNumber, Position = new Point(s.Position.X + offsetX, s.Position.Y + offsetY), Row = s.Row }).ToList();
            return list;
        }

        public List<Room> PasteRooms(double offsetX = 10, double offsetY = 10)
        {
            var list = _copiedRooms.Select(r => new Room { Name = r.Name + " Copy", Points = r.Points.Select(p => new Point(p.X + offsetX, p.Y + offsetY)).ToList() }).ToList();
            return list;
        }
    }
}