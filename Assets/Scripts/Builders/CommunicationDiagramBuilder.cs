using System;
using System.Collections.Generic;
using Assets.Scripts.Data;
using Assets.Scripts.Models;

namespace Assets.Scripts.Builders
{
    public class CommunicationDiagramBuilder : BaseDiagramBuilder
    {
        public CommunicationDiagramBuilder(ITextMeasurer textMeasurer) : base(textMeasurer) { }

        public override DiagramModel Build(GraphMetadata metadata, List<NodeData> nodes, List<EdgeData> edges)
        {
            var diagram = new DiagramModel
            {
                Id = metadata.Key,
                Name = metadata.Name,
                DiagramType = DiagramTypes.COMMUNICATION_DIAGRAM
            };
            var nesting = BuildNestingHierarchy(nodes, edges);
            foreach (var node in nodes)
            {
                if (node == nesting.RootDiagram) continue;
                int depth = nesting.GetDepth(node.Key);
                float currentElevation = (depth + 1) * Y_ELEVATION;

                NodeModel nodeModel;
                if (node.Type == DiagramNodeTypes.ACTOR)
                    nodeModel = BuildActorNode(node, currentElevation);
                else
                    nodeModel = BuildLifelineNode(node, currentElevation);
                diagram.Nodes.Add(nodeModel);
            }
            diagram.Edges = BuildEdgeModels(edges);
            return diagram;
        }
        private NodeModel BuildActorNode(NodeData node, float currentElevation)
        {
            float textWidth = MeasureText(node.GetNodeName(), HEADER_FONT_SIZE, true);
            float width = 1f;
            Vec3 pos = new Vec3(node.GetNodePosition().x, node.GetNodePosition().y + currentElevation, node.GetNodePosition().z);
            var nodeModel = BuildNodeModel(node, pos, width, width, RGBA.White, RGBA.Black, currentElevation, true);
            nodeModel.Labels.Add(CreateLabel(
                node.GetNodeName(),
                new Vec3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, 2f),
                textWidth + 3f,
                HEADER_FONT_SIZE,
                nodeModel.TextColor,
                TextAlignment.Center,
                FontStyle.Bold
            ));
            return nodeModel;
        }
        private NodeModel BuildLifelineNode(NodeData node, float currentElevation)
        {
            float textWidth = MeasureText(node.GetNodeName(), HEADER_FONT_SIZE, true);
            float width = Math.Max(textWidth + 3f, 6f);
            float height = 3f;

            Vec3 pos = new Vec3(node.GetNodePosition().x, node.GetNodePosition().y + currentElevation, node.GetNodePosition().z);
            var nodeModel = BuildNodeModel(node, pos, width, height, RGBA.Aquamarine, RGBA.Black, currentElevation, false);
            nodeModel.Labels.Add(CreateLabel(
                node.GetNodeName(),
                new Vec3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, 0),
                textWidth,
                HEADER_FONT_SIZE,
                nodeModel.TextColor,
                TextAlignment.Center,
                FontStyle.Bold
            ));
            return nodeModel;
        }
    }
}