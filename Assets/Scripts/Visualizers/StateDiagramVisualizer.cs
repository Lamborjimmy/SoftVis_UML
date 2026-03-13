using Assets.Scripts.Data;
using UnityEngine;
using System.Collections.Generic;
using TMPro;

namespace Assets.Scripts.Visualizers
{
    public class StateDiagramVisualizer : BaseGraphVisualizer
    {
        protected override void DrawDiagramContent(GameObject container, List<NodeData> nodes, List<EdgeData> edges)
        {
            var (nodesParent, edgesParent) = CreateParentObjects(container);
            var nodeObjects = BuildNodes(nodesParent, nodes);
            FilterAndRenderEdges(edges, nodeObjects, edgesParent.transform);
        }
        private Dictionary<string, GameObject> BuildNodes(GameObject nodesParent, List<NodeData> nodes)
        {
            var nodeObjects = new Dictionary<string, GameObject>();
            foreach (var node in nodes)
            {
                if (node.Type == DiagramNodeTypes.DIAGRAM) continue;
                string nodeLabel = "Node_" + (node.GetNodeName() ?? node.Key);
                Vector3 nodePosition = new Vector3(node.GetNodePosition().x, node.GetNodePosition().y + Y_ELEVATION, node.GetNodePosition().z);
                GameObject nodeGameObject = CreateEmptyGameObject(nodesParent.transform, nodeLabel, nodePosition);
                if (node.Type == DiagramNodeTypes.PSEUDOSTATE)
                    BuildPseudostateNode(nodeGameObject, node);
                else
                    BuildStateNode(nodeGameObject, node);
                nodeObjects[node.Key] = nodeGameObject;
            }
            return nodeObjects;
        }
        private void BuildPseudostateNode(GameObject nodeContainer, NodeData node)
        {
            string prefabKey = node.GetNodeName() == "initial" ? DiagramNodeTypes.INITIAL : DiagramNodeTypes.FINAL;
            GameObject backgroundGroup = CreateEmptyGameObject(nodeContainer.transform, "Background", Vector3.zero);
            CreateNodeGameObject(prefabKey, backgroundGroup.transform, 1f, 1f, true);
        }

        private void BuildStateNode(GameObject nodeContainer, NodeData node)
        {
            float textWidth = MeasureText(node.GetNodeName(), HEADER_FONT_SIZE, true);

            float nodeWidth = Mathf.Max(textWidth, 4f);
            float nodeHeight = 2.5f;

            GameObject backgroundGroup = CreateEmptyGameObject(nodeContainer.transform, "Background", Vector3.zero);
            CreateNodeGameObject(node.Type, backgroundGroup.transform, nodeWidth, nodeHeight);
            CreateTextLabel(backgroundGroup.transform, node.GetNodeName(), new Vector3(0, Y_ELEVATION * 2 + Y_ELEVATION_TEXT_OFFSET, 0), nodeWidth, HEADER_FONT_SIZE, TextAlignmentOptions.Center);

        }
    }
}