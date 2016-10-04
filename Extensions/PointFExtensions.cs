using System;
using System.Drawing;

namespace Apex.Extensions
{
    public static class PointFExtensions
    {
        public static Rectangle GetRect(this PointF A, PointF B)
        {
            var minX = (int)(A.X < B.X ? A.X : B.X);
            var minY = (int)(A.Y < B.Y ? A.Y : B.Y);
            var maxX = (int)(A.X > B.X ? A.X : B.X);
            var maxY = (int)(A.Y > B.Y ? A.Y : B.Y);

            return new Rectangle(minX, minY, maxX - minX, maxY - minY);
        }

        public static double Distance(this PointF A, PointF B)
        {
            return Math.Sqrt((A.X - B.X) * (A.X - B.X) + (A.Y - B.Y) * (A.Y - B.Y));
        }

        public static double Distance(this PointF A, Point B)
        {
            return Math.Sqrt((A.X - B.X) * (A.X - B.X) + (A.Y - B.Y) * (A.Y - B.Y));
        }

        public static double Angle(this PointF A, PointF B)
        {
            var deltaX = B.X - A.X;
            var deltaY = B.Y - A.Y;

            return Math.Atan2(deltaY, deltaX);
        }

        public static double Angle(this PointF A)
        {
            var deltaX = 0 - A.X;
            var deltaY = 0 - A.Y;

            return Math.Atan2(deltaY, deltaX);
        }

        public static PointF Mid(this PointF A, PointF B)
        {
            return new PointF((A.X + B.X) / 2, (A.Y + B.Y) / 2);
        }

        public static double Magnitude(this PointF A)
        {
            return Math.Sqrt(A.X * A.X + A.Y * A.Y);
        }

        public static PointF Unit(this PointF A)
        {
            return new PointF((float)(A.X / A.Magnitude()), (float)(A.Y / A.Magnitude()));
        }

        public static PointF Multiply(this PointF Point, float Scalar)
        {
            return new PointF(Point.X * Scalar, Point.Y * Scalar);
        }
    }
}
