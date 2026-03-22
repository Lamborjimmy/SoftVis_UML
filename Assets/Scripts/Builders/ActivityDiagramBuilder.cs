using System;
using System.Collections.Generic;
using System.Linq;
using Softviz.UML.Data;
using Softviz.UML.Models;
using Softviz.UML.Interfaces;

namespace Softviz.UML.Builders
{
    public class ActivityDiagramBuilder : BaseDiagramBuilder
    {
        public ActivityDiagramBuilder(ITextMeasurer textMeasurer) : base(textMeasurer) { }

        public override DiagramModel Build(GraphMetadata metadata, List<NodeData> nodes, List<EdgeData> edges)
        {
            var diagram = new DiagramModel
            {
                Id = metadata.Key,
                Name = metadata.Name,
                DiagramType = DiagramTypes.ACTIVITY_DIAGRAM
            };

            var nesting = BuildNestingHierarchy(nodes, edges);

            var sortedNodes = nodes.Where(n => n != nesting.RootDiagram && n.Type != DiagramNodeTypes.DIAGRAM).OrderBy(n => nesting.GetDepth(n.Key)).ToList();

            var overriddenPositions = new Dictionary<string, Vec3>();
            float zSpacingMultiplier = 0.7f;
            foreach (var node in nodes)
            {
                var pos = node.GetNodePosition();
                overriddenPositions[node.Key] = new Vec3(pos.X, pos.Y, pos.Z * zSpacingMultiplier);
            }

            ApplyRankBasedSpacing(sortedNodes, edges, overriddenPositions);

            var swimlaneBoundsDict = CalculateSwimlaneBounds(sortedNodes, nesting.ParentToChildren, overriddenPositions);

            foreach (var node in sortedNodes)
            {
                int depth = nesting.GetDepth(node.Key);
                float dampeningFactor = 0.6f;
                float currentElevation = Y_ELEVATION * (1f + (depth * dampeningFactor));

                bool isContainer = nesting.ParentToChildren.ContainsKey(node.Key) && nesting.ParentToChildren[node.Key].Count > 0;

                NodeModel nodeModel;

                if (node.Type == DiagramNodeTypes.SWIMLANE)
                    nodeModel = BuildSwimlaneNode(node, swimlaneBoundsDict, currentElevation, depth);
                else if (isContainer)
                    nodeModel = BuildContainerNode(node, nesting.ParentToChildren, overriddenPositions, currentElevation, depth);
                else if (node.Type == DiagramNodeTypes.INITIAL || node.Type == DiagramNodeTypes.FINAL)
                    nodeModel = BuildInitialFinalNode(node, overriddenPositions, currentElevation);
                else if (node.Type == DiagramNodeTypes.DECISION)
                    nodeModel = BuildDecisionNode(node, overriddenPositions, currentElevation);
                else if (node.Type == DiagramNodeTypes.FORK || node.Type == DiagramNodeTypes.JOIN)
                    nodeModel = BuildForkJoinNode(node, overriddenPositions, currentElevation);
                else
                    nodeModel = BuildActionNode(node, overriddenPositions, currentElevation);

                diagram.Nodes.Add(nodeModel);
            }

            diagram.Edges = BuildEdgeModels(edges);

            return diagram;
        }

        private NodeModel BuildSwimlaneNode(NodeData node, Dictionary<string, (float minX, float maxX, float minZ, float maxZ)> swimlaneBoundsDict, float currentElevation, int depth)
        {
            var sb = swimlaneBoundsDict[node.Key];
            float width = sb.maxX - sb.minX;
            float height = sb.maxZ - sb.minZ;
            float centerZ = (sb.minZ + sb.maxZ) / 2f;

            Vec3 position = new Vec3((sb.minX + sb.maxX) / 2f, currentElevation - (Y_ELEVATION / 2f), centerZ);

            var nodeModel = BuildNodeModel(node, position, width, height, GetNodeColorByDepth(depth), RGBA.Black, 0, false);

            float textZ = (height / 2f) - 1.5f;
            nodeModel.Labels.Add(CreateLabel(
                "<<swimlane>>",
                new Vec3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, textZ),
                width - 2f,
                LABEL_FONT_SIZE,
                nodeModel.TextColor,
                TextAlignment.TopLeft,
                FontStyle.Normal
            ));

