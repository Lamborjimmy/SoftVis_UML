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
        public static RGBA SoftYellow => new RGBA(1f, 0.98f, 0.8f, 1f);
        public static RGBA LightBlue => new RGBA(0.58f, 0.72f, 0.76f, 1f);
        public static RGBA Lavender => new RGBA(0.73f, 0.73f, 0.83f, 1f);
        public static RGBA Peach => new RGBA(0.85f, 0.72f, 0.61f, 1f);
        public static RGBA Mint => new RGBA(0.64f, 0.81f, 0.72f, 1f);
        public static RGBA Khaki => new RGBA(0.80f, 0.76f, 0.47f, 1f);
        public static RGBA MistyRose => new RGBA(0.85f, 0.76f, 0.75f, 1f);
        public static RGBA Thistle => new RGBA(0.72f, 0.64f, 0.72f, 1f);

        public static RGBA Layer0 => new RGBA(0.40f, 0.50f, 0.65f, 1f); // Slate Blue
        public static RGBA Layer1 => new RGBA(0.45f, 0.60f, 0.50f, 1f); // Muted Green
        public static RGBA Layer2 => new RGBA(0.60f, 0.45f, 0.55f, 1f); // Dusty Plum
        public static RGBA Layer3 => new RGBA(0.65f, 0.55f, 0.40f, 1f); // Warm Bronze
        public static RGBA Layer4 => new RGBA(0.45f, 0.55f, 0.60f, 1f); // Steel Grey
        public static RGBA[] NestingPalette => new RGBA[] { Layer0, Layer1, Layer2, Layer3, Layer4 };
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