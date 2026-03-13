using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Data;
using UnityEngine;

namespace Assets.Scripts.Visualizers
{
    public class DefaultGraphVisualizer : BaseGraphVisualizer
    {
        protected override void DrawDiagramContent(GameObject container, List<NodeData> nodes, List<EdgeData> edges)
        {
            GameObject nodesParent = new GameObject("Nodes");
            GameObject edgesParent = new GameObject("Edges");

            nodesParent.transform.SetParent(container.transform, false);
            edgesParent.transform.SetParent(container.transform, false);

            var nodeObjects = new Dictionary<string, GameObject>();

            foreach (var node in nodes.Where(n => n.Type != DiagramNodeTypes.DIAGRAM))
            {
                var nodeObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                nodeObj.name = node.GetNodeName() ?? node.Key;

                nodeObj.transform.SetParent(nodesParent.transform, false);

                nodeObj.transform.localPosition = new Vector3(node.GetNodePosition().x, node.GetNodePosition().y + 0.4f, node.GetNodePosition().z);

                nodeObj.transform.localScale = Vector3.one * 0.6f;

                if (nodeObj.TryGetComponent<Renderer>(out var rend))
                {
                    rend.material = cachedNodeMaterial;
                    rend.material.color = Color.black;
                }

                nodeObjects[node.Key] = nodeObj;
            }

            foreach (var edge in edges)
            {
                string fromKey = ExtractKeyFromId(edge.From);
                string toKey = ExtractKeyFromId(edge.To);

                if (nodeObjects.TryGetValue(fromKey, out var a) && nodeObjects.TryGetValue(toKey, out var b))
                {
                    //DrawEdge(edgesParent, a, b, edge);
                }
            }

            Debug.Log($"Rendered default graph inside {container.name}");
        }
    }

}
