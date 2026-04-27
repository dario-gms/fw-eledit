using System;

namespace FWEledit
{
    public struct Vector3f
    {
        public float X;
        public float Y;
        public float Z;

        public Vector3f(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Vector3f Zero
        {
            get { return new Vector3f(0f, 0f, 0f); }
        }

        public static Vector3f operator +(Vector3f a, Vector3f b)
        {
            return new Vector3f(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public static Vector3f operator -(Vector3f a, Vector3f b)
        {
            return new Vector3f(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        public static Vector3f operator /(Vector3f a, float value)
        {
            if (Math.Abs(value) < 0.000001f)
            {
                return a;
            }
            return new Vector3f(a.X / value, a.Y / value, a.Z / value);
        }

        public static Vector3f Cross(Vector3f a, Vector3f b)
        {
            return new Vector3f(
                a.Y * b.Z - a.Z * b.Y,
                a.Z * b.X - a.X * b.Z,
                a.X * b.Y - a.Y * b.X);
        }

        public static float Dot(Vector3f a, Vector3f b)
        {
            return (a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z);
        }

        public float Length()
        {
            return (float)Math.Sqrt((X * X) + (Y * Y) + (Z * Z));
        }

        public Vector3f Normalize()
        {
            float len = Length();
            if (len <= 0.000001f)
            {
                return this;
            }
            return this / len;
        }
    }
}
