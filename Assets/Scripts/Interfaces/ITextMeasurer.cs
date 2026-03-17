namespace Assets.Scripts.Builders
{
    public interface ITextMeasurer
    {
        float MeasureWidth(string text, float fontSize, bool isBold = false);
    }
}
