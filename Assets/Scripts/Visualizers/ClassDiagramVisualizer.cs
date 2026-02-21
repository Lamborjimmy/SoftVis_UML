using Assets.Scripts.Data;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Assets.Scripts.Visualizers
{
    public class ClassDiagramVisualizer : BaseGraphVisualizer
    {
        protected override void DrawDiagramContent(GameObject container, List<NodeData> nodes, List<EdgeData> edges)
        {
            GameObject nodesParent = new GameObject("Nodes");
            GameObject edgesParent = new GameObject("Edges");

            nodesParent.transform.SetParent(container.transform, false);
            edgesParent.transform.SetParent(container.transform, false);

            var nodeObjects = new Dictionary<string, GameObject>();

            foreach (var node in nodes.Where(n => n.Type != "DIAGRAM"))
            {
                var nodeObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                nodeObj.name = node.Label ?? node.Key;

                nodeObj.transform.SetParent(nodesParent.transform, false);

                nodeObj.transform.localPosition = new Vector3(node.GetNodePosition().x, node.GetNodePosition().y + 0.4f, node.GetNodePosition().z);

                nodeObj.transform.localScale = Vector3.one * 0.6f;

                if (nodeObj.TryGetComponent<Renderer>(out var rend))
                {
                    rend.material = Resources.Load<Material>("Materials/DefaultMat");
                    rend.material.color = Color.bisque;
                }

                nodeObjects[node.Key] = nodeObj;
            }

            foreach (var edge in edges)
            {
                string fromKey = ExtractKeyFromId(edge.From);
                string toKey = ExtractKeyFromId(edge.To);

                if (nodeObjects.TryGetValue(fromKey, out var a) && nodeObjects.TryGetValue(toKey, out var b))
                {
                    DrawEdge(edgesParent, a.transform.localPosition, b.transform.localPosition, edge.Key);
                }
            }

            Debug.Log($"Rendered organized Class Diagram inside {container.name}");
        }

        private void DrawEdge(GameObject parent, Vector3 start, Vector3 end, string edgeKey)
        {
            var edgeGo = new GameObject($"Edge_{edgeKey}");
            edgeGo.transform.SetParent(parent.transform, false);

            var lr = edgeGo.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.useWorldSpace = false;
            lr.SetPosition(0, start);
            lr.SetPosition(1, end);

            lr.startWidth = lr.endWidth = 0.04f;
            lr.material = Resources.Load<Material>("Materials/DefaultMat");
            lr.startColor = lr.endColor = Color.black;
        }
    }
}