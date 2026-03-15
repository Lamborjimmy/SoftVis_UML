using Assets.Scripts.Data;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;

namespace Assets.Scripts.Visualizers
{
    public class ClassDiagramVisualizer : BaseGraphVisualizer
    {
        private const float PADDING_Z = 1.0f;

        protected override Dictionary<string, GameObject> BuildDiagramNodes(GameObject nodesParent, List<NodeData> nodes, List<EdgeData> edges, NestingContext nesting)
        {
            return BuildNodes(nodesParent, nodes, nesting);
        }

        private Dictionary<string, GameObject> BuildNodes(GameObject nodesParent, List<NodeData> nodes, NestingContext ctx)
        {
            var nodeObjects = new Dictionary<string, GameObject>();

            foreach (var node in nodes)
            {
                if (node == ctx.RootDiagram) continue;

                if (ctx.ChildToParent.TryGetValue(node.Key, out string parentKey) && ctx.RootDiagram != null && parentKey != ctx.RootDiagram.Key)
                    continue;

                ctx.ParentToChildren.TryGetValue(node.Key, out var members);
                BuildNode(nodesParent, node, members, nodeObjects);
            }

            return nodeObjects;
        }

        private void BuildNode(GameObject nodesParent, NodeData node, List<NodeData> members, Dictionary<string, GameObject> nodeObjects)
        {
            int memberCount = members?.Count ?? 0;
            var (totalX, totalZ) = CalculateClassDimensions(node, members, memberCount);

            string nodeLabel = "Node_" + (node.GetNodeName() ?? node.Key);
            Vector3 nodePosition = new Vector3(node.GetNodePosition().x, node.GetNodePosition().y, node.GetNodePosition().z);
            GameObject nodeGameObject = CreateEmptyGameObject(nodesParent.transform, nodeLabel, nodePosition);
            var (_, background) = BuildNode(nodeGameObject, node, Y_ELEVATION, nodePosition, totalX, totalZ, Color.bisque, false);

            SpawnClassLabels(background.transform, node, members, totalX, totalZ);
            if (members != null)
            {
                foreach (var member in members)
                    nodeObjects[member.Key] = nodeGameObject;
            }

            nodeObjects[node.Key] = nodeGameObject;
        }

        private (float totalX, float totalZ) CalculateClassDimensions(NodeData node, List<NodeData> members, int memberCount)
        {
            float maxTextWidth = MeasureText(node.GetNodeName() ?? "", HEADER_FONT_SIZE, true);
            float totalZ;

            if (node.Type == DiagramNodeTypes.ENUMERATION || node.Type == DiagramNodeTypes.INTERFACE)
            {
                totalZ = (memberCount + 2) * LINE_HEIGHT + PADDING_Z;//+2 heading and stereotype
                string stereotype = node.Type == DiagramNodeTypes.INTERFACE ? "<<interface>>" : "<<enumeration>>";
                float stereoWidth = MeasureText(stereotype, LABEL_FONT_SIZE, false);

                if (maxTextWidth < stereoWidth)
                    maxTextWidth = stereoWidth; ;
            }
            else
                totalZ = (memberCount + 1) * LINE_HEIGHT + PADDING_Z;//+1 heading

            if (members != null)
            {
                foreach (var m in members)
                {
                    string displayText = m.Type == DiagramNodeTypes.METHOD
                        ? "+ " + m.GetNodeName() + "()"
                        : "- " + m.GetNodeName() + " : " + m.Properties["type_name"];

                    float w = MeasureText(displayText, LABEL_FONT_SIZE, false);
                    if (w > maxTextWidth) maxTextWidth = w;
                }
            }

            float totalX = Mathf.Max(maxTextWidth + 3f, 7f);
            return (totalX, totalZ);
        }

        private void SpawnClassLabels(Transform classTransform, NodeData node, List<NodeData> members, float totalX, float totalZ)
        {
            float currentZ = (totalZ / 2f) - (PADDING_Z / 2f);

            CreateTextLabel(classTransform, node.GetNodeName(), new Vector3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, currentZ), totalX, HEADER_FONT_SIZE, TextAlignmentOptions.Center, FontStyles.Bold);

            if (node.Type == DiagramNodeTypes.ENUMERATION || node.Type == DiagramNodeTypes.INTERFACE)
            {
                currentZ -= LINE_HEIGHT;
                string stereoType = node.Type == DiagramNodeTypes.INTERFACE ? "<<interface>>" : "<<enumeration>>";
                CreateTextLabel(classTransform, stereoType, new Vector3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, currentZ), totalX, LABEL_FONT_SIZE, TextAlignmentOptions.Center);
            }

            if (members != null)
            {
                foreach (var member in members)
                {
                    currentZ -= LINE_HEIGHT;

                    string memberString = member.Type == DiagramNodeTypes.METHOD
                        ? "+ " + member.GetNodeName() + "()"
                        : "- " + member.GetNodeName() + ":" + member.Properties["type_name"];

                    CreateTextLabel(classTransform, memberString, new Vector3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, currentZ), totalX, LABEL_FONT_SIZE, TextAlignmentOptions.Left);
                }
            }
        }
    }
}