using System;
using System.Collections.Generic;
using Softviz.UML.Data;
using Softviz.UML.Models;
using Softviz.UML.Interfaces;

namespace Softviz.UML.Builders
{
    public class DeploymentDiagramBuilder : BaseDiagramBuilder
    {
        public DeploymentDiagramBuilder(ITextMeasurer textMeasurer) : base(textMeasurer) { }

        public override DiagramModel Build(GraphMetadata metadata, List<NodeData> nodes, List<EdgeData> edges)
        {
            var diagram = new DiagramModel
            {
                Id = metadata.Key,
                Name = metadata.Name,
                DiagramType = DiagramTypes.DEPLOYMENT_DIAGRAM
            };
            var nesting = BuildNestingHierarchy(nodes, edges);

            foreach (var node in nodes)
            {
                if (node == nesting.RootDiagram) continue;

                int depth = nesting.GetDepth(node.Key);
                float currentElevation = (depth + 1) * Y_ELEVATION;

                NodeModel nodeModel;
                if (nesting.IsContainer(node.Key))
                    nodeModel = BuildContainerNode(node, nesting, currentElevation, depth);
                else if (node.Type == DiagramNodeTypes.REQUIRED_INTERFACE || node.Type == DiagramNodeTypes.PROVIDED_INTERFACE)
                    nodeModel = BuildInterfaceNode(node, currentElevation);
                else
                    nodeModel = BuildStandardNode(node, currentElevation, depth);

                diagram.Nodes.Add(nodeModel);
            }

            diagram.Edges = BuildEdgeModels(edges);

            return diagram;
        }
        private NodeModel BuildContainerNode(NodeData node, NestingContext nesting, float currentElevation, int depth)
        {
            GetRecursiveBounds(node.Key, nesting.ParentToChildren, out float minX, out float maxX, out float minZ, out float maxZ);

            float width = (maxX - minX) + 12f;
            float height = (maxZ - minZ) + 8f;
            float centerZ = (minZ + maxZ) / 2f;

            Vec3 position = new Vec3((minX + maxX) / 2f, node.GetNodePosition().Y + currentElevation - (Y_ELEVATION / 2f), centerZ);

            var nodeModel = BuildNodeModel(node, position, width, height, GetNodeColorByDepth(depth), RGBA.Black, 0, false);

            float textZ = (height / 2f) - 1.5f;
            nodeModel.Labels.Add(CreateLabel(
                GetStereotype(node.Type),
                new Vec3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, textZ),
                width,
                LABEL_FONT_SIZE,
                nodeModel.TextColor,
                TextAlignment.Top,
                FontStyle.Normal
            ));

            nodeModel.Labels.Add(CreateLabel(
                node.GetNodeName(),
                new Vec3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, textZ - LINE_HEIGHT),
                width,
                HEADER_FONT_SIZE,
                nodeModel.TextColor,
                TextAlignment.Top,
                FontStyle.Bold
            ));

            return nodeModel;
        }
        private NodeModel BuildInterfaceNode(NodeData node, float currentElevation)
        {
            float width = 1f;
            Vec3 pos = new Vec3(node.GetNodePosition().X, node.GetNodePosition().Y + currentElevation, node.GetNodePosition().Z);

            var nodeModel = BuildNodeModel(node, pos, width, width, RGBA.White, RGBA.Black, currentElevation, true);

            nodeModel.Labels.Add(CreateLabel(
                node.GetNodeName(),
                new Vec3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, -1.5f),
                8f,
                HEADER_FONT_SIZE,
                nodeModel.TextColor,
                TextAlignment.Top,
                FontStyle.Bold
            ));

            return nodeModel;
        }

        private NodeModel BuildStandardNode(NodeData node, float currentElevation, int depth)
        {
            float textWidth = MeasureText(node.GetNodeName() ?? "", HEADER_FONT_SIZE, true);
            float width = Math.Max(textWidth + 3f, 6f);
            float height = 4f;

            Vec3 pos = new Vec3(node.GetNodePosition().X, node.GetNodePosition().Y + currentElevation, node.GetNodePosition().Z);
            var nodeModel = BuildNodeModel(node, pos, width, height, RGBA.LightBlue, RGBA.Black, currentElevation, false);

            nodeModel.Labels.Add(CreateLabel(
                GetStereotype(node.Type),
                new Vec3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, 0.5f),
                width,
                LABEL_FONT_SIZE,
                nodeModel.TextColor,
                TextAlignment.Center,
                FontStyle.Normal
            ));

            nodeModel.Labels.Add(CreateLabel(
                node.GetNodeName(),
                new Vec3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, 0.5f - LINE_HEIGHT),
                width,
                HEADER_FONT_SIZE,
                nodeModel.TextColor,
                TextAlignment.Center,
                FontStyle.Bold
            ));

            return nodeModel;
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