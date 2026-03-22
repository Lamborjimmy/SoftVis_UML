using NUnit.Framework;
using Softviz.UML.Data;

namespace Softviz.Tests.EditMode
{
    public class Vec3Tests
    {
        [Test]
        public void Constructor_SetsXYZ()
        {
            var v = new Vec3(1f, 2f, 3f);
            Assert.AreEqual(1f, v.X);
            Assert.AreEqual(2f, v.Y);
            Assert.AreEqual(3f, v.Z);
        }

        [Test]
        public void Zero_ReturnsAllZeros()
        {
            var v = Vec3.Zero;
            Assert.AreEqual(0f, v.X);
            Assert.AreEqual(0f, v.Y);
            Assert.AreEqual(0f, v.Z);
        }

        [Test]
        public void One_ReturnsAllOnes()
        {
            var v = Vec3.One;
            Assert.AreEqual(1f, v.X);
            Assert.AreEqual(1f, v.Y);
            Assert.AreEqual(1f, v.Z);
        }

        [Test]
        public void Addition_AddsComponents()
        {
            var a = new Vec3(1f, 2f, 3f);
            var b = new Vec3(4f, 5f, 6f);
            var result = a + b;
            Assert.AreEqual(5f, result.X);
            Assert.AreEqual(7f, result.Y);
            Assert.AreEqual(9f, result.Z);
        }

        [Test]
        public void Subtraction_SubtractsComponents()
        {
            var a = new Vec3(5f, 7f, 9f);
            var b = new Vec3(1f, 2f, 3f);
            var result = a - b;
            Assert.AreEqual(4f, result.X);
            Assert.AreEqual(5f, result.Y);
            Assert.AreEqual(6f, result.Z);
        }

        [Test]
        public void ScalarMultiplication_MultipliesAllComponents()
        {
            var v = new Vec3(2f, 3f, 4f);
            var result = v * 3f;
            Assert.AreEqual(6f, result.X);
            Assert.AreEqual(9f, result.Y);
            Assert.AreEqual(12f, result.Z);
        }

        [Test]
        public void ScalarDivision_DividesAllComponents()
        {
            var v = new Vec3(6f, 9f, 12f);
            var result = v / 3f;
            Assert.AreEqual(2f, result.X);
            Assert.AreEqual(3f, result.Y);
            Assert.AreEqual(4f, result.Z);
        }

        [Test]
        public void Magnitude_ReturnsCorrectLength()
        {
            var v = new Vec3(3f, 4f, 0f);
            Assert.AreEqual(5f, v.Magnitude, 0.0001f);
        }

        [Test]
        public void SqrMagnitude_ReturnsSquaredLength()
        {
            var v = new Vec3(3f, 4f, 0f);
            Assert.AreEqual(25f, v.SqrMagnitude, 0.0001f);
        }

        [Test]
        public void Normalized_ReturnsUnitVector()
        {
            var v = new Vec3(3f, 0f, 4f);
            var n = v.Normalized;
            Assert.AreEqual(1f, n.Magnitude, 0.001f);
            Assert.AreEqual(0.6f, n.X, 0.001f);
            Assert.AreEqual(0f, n.Y, 0.001f);
            Assert.AreEqual(0.8f, n.Z, 0.001f);
        }

        [Test]
        public void Normalized_ZeroVector_ReturnsZero()
        {
            var v = Vec3.Zero;
            var n = v.Normalized;
            Assert.AreEqual(0f, n.X);
            Assert.AreEqual(0f, n.Y);
            Assert.AreEqual(0f, n.Z);
        }

        [Test]
        public void Distance_ReturnsCorrectValue()
        {
            var a = new Vec3(1f, 0f, 0f);
            var b = new Vec3(4f, 0f, 0f);
            Assert.AreEqual(3f, Vec3.Distance(a, b), 0.0001f);
        }

        [Test]
        public void Distance_SamePoint_ReturnsZero()
        {
            var a = new Vec3(5f, 5f, 5f);
            Assert.AreEqual(0f, Vec3.Distance(a, a), 0.0001f);
        }

