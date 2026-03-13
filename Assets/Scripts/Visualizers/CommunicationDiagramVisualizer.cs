using Assets.Scripts.Data;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;

namespace Assets.Scripts.Visualizers
{
    public class CommunicationDiagramVisualizer : BaseGraphVisualizer
    {
        protected override void DrawDiagramContent(GameObject container, List<NodeData> nodes, List<EdgeData> edges)
        {
            GameObject nodesParent = new GameObject("Nodes");
            GameObject edgesParent = new GameObject("Edges");

            nodesParent.transform.SetParent(container.transform, false);
            edgesParent.transform.SetParent(container.transform, false);

            var nodeObjects = new Dictionary<string, GameObject>();

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

            foreach (var node in nodes)
            {
                if (node == rootDiagram) continue;

                int depth = GetDepth(node.Key);
                float currentElevation = (depth + 1) * Y_ELEVATION;

                bool isActor = node.Type == DiagramNodeTypes.ACTOR;

                GameObject nodeContainer = new GameObject("Node_" + (node.GetNodeName() ?? node.Key));
                nodeContainer.transform.SetParent(nodesParent.transform, false);

                float nodeWidth, nodeHeight;
                Vector3 position;

                if (isActor)
                {
                    nodeWidth = 2.5f;
                    nodeHeight = 2.5f;
                    position = new Vector3(node.GetNodePosition().x, node.GetNodePosition().y + currentElevation, node.GetNodePosition().z);
                }
                else
                {
                    float textWidth = MeasureText(node.GetNodeName() ?? "", HEADER_FONT_SIZE, true);
                    nodeWidth = Mathf.Max(textWidth + 4f, 6f);
                    nodeHeight = 3.5f;
                    position = new Vector3(node.GetNodePosition().x, node.GetNodePosition().y + currentElevation, node.GetNodePosition().z);
                }

                nodeContainer.transform.localPosition = position;

                GameObject visualObj;
                string prefabKey = isActor ? DiagramNodeTypes.ACTOR : node.Type;

                if (prefabsDictionary != null && prefabsDictionary.TryGetValue(prefabKey, out GameObject prefab) && prefab != null)
                {
                    visualObj = Object.Instantiate(prefab, nodeContainer.transform);
                    visualObj.name = "Background";
                    visualObj.transform.localPosition = Vector3.zero;

                    if (isActor)
                        visualObj.transform.localScale = Vector3.one * nodeWidth;
                    else
                        visualObj.transform.localScale = new Vector3(nodeWidth, 0.2f, nodeHeight);

                    foreach (var txt in visualObj.GetComponentsInChildren<TextMeshPro>()) Object.Destroy(txt.gameObject);
                }
                else
                {
                    PrimitiveType prim = isActor ? PrimitiveType.Cylinder : PrimitiveType.Cube;

                    visualObj = GameObject.CreatePrimitive(prim);
                    visualObj.transform.SetParent(nodeContainer.transform, false);
                    visualObj.name = "Background";
                    visualObj.transform.localPosition = Vector3.zero;

                    if (isActor)
                        visualObj.transform.localScale = Vector3.one * nodeWidth;
                    else
                        visualObj.transform.localScale = new Vector3(nodeWidth, 0.2f, nodeHeight);

                    if (visualObj.TryGetComponent<Renderer>(out var rend))
                    {
                        rend.material = cachedNodeMaterial;
                        if (isActor)
                            rend.material.color = new Color(0.9f, 0.8f, 0.7f);
                        else
                            rend.material.color = new Color(0.7f, 0.85f, 0.9f);
                    }
                }

                if (isActor)
                {
                    CreateTextLabel(nodeContainer.transform, node.GetNodeName(), new Vector3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, -2.0f), Mathf.Max(nodeWidth, 8f), HEADER_FONT_SIZE, TextAlignmentOptions.Top, FontStyles.Bold);
                }
                else
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
    }
}