            nodeModel.Labels.Add(CreateLabel(
                node.GetNodeName(),
                new Vec3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, textZ - LINE_HEIGHT),
                width - 2f,
                HEADER_FONT_SIZE,
                nodeModel.TextColor,
                TextAlignment.TopLeft,
                FontStyle.Bold
            ));

            return nodeModel;
        }

        private NodeModel BuildContainerNode(NodeData node, Dictionary<string, List<NodeData>> parentToChildren, Dictionary<string, Vec3> overriddenPositions, float currentElevation, int depth)
        {
            GetRecursiveBounds(node.Key, parentToChildren, out float minX, out float maxX, out float minZ, out float maxZ, overriddenPositions);
            if (minX == float.MaxValue) { minX = -2f; maxX = 2f; minZ = -2f; maxZ = 2f; }

            float paddingX = 2.0f;
            float paddingZ = 2.0f;

            float width = (maxX - minX) + (paddingX * 2);
            float height = (maxZ - minZ) + (paddingZ * 2);
            float centerZ = (minZ + maxZ) / 2f;

            Vec3 basePos = overriddenPositions[node.Key];
            Vec3 position = new Vec3((minX + maxX) / 2f, basePos.Y + currentElevation - (Y_ELEVATION / 2f), centerZ);
            var nodeModel = BuildNodeModel(node, position, width, height, GetNodeColorByDepth(depth), RGBA.Black, 0, false);

            float textZ = (height / 2f) - 1.5f;
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

        private NodeModel BuildInitialFinalNode(NodeData node, Dictionary<string, Vec3> overriddenPositions, float currentElevation)
        {
            float width = 1f;
            Vec3 pos = overriddenPositions[node.Key];
            var nodeModel = BuildNodeModel(node, pos, width, width, RGBA.Black, RGBA.Black, currentElevation, true);
            return nodeModel;
        }

        private NodeModel BuildDecisionNode(NodeData node, Dictionary<string, Vec3> overriddenPositions, float currentElevation)
        {
            float width = 1f;
            Vec3 pos = overriddenPositions[node.Key];
            var nodeModel = BuildNodeModel(node, pos, width, width, RGBA.Black, RGBA.Black, currentElevation, true);
            return nodeModel;
        }

        private NodeModel BuildForkJoinNode(NodeData node, Dictionary<string, Vec3> overriddenPositions, float currentElevation)
        {
            float width = 0.5f;
            float height = 3f;
            Vec3 pos = overriddenPositions[node.Key];
            var nodeModel = BuildNodeModel(node, pos, width, height, RGBA.Black, RGBA.Black, currentElevation, false);
            return nodeModel;
        }

        private NodeModel BuildActionNode(NodeData node, Dictionary<string, Vec3> overriddenPositions, float currentElevation)
        {
            float textWidth = MeasureText(node.GetNodeName() ?? "", HEADER_FONT_SIZE, true);
            float width = Math.Max(textWidth + 2f, 3f);
            float height = 2f;
            Vec3 pos = overriddenPositions[node.Key];
            var nodeModel = BuildNodeModel(node, pos, width, height, RGBA.Lavender, RGBA.Black, currentElevation, false);

            nodeModel.Labels.Add(CreateLabel(
                node.GetNodeName(),
                new Vec3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, 0),
                width,
                HEADER_FONT_SIZE,
                nodeModel.TextColor,
                TextAlignment.Center,
                FontStyle.Bold
            ));
            return nodeModel;
        }

        private void ApplyRankBasedSpacing(List<NodeData> sortedNodes, List<EdgeData> edges, Dictionary<string, Vec3> overriddenPositions)
        {
            var flowEdges = edges.Where(e => e.Type == DiagramEdgeTypes.FLOWS_TO || e.Type == DiagramEdgeTypes.OBJECT_FLOW).ToList();
            var adj = new Dictionary<string, List<string>>();

            foreach (var node in sortedNodes) adj[node.Key] = new List<string>();

            foreach (var edge in flowEdges)
            {
                string from = edge.From;
                string to = edge.To;
                if (adj.ContainsKey(from)) adj[from].Add(to);
            }

            var nodeRanks = new Dictionary<string, int>();
            var queue = new Queue<string>();

            foreach (var node in sortedNodes)
            {
                if (node.Type == DiagramNodeTypes.INITIAL)
                {
                    queue.Enqueue(node.Key);
                    nodeRanks[node.Key] = 0;
                }
            }

            while (queue.Count > 0)
            {
                var curr = queue.Dequeue();
                int currentRank = nodeRanks[curr];

                foreach (var nxt in adj[curr])
                {
                    if (!nodeRanks.ContainsKey(nxt))
                    {
                        nodeRanks[nxt] = currentRank + 1;
                        queue.Enqueue(nxt);
                    }
                    else if (currentRank + 1 > nodeRanks[nxt] && currentRank < 50)
                    {
                        nodeRanks[nxt] = currentRank + 1;
                        queue.Enqueue(nxt);
                    }
                }
            }

            int maxRank = nodeRanks.Values.Count > 0 ? nodeRanks.Values.Max() : 0;
            float xSpacing = 9.0f;

            foreach (var node in sortedNodes)
            {
                if (node.Type != DiagramNodeTypes.SWIMLANE)
                {
                    int rank = nodeRanks.ContainsKey(node.Key) ? nodeRanks[node.Key] : 0;
                    if (node.Type == DiagramNodeTypes.FINAL) rank = maxRank + 1;

                    Vec3 pos = overriddenPositions[node.Key];
                    pos.X = rank * xSpacing;
                    overriddenPositions[node.Key] = pos;
                }
            }
        }

        private Dictionary<string, (float minX, float maxX, float minZ, float maxZ)> CalculateSwimlaneBounds(List<NodeData> sortedNodes, Dictionary<string, List<NodeData>> parentToChildren, Dictionary<string, Vec3> overriddenPositions)
        {
            var swimlanes = sortedNodes.Where(n => n.Type == DiagramNodeTypes.SWIMLANE).ToList();
            var swimlaneBoundsDict = new Dictionary<string, (float minX, float maxX, float minZ, float maxZ)>();

            if (swimlanes.Count == 0) return swimlaneBoundsDict;

            float globalMinX = float.MaxValue;
            float globalMaxX = float.MinValue;

            foreach (var sl in swimlanes)
            {
                GetRecursiveBounds(sl.Key, parentToChildren, out float minX, out float maxX, out float minZ, out float maxZ, overriddenPositions);
                if (minX == float.MaxValue) { minX = -2f; maxX = 2f; minZ = -2f; maxZ = 2f; }
                swimlaneBoundsDict[sl.Key] = (minX, maxX, minZ, maxZ);
            }

            foreach (var node in sortedNodes)
            {
                Vec3 pos = overriddenPositions[node.Key];
                globalMinX = Math.Min(globalMinX, pos.X);
                globalMaxX = Math.Max(globalMaxX, pos.X);
            }

            globalMinX -= 2.0f;
            globalMaxX += 2.0f;

            swimlanes = swimlanes.OrderBy(sl => (swimlaneBoundsDict[sl.Key].minZ + swimlaneBoundsDict[sl.Key].maxZ) / 2f).ToList();

            for (int i = 0; i < swimlanes.Count; i++)
            {
                string slKey = swimlanes[i].Key;
                var currentBounds = swimlaneBoundsDict[slKey];

                float newMinZ = currentBounds.minZ;
                float newMaxZ = currentBounds.maxZ;

                if (i > 0)
                {
                    var prevBounds = swimlaneBoundsDict[swimlanes[i - 1].Key];
                    newMinZ = (currentBounds.minZ + prevBounds.maxZ) / 2f;
                }
                else newMinZ -= 2.0f;

                if (i < swimlanes.Count - 1)
                {
                    var nextBounds = swimlaneBoundsDict[swimlanes[i + 1].Key];
                    newMaxZ = (currentBounds.maxZ + nextBounds.minZ) / 2f;
                }
                else newMaxZ += 2.0f;

                swimlaneBoundsDict[slKey] = (globalMinX, globalMaxX, newMinZ, newMaxZ);
            }

            return swimlaneBoundsDict;
        }
    }
}