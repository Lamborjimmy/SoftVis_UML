using System;
using System.Collections.Generic;
using Softviz.UML.Data;
using Softviz.UML.Models;
using Softviz.UML.Interfaces;

namespace Softviz.UML.Builders
{
    public class ClassDiagramBuilder : BaseDiagramBuilder
    {
        public ClassDiagramBuilder(ITextMeasurer textMeasurer) : base(textMeasurer) { }

        public override DiagramModel Build(GraphMetadata metadata, List<NodeData> nodes, List<EdgeData> edges)
        {
            var diagram = new DiagramModel
            {
                Id = metadata.Key,
                Name = metadata.Name,
                DiagramType = DiagramTypes.CLASS_DIAGRAM
            };
            var nesting = BuildNestingHierarchy(nodes, edges);
            foreach (var node in nodes)
            {
                if (node == nesting.RootDiagram) continue;
                if (nesting.ChildToParent.TryGetValue(node.Key, out string parentKey) && nesting.RootDiagram != null && parentKey != nesting.RootDiagram.Key) continue;
                nesting.ParentToChildren.TryGetValue(node.Key, out var members);
                NodeModel nodeModel = BuildClassNodeModel(node, members);
                diagram.Nodes.Add(nodeModel);
            }
            diagram.Edges = BuildEdgeModels(edges);
            return diagram;
        }
        private NodeModel BuildClassNodeModel(NodeData node, List<NodeData> members)
        {
            int memberCount = members?.Count ?? 0;
            var (width, height) = CalculateClassDimensions(node, members, memberCount);
            var pos = new Vec3(node.GetNodePosition().X, node.GetNodePosition().Y + Y_ELEVATION, node.GetNodePosition().Z);

            var nodeModel = BuildNodeModel(node, pos, width, height, RGBA.SoftYellow, RGBA.Black, Y_ELEVATION, false);

            if (node.Type == DiagramNodeTypes.INTERFACE || node.Type == DiagramNodeTypes.ENUMERATION)
            {
                nodeModel.StereotypeLabel = node.Type == DiagramNodeTypes.INTERFACE
                    ? "<<interface>>"
                    : "<<enumeration>>";
            }

            if (members != null)
            {
                foreach (var member in members)
                {
                    string typeName = "";
                    if (member.Properties != null && member.Properties.TryGetValue("type_name", out var typeObj))
                        typeName = typeObj?.ToString() ?? "";

                    string displayText = member.Type == DiagramNodeTypes.METHOD
                        ? "+ " + member.GetNodeName() + "()"
                        : "- " + member.GetNodeName() + ":" + typeName;

                    nodeModel.Members.Add(new MemberModel
                    {
                        Id = member.Key,
                        Text = displayText,
                        MemberType = member.Type == DiagramNodeTypes.METHOD ? "method" : "attribute",
                        FontSize = LABEL_FONT_SIZE,
                        Alignment = TextAlignment.Left
                    });
                }
            }

            BuildClassLabels(nodeModel, node, members, width, height);

            return nodeModel;
        }
        private (float width, float height) CalculateClassDimensions(NodeData node, List<NodeData> members, int memberCount)
        {
            float maxTextWidth = MeasureText(node.GetNodeName() ?? "", HEADER_FONT_SIZE, true);
            float totalZ;

            if (node.Type == DiagramNodeTypes.ENUMERATION || node.Type == DiagramNodeTypes.INTERFACE)
            {
                totalZ = (memberCount + 2) * LINE_HEIGHT + PADDING_Z;
                string stereotype = node.Type == DiagramNodeTypes.INTERFACE ? "<<interface>>" : "<<enumeration>>";
                float stereoWidth = MeasureText(stereotype, LABEL_FONT_SIZE, false);

                if (maxTextWidth < stereoWidth)
                    maxTextWidth = stereoWidth;
            }
            else
            {
                totalZ = (memberCount + 1) * LINE_HEIGHT + PADDING_Z;
            }

            if (members != null)
            {
                foreach (var m in members)
                {
                    string typeName = "";
                    if (m.Properties != null && m.Properties.TryGetValue("type_name", out var typeObj))
                        typeName = typeObj?.ToString() ?? "";

                    string displayText = m.Type == DiagramNodeTypes.METHOD
                        ? "+ " + m.GetNodeName() + "()"
                        : "- " + m.GetNodeName() + " : " + typeName;

                    float w = MeasureText(displayText, LABEL_FONT_SIZE, false);
                    if (w > maxTextWidth) maxTextWidth = w;
                }
            }

            float totalX = Math.Max(maxTextWidth + 3f, 7f);
            return (totalX, totalZ);
        }
        private void BuildClassLabels(NodeModel nodeModel, NodeData node, List<NodeData> members, float width, float height)
        {
            float currentZ = (height / 2f) - (PADDING_Z / 2f);

            nodeModel.Labels.Add(CreateLabel(
                node.GetNodeName(),
                new Vec3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, currentZ),
                width,
                HEADER_FONT_SIZE,
                nodeModel.TextColor,
                TextAlignment.Center,
                FontStyle.Bold
            ));

            if (node.Type == DiagramNodeTypes.ENUMERATION || node.Type == DiagramNodeTypes.INTERFACE)
            {
                currentZ -= LINE_HEIGHT;
                nodeModel.Labels.Add(CreateLabel(
                    nodeModel.StereotypeLabel,
                    new Vec3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, currentZ),
                    width,
                    LABEL_FONT_SIZE,
                    nodeModel.TextColor,
                    TextAlignment.Center,
                    FontStyle.Normal
                ));
            }

            if (members != null)
            {
                foreach (var member in members)
                {
                    currentZ -= LINE_HEIGHT;

                    string typeName = "";
                    if (member.Properties != null && member.Properties.TryGetValue("type_name", out var typeObj))
                        typeName = typeObj?.ToString() ?? "";

                    string memberString = member.Type == DiagramNodeTypes.METHOD
                        ? "+ " + member.GetNodeName() + "()"
                        : "- " + member.GetNodeName() + ":" + typeName;

                    nodeModel.Labels.Add(CreateLabel(
                        memberString,
                        new Vec3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, currentZ),
                        width,
                        LABEL_FONT_SIZE,
                        nodeModel.TextColor,
                        TextAlignment.Left,
                        FontStyle.Normal
                    ));
                }
            }
        }
    }
}