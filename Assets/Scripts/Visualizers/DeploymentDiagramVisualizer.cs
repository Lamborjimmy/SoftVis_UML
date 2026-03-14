using Assets.Scripts.Data;
using UnityEngine;
using System.Collections.Generic;
using TMPro;

namespace Assets.Scripts.Visualizers
{
    public class DeploymentDiagramVisualizer : BaseGraphVisualizer
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
                GameObject nodeGameObject = CreateEmptyGameObject(nodesParent.transform, nodeLabel, Vector3.zero);


                if (nesting.IsContainer(node.Key))
                    BuildContainerNode(nodeGameObject, node, nesting, currentElevation);
                else if (node.Type == DiagramNodeTypes.REQUIRED_INTERFACE || node.Type == DiagramNodeTypes.PROVIDED_INTERFACE)
                    BuildInterfaceNode(nodeGameObject, node, currentElevation);
                else
                    BuildStandardNode(nodeGameObject, node, currentElevation);

                nodeObjects[node.Key] = nodeGameObject;
            }
            return nodeObjects;
        }

        private void BuildContainerNode(GameObject nodeContainer, NodeData node, NestingContext nesting, float currentElevation)
        {
            GetRecursiveBounds(node.Key, nesting.ParentToChildren, out float minX, out float maxX, out float minZ, out float maxZ);

            float width = (maxX - minX) + 12f;
            float height = (maxZ - minZ) + 8f;
            float centerZ = (minZ + maxZ) / 2f;

            nodeContainer.transform.localPosition = new Vector3((minX + maxX) / 2f, node.GetNodePosition().y + currentElevation - (Y_ELEVATION / 2f), centerZ);

            GameObject backgroundGroup = CreateEmptyGameObject(nodeContainer.transform, "Background", Vector3.zero);
            GameObject visualsObj = CreateNodeGameObject(node.Type, backgroundGroup.transform, width, height);

            ApplyColorToHierarchy(visualsObj, GetNodeColor(node.Type));

            float textZ = (height / 2f) - 1.5f;
            CreateTextLabel(backgroundGroup.transform, GetStereotype(node.Type), new Vector3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, textZ), width, LABEL_FONT_SIZE, TextAlignmentOptions.Top);
            CreateTextLabel(backgroundGroup.transform, node.GetNodeName(), new Vector3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, textZ - LINE_HEIGHT), width, HEADER_FONT_SIZE, TextAlignmentOptions.Top, FontStyles.Bold);
        }

        private void BuildInterfaceNode(GameObject nodeContainer, NodeData node, float currentElevation)
        {
            nodeContainer.transform.localPosition = new Vector3(node.GetNodePosition().x, node.GetNodePosition().y + currentElevation, node.GetNodePosition().z);

            GameObject backgroundGroup = CreateEmptyGameObject(nodeContainer.transform, "Background", Vector3.zero);
            GameObject visualsObj = CreateNodeGameObject(node.Type, backgroundGroup.transform, 1.0f, 1.0f, true);

            ApplyColorToHierarchy(visualsObj, new Color(0.9f, 0.8f, 0.9f));
            CreateTextLabel(backgroundGroup.transform, node.GetNodeName(), new Vector3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, -1.5f), 8f, HEADER_FONT_SIZE, TextAlignmentOptions.Top, FontStyles.Bold);
        }

        private void BuildStandardNode(GameObject nodeContainer, NodeData node, float currentElevation)
        {
            nodeContainer.transform.localPosition = new Vector3(node.GetNodePosition().x, node.GetNodePosition().y + currentElevation, node.GetNodePosition().z);

            float textWidth = MeasureText(node.GetNodeName() ?? "", HEADER_FONT_SIZE, true);
            float width = Mathf.Max(textWidth + 3f, 6f);
            float height = 4f;

            GameObject backgroundGroup = CreateEmptyGameObject(nodeContainer.transform, "Background", Vector3.zero);
            GameObject visualsObj = CreateNodeGameObject(node.Type, backgroundGroup.transform, width, height);

            ApplyColorToHierarchy(visualsObj, GetNodeColor(node.Type));

            CreateTextLabel(backgroundGroup.transform, GetStereotype(node.Type), new Vector3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, 0.5f), width, LABEL_FONT_SIZE, TextAlignmentOptions.Center);
            CreateTextLabel(backgroundGroup.transform, node.GetNodeName(), new Vector3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, 0.5f - LINE_HEIGHT), width, HEADER_FONT_SIZE, TextAlignmentOptions.Center, FontStyles.Bold);
        }

        private Color GetNodeColor(string nodeType)
        {
            if (nodeType == DiagramNodeTypes.NODE) return new Color(0.35f, 0.35f, 0.45f);
            if (nodeType == DiagramNodeTypes.COMPONENT) return new Color(0.85f, 0.95f, 0.85f);
            if (nodeType == DiagramNodeTypes.ARTIFACT) return new Color(0.95f, 0.95f, 0.85f);
            return Color.white;
        }

        private string GetStereotype(string nodeType)
        {
            if (nodeType == DiagramNodeTypes.NODE) return "<<node>>";
            if (nodeType == DiagramNodeTypes.COMPONENT) return "<<component>>";
            if (nodeType == DiagramNodeTypes.ARTIFACT) return "<<artifact>>";
            return "";
        }
    }
}