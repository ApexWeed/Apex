using System;
using System.Drawing;

namespace Apex.Extensions
{
    public static class PointExtensions
    {
        public static Rectangle GetRect(this Point A, Point B)
        {
            var minX = A.X < B.X ? A.X : B.X;
            var minY = A.Y < B.Y ? A.Y : B.Y;
            var maxX = A.X > B.X ? A.X : B.X;
            var maxY = A.Y > B.Y ? A.Y : B.Y;

            return new Rectangle(minX, minY, maxX - minX, maxY - minY);
        }

        public static double Distance(this Point A, Point B)
        {
            return Math.Sqrt((A.X - B.X) * (A.X - B.X) + (A.Y - B.Y) * (A.Y - B.Y));
        }

        public static double Distance(this Point A, PointF B)
        {
            return Math.Sqrt((A.X - B.X) * (A.X - B.X) + (A.Y - B.Y) * (A.Y - B.Y));
        }

        public static double Angle(this Point A, Point B)
        {
            var deltaX = B.X - A.X;
            var deltaY = B.Y - A.Y;

            return Math.Atan2(deltaY, deltaX);
        }

        public static double Angle(this Point A, PointF B)
        {
            var deltaX = B.X - A.X;
            var deltaY = B.Y - A.Y;

            return Math.Atan2(deltaY, deltaX);
        }

        public static double Angle(this Point A)
        {
            var deltaX = 0 - A.X;
            var deltaY = 0 - A.Y;

            return Math.Atan2(deltaY, deltaX);
        }

        public static Point Mid(this Point A, Point B)
        {
            return new Point((A.X + B.X) / 2, (A.Y + B.Y) / 2);
        }

        public static double Magnitude(this Point A)
        {
            return Math.Sqrt(A.X * A.X + A.Y * A.Y);
        }

        public static PointF Unit(this Point A)
        {
            return new PointF((float)(A.X / A.Magnitude()), (float)(A.Y / A.Magnitude()));
        }
    }
}
