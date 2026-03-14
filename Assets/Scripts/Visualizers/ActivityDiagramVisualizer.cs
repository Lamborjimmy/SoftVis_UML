using Assets.Scripts.Data;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;

namespace Assets.Scripts.Visualizers
{
    public class ActivityDiagramVisualizer : BaseGraphVisualizer
    {
        protected override void DrawDiagramContent(GameObject container, List<NodeData> nodes, List<EdgeData> edges)
        {
            var (nodesParent, edgesParent) = CreateParentObjects(container);

            NestingContext nesting = BuildNestingHierarchy(nodes, edges);

            var sortedNodes = nodes.Where(n => n != nesting.RootDiagram && n.Type != DiagramNodeTypes.DIAGRAM)
                                   .OrderBy(n => nesting.GetDepth(n.Key))
                                   .ToList();

            var overriddenPositions = new Dictionary<string, Vector3>();
            float zSpacingMultiplier = 0.7f;
            foreach (var node in nodes)
            {
                Vector3 pos = node.GetNodePosition();
                pos.z *= zSpacingMultiplier;
                overriddenPositions[node.Key] = pos;
            }

            ApplyRankBasedSpacing(sortedNodes, edges, overriddenPositions);

            var swimlaneBoundsDict = CalculateSwimlaneBounds(sortedNodes, nesting.ParentToChildren, overriddenPositions);

            var nodeBounds = new Dictionary<string, Bounds>();
            var nodeObjects = BuildNodes(nodesParent, sortedNodes, nesting, overriddenPositions, swimlaneBoundsDict, nodeBounds);

            FilterAndRenderEdges(edges, nodeObjects, edgesParent.transform);
        }

        private Dictionary<string, GameObject> BuildNodes(
            GameObject nodesParent,
            List<NodeData> sortedNodes,
            NestingContext nesting,
            Dictionary<string, Vector3> overriddenPositions,
            Dictionary<string, (float minX, float maxX, float minZ, float maxZ)> swimlaneBoundsDict,
            Dictionary<string, Bounds> nodeBounds)
        {
            var nodeObjects = new Dictionary<string, GameObject>();

            var swimlanesList = sortedNodes.Where(n => n.Type == DiagramNodeTypes.SWIMLANE).ToList();

            foreach (var node in sortedNodes)
            {
                int depth = nesting.GetDepth(node.Key);
                float dampeningFactor = 0.6f;
                float currentElevation = Y_ELEVATION * (1f + (depth * dampeningFactor));

                string nodeLabel = "Node_" + (node.GetNodeName() ?? node.Key);
                GameObject nodeContainer = CreateEmptyGameObject(nodesParent.transform, nodeLabel, Vector3.zero);

                bool isInitial = node.Type == DiagramNodeTypes.INITIAL;
                bool isFinal = node.Type == DiagramNodeTypes.FINAL;
                bool isDecision = node.Type == DiagramNodeTypes.DECISION;
                bool isForkJoin = node.Type == DiagramNodeTypes.FORK || node.Type == DiagramNodeTypes.JOIN;
                bool isSwimlane = node.Type == DiagramNodeTypes.SWIMLANE;
                bool isContainer = nesting.ParentToChildren.ContainsKey(node.Key) && nesting.ParentToChildren[node.Key].Count > 0;

                Bounds bounds;

                if (node.Type == DiagramNodeTypes.SWIMLANE)
                    bounds = BuildSwimlaneNode(nodeContainer, node, swimlaneBoundsDict, currentElevation, depth, swimlanesList);
                else if (isContainer)
                    bounds = BuildContainerNode(nodeContainer, node, nesting.ParentToChildren, overriddenPositions, currentElevation, depth);
                else if (node.Type == DiagramNodeTypes.INITIAL || node.Type == DiagramNodeTypes.FINAL)
                    bounds = BuildInitialFinalNode(nodeContainer, node, overriddenPositions, currentElevation);
                else if (node.Type == DiagramNodeTypes.DECISION)
                    bounds = BuildDecisionNode(nodeContainer, node, overriddenPositions, currentElevation);
                else if (node.Type == DiagramNodeTypes.FORK || node.Type == DiagramNodeTypes.JOIN)
                    bounds = BuildForkJoinNode(nodeContainer, node, overriddenPositions, currentElevation);
                else
                    bounds = BuildActionNode(nodeContainer, node, overriddenPositions, currentElevation, depth);

                nodeBounds[node.Key] = bounds;
                nodeObjects[node.Key] = nodeContainer;
            }

            return nodeObjects;
        }
        private Bounds BuildSwimlaneNode(GameObject nodeContainer, NodeData node, Dictionary<string, (float minX, float maxX, float minZ, float maxZ)> swimlaneBoundsDict, float currentElevation, int depth, List<NodeData> swimlanesList)
        {
            var sb = swimlaneBoundsDict[node.Key];
            float width = sb.maxX - sb.minX;
            float height = sb.maxZ - sb.minZ;
            float centerZ = (sb.minZ + sb.maxZ) / 2f;

            Vector3 position = new Vector3((sb.minX + sb.maxX) / 2f, currentElevation - (Y_ELEVATION / 2f), centerZ);
            nodeContainer.transform.localPosition = position;

            GameObject backgroundGroup = CreateEmptyGameObject(nodeContainer.transform, "Background", Vector3.zero);
            GameObject visualsObj = CreateNodeGameObject(node.Type, backgroundGroup.transform, width, height);

            ApplyColorToHierarchy(visualsObj, GetLayerColor(depth, swimlanesList.FindIndex(s => s.Key == node.Key)));

            float textZ = (height / 2f) - 1.5f;
            CreateTextLabel(backgroundGroup.transform, "<<swimlane>>", new Vector3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, textZ), width - 2f, LABEL_FONT_SIZE, TextAlignmentOptions.TopLeft);
            CreateTextLabel(backgroundGroup.transform, node.GetNodeName(), new Vector3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, textZ - LINE_HEIGHT), width - 2f, HEADER_FONT_SIZE, TextAlignmentOptions.TopLeft, FontStyles.Bold);

