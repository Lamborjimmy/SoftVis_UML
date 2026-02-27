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

            foreach (var node in nodes.Where(n => n.Type != DiagramNodeTypes.DIAGRAM))
            {
                GameObject nodeContainer = new GameObject("Node_" + (node.Label ?? node.Key));
                nodeContainer.transform.SetParent(nodesParent.transform, false);
                nodeContainer.transform.localPosition = new Vector3(node.GetNodePosition().x, node.GetNodePosition().y + Y_ELEVATION, node.GetNodePosition().z);

                GameObject visualObj;
                float textWidth = MeasureText(node.Label, HEADER_FONT_SIZE, true);

                if (node.Type == DiagramNodeTypes.ACTOR)
                {
                    if (prefabsDictionary.TryGetValue(DiagramNodeTypes.ACTOR, out GameObject actorPrefab))
                    {
                        visualObj = Object.Instantiate(actorPrefab, nodeContainer.transform);
                    }
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
                        foreach (var p in points)
                        {
                            labelText += $"\n<size=70%>{p}</size>";
                        }
                        lineCount = 2 + points.Count;
                    }

                    float ovalWidth = Mathf.Max(textWidth + 2.0f, 6f);
                    float baseHeight = ovalWidth / 2f;
                    // Standardized line height requirement
                    float textHeightRequirement = lineCount * LINE_HEIGHT;
                    float ovalHeight = Mathf.Max(baseHeight, textHeightRequirement);

                    // Unified Thickness: Y = 0.2f
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

            foreach (var edge in edges)
            {
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