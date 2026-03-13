using Assets.Scripts.Data;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;

namespace Assets.Scripts.Visualizers
{
    public class UseCaseDiagramVisualizer : BaseGraphVisualizer
    {
        protected override void DrawDiagramContent(GameObject container, List<NodeData> nodes, List<EdgeData> edges)
        {
            var (nodesParent, edgesParent) = CreateParentObjects(container);
            var extensionPointsMap = BuildExtensionPointsMap(nodes, edges);
            NestingContext ctx = BuildNestingHierarchy(nodes, edges);
            var nodeObjects = BuildNodes(nodesParent, nodes, ctx, extensionPointsMap);
            FilterAndRenderEdges(edges, nodeObjects, edgesParent.transform);
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
                Vector3 nodePosition = new Vector3(node.GetNodePosition().x, node.GetNodePosition().y + currentElevation, node.GetNodePosition().z);
                GameObject nodeGameObject = CreateEmptyGameObject(nodesParent.transform, nodeLabel, nodePosition);
                if (nesting.IsContainer(node.Key))
                {
                    BuildContainerNode(nodeGameObject, node, nesting, currentElevation);
                    nodeObjects[node.Key] = nodeGameObject;
                    continue;
                }
                if (node.Type == DiagramNodeTypes.ACTOR)
                    BuildActorNode(nodeGameObject, node);
                else if (node.Type == DiagramNodeTypes.USECASE)
                    BuildUseCaseNode(nodeGameObject, node, extensionPointsMap);
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
            float paddingX = 6.0f;//TODO make this padding same in all diagrams and add it to the base visualizer
            float paddingZ = 4.0f;//TODO make this padding same in all diagrams and add it to the base visualizer
            float width = (maxX - minX) + paddingX * 2;
            float height = (maxZ - minZ) + paddingZ * 2;
            Vector3 center = new Vector3((minX + maxX) / 2f, node.GetNodePosition().y + currentElevation - (Y_ELEVATION / 2f), (minZ + maxZ) / 2f);
            nodeContainer.transform.localPosition = center;
            Vector3 scale = new Vector3(width, Y_ELEVATION, height);
            GameObject backgroundGroup = CreatePrimitive(PrimitiveType.Cube, nodeContainer.transform, "Background", Vector3.zero, Quaternion.identity, scale);
            ApplyMaterialToSingle(backgroundGroup, new Color(0.0f, 0.2f, 0.9f, 0.8f));
            CreateTextLabel(nodeContainer.transform, node.GetNodeName(), new Vector3(0, (Y_ELEVATION / 2f) + 0.1f, (height / 2f) - 1.2f), width, HEADER_FONT_SIZE, TextAlignmentOptions.Center, FontStyles.Bold);
        }
        private void BuildActorNode(GameObject nodeContainer, NodeData node)
        {
            float textWidth = MeasureText(node.GetNodeName(), HEADER_FONT_SIZE, true);
            GameObject backgroundGroup = CreateEmptyGameObject(nodeContainer.transform, "Background", Vector3.zero);
            CreateNodeGameObject(DiagramNodeTypes.ACTOR, backgroundGroup.transform, 1f, 1f, true);
            CreateTextLabel(backgroundGroup.transform, node.GetNodeName(), new Vector3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, 2f), textWidth + 3f, HEADER_FONT_SIZE, TextAlignmentOptions.Center, FontStyles.Bold);
        }

        private void BuildUseCaseNode(GameObject nodeContainer, NodeData node, Dictionary<string, List<string>> extensionPointsMap)
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

            GameObject backgroundGroup = CreateEmptyGameObject(nodeContainer.transform, "Background", Vector3.zero);
            GameObject nodeVisualsObj = CreateNodeGameObject(node.Type, backgroundGroup.transform, ovalWidth, ovalHeight);

            ApplyMaterialToSingle(nodeVisualsObj, new Color(0.75f, 0.95f, 0.75f));
            CreateTextLabel(backgroundGroup.transform, labelText, new Vector3(0, Y_ELEVATION * 2f + Y_ELEVATION_TEXT_OFFSET, 0), ovalWidth, LABEL_FONT_SIZE);

        }
    }
}