            return new Bounds(position, new Vector3(width, 0f, height));
        }

        private Bounds BuildContainerNode(GameObject nodeContainer, NodeData node, Dictionary<string, List<NodeData>> parentToChildren, Dictionary<string, Vector3> overriddenPositions, float currentElevation, int depth)
        {
            GetRecursiveBounds(node.Key, parentToChildren, out float minX, out float maxX, out float minZ, out float maxZ, overriddenPositions);
            if (minX == float.MaxValue) { minX = -2f; maxX = 2f; minZ = -2f; maxZ = 2f; }

            float paddingX = 2.0f;
            float paddingZ = 2.0f;

            float width = (maxX - minX) + (paddingX * 2);
            float height = (maxZ - minZ) + (paddingZ * 2);
            float centerZ = (minZ + maxZ) / 2f;

            Vector3 basePos = overriddenPositions[node.Key];
            Vector3 position = new Vector3((minX + maxX) / 2f, basePos.y + currentElevation - (Y_ELEVATION / 2f), centerZ);
            nodeContainer.transform.localPosition = position;

            GameObject backgroundGroup = CreateEmptyGameObject(nodeContainer.transform, "Background", Vector3.zero);
            GameObject visualsObj = CreateNodeGameObject(node.Type, backgroundGroup.transform, width, height);

            ApplyColorToHierarchy(visualsObj, GetLayerColor(depth));

            float textZ = (height / 2f) - 1.5f;
            CreateTextLabel(backgroundGroup.transform, node.GetNodeName(), new Vector3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, textZ - LINE_HEIGHT), width, HEADER_FONT_SIZE, TextAlignmentOptions.Top, FontStyles.Bold);

            return new Bounds(position, new Vector3(width, 0f, height));
        }

        private Bounds BuildInitialFinalNode(GameObject nodeContainer, NodeData node, Dictionary<string, Vector3> overriddenPositions, float currentElevation)
        {
            float width = 1.0f;
            float height = 1.0f;

            Vector3 basePos = overriddenPositions[node.Key];
            Vector3 position = new Vector3(basePos.x, basePos.y + currentElevation, basePos.z);
            nodeContainer.transform.localPosition = position;

            GameObject backgroundGroup = CreateEmptyGameObject(nodeContainer.transform, "Background", Vector3.zero);
            GameObject visualsObj = CreateNodeGameObject(node.Type, backgroundGroup.transform, width, height, true);

            ApplyColorToHierarchy(visualsObj, Color.black);

            return new Bounds(position, new Vector3(width, 0f, height));
        }

        private Bounds BuildDecisionNode(GameObject nodeContainer, NodeData node, Dictionary<string, Vector3> overriddenPositions, float currentElevation)
        {
            float width = 1.0f;
            float height = 1.0f;

            Vector3 basePos = overriddenPositions[node.Key];
            Vector3 position = new Vector3(basePos.x, basePos.y + currentElevation, basePos.z);
            nodeContainer.transform.localPosition = position;

            GameObject backgroundGroup = CreateEmptyGameObject(nodeContainer.transform, "Background", Vector3.zero);
            GameObject visualsObj = CreateNodeGameObject(node.Type, backgroundGroup.transform, width, height, true);

            visualsObj.transform.localRotation = Quaternion.Euler(0, 45, 0);

            ApplyColorToHierarchy(visualsObj, Color.green);

            return new Bounds(position, new Vector3(width, 0f, height));
        }

        private Bounds BuildForkJoinNode(GameObject nodeContainer, NodeData node, Dictionary<string, Vector3> overriddenPositions, float currentElevation)
        {
            float width = 0.5f;
            float height = 3.0f;

            Vector3 basePos = overriddenPositions[node.Key];
            Vector3 position = new Vector3(basePos.x, basePos.y + currentElevation, basePos.z);
            nodeContainer.transform.localPosition = position;

            GameObject backgroundGroup = CreateEmptyGameObject(nodeContainer.transform, "Background", Vector3.zero);
            GameObject visualsObj = CreateNodeGameObject(node.Type, backgroundGroup.transform, width, height);

            ApplyColorToHierarchy(visualsObj, Color.black);
            return new Bounds(position, new Vector3(width, 0f, height));
        }

        private Bounds BuildActionNode(GameObject nodeContainer, NodeData node, Dictionary<string, Vector3> overriddenPositions, float currentElevation, int depth)
        {
            float textWidth = MeasureText(node.GetNodeName() ?? "", HEADER_FONT_SIZE, true);
            float width = Mathf.Max(textWidth + 2f, 3f);
            float height = 2f;

            Vector3 basePos = overriddenPositions[node.Key];
            Vector3 position = new Vector3(basePos.x, basePos.y + currentElevation, basePos.z);
            nodeContainer.transform.localPosition = position;

            GameObject backgroundGroup = CreateEmptyGameObject(nodeContainer.transform, "Background", Vector3.zero);
            GameObject visualsObj = CreateNodeGameObject(node.Type, backgroundGroup.transform, width, height);

            ApplyColorToHierarchy(visualsObj, Color.cyan);
            CreateTextLabel(backgroundGroup.transform, node.GetNodeName(), new Vector3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, 0), width, HEADER_FONT_SIZE, TextAlignmentOptions.Center, FontStyles.Bold);

            return new Bounds(position, new Vector3(width, 0f, height));
        }

        private void ApplyRankBasedSpacing(List<NodeData> sortedNodes, List<EdgeData> edges, Dictionary<string, Vector3> overriddenPositions)
        {
            var flowEdges = edges.Where(e => e.Type == DiagramEdgeTypes.FLOWS_TO || e.Type == DiagramEdgeTypes.OBJECT_FLOW).ToList();
            var adj = new Dictionary<string, List<string>>();

            foreach (var node in sortedNodes) adj[node.Key] = new List<string>();

            foreach (var edge in flowEdges)
            {
                string from = ExtractKeyFromId(edge.From);
                string to = ExtractKeyFromId(edge.To);
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

                    Vector3 pos = overriddenPositions[node.Key];
                    pos.x = rank * xSpacing;
                    overriddenPositions[node.Key] = pos;
                }
            }
        }

        private Dictionary<string, (float minX, float maxX, float minZ, float maxZ)> CalculateSwimlaneBounds(List<NodeData> sortedNodes, Dictionary<string, List<NodeData>> parentToChildren, Dictionary<string, Vector3> overriddenPositions)
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
                Vector3 pos = overriddenPositions[node.Key];
                globalMinX = Mathf.Min(globalMinX, pos.x);
                globalMaxX = Mathf.Max(globalMaxX, pos.x);
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

        private Color GetLayerColor(int depth, int colorOffset = 0)
        {
            Color[] palette = new Color[]
            {
                new Color(0.4f, 0.4f, 0.4f, 0.5f),
                new Color(0.3f, 0.35f, 0.4f, 0.6f),
                new Color(0.35f, 0.4f, 0.35f, 0.6f)
            };
            return palette[(depth + colorOffset) % palette.Length];
        }
    }
}