using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Data;
using Assets.Scripts.Interfaces;
using Assets.Scripts.Models;

namespace Assets.Scripts.Builders
{
    public abstract class BaseDiagramBuilder : IDiagramBuilder
    {
        protected const float LABEL_FONT_SIZE = 4f;
        protected const float HEADER_FONT_SIZE = 5f;
        protected const float LINE_HEIGHT = 0.8f;
        protected const float Y_ELEVATION = 0.1f;
        protected const float Y_ELEVATION_TEXT_OFFSET = 0.05f;
        protected const float EDGE_WIDTH = 0.04f;
        protected const float PADDING_X = 1f;
        protected const float PADDING_Z = 1f;
        protected ITextMeasurer TextMeasurer { get; private set; }
        public BaseDiagramBuilder(ITextMeasurer textMeasurer)
        {
            TextMeasurer = textMeasurer;
        }
        public abstract DiagramModel Build(GraphMetadata metadata, List<NodeData> nodes, List<EdgeData> edges);
        #region Nesting Context
        public class NestingContext
        {
            public Dictionary<string, List<NodeData>> ParentToChildren { get; }
            public Dictionary<string, string> ChildToParent { get; }
            public HashSet<string> NestedChildKeys { get; }
            public NodeData RootDiagram { get; }
            public NestingContext(Dictionary<string, List<NodeData>> parentToChildren, Dictionary<string, string> childToParent, HashSet<string> nestedChildKeys, NodeData rootDiagram)
            {
                ParentToChildren = parentToChildren;
                ChildToParent = childToParent;
                NestedChildKeys = nestedChildKeys;
                RootDiagram = rootDiagram;
            }
            public int GetDepth(string nodeKey)
            {
                int depth = 0;
                string current = nodeKey;
                while (ChildToParent.ContainsKey(current))
                {
                    current = ChildToParent[current];
                    if (RootDiagram != null && current != RootDiagram.Key)
                        depth++;
                }
                return depth;
            }
            public bool IsContainer(string nodeKey)
            {
                return ParentToChildren.ContainsKey(nodeKey) && ParentToChildren[nodeKey].Count > 0;
            }
        }
        protected NestingContext BuildNestingHierarchy(List<NodeData> nodes, List<EdgeData> edges)
        {
            var parentToChildren = new Dictionary<string, List<NodeData>>();
            var childToParent = new Dictionary<string, string>();
            var nestedChildKeys = new HashSet<string>();
            var nodeLookup = nodes.ToDictionary(n => n.Key);
            foreach (var edge in edges.Where(e => e.Type == DiagramEdgeTypes.NESTED))
            {
                string parentKey = edge.From;
                string childKey = edge.To;
                nestedChildKeys.Add(childKey);
                childToParent[childKey] = parentKey;
                if (!parentToChildren.ContainsKey(parentKey))
                    parentToChildren[parentKey] = new List<NodeData>();
                if (nodeLookup.TryGetValue(childKey, out var childNode))
                    parentToChildren[parentKey].Add(childNode);
            }
            var rootDiagram = nodes.FirstOrDefault(n => n.Type == DiagramNodeTypes.DIAGRAM && !nestedChildKeys.Contains(n.Key));
            return new NestingContext(parentToChildren, childToParent, nestedChildKeys, rootDiagram);
        }
        #endregion
        #region Node Building
        protected NodeModel BuildNodeModel(NodeData node, Vec3 basePosition, float width, float height, RGBA color, RGBA textColor, float elevation, bool useUniformScale)
        {
            var model = new NodeModel
            {
                Id = node.Key,
                Label = node.GetNodeName() ?? node.Key,
                NodeType = node.Type,
                Position = new Vec3(basePosition.X, basePosition.Y + elevation, basePosition.Z),
                Scale = new Vec3(width, 0.2f, height),
                BackgroundColor = color,
                TextColor = textColor,
                Elevation = elevation,
                UseUniformScale = useUniformScale
            };
            model.Bounds = new BoundsData(model.Position, model.Scale);
            return model;
        }
        #endregion
        #region Edge Building
        protected List<EdgeModel> BuildEdgeModels(List<EdgeData> edges)
        {
            var result = new List<EdgeModel>();
            var notNestedEdges = edges.Where(e => e.Type != DiagramEdgeTypes.NESTED).ToList();
            foreach (var edge in notNestedEdges)
            {
                var edgeModel = new EdgeModel
                {
                    Id = edge.Key,
                    FromId = edge.From,
                    ToId = edge.To,
                    EdgeType = edge.Type,
                    IsSelfLoop = edge.From == edge.To,
                    IsDashed = IsDashedEdgeType(edge.Type),
                    LineWidth = EDGE_WIDTH,
                    LineColor = RGBA.White,
                    StartDecorator = GetStartDecorator(edge.Type),
                    EndDecorator = GetEndDecorator(edge.Type)
                };
                result.Add(edgeModel);
            }
            return result;
        }
        private bool IsDashedEdgeType(string edgeType)
        {
            return edgeType == DiagramEdgeTypes.INCLUDES_UML
                || edgeType == DiagramEdgeTypes.EXTENDS_UML
                || edgeType == DiagramEdgeTypes.DEPENDENCY;
        }
        private DecoratorType GetStartDecorator(string edgeType)
        {
            return edgeType switch
            {
                DiagramEdgeTypes.AGGREGATES => DecoratorType.DiamondHollow,
                DiagramEdgeTypes.COMPOSES => DecoratorType.DiamondFilled,
                _ => DecoratorType.None
            };
        }
        private DecoratorType GetEndDecorator(string edgeType)
        {
            return edgeType switch
            {
                DiagramEdgeTypes.GENERALIZES => DecoratorType.Triangle,
                DiagramEdgeTypes.INCLUDES_UML => DecoratorType.Arrow,
                DiagramEdgeTypes.EXTENDS_UML => DecoratorType.Arrow,
                DiagramEdgeTypes.DEPENDENCY => DecoratorType.Arrow,
                DiagramEdgeTypes.TRANSITIONS_TO => DecoratorType.Arrow,
                DiagramEdgeTypes.FLOWS_TO => DecoratorType.Arrow,
                DiagramEdgeTypes.OBJECT_FLOW => DecoratorType.Arrow,
                _ => DecoratorType.None
            };
        }
        #endregion
        #region Bounds Calculation
        protected void GetRecursiveBounds(string parentKey, Dictionary<string, List<NodeData>> parentToChildren, out float minX, out float maxX, out float minZ, out float maxZ, Dictionary<string, Vec3> positions = null)
        {
            minX = minZ = float.MaxValue;
            maxX = maxZ = float.MinValue;

            if (!parentToChildren.ContainsKey(parentKey)) return;

            foreach (var child in parentToChildren[parentKey])
            {
                Vec3 pos;
                if (positions != null && positions.ContainsKey(child.Key))
                    pos = positions[child.Key];
                else
                    pos = child.GetNodePosition();

                float childMinX = pos.X;
                float childMaxX = pos.X;
                float childMinZ = pos.Z;
                float childMaxZ = pos.Z;

                if (parentToChildren.ContainsKey(child.Key) && parentToChildren[child.Key].Count > 0)
                {
                    GetRecursiveBounds(child.Key, parentToChildren, out childMinX, out childMaxX, out childMinZ, out childMaxZ, positions);

                    childMinX -= PADDING_X;
                    childMaxX += PADDING_X;
                    childMinZ -= PADDING_Z;
                    childMaxZ += PADDING_Z;
                }

                minX = Math.Min(minX, childMinX);
                maxX = Math.Max(maxX, childMaxX);
                minZ = Math.Min(minZ, childMinZ);
                maxZ = Math.Max(maxZ, childMaxZ);
            }
        }
        #endregion
        #region Helpers
        protected float MeasureText(string text, float fontSize, bool isBold = false)
        {
            return TextMeasurer.MeasureWidth(text, fontSize, isBold);
        }
        protected TextLabelModel CreateLabel(string text, Vec3 localPos, float width, float fontSize, RGBA color, TextAlignment alignment = TextAlignment.Center, FontStyle style = FontStyle.Normal)
        {
            return new TextLabelModel
            {
                Text = text,
                Position = localPos,
                Width = width,
                FontSize = fontSize,
                Alignment = alignment,
                Style = style,
                Color = color
            };
        }
        #endregion
    }
}