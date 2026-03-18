using System;
using System.Collections.Generic;
using Assets.Scripts.Data;
using Assets.Scripts.Models;

namespace Assets.Scripts.Builders
{
    public class PackageDiagramBuilder : BaseDiagramBuilder
    {
        public PackageDiagramBuilder(ITextMeasurer textMeasurer) : base(textMeasurer) { }

        public override DiagramModel Build(GraphMetadata metadata, List<NodeData> nodes, List<EdgeData> edges)
        {
            var diagram = new DiagramModel
            {
                Id = metadata.Key,
                Name = metadata.Name,
                DiagramType = DiagramTypes.PACKAGE_DIAGRAM
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
                else if (node.Type == DiagramNodeTypes.PACKAGE)
                    nodeModel = BuildPackageNode(node, currentElevation, depth);
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

            float paddingX = 4.0f;
            float paddingZ = 4.5f;
            float nodeWidth = (maxX - minX) + paddingX * 2;
            float nodeHeight = (maxZ - minZ) + paddingZ * 2;

            float currentTabHeight = 1.5f;
            float totalHeight = nodeHeight + currentTabHeight;
            float centerZ = (minZ + maxZ) / 2f;

            var pos = node.GetNodePosition();
            Vec3 position = new Vec3((minX + maxX) / 2f, pos.y + currentElevation - (Y_ELEVATION / 2f), centerZ + (currentTabHeight / 2f));

            var nodeModel = new NodeModel
            {
                Id = node.Key,
                Label = node.GetNodeName() ?? node.Key,
                NodeType = node.Type,
                Position = position,
                Scale = new Vec3(nodeWidth, 0.2f, totalHeight),
                BackgroundColor = GetLayerColor(depth),
                Elevation = currentElevation,
                UseUniformScale = false
            };

            nodeModel.Bounds = new BoundsData(position, nodeModel.Scale);

            float textZ = (nodeHeight / 2f) - (currentTabHeight / 2f) - 1.5f;
            nodeModel.Labels.Add(CreateLabel(
                node.GetNodeName(),
                new Vec3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, textZ),
                nodeWidth,
                HEADER_FONT_SIZE,
                nodeModel.TextColor,
                TextAlignment.Top,
                FontStyle.Bold
            ));

            return nodeModel;
        }
        private NodeModel BuildPackageNode(NodeData node, float currentElevation, int depth)
        {
            float textWidth = MeasureText(node.GetNodeName(), HEADER_FONT_SIZE, true);
            float nodeWidth = Math.Max(textWidth + 3f, 6f);
            float nodeHeight = 3f;
            float currentTabHeight = 1.5f;
            float totalHeight = nodeHeight + currentTabHeight;

            Vec3 pos = new Vec3(node.GetNodePosition().x, node.GetNodePosition().y + currentElevation, node.GetNodePosition().z + (currentTabHeight / 2f));
            var nodeModel = BuildNodeModel(node, pos, nodeWidth, totalHeight, GetLayerColor(depth), RGBA.Black, currentElevation, false);


            float textZ = (nodeHeight / 2f) - (currentTabHeight / 2f) - 1.5f;
            nodeModel.Labels.Add(CreateLabel(
                node.GetNodeName(),
                new Vec3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, textZ),
                nodeWidth,
                HEADER_FONT_SIZE,
                nodeModel.TextColor,
                TextAlignment.Top,
                FontStyle.Bold
            ));

            return nodeModel;
        }

        private NodeModel BuildStandardNode(NodeData node, float currentElevation, int depth)
        {
            float textWidth = MeasureText(node.GetNodeName(), HEADER_FONT_SIZE, true);
            float nodeWidth = Math.Max(textWidth + 3f, 6f);
            float nodeHeight = 3f;

            Vec3 pos = new Vec3(node.GetNodePosition().x, node.GetNodePosition().y + currentElevation, node.GetNodePosition().z);
            var nodeModel = BuildNodeModel(node, pos, nodeWidth, nodeHeight, GetLayerColor(depth), RGBA.Black, currentElevation, false);

            nodeModel.Labels.Add(CreateLabel(
                node.GetNodeName(),
                new Vec3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, 0),
                nodeWidth,
                HEADER_FONT_SIZE,
                nodeModel.TextColor,
                TextAlignment.Center,
                FontStyle.Bold
            ));

            return nodeModel;
        }
        private RGBA GetLayerColor(int depth)
        {
            RGBA[] palette = new RGBA[]
            {
                new RGBA(0.40f, 0.50f, 0.65f),
                new RGBA(0.45f, 0.60f, 0.50f),
                new RGBA(0.60f, 0.45f, 0.55f),
                new RGBA(0.65f, 0.55f, 0.40f),
                new RGBA(0.45f, 0.55f, 0.60f)
            };

            RGBA c = palette[depth % palette.Length];
            return c;
        }
    }
}