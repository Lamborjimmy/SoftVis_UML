using Assets.Scripts.Data;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;

namespace Assets.Scripts.Visualizers
{
    public class UseCaseDiagramVisualizer : BaseGraphVisualizer
    {
        protected override void DrawDiagramContent(GameObject container, List<NodeData> nodes, List<EdgeData> edges)
        {
            GameObject nodesParent = new GameObject("Nodes");
            GameObject edgesParent = new GameObject("Edges");

            nodesParent.transform.SetParent(container.transform, false);
            edgesParent.transform.SetParent(container.transform, false);

            var nodeObjects = new Dictionary<string, GameObject>();

            var extensionPointsMap = edges
                .Where(e => e.Type == DiagramEdgeTypes.EXTENDS_UML)
                .GroupBy(e => ExtractKeyFromId(e.To))
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => nodes.FirstOrDefault(n => n.Key == ExtractKeyFromId(e.From))?.Label ?? "Unknown").ToList()
                );

            // 2. Map Nesting Hierarchy
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

            var rootDiagram = nodes.FirstOrDefault(n => n.Type == DiagramNodeTypes.DIAGRAM && !nestedChildKeys.Contains(n.Key));

            // Helper to get nesting depth
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

            // 3. Draw Nodes and Containers
            foreach (var node in nodes)
            {
                if (node == rootDiagram) continue;

                int depth = GetDepth(node.Key);
                float currentElevation = (depth + 1) * Y_ELEVATION;

                GameObject nodeContainer = new GameObject("Node_" + (node.Label ?? node.Key));
                nodeContainer.transform.SetParent(nodesParent.transform, false);

                // NESTED CONTAINER
                if (parentToChildren.TryGetValue(node.Key, out var children) && children.Count > 0)
                {
                    GetBounds(node.Key, parentToChildren, out float minX, out float maxX, out float minZ, out float maxZ);

                    float paddingX = 6.0f;
                    float paddingZ = 4.0f;

                    float width = (maxX - minX) + paddingX * 2;
                    float height = (maxZ - minZ) + paddingZ * 2;

                    Vector3 center = new Vector3((minX + maxX) / 2f, node.GetNodePosition().y + currentElevation - (Y_ELEVATION / 2f), (minZ + maxZ) / 2f);
                    nodeContainer.transform.localPosition = center;

                    GameObject boundaryObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    boundaryObj.name = "Background";
                    boundaryObj.transform.SetParent(nodeContainer.transform, false);
                    boundaryObj.transform.localPosition = Vector3.zero;

                    boundaryObj.transform.localScale = new Vector3(width, Y_ELEVATION, height);

                    if (boundaryObj.TryGetComponent<Renderer>(out var rend))
                    {
                        rend.material = Resources.Load<Material>("Materials/DefaultMat");
                        rend.material.color = new Color(0.0f, 0.2f, 0.9f, 0.8f);
                    }

                    CreateTextLabel(nodeContainer.transform, node.Label, new Vector3(0, (Y_ELEVATION / 2f) + 0.1f, (height / 2f) - 1.2f), width, HEADER_FONT_SIZE, TextAlignmentOptions.Center, FontStyles.Bold);

                    nodeObjects[node.Key] = nodeContainer;
                    continue;
                }

                nodeContainer.transform.localPosition = new Vector3(node.GetNodePosition().x, node.GetNodePosition().y + currentElevation, node.GetNodePosition().z);
                GameObject visualObj;
                float textWidth = MeasureText(node.Label, HEADER_FONT_SIZE, true);

                if (node.Type == DiagramNodeTypes.ACTOR)
                {
                    if (prefabsDictionary != null && prefabsDictionary.TryGetValue(DiagramNodeTypes.ACTOR, out GameObject actorPrefab))
                        visualObj = Object.Instantiate(actorPrefab, nodeContainer.transform);
                    else
                    {
                        visualObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                        visualObj.transform.SetParent(nodeContainer.transform, false);
                    }

                    visualObj.name = "Background";
                    visualObj.transform.localPosition = Vector3.zero;
                    visualObj.transform.localScale = new Vector3(1f, 1f, 1f);

                    CreateTextLabel(nodeContainer.transform, node.Label, new Vector3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, 2f), textWidth + 3f, HEADER_FONT_SIZE, TextAlignmentOptions.Center, FontStyles.Bold);
                }
                else if (node.Type == DiagramNodeTypes.USECASE)
                {
                    visualObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    visualObj.name = "Background";
                    visualObj.transform.SetParent(nodeContainer.transform, false);
                    visualObj.transform.localPosition = Vector3.zero;

                    string labelText = node.Label;
                    int lineCount = 1;
                    if (extensionPointsMap.TryGetValue(node.Key, out List<string> points))
                    {
                        labelText = $"<b>{node.Label}</b>\n<size=80%>extension points</size>";
                        foreach (var p in points) labelText += $"\n<size=70%>{p}</size>";
                        lineCount = 2 + points.Count;
                    }

                    float ovalWidth = Mathf.Max(textWidth + 2.0f, 6f);
                    float baseHeight = ovalWidth / 2f;
                    float textHeightRequirement = lineCount * LINE_HEIGHT;
                    float ovalHeight = Mathf.Max(baseHeight, textHeightRequirement);

                    visualObj.transform.localScale = new Vector3(ovalWidth, 0.2f, ovalHeight);

                    if (visualObj.TryGetComponent<Renderer>(out var rend))
                    {
                        rend.sharedMaterial = cachedNodeMaterial;
                        rend.material.color = new Color(0.75f, 0.95f, 0.75f);
                    }
                    CreateTextLabel(nodeContainer.transform, labelText, new Vector3(0, Y_ELEVATION * 2f + Y_ELEVATION_TEXT_OFFSET, 0), ovalWidth, LABEL_FONT_SIZE); //Elevation * 2 because the shape is a cylinder not a cube
                }
                else
                {
                    visualObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    visualObj.name = "Background";
                    visualObj.transform.SetParent(nodeContainer.transform, false);
                    visualObj.transform.localScale = Vector3.one * 1.5f;
                }

                nodeObjects[node.Key] = nodeContainer;
            }

            // 4. Draw Edges
            foreach (var edge in edges)
            {
                if (edge.Type == DiagramEdgeTypes.NESTED) continue;
                string fromKey = ExtractKeyFromId(edge.From);
                string toKey = ExtractKeyFromId(edge.To);

                if (nodeObjects.TryGetValue(fromKey, out var a) && nodeObjects.TryGetValue(toKey, out var b))
                {
                    DrawEdge(edgesParent, a, b, edge);
                }
            }
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
                    minX = Mathf.Min(minX, cMinX);
                    maxX = Mathf.Max(maxX, cMaxX);
                    minZ = Mathf.Min(minZ, cMinZ);
                    maxZ = Mathf.Max(maxZ, cMaxZ);
                }
            }
        }
    }
}