        [Test]
        public void Min_ReturnsSmallerValue()
        {
            Assert.AreEqual(2f, Vec3.Min(2f, 5f));
            Assert.AreEqual(-1f, Vec3.Min(-1f, 0f));
        }

        [Test]
        public void Max_ReturnsLargerValue()
        {
            Assert.AreEqual(5f, Vec3.Max(2f, 5f));
            Assert.AreEqual(0f, Vec3.Max(-1f, 0f));
        }
    }

    public class RGBATests
    {
        [Test]
        public void Constructor_SetsComponents()
        {
            var c = new RGBA(0.1f, 0.2f, 0.3f, 0.4f);
            Assert.AreEqual(0.1f, c.R, 0.0001f);
            Assert.AreEqual(0.2f, c.G, 0.0001f);
            Assert.AreEqual(0.3f, c.B, 0.0001f);
            Assert.AreEqual(0.4f, c.A, 0.0001f);
        }

        [Test]
        public void Constructor_DefaultAlpha_IsOne()
        {
            var c = new RGBA(0.5f, 0.5f, 0.5f);
            Assert.AreEqual(1f, c.A, 0.0001f);
        }

        [Test]
        public void Black_IsCorrect()
        {
            var c = RGBA.Black;
            Assert.AreEqual(0f, c.R);
            Assert.AreEqual(0f, c.G);
            Assert.AreEqual(0f, c.B);
            Assert.AreEqual(1f, c.A);
        }

        [Test]
        public void White_IsCorrect()
        {
            var c = RGBA.White;
            Assert.AreEqual(1f, c.R);
            Assert.AreEqual(1f, c.G);
            Assert.AreEqual(1f, c.B);
            Assert.AreEqual(1f, c.A);
        }

        [Test]
        public void NestingPalette_HasFiveLayers()
        {
            var palette = RGBA.NestingPalette;
            Assert.AreEqual(5, palette.Length);
        }

        [Test]
        public void NestingPalette_AllLayersAreOpaque()
        {
            foreach (var color in RGBA.NestingPalette)
                Assert.AreEqual(1f, color.A, 0.0001f);
        }
    }

    public class BoundsDataTests
    {
        [Test]
        public void Constructor_SetsCenterAndSize()
        {
            var center = new Vec3(1f, 2f, 3f);
            var size = new Vec3(4f, 6f, 8f);
            var bounds = new BoundsData(center, size);
            Assert.AreEqual(1f, bounds.Center.X);
            Assert.AreEqual(2f, bounds.Center.Y);
            Assert.AreEqual(3f, bounds.Center.Z);
            Assert.AreEqual(4f, bounds.Size.X);
            Assert.AreEqual(6f, bounds.Size.Y);
            Assert.AreEqual(8f, bounds.Size.Z);
        }

        [Test]
        public void Extents_IsHalfSize()
        {
            var bounds = new BoundsData(Vec3.Zero, new Vec3(10f, 6f, 8f));
            var extents = bounds.Extents;
            Assert.AreEqual(5f, extents.X, 0.0001f);
            Assert.AreEqual(3f, extents.Y, 0.0001f);
            Assert.AreEqual(4f, extents.Z, 0.0001f);
        }

        [Test]
        public void MinX_MaxX_AreCorrect()
        {
            var bounds = new BoundsData(new Vec3(5f, 0f, 0f), new Vec3(10f, 0f, 0f));
            Assert.AreEqual(0f, bounds.MinX, 0.0001f);
            Assert.AreEqual(10f, bounds.MaxX, 0.0001f);
        }

        [Test]
        public void MinZ_MaxZ_AreCorrect()
        {
            var bounds = new BoundsData(new Vec3(0f, 0f, 3f), new Vec3(0f, 0f, 6f));
            Assert.AreEqual(0f, bounds.MinZ, 0.0001f);
            Assert.AreEqual(6f, bounds.MaxZ, 0.0001f);
        }
    }
}
