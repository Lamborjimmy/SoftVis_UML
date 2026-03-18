using Assets.Scripts.Builders;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.Renderers.Unity
{
    public class UnityTextMeasurer : ITextMeasurer
    {
        private TextMeshPro measurer;

        public float MeasureWidth(string text, float fontSize, bool isBold = false)
        {
            if (string.IsNullOrEmpty(text)) return 0f;

            EnsureMeasurerExists();
            measurer.fontSize = fontSize;
            measurer.fontStyle = isBold ? FontStyles.Bold : FontStyles.Normal;
            measurer.text = text;
            measurer.ForceMeshUpdate(true);

            return measurer.preferredWidth;
        }

        private void EnsureMeasurerExists()
        {
            if (measurer != null) return;

            var go = new GameObject("TextMeasurer");
            measurer = go.AddComponent<TextMeshPro>();
            measurer.enableAutoSizing = false;
            measurer.overflowMode = TextOverflowModes.Overflow;
            measurer.alignment = TextAlignmentOptions.Left;
            go.SetActive(false);
        }
    }
}
