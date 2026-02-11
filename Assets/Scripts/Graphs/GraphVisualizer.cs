using UnityEngine;
using System.Collections.Generic;
using Assets.Scripts.Data;

namespace Assets.Scripts.Graphs
{
    public class GraphVisualizer : MonoBehaviour
    {
        [SerializeField] private GraphDataManager graphManager;
        [SerializeField] private float verticalSpacing;
        private int graphStackCount;
        private Dictionary<string, GameObject> nodeObjects = new Dictionary<string, GameObject>();
        private GameObject nodesParent;
        private GameObject edgesParent;
        private List<GameObject> graphContainers = new List<GameObject>();

        void OnEnable()
        {
            if (graphManager != null)
            {
                graphManager.OnFullGraphFetched += VisualizeFullGraph;
            }
        }
        void OnDisable()
        {
            if (graphManager != null)
            {
                graphManager.OnFullGraphFetched -= VisualizeFullGraph;
            }
        }
        void VisualizeFullGraph(string graphId, List<NodeData> nodes, List<EdgeData> edges)
        {
            if (nodes.Count == 0) return;

            float currentYOffset = graphStackCount * verticalSpacing;

            GameObject graphRoot = new GameObject($"Graph_{graphId}_{graphStackCount}");
            graphContainers.Add(graphRoot);

            GameObject nodesParent = new GameObject("Nodes");
            nodesParent.transform.parent = graphRoot.transform;

            GameObject edgesParent = new GameObject("Edges");
            edgesParent.transform.parent = graphRoot.transform;

            Dictionary<string, GameObject> localNodeObjects = new Dictionary<string, GameObject>();

            foreach (var node in nodes)
            {
                GameObject nodeObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                nodeObj.name = node.Label ?? node.Key;
                nodeObj.transform.parent = nodesParent.transform;

                Vector3 basePos = node.GetNodePosition();
                nodeObj.transform.position = new Vector3(basePos.x, basePos.y + currentYOffset, basePos.z);

                nodeObj.transform.localScale = Vector3.one * 0.6f;

                var renderer = nodeObj.GetComponent<Renderer>();
                if (renderer != null)
                    renderer.material.color = GetColorForNodeType(node.Type);

                localNodeObjects[node.Key] = nodeObj;
            }

            foreach (var edge in edges)
            {
                string fromKey = ExtractKeyFromId(edge.From);
                string toKey = ExtractKeyFromId(edge.To);

                if (localNodeObjects.TryGetValue(fromKey, out GameObject fromObj) &&
                    localNodeObjects.TryGetValue(toKey, out GameObject toObj))
                {
                    GameObject edgeObj = new GameObject($"Edge_{edge.Key}");
                    edgeObj.transform.parent = edgesParent.transform;

                    LineRenderer lr = edgeObj.AddComponent<LineRenderer>();
                    lr.positionCount = 2;
                    lr.SetPosition(0, fromObj.transform.position);
                    lr.SetPosition(1, toObj.transform.position);
                    lr.startWidth = 0.05f;
                    lr.endWidth = 0.05f;
                    lr.material = new Material(Shader.Find("Unlit/Color"));
                    lr.material.color = GetColorForEdgeType(edge.Type);
                }
            }

            graphStackCount++;
            Debug.Log($"✅ Stacked graph {graphId} at Height: {currentYOffset}");
        }

        void ClearVisualization()
        {
            nodeObjects.Clear();

            if (nodesParent != null) Destroy(nodesParent);
            if (edgesParent != null) Destroy(edgesParent);
        }

        string ExtractKeyFromId(string id)
        {
            if (string.IsNullOrEmpty(id)) return "";
            int slashIndex = id.LastIndexOf('/');
            return slashIndex >= 0 ? id.Substring(slashIndex + 1) : id;
        }

        Color GetColorForNodeType(string nodeType)
        {
            if (string.IsNullOrEmpty(nodeType)) return Color.gray;

            return nodeType.ToUpper() switch
            {
                "DIAGRAM" => Color.magenta,
                "UML_LIFELINE" => Color.cyan,
                "UML_CLASS" => Color.yellow,
                _ => Color.gray
            };
        }

        Color GetColorForEdgeType(string edgeType)
        {
            if (string.IsNullOrEmpty(edgeType)) return Color.white;

            return edgeType.ToUpper() switch
            {
                "NESTED" => Color.green,
                "MESSAGES" => Color.blue,
                _ => Color.white
            };
        }
    }
}