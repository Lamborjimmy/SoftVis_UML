using Assets.Scripts.Data;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;

namespace Assets.Scripts.Visualizers
{
    public class PackageDiagramVisualizer : BaseGraphVisualizer
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

            var rootDiagram = nodes.FirstOrDefault(n => n.Type == DiagramNodeTypes.DIAGRAM && !nestedChildKeys.Contains(n.Key));

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

            // 2. Draw Nodes and Containers
            foreach (var node in nodes)
            {
                if (node == rootDiagram) continue;

                int depth = GetDepth(node.Key);
                float currentElevation = (depth + 1) * Y_ELEVATION;

                GameObject nodeContainer = new GameObject("Node_" + (node.Label ?? node.Key));
                nodeContainer.transform.SetParent(nodesParent.transform, false);

                // --- NESTED CONTAINER (Package holding elements) ---
                if (parentToChildren.TryGetValue(node.Key, out var children) && children.Count > 0)
                {
                    GetBounds(node.Key, parentToChildren, out float minX, out float maxX, out float minZ, out float maxZ);

                    float paddingX = 4.0f;
                    float paddingZ = 4.5f;

                    float width = (maxX - minX) + paddingX * 2;
                    float height = (maxZ - minZ) + paddingZ * 2;

                    Vector3 center = new Vector3((minX + maxX) / 2f, node.GetNodePosition().y + currentElevation - (Y_ELEVATION / 2f), (minZ + maxZ) / 2f);
                    nodeContainer.transform.localPosition = center;

                    // Draw simple rectangle for container
                    GameObject boundaryObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    boundaryObj.name = "Background";
                    boundaryObj.transform.SetParent(nodeContainer.transform, false);
                    boundaryObj.transform.localPosition = Vector3.zero;
                    boundaryObj.transform.localScale = new Vector3(width, Y_ELEVATION, height);

                    if (boundaryObj.TryGetComponent<Renderer>(out var rend))
                    {
                        rend.material = Resources.Load<Material>("Materials/DefaultMat");
                        rend.material.color = GetLayerColor(depth, true);
                    }

                    // Place text at the top-center of the rectangle
                    string containerLabel = $"<size=70%><<package>></size>\n<b>{node.Label}</b>";
                    CreateTextLabel(nodeContainer.transform, containerLabel, new Vector3(0, (Y_ELEVATION / 2f) + 0.1f, (height / 2f) - 2f), width, HEADER_FONT_SIZE, TextAlignmentOptions.Center);

                    nodeObjects[node.Key] = nodeContainer;
                    continue;
                }

                // --- LEAF NODES (Empty Packages, Components, Classes) ---
                nodeContainer.transform.localPosition = new Vector3(node.GetNodePosition().x, node.GetNodePosition().y + currentElevation, node.GetNodePosition().z);

                float textWidth = MeasureText(node.Label, HEADER_FONT_SIZE, true);
                float nodeWidth = Mathf.Max(textWidth + 3f, 6f);
                float nodeHeight = 4f;

                Color leafColor = GetLayerColor(depth, false);

                GameObject leafObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leafObj.name = "Background";
                leafObj.transform.SetParent(nodeContainer.transform, false);
                leafObj.transform.localPosition = Vector3.zero;
                leafObj.transform.localScale = new Vector3(nodeWidth, 0.2f, nodeHeight);

                if (leafObj.TryGetComponent<Renderer>(out var leafRend))
                {
                    leafRend.material = cachedNodeMaterial;
                    leafRend.material.color = leafColor;
                }

                // Determine stereotype based on node type
                string labelText;
                if (node.Type == DiagramNodeTypes.COMPONENT)
                {
                    labelText = $"<size=70%>&lt;&lt;component&gt;&gt;</size>\n<b>{node.Label}</b>";
                }
                else if (node.Type == DiagramNodeTypes.PACKAGE)
                {
                    labelText = $"<size=70%><<package>></size>\n<b>{node.Label}</b>";
                }
                else
                {
                    labelText = $"<b>{node.Label}</b>";
                }

                CreateTextLabel(nodeContainer.transform, labelText, new Vector3(0, 0.11f, 0), nodeWidth, HEADER_FONT_SIZE, TextAlignmentOptions.Center);

                nodeObjects[node.Key] = nodeContainer;
            }

            // 3. Draw Edges 
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

        // Helper to get a distinct darker color based on the nesting depth
        private Color GetLayerColor(int depth, bool isContainer)
        {
            Color[] palette = new Color[]
            {
                new Color(0.40f, 0.50f, 0.65f), // Slate Blue
                new Color(0.45f, 0.60f, 0.50f), // Muted Green
                new Color(0.60f, 0.45f, 0.55f), // Dusty Purple
                new Color(0.65f, 0.55f, 0.40f), // Earthy Orange/Brown
                new Color(0.45f, 0.55f, 0.60f)  // Teal Grey
            };

            Color c = palette[depth % palette.Length];
            c.a = isContainer ? 0.85f : 1f;
            return c;
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