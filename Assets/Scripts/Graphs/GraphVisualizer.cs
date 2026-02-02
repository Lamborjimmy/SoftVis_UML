using UnityEngine;
using System.Collections.Generic;
using Assets.Scripts.Data;

namespace Assets.Scripts.Graphs
{
    public class GraphVisualizer : MonoBehaviour
    {
        private GraphDataManager graphManager;
        private Dictionary<string, GameObject> nodeObjects = new Dictionary<string, GameObject>();
        private GameObject nodesParent;
        private GameObject edgesParent;

        void Start()
        {
            graphManager = gameObject.AddComponent<GraphDataManager>();

            // Subscribe to the new full-graph event
            graphManager.OnFullGraphFetched += VisualizeFullGraph;
            graphManager.OnPipelineError += (id, err) => Debug.LogError($"Graph {id} error: {err}");

            // Example: fetch a graph with subgraph mode (which now returns edges)
            //FetchGraphWithEdges("2f367328-91bb-44ec-99ff-8b3dfcbd47ce"); // use your graph ID
            graphManager.ListGraphs(true);
        }

        void FetchGraphWithEdges(string graphId)
        {
            Debug.Log($"🔄 Fetching full graph (nodes + edges) for: {graphId}");

            var steps = new List<PipelineStep>
            {
                new PipelineStep
                {
                    Type = PipelineStepTypes.COLLECT_SUBGRAPH,
                    Params = null
                }
            };
            var pipeline = new Pipeline
            {
                ReturnMode = PipelineReturnModes.SUBGRAPH,
                Steps = steps
            };
            graphManager.RunPipeline(graphId, pipeline);
        }

        void VisualizeFullGraph(string graphId, List<NodeData> nodes, List<EdgeData> edges)
        {
            Debug.Log($"\n🎨 === VISUALIZING FULL GRAPH (ID: {graphId}) ===");
            Debug.Log($"📊 Nodes: {nodes.Count} | Edges: {edges.Count}");

            if (nodes.Count == 0)
            {
                Debug.LogWarning("⚠️ No nodes to visualize");
                return;
            }

            // Clear previous visualization
            ClearVisualization();

            // Create parent objects
            nodesParent = new GameObject("Nodes");
            edgesParent = new GameObject("Edges");

            // === Create nodes ===
            foreach (var node in nodes)
            {
                GameObject nodeObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                nodeObj.name = node.Label ?? node.Key;
                nodeObj.transform.parent = nodesParent.transform;

                // Simple circle layout for better visibility (instead of pure random)
                float angle = (float)nodes.IndexOf(node) / nodes.Count * Mathf.PI * 2f;
                Vector3 position = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * 8f;
                // Add small random offset to avoid perfect overlap
                position += Random.insideUnitSphere * 1.5f;
                nodeObj.transform.position = position;

                nodeObj.transform.localScale = Vector3.one * 0.6f;

                var renderer = nodeObj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = GetColorForNodeType(node.Type);
                }

                // Store for edge drawing
                nodeObjects[node.Key] = nodeObj;
            }

            // === Draw edges ===
            int drawnEdges = 0;
            foreach (var edge in edges)
            {
                string fromKey = ExtractKeyFromId(edge.From);
                string toKey = ExtractKeyFromId(edge.To);

                if (nodeObjects.TryGetValue(fromKey, out GameObject fromObj) &&
                    nodeObjects.TryGetValue(toKey, out GameObject toObj))
                {
                    // Create a line using LineRenderer
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

                    drawnEdges++;
                }
            }

            Debug.Log($"✅ Visualization complete: {nodes.Count} nodes, {drawnEdges} edges drawn");
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