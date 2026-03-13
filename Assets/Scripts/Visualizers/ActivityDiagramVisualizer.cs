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
            GameObject nodesParent = new GameObject("Nodes");
            GameObject edgesParent = new GameObject("Edges");

            nodesParent.transform.SetParent(container.transform, false);
            edgesParent.transform.SetParent(container.transform, false);

            var nodeObjects = new Dictionary<string, GameObject>();

            // 1. Map Nesting Hierarchy
            var parentToChildren = new Dictionary<string, List<NodeData>>();
            var childToParent = new Dictionary<string, string>();
            var nestedChildKeys = new HashSet<string>();

            foreach (var edge in edges.Where(e => e.Type == DiagramEdgeTypes.NESTED))
            {
                string pKey = ExtractKeyFromId(edge.From);
                string cKey = ExtractKeyFromId(edge.To);

                nestedChildKeys.Add(cKey);
                childToParent[cKey] = pKey;

                if (!parentToChildren.ContainsKey(pKey))
                    parentToChildren[pKey] = new List<NodeData>();

                var childNode = nodes.FirstOrDefault(n => n.Key == cKey);
                if (childNode != null)
                    parentToChildren[pKey].Add(childNode);
            }

            var rootDiagram = nodes.FirstOrDefault(n => n.Type == DiagramNodeTypes.DIAGRAM);

            int GetDepth(string nodeKey)
            {
                int depth = 0;
                string current = nodeKey;
                while (childToParent.ContainsKey(current))
                {
                    current = childToParent[current];
                    if (rootDiagram != null && current != rootDiagram.Key)
                        depth++;
                }
                return depth;
            }

            var sortedNodes = nodes.Where(n => n != rootDiagram)
                                   .OrderBy(n => GetDepth(n.Key))
                                   .ToList();

            var overriddenPositions = new Dictionary<string, Vector3>();
            float zSpacingMultiplier = 0.7f;
            foreach (var node in nodes)
            {
                Vector3 pos = node.GetNodePosition();
                pos.z *= zSpacingMultiplier;
                overriddenPositions[node.Key] = pos;
            }

            var flowEdges = edges.Where(e => e.Type == DiagramEdgeTypes.FLOWS_TO || e.Type == DiagramEdgeTypes.OBJECT_FLOW).ToList();
            var adj = new Dictionary<string, List<string>>();

            foreach (var node in sortedNodes)
            {
                adj[node.Key] = new List<string>();
            }

            foreach (var edge in flowEdges)
            {
                string from = ExtractKeyFromId(edge.From);
                string to = ExtractKeyFromId(edge.To);
                if (adj.ContainsKey(from))
                {
                    adj[from].Add(to);
                }
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
                bool isContainerNode = parentToChildren.ContainsKey(node.Key) && parentToChildren[node.Key].Count > 0;
                bool isSwimlaneNode = node.Type == DiagramNodeTypes.SWIMLANE;
                bool isFinal = node.Type == DiagramNodeTypes.FINAL;

                if (!isContainerNode && !isSwimlaneNode)
                {
                    int rank = nodeRanks.ContainsKey(node.Key) ? nodeRanks[node.Key] : 0;
                    if (isFinal) rank = maxRank + 1;

                    Vector3 pos = overriddenPositions[node.Key];
                    pos.x = rank * xSpacing;
                    overriddenPositions[node.Key] = pos;
                }
            }

            var swimlanes = sortedNodes.Where(n => n.Type == DiagramNodeTypes.SWIMLANE).ToList();
            var swimlaneBoundsDict = new Dictionary<string, (float minX, float maxX, float minZ, float maxZ)>();

            if (swimlanes.Count > 0)
            {
                float globalMinX = float.MaxValue;
                float globalMaxX = float.MinValue;

                foreach (var sl in swimlanes)
                {
                    GetBounds(sl.Key, parentToChildren, overriddenPositions, out float minX, out float maxX, out float minZ, out float maxZ);
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
            }

            // 2. Draw Nodes and Containers
            foreach (var node in sortedNodes)
            {
                int depth = GetDepth(node.Key);
                float dampeningFactor = 0.6f;
                float currentElevation = Y_ELEVATION * (1f + (depth * dampeningFactor));

                bool isInitial = node.Type == DiagramNodeTypes.INITIAL;
                bool isFinal = node.Type == DiagramNodeTypes.FINAL;
                bool isDecision = node.Type == DiagramNodeTypes.DECISION;
                bool isForkJoin = node.Type == DiagramNodeTypes.FORK || node.Type == DiagramNodeTypes.JOIN;
                bool isSwimlane = node.Type == DiagramNodeTypes.SWIMLANE;

                bool isContainer = parentToChildren.TryGetValue(node.Key, out var children) && children.Count > 0;

                GameObject nodeContainer = new GameObject("Node_" + (node.GetNodeName() ?? node.Key));
                nodeContainer.transform.SetParent(nodesParent.transform, false);

                float nodeWidth, nodeHeight;
                Vector3 position;
                Vector3 basePos = overriddenPositions[node.Key];

                if (isContainer || isSwimlane)
                {
                    float minX, maxX, minZ, maxZ;
                    float paddingX = 2.0f;
                    float paddingZ = 2.0f;

                    if (isSwimlane && swimlaneBoundsDict.ContainsKey(node.Key))
                    {
                        var sb = swimlaneBoundsDict[node.Key];
                        minX = sb.minX; maxX = sb.maxX;
                        minZ = sb.minZ; maxZ = sb.maxZ;
                        paddingX = 0f;
                        paddingZ = 0f;
                    }
                    else
                    {
                        GetBounds(node.Key, parentToChildren, overriddenPositions, out minX, out maxX, out minZ, out maxZ);
                        if (minX == float.MaxValue) { minX = -2f; maxX = 2f; minZ = -2f; maxZ = 2f; }
                    }

                    nodeWidth = (maxX - minX) + paddingX * 2;
                    nodeHeight = (maxZ - minZ) + paddingZ * 2;
                    float centerZ = (minZ + maxZ) / 2f;

                    position = new Vector3((minX + maxX) / 2f, basePos.y + currentElevation - (Y_ELEVATION / 2f), centerZ);
                }
                else if (isInitial || isFinal)
                {
                    nodeWidth = 1.8f;
                    nodeHeight = 1.8f;
                    position = new Vector3(basePos.x, basePos.y + currentElevation, basePos.z);
                }
                else if (isDecision)
                {
                    nodeWidth = 2.5f;
                    nodeHeight = 2.5f;
                    position = new Vector3(basePos.x, basePos.y + currentElevation, basePos.z);
                }
                else if (isForkJoin)
                {
                    nodeWidth = 0.5f;
                    nodeHeight = 3.0f;
                    position = new Vector3(basePos.x, basePos.y + currentElevation, basePos.z);
                }
                else
                {
                    float textWidth = MeasureText(node.GetNodeName() ?? "", HEADER_FONT_SIZE, true);
                    nodeWidth = Mathf.Max(textWidth + 2f, 3f);
                    nodeHeight = 2f;
                    position = new Vector3(basePos.x, basePos.y + currentElevation, basePos.z);
                }

                nodeContainer.transform.localPosition = position;

                GameObject visualObj;
                if (prefabsDictionary != null && prefabsDictionary.TryGetValue(node.Type, out GameObject prefab) && prefab != null)
                {
                    visualObj = Object.Instantiate(prefab, nodeContainer.transform);
                    visualObj.name = "Background";
                    visualObj.transform.localPosition = Vector3.zero;

                    if (isInitial || isFinal || isDecision)
                        visualObj.transform.localScale = Vector3.one;
                    else
                        visualObj.transform.localScale = new Vector3(nodeWidth, 0.2f, nodeHeight);

                    foreach (var txt in visualObj.GetComponentsInChildren<TextMeshPro>()) Object.Destroy(txt.gameObject);
                }
                else
                {
                    PrimitiveType prim = PrimitiveType.Cube;
                    if (isInitial || isFinal) prim = PrimitiveType.Sphere;

                    visualObj = GameObject.CreatePrimitive(prim);
                    visualObj.transform.SetParent(nodeContainer.transform, false);
                    visualObj.name = "Background";
                    visualObj.transform.localPosition = Vector3.zero;

                    if (isInitial || isFinal)
                    {
                        visualObj.transform.localScale = Vector3.one;
                    }
                    else if (isDecision)
                    {
                        visualObj.transform.localRotation = Quaternion.Euler(0, 45, 0);
                        visualObj.transform.localScale = Vector3.one;
                    }
                    else
                    {
                        visualObj.transform.localScale = new Vector3(nodeWidth, 0.2f, nodeHeight);
                    }

                    if (visualObj.TryGetComponent<Renderer>(out var rend))
                    {
                        rend.material = cachedNodeMaterial;

                        if (isInitial || isFinal || isForkJoin)
                            rend.material.color = Color.black;
                        else if (isDecision)
                            rend.material.color = new Color(0.3f, 0.55f, 0.3f);
                        else if (isSwimlane)
                        {
                            int colorOffset = swimlanes.FindIndex(s => s.Key == node.Key);
                            rend.material.color = GetLayerColor(depth, colorOffset);
                        }
                        else if (isContainer)
                            rend.material.color = GetLayerColor(depth);
                        else
                            rend.material.color = new Color(0.35f, 0.4f, 0.5f);
                    }
                }

                string stereotype = "";
                if (isSwimlane) stereotype = "<<swimlane>>";

                if (isSwimlane)
                {
                    float textZ = (nodeHeight / 2f) - 1.5f;
                    CreateTextLabel(nodeContainer.transform, stereotype, new Vector3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, textZ), nodeWidth - 2f, LABEL_FONT_SIZE, TextAlignmentOptions.TopLeft);
                    CreateTextLabel(nodeContainer.transform, node.GetNodeName(), new Vector3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, textZ - LINE_HEIGHT), nodeWidth - 2f, HEADER_FONT_SIZE, TextAlignmentOptions.TopLeft, FontStyles.Bold);
                }
                else if (isContainer)
                {
                    float textZ = (nodeHeight / 2f) - 1.5f;
                    CreateTextLabel(nodeContainer.transform, node.GetNodeName(), new Vector3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, textZ - LINE_HEIGHT), nodeWidth, HEADER_FONT_SIZE, TextAlignmentOptions.Top, FontStyles.Bold);
                }
                else if (isDecision || isForkJoin) { }
                else if (!isInitial && !isFinal)
                {
                    CreateTextLabel(nodeContainer.transform, node.GetNodeName(), new Vector3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, 0), nodeWidth, HEADER_FONT_SIZE, TextAlignmentOptions.Center, FontStyles.Bold);
                }

                nodeObjects[node.Key] = nodeContainer;
            }

            var validEdges = edges.Where(e => e.Type != DiagramEdgeTypes.NESTED).ToList();
            var selfLoops = validEdges.Where(e => ExtractKeyFromId(e.From) == ExtractKeyFromId(e.To));
            var normalEdges = validEdges.Where(e => ExtractKeyFromId(e.From) != ExtractKeyFromId(e.To));

            DrawDiagramEdges(selfLoops, nodeObjects, edgesParent, normalEdges);
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

        private void GetBounds(string parentKey, Dictionary<string, List<NodeData>> parentToChildren, Dictionary<string, Vector3> positions, out float minX, out float maxX, out float minZ, out float maxZ)
        {
            minX = minZ = float.MaxValue;
            maxX = maxZ = float.MinValue;

            if (!parentToChildren.ContainsKey(parentKey)) return;

            float paddingX = 2.0f;
            float paddingZ = 2.0f;

            foreach (var child in parentToChildren[parentKey])
            {
                Vector3 pos = positions.ContainsKey(child.Key) ? positions[child.Key] : child.GetNodePosition();
                float childMinX = pos.x;
                float childMaxX = pos.x;
                float childMinZ = pos.z;
                float childMaxZ = pos.z;

                if (parentToChildren.ContainsKey(child.Key) && parentToChildren[child.Key].Count > 0)
                {
                    GetBounds(child.Key, parentToChildren, positions, out childMinX, out childMaxX, out childMinZ, out childMaxZ);
                    if (childMinX != float.MaxValue)
                    {
                        childMinX -= paddingX;
                        childMaxX += paddingX;
                        childMinZ -= paddingZ;
                        childMaxZ += paddingZ;
                    }
                }

                minX = Mathf.Min(minX, childMinX);
                maxX = Mathf.Max(maxX, childMaxX);
                minZ = Mathf.Min(minZ, childMinZ);
                maxZ = Mathf.Max(maxZ, childMaxZ);
            }
        }
    }
}