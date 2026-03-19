using System;
using System.Collections.Generic;
using Assets.Scripts.Data;
using Assets.Scripts.Models;

namespace Assets.Scripts.Builders
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
                    nodeModel = BuildContainerNode(node, nesting, currentElevation);
                else if (node.Type == DiagramNodeTypes.REQUIRED_INTERFACE || node.Type == DiagramNodeTypes.PROVIDED_INTERFACE)
                    nodeModel = BuildInterfaceNode(node, currentElevation);
                else
                    nodeModel = BuildStandardNode(node, currentElevation);

                diagram.Nodes.Add(nodeModel);
            }

            diagram.Edges = BuildEdgeModels(edges);

            return diagram;
        }
        private NodeModel BuildContainerNode(NodeData node, NestingContext nesting, float currentElevation)
        {
            GetRecursiveBounds(node.Key, nesting.ParentToChildren, out float minX, out float maxX, out float minZ, out float maxZ);

            float width = (maxX - minX) + 12f;
            float height = (maxZ - minZ) + 8f;
            float centerZ = (minZ + maxZ) / 2f;

            Vec3 position = new Vec3((minX + maxX) / 2f, node.GetNodePosition().Y + currentElevation - (Y_ELEVATION / 2f), centerZ);

            var nodeModel = BuildNodeModel(node, position, width, height, GetNodeColor(node.Type), RGBA.Black, 0, false);

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

        private NodeModel BuildStandardNode(NodeData node, float currentElevation)
        {
            float textWidth = MeasureText(node.GetNodeName() ?? "", HEADER_FONT_SIZE, true);
            float width = Math.Max(textWidth + 3f, 6f);
            float height = 4f;

            Vec3 pos = new Vec3(node.GetNodePosition().X, node.GetNodePosition().Y + currentElevation, node.GetNodePosition().Z);
            var nodeModel = BuildNodeModel(node, pos, width, height, GetNodeColor(node.Type), RGBA.Black, currentElevation, false);

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
        private RGBA GetNodeColor(string nodeType)
        {
            if (nodeType == DiagramNodeTypes.NODE) return new RGBA(0.35f, 0.35f, 0.45f, 1f);
            if (nodeType == DiagramNodeTypes.COMPONENT) return new RGBA(0.85f, 0.95f, 0.85f, 1f);
            if (nodeType == DiagramNodeTypes.ARTIFACT) return new RGBA(0.95f, 0.95f, 0.85f, 1f);
            return RGBA.White;
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