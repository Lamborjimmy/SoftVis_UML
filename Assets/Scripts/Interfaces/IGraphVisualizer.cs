using System.Collections.Generic;
using Assets.Scripts.Data;
using UnityEngine;
namespace Assets.Scripts.Interfaces
{
    public interface IGraphVisualizer
    {
        void Initialize(Dictionary<string, GameObject> prefabs);
        void RenderGraph(GraphMetadata graph, GameObject container, List<NodeData> nodes, List<EdgeData> edges);
    }
}