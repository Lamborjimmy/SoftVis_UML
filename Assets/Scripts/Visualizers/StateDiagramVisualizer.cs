using Assets.Scripts.Data;
using UnityEngine;
using System.Collections.Generic;
using TMPro;

namespace Assets.Scripts.Visualizers
{
    public class StateDiagramVisualizer : BaseGraphVisualizer
    {
        protected override void DrawDiagramContent(GameObject container, List<NodeData> nodes, List<EdgeData> edges)
        {
            GameObject nodesParent = new GameObject("Nodes");
            GameObject edgesParent = new GameObject("Edges");

            nodesParent.transform.SetParent(container.transform, false);
            edgesParent.transform.SetParent(container.transform, false);

            var nodeObjects = new Dictionary<string, GameObject>();

            // 1. Draw all Nodes (States, Initial, Final)
            foreach (var node in nodes)
            {
                if (node.Type == DiagramNodeTypes.DIAGRAM) continue;

                GameObject nodeContainer = new GameObject("Node_" + (node.Label ?? node.Key));
                nodeContainer.transform.SetParent(nodesParent.transform, false);

                // Position the container
                nodeContainer.transform.localPosition = new Vector3(node.GetNodePosition().x, node.GetNodePosition().y + Y_ELEVATION, node.GetNodePosition().z);

                GameObject visualObj;
                float textWidth = MeasureText(node.Label, HEADER_FONT_SIZE, true);


                // --- INITIAL & FINAL NODES ---
                if (node.Type == DiagramNodeTypes.PSEUDOSTATE)
                {
                    string prefabKey = node.Label == "initial" ? DiagramNodeTypes.INITIAL : DiagramNodeTypes.FINAL;

                    if (prefabsDictionary != null && prefabsDictionary.TryGetValue(prefabKey, out GameObject prefab))
                    {
                        visualObj = Object.Instantiate(prefab, nodeContainer.transform);
                    }
                    else
                    {
                        // Fallback primitive for Initial/Final (A classic Sphere)
                        visualObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        visualObj.transform.SetParent(nodeContainer.transform, false);
                    }

                    visualObj.name = "Background";
                    visualObj.transform.localPosition = Vector3.zero;
                    visualObj.transform.localScale = Vector3.one;
                }
                // --- STANDARD STATE NODES ---
                else
                {
                    float nodeWidth = Mathf.Max(textWidth + 0f, 5f);
                    float nodeHeight = 3.5f;

                    if (prefabsDictionary != null && prefabsDictionary.TryGetValue(DiagramNodeTypes.STATE, out GameObject prefab))
                    {
                        visualObj = Object.Instantiate(prefab, nodeContainer.transform);
                        visualObj.name = "Background";
                        visualObj.transform.localPosition = Vector3.zero;
                        visualObj.transform.localScale = new Vector3(nodeWidth, Y_ELEVATION, nodeHeight);
                    }
                    else
                    {
                        // Fallback primitive for State (Cube, acting like a rounded rect)
                        visualObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        visualObj.transform.SetParent(nodeContainer.transform, false);
                        visualObj.name = "Background";
                        visualObj.transform.localPosition = Vector3.zero;
                        visualObj.transform.localScale = new Vector3(nodeWidth, Y_ELEVATION, nodeHeight);
                    }

                    CreateTextLabel(nodeContainer.transform, node.Label, new Vector3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, 0), nodeWidth, HEADER_FONT_SIZE, TextAlignmentOptions.Center);
                }

                nodeObjects[node.Key] = nodeContainer;
            }

            // 2. Draw all Edges (Transitions)
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
    }
}