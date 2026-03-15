using Assets.Scripts.Data;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;

namespace Assets.Scripts.Visualizers
{
    public class UseCaseDiagramVisualizer : BaseGraphVisualizer
    {
        protected override Dictionary<string, GameObject> BuildDiagramNodes(GameObject nodesParent, List<NodeData> nodes, List<EdgeData> edges, NestingContext nesting)
        {
            var extensionPointsMap = BuildExtensionPointsMap(nodes, edges);
            return BuildNodes(nodesParent, nodes, nesting, extensionPointsMap);
        }
        private Dictionary<string, GameObject> BuildNodes(GameObject nodesParent, List<NodeData> nodes, NestingContext nesting, Dictionary<string, List<string>> extensionPointsMap)
        {
            var nodeObjects = new Dictionary<string, GameObject>();
            foreach (var node in nodes)
            {
                if (node == nesting.RootDiagram) continue;
                int depth = nesting.GetDepth(node.Key);
                float currentElevation = (depth + 1) * Y_ELEVATION;
                string nodeLabel = "Node_" + (node.GetNodeName() ?? node.Key);
                GameObject nodeGameObject = CreateEmptyGameObject(nodesParent.transform, nodeLabel, Vector3.zero);
                if (nesting.IsContainer(node.Key))
                    BuildContainerNode(nodeGameObject, node, nesting, currentElevation);
                else if (node.Type == DiagramNodeTypes.ACTOR)
                    BuildActorNode(nodeGameObject, node, currentElevation);
                else if (node.Type == DiagramNodeTypes.USECASE)
                    BuildUseCaseNode(nodeGameObject, node, extensionPointsMap, currentElevation);
                nodeObjects[node.Key] = nodeGameObject;
            }
            return nodeObjects;
        }
        private Dictionary<string, List<string>> BuildExtensionPointsMap(List<NodeData> nodes, List<EdgeData> edges)
        {
            return edges
                .Where(e => e.Type == DiagramEdgeTypes.EXTENDS_UML)
                .GroupBy(e => ExtractKeyFromId(e.To))
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => nodes.FirstOrDefault(n => n.Key == ExtractKeyFromId(e.From))?.GetNodeName() ?? "Unknown").ToList()
                );
        }
        private void BuildContainerNode(GameObject nodeContainer, NodeData node, NestingContext nesting, float currentElevation)
        {
            GetRecursiveBounds(node.Key, nesting.ParentToChildren, out float minX, out float maxX, out float minZ, out float maxZ);

            float paddingX = 6.0f;
            float paddingZ = 4.0f;
            float width = (maxX - minX) + paddingX * 2;
            float height = (maxZ - minZ) + paddingZ * 2;
            float centerZ = (minZ + maxZ) / 2f;

            nodeContainer.transform.localPosition = new Vector3((minX + maxX) / 2f, node.GetNodePosition().y + currentElevation - (Y_ELEVATION / 2f), centerZ);

            GameObject backgroundGroup = CreateEmptyGameObject(nodeContainer.transform, "Background", Vector3.zero);
            GameObject visualsObj = CreateNodeGameObject(node.Type, backgroundGroup.transform, width, height);

            ApplyColorToHierarchy(visualsObj, new Color(0.0f, 0.2f, 0.9f, 0.8f));

            CreateTextLabel(backgroundGroup.transform, node.GetNodeName(), new Vector3(0, (Y_ELEVATION / 2f) + 0.1f, (height / 2f) - 1.2f), width, HEADER_FONT_SIZE, TextAlignmentOptions.Center, FontStyles.Bold);
        }
        private void BuildActorNode(GameObject nodeContainer, NodeData node, float currentElevation)
        {
            float textWidth = MeasureText(node.GetNodeName(), HEADER_FONT_SIZE, true);
            float width = 1f;

            var (_, background) = BuildNode(nodeContainer, node, currentElevation, node.GetNodePosition(), width, width, Color.white, true);
            CreateTextLabel(background.transform, node.GetNodeName(), new Vector3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, 2f), textWidth + 3f, HEADER_FONT_SIZE, TextAlignmentOptions.Center, FontStyles.Bold);
        }

        private void BuildUseCaseNode(GameObject nodeContainer, NodeData node, Dictionary<string, List<string>> extensionPointsMap, float currentElevation)
        {
            float textWidth = MeasureText(node.GetNodeName(), HEADER_FONT_SIZE, true);

            string labelText = node.GetNodeName();
            int lineCount = 1;
            if (extensionPointsMap.TryGetValue(node.Key, out List<string> points))
            {
                labelText = $"<b>{node.GetNodeName()}</b>\n<size=80%>extension points</size>";
                foreach (var p in points) labelText += $"\n<size=70%>{p}</size>";
                lineCount = 2 + points.Count;
            }

            float ovalWidth = Mathf.Max(textWidth + 2.0f, 6f);
            float baseHeight = ovalWidth / 2f;
            float textHeightRequirement = lineCount * LINE_HEIGHT;
            float ovalHeight = Mathf.Max(baseHeight, textHeightRequirement);

            var (_, background) = BuildNode(nodeContainer, node, currentElevation, node.GetNodePosition(), ovalWidth, ovalHeight, Color.softRed, false);

            CreateTextLabel(background.transform, labelText, new Vector3(0, Y_ELEVATION * 2f + Y_ELEVATION_TEXT_OFFSET, 0), ovalWidth, LABEL_FONT_SIZE);

        }
    }
}