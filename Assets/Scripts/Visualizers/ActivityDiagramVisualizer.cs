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

            // SORT NODES: Process parents first so containment builds correctly
            var sortedNodes = nodes.Where(n => n != rootDiagram)
                                   .OrderBy(n => GetDepth(n.Key))
                                   .ToList();

            // 2. Draw Nodes and Containers
            foreach (var node in sortedNodes)
            {
                int depth = GetDepth(node.Key);
                float dampeningFactor = 0.6f;
                float currentElevation = Y_ELEVATION * (1f + (depth * dampeningFactor));

                // Identify Node Types
                bool isAction = node.Type == DiagramNodeTypes.ACTION;
                bool isInitial = node.Type == DiagramNodeTypes.INITIAL;
                bool isFinal = node.Type == DiagramNodeTypes.FINAL;
                bool isDecision = node.Type == DiagramNodeTypes.DECISION;
                bool isForkJoin = node.Type == DiagramNodeTypes.FORK || node.Type == DiagramNodeTypes.JOIN;

                // We'll treat Swimlanes and Activities as containers for now
                bool isContainer = parentToChildren.TryGetValue(node.Key, out var children) && children.Count > 0;

                GameObject nodeContainer = new GameObject("Node_" + (node.Label ?? node.Key));
                nodeContainer.transform.SetParent(nodesParent.transform, false);

                float nodeWidth, nodeHeight;
                Vector3 position;

                if (isContainer)
                {
                    GetBounds(node.Key, parentToChildren, out float minX, out float maxX, out float minZ, out float maxZ);

                    float paddingX = 4.0f;
                    float paddingZ = 4.5f;

                    nodeWidth = (maxX - minX) + paddingX * 2;
                    nodeHeight = (maxZ - minZ) + paddingZ * 2;
                    float centerZ = (minZ + maxZ) / 2f;

                    position = new Vector3((minX + maxX) / 2f, node.GetNodePosition().y + currentElevation - (Y_ELEVATION / 2f), centerZ);
                }
                else if (isInitial || isFinal)
                {
                    nodeWidth = 1.8f;
                    nodeHeight = 1.8f;
                    position = new Vector3(node.GetNodePosition().x, node.GetNodePosition().y + currentElevation, node.GetNodePosition().z);
                }
                else if (isDecision)
                {
                    nodeWidth = 2.5f;
                    nodeHeight = 2.5f;
                    position = new Vector3(node.GetNodePosition().x, node.GetNodePosition().y + currentElevation, node.GetNodePosition().z);
                }
                else if (isForkJoin)
                {
                    // Draw forks/joins as thick synchronization bars
                    nodeWidth = 0.5f;
                    nodeHeight = 3.0f;
                    position = new Vector3(node.GetNodePosition().x, node.GetNodePosition().y + currentElevation, node.GetNodePosition().z);
                }
                else // Default Action / Object Node
                {
                    float textWidth = MeasureText(node.Label ?? "", HEADER_FONT_SIZE, true);
                    nodeWidth = Mathf.Max(textWidth + 4f, 6f);
                    nodeHeight = 4f;
                    position = new Vector3(node.GetNodePosition().x, node.GetNodePosition().y + currentElevation, node.GetNodePosition().z);
                }

                nodeContainer.transform.localPosition = position;

                // B. Instantiate Visual Prefab or Primitive directly as Background
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
                    // Select Fallback Primitives
                    PrimitiveType prim = PrimitiveType.Cube;
                    if (isInitial || isFinal) prim = PrimitiveType.Sphere;

                    visualObj = GameObject.CreatePrimitive(prim);
                    visualObj.transform.SetParent(nodeContainer.transform, false);
                    visualObj.name = "Background";
                    visualObj.transform.localPosition = Vector3.zero;

                    // Scale logic
                    if (isInitial || isFinal)
                    {
                        visualObj.transform.localScale = Vector3.one;
                    }
                    else if (isDecision)
                    {
                        // Rotate cube 45 degrees to look like a diamond
                        visualObj.transform.localRotation = Quaternion.Euler(0, 45, 0);
                        visualObj.transform.localScale = Vector3.one;
                    }
                    else
                    {
                        visualObj.transform.localScale = new Vector3(nodeWidth, 0.2f, nodeHeight);
                    }

                    // Material & Color logic
                    if (visualObj.TryGetComponent<Renderer>(out var rend))
                    {
                        rend.material = cachedNodeMaterial ?? Resources.Load<Material>("Materials/DefaultMat");

                        if (isInitial || isFinal || isForkJoin)
                            rend.material.color = Color.black;
                        else if (isDecision)
                            rend.material.color = new Color(0.3f, 0.55f, 0.3f); // Light Diamond Yellow
                        else if (isContainer)
                            rend.material.color = GetLayerColor(depth); // Activity / Swimlane
                        else
                            rend.material.color = new Color(0.35f, 0.4f, 0.5f); // Light Action Blue
                    }
                }

                // C. Spawn Labels
                if (isContainer)
                {
                    float textZ = (nodeHeight / 2f) - 1.5f;
                    CreateTextLabel(nodeContainer.transform, node.Label, new Vector3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, textZ - LINE_HEIGHT), nodeWidth, HEADER_FONT_SIZE, TextAlignmentOptions.Top, FontStyles.Bold);
                }
                else if (isDecision || isForkJoin)
                {

                }
                else if (!isInitial && !isFinal)
                {
                    // Standard action centered text
                    CreateTextLabel(nodeContainer.transform, node.Label, new Vector3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, 0), nodeWidth, HEADER_FONT_SIZE, TextAlignmentOptions.Center, FontStyles.Bold);
                }

                nodeObjects[node.Key] = nodeContainer;
            }

            // 3. Draw Edges
            var validEdges = edges.Where(e => e.Type != DiagramEdgeTypes.NESTED).ToList();
            var selfLoops = validEdges.Where(e => ExtractKeyFromId(e.From) == ExtractKeyFromId(e.To));
            var normalEdges = validEdges.Where(e => ExtractKeyFromId(e.From) != ExtractKeyFromId(e.To));

            DrawDiagramEdges(selfLoops, nodeObjects, edgesParent, normalEdges);
        }

        private Color GetLayerColor(int depth)
        {
            Color[] palette = new Color[]
            {
                new Color(0.4f, 0.4f, 0.4f, 0.5f), // Transparent Grey (Swimlane base)
                new Color(0.3f, 0.35f, 0.4f, 0.6f),
                new Color(0.35f, 0.4f, 0.35f, 0.6f)
            };
            return palette[depth % palette.Length];
        }

        private void GetBounds(string parentKey, Dictionary<string, List<NodeData>> parentToChildren, out float minX, out float maxX, out float minZ, out float maxZ)
        {
            minX = minZ = float.MaxValue;
            maxX = maxZ = float.MinValue;

            if (!parentToChildren.ContainsKey(parentKey)) return;

            foreach (var child in parentToChildren[parentKey])
            {
                Vector3 pos = child.GetNodePosition();
                minX = Mathf.Min(minX, pos.x);
                maxX = Mathf.Max(maxX, pos.x);
                minZ = Mathf.Min(minZ, pos.z);
                maxZ = Mathf.Max(maxZ, pos.z);

                if (parentToChildren.ContainsKey(child.Key))
                {
                    GetBounds(child.Key, parentToChildren, out float cMinX, out float cMaxX, out float cMinZ, out float cMaxZ);
                    if (cMinX != float.MaxValue)
                    {
                        minX = Mathf.Min(minX, cMinX);
                        maxX = Mathf.Max(maxX, cMaxX);
                        minZ = Mathf.Min(minZ, cMinZ);
                        maxZ = Mathf.Max(maxZ, cMaxZ);
                    }
                }
            }
        }
    }
}