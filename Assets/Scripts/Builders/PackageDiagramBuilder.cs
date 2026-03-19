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

            Vec3 position = new Vec3((minX + maxX) / 2f, node.GetNodePosition().Y + currentElevation - (Y_ELEVATION / 2f), centerZ + (currentTabHeight / 2f));
            var nodeModel = BuildNodeModel(node, position, nodeWidth, totalHeight, GetNodeColorByDepth(depth), RGBA.Black, 0, false);

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

            Vec3 pos = new Vec3(node.GetNodePosition().X, node.GetNodePosition().Y + currentElevation, node.GetNodePosition().Z + (currentTabHeight / 2f));
            var nodeModel = BuildNodeModel(node, pos, nodeWidth, totalHeight, GetNodeColorByDepth(depth), RGBA.Black, currentElevation, false);


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

            Vec3 pos = new Vec3(node.GetNodePosition().X, node.GetNodePosition().Y + currentElevation, node.GetNodePosition().Z);
            var nodeModel = BuildNodeModel(node, pos, nodeWidth, nodeHeight, GetNodeColorByDepth(depth), RGBA.Black, currentElevation, false);

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
    }
}