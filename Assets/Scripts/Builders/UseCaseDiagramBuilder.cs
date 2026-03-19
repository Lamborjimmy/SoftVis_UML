using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Data;
using Assets.Scripts.Models;

namespace Assets.Scripts.Builders
{
    public class UseCaseDiagramBuilder : BaseDiagramBuilder
    {
        public UseCaseDiagramBuilder(ITextMeasurer textMeasurer) : base(textMeasurer) { }

        public override DiagramModel Build(GraphMetadata metadata, List<NodeData> nodes, List<EdgeData> edges)
        {
            var diagram = new DiagramModel
            {
                Id = metadata.Key,
                Name = metadata.Name,
                DiagramType = DiagramTypes.USECASE_DIAGRAM
            };

            var nesting = BuildNestingHierarchy(nodes, edges);
            var extensionPointsMap = BuildExtensionPointsMap(nodes, edges);

            foreach (var node in nodes)
            {
                if (node == nesting.RootDiagram) continue;

                int depth = nesting.GetDepth(node.Key);
                float currentElevation = (depth + 1) * Y_ELEVATION;

                NodeModel nodeModel;
                if (nesting.IsContainer(node.Key))
                    nodeModel = BuildContainerNode(node, nesting, currentElevation);
                else if (node.Type == DiagramNodeTypes.ACTOR)
                    nodeModel = BuildActorNode(node, currentElevation);
                else if (node.Type == DiagramNodeTypes.USECASE)
                    nodeModel = BuildUseCaseNode(node, extensionPointsMap, currentElevation);
                else
                    continue;

                diagram.Nodes.Add(nodeModel);
            }

            diagram.Edges = BuildEdgeModels(edges);

            return diagram;
        }
        private Dictionary<string, List<string>> BuildExtensionPointsMap(List<NodeData> nodes, List<EdgeData> edges)
        {
            return edges
                .Where(e => e.Type == DiagramEdgeTypes.EXTENDS_UML)
                .GroupBy(e => e.To)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => nodes.FirstOrDefault(n => n.Key == e.From)?.GetNodeName() ?? "Unknown").ToList()
                );
        }
        private NodeModel BuildContainerNode(NodeData node, NestingContext nesting, float currentElevation)
        {
            GetRecursiveBounds(node.Key, nesting.ParentToChildren, out float minX, out float maxX, out float minZ, out float maxZ);

            float paddingX = 6.0f;
            float paddingZ = 4.0f;
            float width = (maxX - minX) + paddingX * 2;
            float height = (maxZ - minZ) + paddingZ * 2;
            float centerZ = (minZ + maxZ) / 2f;

            Vec3 position = new Vec3((minX + maxX) / 2f, node.GetNodePosition().Y + currentElevation - (Y_ELEVATION / 2f), centerZ);

            var nodeModel = new NodeModel
            {
                Id = node.Key,
                Label = node.GetNodeName() ?? node.Key,
                NodeType = node.Type,
                Position = position,
                Scale = new Vec3(width, 0.2f, height),
                BackgroundColor = new RGBA(0.0f, 0.2f, 0.9f, 0.8f),
                Elevation = currentElevation,
                UseUniformScale = false
            };

            nodeModel.Bounds = new BoundsData(position, nodeModel.Scale);

            nodeModel.Labels.Add(CreateLabel(
                node.GetNodeName(),
                new Vec3(0, (Y_ELEVATION / 2f) + 0.1f, (height / 2f) - 1.2f),
                width,
                HEADER_FONT_SIZE,
                nodeModel.TextColor,
                TextAlignment.Center,
                FontStyle.Bold
            ));
            return nodeModel;
        }
        private NodeModel BuildActorNode(NodeData node, float currentElevation)
        {
            float textWidth = MeasureText(node.GetNodeName(), HEADER_FONT_SIZE, true);
            float width = 1f;
            var pos = new Vec3(node.GetNodePosition().X, node.GetNodePosition().Y + currentElevation, node.GetNodePosition().Z);

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

        private NodeModel BuildUseCaseNode(NodeData node, Dictionary<string, List<string>> extensionPointsMap, float currentElevation)
        {
            float textWidth = MeasureText(node.GetNodeName(), HEADER_FONT_SIZE, true);

            string labelText = node.GetNodeName();
            int lineCount = 1;
            if (extensionPointsMap.TryGetValue(node.Key, out List<string> points))
            {
                labelText = $"<b>{node.GetNodeName()}</b>\n<size=80%>extension points</size>";
                foreach (var p in points) labelText += $"\n<size=70%>{p}</size>";
                lineCount = 2 + points.Count;
            }

            float ovalWidth = Math.Max(textWidth + 2.0f, 6f);
            float baseHeight = ovalWidth / 2f;
            float textHeightRequirement = lineCount * LINE_HEIGHT;
            float ovalHeight = Math.Max(baseHeight, textHeightRequirement);

            var pos = new Vec3(node.GetNodePosition().X, node.GetNodePosition().Y + currentElevation, node.GetNodePosition().Z);
            var nodeModel = BuildNodeModel(node, pos, ovalWidth, ovalHeight, RGBA.SoftRed, RGBA.Black, currentElevation, false);

            nodeModel.Labels.Add(CreateLabel(
                labelText,
                new Vec3(0, Y_ELEVATION * 2f + Y_ELEVATION_TEXT_OFFSET, 0),
                ovalWidth,
                LABEL_FONT_SIZE,
                nodeModel.TextColor,
                TextAlignment.Center,
                FontStyle.Normal
            ));

            return nodeModel;
        }
    }
}