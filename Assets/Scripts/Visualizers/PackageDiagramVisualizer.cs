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

                bool isContainer = parentToChildren.TryGetValue(node.Key, out var children) && children.Count > 0;
                bool isPackage = isContainer || node.Type == DiagramNodeTypes.PACKAGE;

                float currentTabHeight = isPackage ? 1.5f : 0f;

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

                    position = new Vector3((minX + maxX) / 2f, node.GetNodePosition().y + currentElevation - (Y_ELEVATION / 2f), centerZ + (currentTabHeight / 2f));
                }
                else
                {
                    float textWidth = MeasureText(node.Label, HEADER_FONT_SIZE, true);
                    nodeWidth = Mathf.Max(textWidth + 3f, 6f);
                    nodeHeight = 4f;

                    float centerZ = node.GetNodePosition().z;
                    position = new Vector3(node.GetNodePosition().x, node.GetNodePosition().y + currentElevation, centerZ + (currentTabHeight / 2f));
                }

                nodeContainer.transform.localPosition = position;

                float totalHeight = nodeHeight + currentTabHeight;

                GameObject visualObj;
                if (isPackage && prefabsDictionary != null && prefabsDictionary.TryGetValue(DiagramNodeTypes.PACKAGE, out GameObject packagePrefab) && packagePrefab != null)
                {
                    visualObj = Object.Instantiate(packagePrefab, nodeContainer.transform);
                }
                else
                {
                    visualObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    visualObj.transform.SetParent(nodeContainer.transform, false);
                }

                visualObj.name = "Background";
                visualObj.transform.localPosition = Vector3.zero;
                visualObj.transform.localScale = new Vector3(nodeWidth, Y_ELEVATION, totalHeight);

                if (isPackage && visualObj.transform.childCount > 0)
                {
                    float tabWorldWidth = Mathf.Min(2.5f, nodeWidth * 0.5f);
                    Transform pkgBody = visualObj.transform.Find("Package") ?? visualObj.transform.Find("Background");
                    if (pkgBody == null) pkgBody = visualObj.transform.GetChild(0);

                    if (pkgBody != null)
                    {
                        pkgBody.localScale = new Vector3(1f, 1f, nodeHeight / totalHeight);
                        pkgBody.localPosition = new Vector3(0f, 0f, -currentTabHeight / (2f * totalHeight));
                    }

                    Transform tabBody = visualObj.transform.Find("Tab");
                    if (tabBody == null && visualObj.transform.childCount > 1) tabBody = visualObj.transform.GetChild(1);

                    if (tabBody != null)
                    {
                        tabBody.localScale = new Vector3(tabWorldWidth / nodeWidth, 1f, currentTabHeight / totalHeight);
                        float tabLocalX = -0.5f + (tabWorldWidth / (2f * nodeWidth));
                        float tabLocalZ = 0.5f - (currentTabHeight / (2f * totalHeight));
                        tabBody.localPosition = new Vector3(tabLocalX, 0f, tabLocalZ);
                    }
                }

                Color layerColor = GetLayerColor(depth, isContainer);
                foreach (var rend in visualObj.GetComponentsInChildren<Renderer>())
                {
                    if (rend.enabled)
                    {
                        rend.material = cachedNodeMaterial;
                        rend.material.color = layerColor;
                    }
                }

                float textZ = (nodeHeight / 2f) - (currentTabHeight / 2f) - 1.5f;
                Vector3 textPos = new Vector3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, textZ);

                CreateTextLabel(nodeContainer.transform, node.Label, textPos, nodeWidth, HEADER_FONT_SIZE, TextAlignmentOptions.Top);

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