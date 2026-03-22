using System;
using System.Collections.Generic;
using System.Linq;
using Softviz.UML.Data;
using Softviz.UML.Models;
using Softviz.UML.Interfaces;

namespace Softviz.UML.Builders
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

            var internalBehaviors = new HashSet<string>();
            foreach (var node in nodes)
            {
                if (node.Properties != null && node.Properties.ContainsKey("behavior_type"))
                    internalBehaviors.Add(node.Key);
            }

            var stateChildrenOnly = new Dictionary<string, List<NodeData>>();
            foreach (var kvp in nesting.ParentToChildren)
            {
                var filtered = kvp.Value.Where(n => !internalBehaviors.Contains(n.Key)).ToList();
                if (filtered.Count > 0)
                {
                    stateChildrenOnly[kvp.Key] = filtered;
                }
            }

            foreach (var node in nodes)
            {
                if (node == nesting.RootDiagram) continue;
                if (internalBehaviors.Contains(node.Key)) continue;

                int depth = nesting.GetDepth(node.Key);
                float currentElevation = (depth + 1) * Y_ELEVATION;

                nesting.ParentToChildren.TryGetValue(node.Key, out var allMembers);

                var behaviors = allMembers?.Where(m => internalBehaviors.Contains(m.Key)).ToList();
                var childStates = allMembers?.Where(m => !internalBehaviors.Contains(m.Key)).ToList();

                NodeModel nodeModel;
                if (node.Type == DiagramNodeTypes.PSEUDOSTATE)
                {
                    nodeModel = BuildPseudostateNode(node, currentElevation);
                }
                else if (childStates != null && childStates.Count > 0)
                {
                    nodeModel = BuildCompositeStateNode(node, stateChildrenOnly, behaviors, currentElevation, depth);
                }
                else
                {
                    nodeModel = BuildStateNode(node, behaviors, currentElevation);
                }

                diagram.Nodes.Add(nodeModel);
            }

            diagram.Edges = BuildEdgeModels(edges);

            return diagram;
        }

        private NodeModel BuildPseudostateNode(NodeData node, float currentElevation)
        {
            float width = 1f;
            var pos = new Vec3(node.GetNodePosition().X, node.GetNodePosition().Y + currentElevation, node.GetNodePosition().Z);
            NodeData tempNode = node;
            tempNode.Type = node.GetNodeName() == "initial" ? DiagramNodeTypes.INITIAL : DiagramNodeTypes.FINAL;

            return BuildNodeModel(tempNode, pos, width, width, RGBA.Black, RGBA.Black, currentElevation, true);
        }

        private NodeModel BuildCompositeStateNode(NodeData node, Dictionary<string, List<NodeData>> stateChildrenOnly, List<NodeData> behaviors, float currentElevation, int depth)
        {
            GetRecursiveBounds(node.Key, stateChildrenOnly, out float minX, out float maxX, out float minZ, out float maxZ);

            float width = (maxX - minX) + 10f;
            float height = (maxZ - minZ) + 8f;
            float centerZ = (minZ + maxZ) / 2f;

            Vec3 position = new Vec3((minX + maxX) / 2f, node.GetNodePosition().Y + currentElevation - (Y_ELEVATION / 2f), centerZ);

            var nodeModel = BuildNodeModel(node, position, width, height, GetNodeColorByDepth(depth), RGBA.Black, 0, false);

            float textZ = (height / 2f) - 1.5f;

            nodeModel.Labels.Add(CreateLabel(
                node.GetNodeName(),
                new Vec3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, textZ),
                width,
                HEADER_FONT_SIZE,
                nodeModel.TextColor,
                TextAlignment.Center,
                FontStyle.Bold
            ));

            if (behaviors != null)
            {
                foreach (var member in behaviors)
                {
                    textZ -= LINE_HEIGHT;

                    string behaviorType = "";
                    if (member.Properties != null && member.Properties.TryGetValue("behavior_type", out var bTypeObj))
                        behaviorType = bTypeObj?.ToString() ?? "";

                    string memberString = $"{behaviorType} / {member.GetNodeName()}";

                    nodeModel.Labels.Add(CreateLabel(
                        memberString,
                        new Vec3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, textZ),
                        width,
                        LABEL_FONT_SIZE,
                        nodeModel.TextColor,
                        TextAlignment.Left,
                        FontStyle.Normal
                    ));
                }
            }

            return nodeModel;
        }

        private NodeModel BuildStateNode(NodeData node, List<NodeData> behaviors, float currentElevation)
        {
            int memberCount = behaviors?.Count ?? 0;
            float textWidth = MeasureText(node.GetNodeName(), HEADER_FONT_SIZE, true);
            float nodeHeight = memberCount > 0 ? (memberCount + 1) * LINE_HEIGHT + PADDING_Z : 2.5f;
            float nodeWidth = Math.Max(textWidth + 3f, 4f);

            var pos = new Vec3(node.GetNodePosition().X, node.GetNodePosition().Y + currentElevation, node.GetNodePosition().Z);

            var nodeModel = BuildNodeModel(node, pos, nodeWidth, nodeHeight, RGBA.MistyRose, RGBA.Black, currentElevation, false);

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

            if (behaviors != null)
            {
                foreach (var member in behaviors)
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