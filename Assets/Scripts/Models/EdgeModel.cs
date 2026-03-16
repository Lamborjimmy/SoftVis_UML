using Assets.Scripts.Data;
using System.Collections.Generic;
using System;
namespace Assets.Scripts.Models
{
    [Serializable]
    public class EdgeModel
    {
        public string Id;
        public string FromId;
        public string ToId;
        public string EdgeType;
        public bool IsDashed;
        public bool IsSelfLoop;
        public float LineWidth;
        public RGBA LineColor;
        public DecoratorType StartDecorator;
        public DecoratorType EndDecorator;
        public List<Vec3> Waypoints;
        public string HubGroupKey;
        public EdgeModel()
        {
            Waypoints = new List<Vec3>();
            LineWidth = 0.04f;
            LineColor = RGBA.White;
            StartDecorator = DecoratorType.None;
            EndDecorator = DecoratorType.None;
        }
    }
    [Serializable]
    public class EdgeHubModel
    {
        public string TargetNodeKey;
        public Vec3 HubPosition;
        public List<string> IncomingEdgeKeys;
        public EdgeModel OutgoingEdge;

        public EdgeHubModel()
        {
            IncomingEdgeKeys = new List<string>();
        }
    }
}