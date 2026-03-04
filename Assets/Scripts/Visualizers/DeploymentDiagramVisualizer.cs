using Assets.Scripts.Data;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;

namespace Assets.Scripts.Visualizers
{
    public class DeploymentDiagramVisualizer : BaseGraphVisualizer
    {
        protected override void DrawDiagramContent(GameObject container, List<NodeData> nodes, List<EdgeData> edges)
        {
            GameObject nodesParent = new GameObject("Nodes");
            GameObject edgesParent = new GameObject("Edges");

            nodesParent.transform.SetParent(container.transform, false);
            edgesParent.transform.SetParent(container.transform, false);

            var nodeObjects = new Dictionary<string, GameObject>();

            // 1. Map Nesting Hierarchy (Crucial for Nodes containing Components)
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

            // 2. Draw Nodes and Containers
            foreach (var node in nodes)
            {
                if (node == rootDiagram) continue;

                int depth = GetDepth(node.Key);
                float currentElevation = (depth + 1) * Y_ELEVATION;

                bool isInterface = node.Type == DiagramNodeTypes.REQUIRED_INTERFACE || node.Type == DiagramNodeTypes.PROVIDED_INTERFACE;

                // A. Root Container
                GameObject nodeContainer = new GameObject("Node_" + (node.Label ?? node.Key));
                nodeContainer.transform.SetParent(nodesParent.transform, false);

                bool isContainer = parentToChildren.TryGetValue(node.Key, out var children) && children.Count > 0;

                float nodeWidth, nodeHeight;
                Vector3 position;

                if (isContainer)
                {
                    GetBounds(node.Key, parentToChildren, out float minX, out float maxX, out float minZ, out float maxZ);

                    float paddingX = 6.0f;
                    float paddingZ = 4.0f;

                    nodeWidth = (maxX - minX) + paddingX * 2;
                    nodeHeight = (maxZ - minZ) + paddingZ * 2;
                    float centerZ = (minZ + maxZ) / 2f;

                    position = new Vector3((minX + maxX) / 2f, node.GetNodePosition().y + currentElevation - (Y_ELEVATION / 2f), centerZ);
                }
                else if (isInterface)
                {
                    nodeWidth = 2.5f;
                    nodeHeight = 2.5f;
                    position = new Vector3(node.GetNodePosition().x, node.GetNodePosition().y + currentElevation, node.GetNodePosition().z);
                }
                else
                {
                    float textWidth = MeasureText(node.Label ?? "", HEADER_FONT_SIZE, true);
                    nodeWidth = Mathf.Max(textWidth + 3f, 6f);
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

                    if (isInterface)
                        visualObj.transform.localScale = Vector3.one * nodeWidth;
                    else
                        visualObj.transform.localScale = new Vector3(nodeWidth, 0.2f, nodeHeight);

                    foreach (var txt in visualObj.GetComponentsInChildren<TextMeshPro>()) Object.Destroy(txt.gameObject);
                }
                else
                {
                    PrimitiveType prim = PrimitiveType.Cube;
                    if (isInterface)
                        prim = PrimitiveType.Sphere;

                    visualObj = GameObject.CreatePrimitive(prim);
                    visualObj.transform.SetParent(nodeContainer.transform, false);
                    visualObj.name = "Background";
                    visualObj.transform.localPosition = Vector3.zero;

                    if (isInterface)
                        visualObj.transform.localScale = Vector3.one * nodeWidth;
                    else
                        visualObj.transform.localScale = new Vector3(nodeWidth, 0.2f, nodeHeight);

                    if (visualObj.TryGetComponent<Renderer>(out var rend))
                    {
                        rend.material = cachedNodeMaterial;

                        if (node.Type == DiagramNodeTypes.NODE)
                            rend.material.color = new Color(0.35f, 0.35f, 0.45f);
                        else if (node.Type == DiagramNodeTypes.COMPONENT)
                            rend.material.color = new Color(0.85f, 0.95f, 0.85f);
                        else if (node.Type == DiagramNodeTypes.ARTIFACT)
                            rend.material.color = new Color(0.95f, 0.95f, 0.85f);
                        else if (isInterface)
                            rend.material.color = new Color(0.9f, 0.8f, 0.9f);
                        else
                            rend.material.color = Color.white;
                    }
                }

                // C. Spawn Stereotype & Title Labels
                string stereotype = "";
                if (node.Type == DiagramNodeTypes.NODE) stereotype = "<<node>>";
                else if (node.Type == DiagramNodeTypes.COMPONENT) stereotype = "<<component>>";
                else if (node.Type == DiagramNodeTypes.ARTIFACT) stereotype = "<<artifact>>";

                if (isContainer)
                {
                    float textZ = (nodeHeight / 2f) - 1.5f;
                    CreateTextLabel(nodeContainer.transform, stereotype, new Vector3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, textZ), nodeWidth, LABEL_FONT_SIZE, TextAlignmentOptions.Top);
                    CreateTextLabel(nodeContainer.transform, node.Label, new Vector3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, textZ - LINE_HEIGHT), nodeWidth, HEADER_FONT_SIZE, TextAlignmentOptions.Top, FontStyles.Bold);
                }
                else if (isInterface)
                {
                    CreateTextLabel(nodeContainer.transform, node.Label, new Vector3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, -2.0f), Mathf.Max(nodeWidth, 8f), HEADER_FONT_SIZE, TextAlignmentOptions.Top, FontStyles.Bold);
                }
                else
                {
                    CreateTextLabel(nodeContainer.transform, stereotype, new Vector3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, 0.5f), nodeWidth, LABEL_FONT_SIZE, TextAlignmentOptions.Center);
                    CreateTextLabel(nodeContainer.transform, node.Label, new Vector3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, 0.5f - LINE_HEIGHT), nodeWidth, HEADER_FONT_SIZE, TextAlignmentOptions.Center, FontStyles.Bold);
                }

                nodeObjects[node.Key] = nodeContainer;
            }

            // 3. Draw Hub Edges using BaseGraphVisualizer
            var validEdges = edges.Where(e => e.Type != DiagramEdgeTypes.NESTED).ToList();
            var selfLoops = validEdges.Where(e => ExtractKeyFromId(e.From) == ExtractKeyFromId(e.To));
            var normalEdges = validEdges.Where(e => ExtractKeyFromId(e.From) != ExtractKeyFromId(e.To));

            DrawDiagramEdges(selfLoops, nodeObjects, edgesParent, normalEdges);
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