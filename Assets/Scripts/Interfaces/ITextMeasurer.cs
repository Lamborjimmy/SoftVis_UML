namespace Softviz.UML.Interfaces
{
    public interface ITextMeasurer
    {
        float MeasureWidth(string text, float fontSize, bool isBold = false);
    }
}
