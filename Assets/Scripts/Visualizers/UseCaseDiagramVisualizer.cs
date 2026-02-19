using System.Collections.Generic;
using Assets.Scripts.Data;
using Assets.Scripts.Interfaces;
using UnityEngine;

namespace Assets.Scripts.Visualizers
{
    public class UseCaseDiagramVisualizer : IGraphVisualizer
    {
        public void RenderGraph(GraphMetadata graph, GameObject container, List<NodeData> nodes, List<EdgeData> edges)
        {
            if (nodes == null || nodes.Count == 0) return;

            GameObject nodesParent = new GameObject("Nodes");
            GameObject edgesParent = new GameObject("Edges");

            nodesParent.transform.parent = container.transform;
            edgesParent.transform.parent = container.transform;

            var nodeObjects = new Dictionary<string, GameObject>();

            foreach (var node in nodes)
            {
                var nodeObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                nodeObj.name = node.Label ?? node.Key;
                nodeObj.transform.SetParent(nodesParent.transform, false);

                nodeObj.transform.localPosition = node.GetNodePosition();
                nodeObj.transform.localScale = Vector3.one * 0.6f;

                if (nodeObj.TryGetComponent<Renderer>(out var rend))
                    rend.material.color = Color.green;

                nodeObjects[node.Key] = nodeObj;
            }

            foreach (var edge in edges)
            {
                string fromKey = ExtractKeyFromId(edge.From);
                string toKey = ExtractKeyFromId(edge.To);

                if (!nodeObjects.TryGetValue(fromKey, out var a) ||
                    !nodeObjects.TryGetValue(toKey, out var b))
                    continue;

                var edgeGo = new GameObject($"Edge_{edge.Key}");
                edgeGo.transform.SetParent(edgesParent.transform, false);

                var lr = edgeGo.AddComponent<LineRenderer>();
                lr.positionCount = 2;
                lr.useWorldSpace = false;
                lr.SetPosition(0, a.transform.localPosition);
                lr.SetPosition(1, b.transform.localPosition);
                lr.startWidth = lr.endWidth = 0.04f;
                lr.material = new Material(Shader.Find("Sprites/Default"));
                lr.startColor = lr.endColor = Color.black;
            }

            Debug.Log($"Rendered use case diagram for {graph?.Key ?? "?"} inside {container.name}");
        }
        string ExtractKeyFromId(string id)
        {
            if (string.IsNullOrEmpty(id)) return "";
            int slashIndex = id.LastIndexOf('/');
            return slashIndex >= 0 ? id.Substring(slashIndex + 1) : id;
        }
    }
}