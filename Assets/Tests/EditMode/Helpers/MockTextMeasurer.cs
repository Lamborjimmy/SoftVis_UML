using Assets.Scripts.Builders;

namespace Assets.Tests.EditMode.Helpers
{
    public class MockTextMeasurer : ITextMeasurer
    {
        public float CharWidth { get; set; } = 0.5f;
        public float BoldMultiplier { get; set; } = 1.2f;

        public float MeasureWidth(string text, float fontSize, bool isBold = false)
        {
            if (string.IsNullOrEmpty(text)) return 0f;
            float baseWidth = text.Length * CharWidth * (fontSize / 10f);
            return isBold ? baseWidth * BoldMultiplier : baseWidth;
        }
    }
}
