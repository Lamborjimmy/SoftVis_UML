using Assets.Scripts.Data;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Assets.Scripts.Visualizers
{
    public class UseCaseDiagramVisualizer : BaseGraphVisualizer
    {
        private const float LABEL_FONT_SIZE = 4f;
        private const float HEADER_FONT_SIZE = 4f;
        protected override void DrawDiagramContent(GameObject container, List<NodeData> nodes, List<EdgeData> edges)
        {
            GameObject nodesParent = new GameObject("Nodes");
            GameObject edgesParent = new GameObject("Edges");

            nodesParent.transform.SetParent(container.transform, false);
            edgesParent.transform.SetParent(container.transform, false);

            var nodeObjects = new Dictionary<string, GameObject>();

            foreach (var node in nodes.Where(n => n.Type != DiagramNodeTypes.DIAGRAM))
            {
                GameObject nodeContainer = new GameObject("Node_" + (node.Label ?? node.Key));
                nodeContainer.transform.SetParent(nodesParent.transform, false);
                nodeContainer.transform.localPosition = new Vector3(node.GetNodePosition().x, node.GetNodePosition().y + 0.2f, node.GetNodePosition().z);

                GameObject visualObj;

                float textWidth = MeasureText(node.Label, LABEL_FONT_SIZE, false);

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

                    CreateTextLabel(nodeContainer.transform, node.Label, new Vector3(0, 0.1f, 2f), textWidth + 3f, HEADER_FONT_SIZE);
                }
                else if (node.Type == DiagramNodeTypes.USECASE)
                {
                    visualObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    visualObj.name = "Background";
                    visualObj.transform.SetParent(nodeContainer.transform, false);
                    visualObj.transform.localPosition = Vector3.zero;

                    // Scale the Oval based on measured text
                    float ovalWidth = Mathf.Max(textWidth + 1.5f, 4f);
                    visualObj.transform.localScale = new Vector3(ovalWidth, 0.1f, ovalWidth / 2f);

                    if (visualObj.TryGetComponent<Renderer>(out var rend))
                    {
                        rend.sharedMaterial = cachedNodeMaterial;
                        rend.material.color = new Color(0.75f, 0.95f, 0.75f);
                    }

                    CreateTextLabel(nodeContainer.transform, node.Label, new Vector3(0, 0.15f, 0), ovalWidth, LABEL_FONT_SIZE);
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

            Debug.Log($"Rendered organized usecase diagram inside {container.name}");
        }

    }
}
