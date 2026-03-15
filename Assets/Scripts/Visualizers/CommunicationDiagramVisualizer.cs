using Assets.Scripts.Data;
using UnityEngine;
using System.Collections.Generic;
using TMPro;

namespace Assets.Scripts.Visualizers
{
    public class CommunicationDiagramVisualizer : BaseGraphVisualizer
    {
        protected override Dictionary<string, GameObject> BuildDiagramNodes(GameObject nodesParent, List<NodeData> nodes, List<EdgeData> edges, NestingContext nesting)
        {
            return BuildNodes(nodesParent, nodes, nesting);
        }
        private Dictionary<string, GameObject> BuildNodes(GameObject nodesParent, List<NodeData> nodes, NestingContext nesting)
        {
            var nodeObjects = new Dictionary<string, GameObject>();
            foreach (var node in nodes)
            {
                if (node == nesting.RootDiagram) continue;
                int depth = nesting.GetDepth(node.Key);
                float currentElevation = (depth + 1) * Y_ELEVATION;
                string nodeLabel = "Node_" + (node.GetNodeName() ?? node.Key);
                Vector3 nodePosition = new Vector3(node.GetNodePosition().x, node.GetNodePosition().y + currentElevation, node.GetNodePosition().z);
                GameObject nodeGameObject = CreateEmptyGameObject(nodesParent.transform, nodeLabel, nodePosition);
                if (node.Type == DiagramNodeTypes.ACTOR)
                    BuildActorNode(nodeGameObject, node);
                else
                    BuildLifelineNode(nodeGameObject, node);
                nodeObjects[node.Key] = nodeGameObject;
            }
            return nodeObjects;
        }
        private void BuildActorNode(GameObject nodeContainer, NodeData node)
        {
            float textWidth = MeasureText(node.GetNodeName(), HEADER_FONT_SIZE, true);
            float width = 1f;

            var (_, background) = BuildNode(nodeContainer, node, Y_ELEVATION, node.GetNodePosition(), width, width, Color.white, true);
            CreateTextLabel(background.transform, node.GetNodeName(), new Vector3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, 2f), textWidth + 3f, HEADER_FONT_SIZE, TextAlignmentOptions.Center, FontStyles.Bold);
        }
        private void BuildLifelineNode(GameObject nodeContainer, NodeData node)
        {
            float textWidth = MeasureText(node.GetNodeName(), HEADER_FONT_SIZE, true);
            float width = Mathf.Max(textWidth + 3f, 6f);
            float height = 3f;
            var (_, background) = BuildNode(nodeContainer, node, Y_ELEVATION, node.GetNodePosition(), width, height, Color.aquamarine, false);
            CreateTextLabel(background.transform, node.GetNodeName(), new Vector3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, 0), textWidth, HEADER_FONT_SIZE, TextAlignmentOptions.Center, FontStyles.Bold);
        }
    }
}