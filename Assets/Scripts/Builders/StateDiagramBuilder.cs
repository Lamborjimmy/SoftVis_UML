using System;
using System.Collections.Generic;
using Assets.Scripts.Data;
using Assets.Scripts.Models;
using UnityEngine.ProBuilder.Shapes;

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

            foreach (var node in nodes)
            {
                if (node.Type == DiagramNodeTypes.DIAGRAM) continue;

                NodeModel nodeModel;
                if (node.Type == DiagramNodeTypes.PSEUDOSTATE)
                    nodeModel = BuildPseudostateNode(node);
                else
                    nodeModel = BuildStateNode(node);

                diagram.Nodes.Add(nodeModel);
            }

            diagram.Edges = BuildEdgeModels(edges);

            return diagram;
        }
        private NodeModel BuildPseudostateNode(NodeData node)
        {
            float width = 1f;
            var pos = new Vec3(node.GetNodePosition().x, node.GetNodePosition().y + Y_ELEVATION, node.GetNodePosition().z);
            NodeData tempNode = node;
            tempNode.Type = node.GetNodeName() == "initial" ? DiagramNodeTypes.INITIAL : DiagramNodeTypes.FINAL;

            var nodeModel = BuildNodeModel(tempNode, pos, width, width, RGBA.Black, RGBA.Black, Y_ELEVATION, true);
            return nodeModel;
        }
        private NodeModel BuildStateNode(NodeData node)
        {
            float textWidth = MeasureText(node.GetNodeName(), HEADER_FONT_SIZE, true);
            float nodeWidth = Math.Max(textWidth, 4f);
            float nodeHeight = 2.5f;
            var pos = new Vec3(node.GetNodePosition().x, node.GetNodePosition().y + Y_ELEVATION, node.GetNodePosition().z);

            var nodeModel = BuildNodeModel(node, pos, nodeWidth, nodeHeight, RGBA.SoftYellow, RGBA.Black, Y_ELEVATION, false);

            nodeModel.Labels.Add(CreateLabel(
                node.GetNodeName(),
                new Vec3(0, Y_ELEVATION * 2 + Y_ELEVATION_TEXT_OFFSET, 0),
                nodeWidth,
                HEADER_FONT_SIZE,
                nodeModel.TextColor,
                TextAlignment.Center,
                FontStyle.Normal
            ));

            return nodeModel;
        }
    }
}