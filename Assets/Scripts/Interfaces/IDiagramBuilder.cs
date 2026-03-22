using System.Collections.Generic;
using Softviz.UML.Data;
using Softviz.UML.Models;

namespace Softviz.UML.Interfaces
{
    public interface IDiagramBuilder
    {
        DiagramModel Build(GraphMetadata metadata, List<NodeData> nodes, List<EdgeData> edges);
    }
}