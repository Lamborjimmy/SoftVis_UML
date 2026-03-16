using Assets.Scripts.Data;
using System;
using System.Collections.Generic;
namespace Assets.Scripts.Models
{
    [Serializable]
    public class NodeModel
    {
        public string Id;
        public string Label;
        public string NodeType;
        public string StereotypeLabel;

        public Vec3 Position;
        public Vec3 Scale;
        public float Elevation;

        public RGBA BackgroundColor;
        public RGBA TextColor;

        public bool UseInformScale;
        public string ParentKey;
        public List<string> ChildKeys;

        public List<MemberModel> Members;
        public List<TextLabelModel> Labels;

        public BoundsData bounds;
        public NodeModel()
        {
            ChildKeys = new List<string>();
            Members = new List<MemberModel>();
            Labels = new List<TextLabelModel>();
            BackgroundColor = RGBA.Black;
            TextColor = RGBA.Black;
        }
    }
    [Serializable]
    public class MemberModel
    {
        public string Id;
        public string Text;
        public string MemberType;
        public float FontSize;
        public TextAlignment Alignment;
    }
    [Serializable]
    public class TextLabelModel
    {
        public string Text;
        public Vec3 Position;
        public float Width;
        public float FontSize;
        public TextAlignment Alignment;
        public FontStyle Style;
        public RGBA Color;
        public TextLabelModel()
        {
            Color = RGBA.Black;
            Style = FontStyle.Normal;
            Alignment = TextAlignment.Center;
        }
    }
}