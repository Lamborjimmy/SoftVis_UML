using System.Collections.Generic;
using Assets.Scripts.Data;
using UnityEngine;
namespace Assets.Scripts.Interfaces
{
    public interface IGraphVisualizer
    {
        void RenderGraph(GraphMetadata graph, GameObject container, List<NodeData> nodes, List<EdgeData> edges);
    }
}