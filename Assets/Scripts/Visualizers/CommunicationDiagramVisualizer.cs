using Assets.Scripts.Data;
using UnityEngine;
using System.Collections.Generic;
using TMPro;

namespace Assets.Scripts.Visualizers
{
    public class CommunicationDiagramVisualizer : BaseGraphVisualizer
    {
        protected override void DrawDiagramContent(GameObject container, List<NodeData> nodes, List<EdgeData> edges)
        {
            var (nodesParent, edgesParent) = CreateParentObjects(container);
            NestingContext ctx = BuildNestingHierarchy(nodes, edges);
            var nodeObjects = BuildNodes(nodesParent, nodes, ctx);
            FilterAndRenderEdges(edges, nodeObjects, edgesParent.transform);
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
            GameObject backgroundGroup = CreateEmptyGameObject(nodeContainer.transform, "Background", Vector3.zero);
            CreateNodeGameObject(node.Type, backgroundGroup.transform, 1f, 1f, true);
            CreateTextLabel(backgroundGroup.transform, node.GetNodeName(), new Vector3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, 2f), textWidth + 3f, HEADER_FONT_SIZE, TextAlignmentOptions.Center, FontStyles.Bold);
        }
        private void BuildLifelineNode(GameObject nodeContainer, NodeData node)
        {
            float textWidth = MeasureText(node.GetNodeName(), HEADER_FONT_SIZE, true);
            float width = Mathf.Max(textWidth + 3f, 6f);
            float height = 4f;
            GameObject backgroundGroup = CreateEmptyGameObject(nodeContainer.transform, "Background", Vector3.zero);
            GameObject visualsObj = CreateNodeGameObject(node.Type, backgroundGroup.transform, width, height);
            ApplyMaterialToHierarchy(visualsObj, new Color(0.7f, 0.85f, 0.9f));
            CreateTextLabel(backgroundGroup.transform, node.GetNodeName(), new Vector3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, 0), textWidth, HEADER_FONT_SIZE, TextAlignmentOptions.Center, FontStyles.Bold);
        }
    }
}