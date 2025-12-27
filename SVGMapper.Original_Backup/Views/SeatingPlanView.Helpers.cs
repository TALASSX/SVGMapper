using System;
using System.Collections.Generic;
using System.Windows;
using SVGMapper.Models;

namespace SVGMapper.Views
{
    public partial class SeatingPlanView
    {
        private List<Point> GenerateSeatPositions(Point a, Point b, double spacing)
        {
            var dx = b.X - a.X;
            var dy = b.Y - a.Y;
            var dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist <= 0 || spacing <= 0) return new List<Point> { a };

            var count = (int)Math.Floor(dist / spacing) + 1;
            if (count < 2) count = 2;

            var list = new List<Point>(count);
            for (int i = 0; i < count; i++)
            {
                var t = (double)i / (count - 1);
                var x = a.X + dx * t;
                var y = a.Y + dy * t;
                list.Add(new Point(x, y));
            }
            return list;
        }

        private List<Seat> GenerateSeatsAlongLine(Point a, Point b, double spacing)
        {
            var positions = GenerateSeatPositions(a, b, spacing);
            var seats = new List<Seat>(positions.Count);
            foreach (var pos in positions)
            {
                var seat = new Seat { SeatNumber = (_nextSeatNumber++).ToString(), Position = pos };
                seats.Add(seat);
            }
            return seats;
        }


    }
}