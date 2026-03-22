using System.Collections.Generic;
using Softviz.UML.Data;
using UnityEngine;
namespace Softviz.UML.Interfaces
{
    public interface IGraphVisualizer
    {
        void Initialize(Dictionary<string, GameObject> prefabs);
        void RenderGraph(GraphMetadata graph, GameObject container, List<NodeData> nodes, List<EdgeData> edges);
    }
}