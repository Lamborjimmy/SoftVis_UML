using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Data;
using UnityEngine;

namespace Assets.Scripts.Visualizers
{
    public class DefaultGraphVisualizer : BaseGraphVisualizer
    {
        protected override Dictionary<string, GameObject> BuildDiagramNodes(GameObject nodesParent, List<NodeData> nodes, List<EdgeData> edges, NestingContext nesting)
        {
            return new Dictionary<string, GameObject>();
        }
    }

}
