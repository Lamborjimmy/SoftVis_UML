using System;
namespace Assets.Scripts.Data
{
    [Serializable]
    public struct Vec3
    {
        public float X, Y, Z;
        public Vec3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
        public static Vec3 Zero => new Vec3(0, 0, 0);
        public static Vec3 One => new Vec3(1, 1, 1);
        public static Vec3 operator +(Vec3 a, Vec3 b) => new Vec3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vec3 operator -(Vec3 a, Vec3 b) => new Vec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vec3 operator *(Vec3 a, float s) => new Vec3(a.X * s, a.Y * s, a.Z * s);
        public static Vec3 operator /(Vec3 a, float s) => new Vec3(a.X / s, a.Y / s, a.Z / s);
        public float Magnitude => (float)Math.Sqrt(X * X + Y * Y + Z * Z);
        public float SqrMagnitude => X * X + Y * Y + Z * Z;
        public Vec3 Normalized
        {
            get
            {
                float mag = Magnitude;
                if (mag > 0.0001f)
                    return new Vec3(X / mag, Y / mag, Z / mag);
                return Zero;
            }
        }
        public static float Distance(Vec3 a, Vec3 b) => (a - b).Magnitude;
        public static float Min(float a, float b) => a < b ? a : b;
        public static float Max(float a, float b) => a > b ? a : b;
    }
    [Serializable]
    public struct RGBA
    {
        public float R, G, B, A;
        public RGBA(float r, float g, float b, float a = 1f) { R = r; G = g; B = b; A = a; }
        public static RGBA Black => new RGBA(0f, 0f, 0f, 1f);
        public static RGBA White => new RGBA(1f, 1f, 1f, 1f);
        public static RGBA Gray => new RGBA(0.2f, 0.2f, 0.2f, 1f);
        public static RGBA Green => new RGBA(0f, 1f, 0f, 1f);
        public static RGBA Bisque => new RGBA(1f, 0.894f, 0.769f, 1f);
    }
    [Serializable]
    public struct BoundsData
    {
        public Vec3 Center;
        public Vec3 Size;
        public Vec3 Extents => Size * 0.5f;
        public BoundsData(Vec3 center, Vec3 size)
        {
            Center = center;
            Size = size;
        }
        public float MinX => Center.X - Size.X / 2f;
        public float MaxX => Center.X + Size.X / 2f;
        public float MinZ => Center.Z - Size.Z / 2f;
        public float MaxZ => Center.Z + Size.Z / 2f;

    }
    public enum DecoratorType
    {
        None,
        Arrow,
        DiamondHollow,
        DiamondFilled,
        Triangle
    }

    public enum DecoratorPlacement
    {
        Start,
        End
    }

    public enum TextAlignment
    {
        Left,
        Center,
        Right,
        Top,
        TopLeft
    }

    public enum FontStyle
    {
        Normal,
        Bold,
        Italic
    }
}