using System.Collections.Generic;
using Assets.Scripts.Data;
using Assets.Scripts.Models;

namespace Assets.Scripts.Interfaces
{
    public interface IDiagramBuilder
    {
        DiagramModel Build(GraphMetadata metadata, List<NodeData> nodes, List<EdgeData> edges);
    }
}