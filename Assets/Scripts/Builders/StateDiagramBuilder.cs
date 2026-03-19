using System;
using System.Collections.Generic;
using Assets.Scripts.Data;
using Assets.Scripts.Models;

namespace Assets.Scripts.Builders
{
    public class StateDiagramBuilder : BaseDiagramBuilder
    {
        public StateDiagramBuilder(ITextMeasurer textMeasurer) : base(textMeasurer) { }

        public override DiagramModel Build(GraphMetadata metadata, List<NodeData> nodes, List<EdgeData> edges)
        {
            var diagram = new DiagramModel
            {
                Id = metadata.Key,
                Name = metadata.Name,
                DiagramType = DiagramTypes.STATE_DIAGRAM
            };

            var nesting = BuildNestingHierarchy(nodes, edges);

            foreach (var node in nodes)
            {
                if (node == nesting.RootDiagram) continue;

                if (nesting.ChildToParent.TryGetValue(node.Key, out string parentKey) && nesting.RootDiagram != null && parentKey != nesting.RootDiagram.Key) continue;


                nesting.ParentToChildren.TryGetValue(node.Key, out var members);

                NodeModel nodeModel;
                if (node.Type == DiagramNodeTypes.PSEUDOSTATE)
                    nodeModel = BuildPseudostateNode(node);
                else
                    nodeModel = BuildStateNode(node, members);

                diagram.Nodes.Add(nodeModel);
            }

            diagram.Edges = BuildEdgeModels(edges);

            return diagram;
        }

        private NodeModel BuildPseudostateNode(NodeData node)
        {
            float width = 1f;
            var pos = new Vec3(node.GetNodePosition().X, node.GetNodePosition().Y + Y_ELEVATION, node.GetNodePosition().Z);
            NodeData tempNode = node;
            tempNode.Type = node.GetNodeName() == "initial" ? DiagramNodeTypes.INITIAL : DiagramNodeTypes.FINAL;

            var nodeModel = BuildNodeModel(tempNode, pos, width, width, RGBA.Black, RGBA.Black, Y_ELEVATION, true);
            return nodeModel;
        }

        private NodeModel BuildStateNode(NodeData node, List<NodeData> members)
        {
            int memberCount = members?.Count ?? 0;
            float textWidth = MeasureText(node.GetNodeName(), HEADER_FONT_SIZE, true);
            float nodeHeight = memberCount > 0 ? (memberCount + 1) * LINE_HEIGHT + PADDING_Z : 2.5f;
            float nodeWidth = Math.Max(textWidth + 3f, 4f);
            var pos = new Vec3(node.GetNodePosition().X, node.GetNodePosition().Y + Y_ELEVATION, node.GetNodePosition().Z);

            var nodeModel = BuildNodeModel(node, pos, nodeWidth, nodeHeight, RGBA.MistyRose, RGBA.Black, Y_ELEVATION, false);

            float currentZ = (nodeHeight / 2f) - PADDING_Z / 8f;

            nodeModel.Labels.Add(CreateLabel(
                node.GetNodeName(),
                new Vec3(0, Y_ELEVATION * 2 + Y_ELEVATION_TEXT_OFFSET, currentZ),
                nodeWidth,
                HEADER_FONT_SIZE,
                nodeModel.TextColor,
                TextAlignment.Center,
                FontStyle.Bold
            ));

            if (members != null)
            {
                foreach (var member in members)
                {
                    currentZ -= LINE_HEIGHT;

                    string behaviorType = "";
                    if (member.Properties != null && member.Properties.TryGetValue("behavior_type", out var bTypeObj))
                        behaviorType = bTypeObj?.ToString() ?? "";

                    string memberString = $"{behaviorType} / {member.GetNodeName()}";

                    nodeModel.Labels.Add(CreateLabel(
                        memberString,
                        new Vec3(0, Y_ELEVATION * 2 + Y_ELEVATION_TEXT_OFFSET, currentZ),
                        nodeWidth,
                        LABEL_FONT_SIZE,
                        nodeModel.TextColor,
                        TextAlignment.Left,
                        FontStyle.Normal
                    ));
                }
            }

            return nodeModel;
        }
    }
}