using System;
using System.Drawing;

namespace Apex.Extensions
{
    public static class RectangleExtensions
    {
        public static Rectangle Merge(this Rectangle A, Rectangle B)
        {
            var x = Math.Min(A.X, B.X);
            var y = Math.Min(A.Y, B.Y);
            var width = Math.Max(A.Right, B.Right) - x;
            var height = Math.Max(A.Bottom, B.Bottom) - y;
            return new Rectangle(x, y, width, height);
        }

        public static Rectangle FromPoints(this Rectangle Rect, Point[] Points)
        {
            var minX = int.MaxValue;
            var minY = int.MaxValue;
            var maxX = int.MinValue;
            var maxY = int.MinValue;

            foreach (var point in Points)
            {
                if (point.X < minX)
                {
                    minX = point.X;
                }
                if (point.Y < minY)
                {
                    minY = point.Y;
                }
                if (point.X > maxX)
                {
                    maxX = point.X;
                }
                if (point.Y > maxY)
                {
                    maxY = point.Y;
                }
            }

            return new Rectangle(minX, minY, maxX - minX, maxY - minY);
        }
    }
}
