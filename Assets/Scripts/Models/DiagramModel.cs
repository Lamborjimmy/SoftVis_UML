using Assets.Scripts.Data;
using System;
using System.Collections.Generic;

namespace Assets.Scripts.Models
{
    [Serializable]
    public class DiagramModel
    {
        public string Id;
        public string Name;
        public string DiagramType;
        public Vec3 BasePlaneCenter;
        public Vec3 BasePlaneScale;
        public RGBA BasePlaneColor;
        public List<NodeModel> Nodes;
        public List<EdgeModel> Edges;
        public List<EdgeHubModel> EdgeHubs;
        public DiagramModel()
        {
            Nodes = new List<NodeModel>();
            Edges = new List<EdgeModel>();
            EdgeHubs = new List<EdgeHubModel>();
            BasePlaneColor = RGBA.Gray;
        }
    }
}