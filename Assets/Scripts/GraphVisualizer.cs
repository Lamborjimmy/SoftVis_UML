using UnityEngine;
using System.Collections.Generic;

public class GraphVisualizer : MonoBehaviour
{
    private GraphDataManager graphManager;
    private List<GraphDataManager.InitialGraphData> availableGraphs = new List<GraphDataManager.InitialGraphData>();

    void Start()
    {
        // Add and configure your existing GraphDataManager
        graphManager = gameObject.AddComponent<GraphDataManager>();

        // Subscribe to events
        graphManager.OnGraphsListed += OnGraphsListed;
        graphManager.OnPipelineNodesFetched += VisualizeNodes;
        graphManager.OnPipelineError += OnPipelineError;

        // Option 1: Directly fetch a known graph ID (your current approach)
        FetchSpecificGraph("2f367328-91bb-44ec-99ff-8b3dfcbd47ce");

        // Option 2: Uncomment below to auto-find and visualize the first non-empty graph
        // FindAndVisualizeFirstValidGraph();
    }

    void FetchSpecificGraph(string graphId)
    {
        Debug.Log($"🔄 Fetching nodes for specific graph: {graphId}");

        var pipeline = new GraphDataManager.PipelineDefinition
        {
            ReturnMode = "subgraph", // Keep your setting (works even if only nodes returned)
            Steps = new List<GraphDataManager.PipelineStep>()
        };

        graphManager.RunPipeline(graphId, pipeline);
    }

    void FindAndVisualizeFirstValidGraph()
    {
        Debug.Log("🔍 Finding a graph with actual nodes...");
        graphManager.ListGraphs(); // This will trigger OnGraphsListed
    }

    void OnGraphsListed(List<GraphDataManager.InitialGraphData> graphs)
    {
        availableGraphs = graphs;
        Debug.Log($"📊 Found {graphs.Count} graphs total");

        if (graphs.Count == 0)
        {
            Debug.LogError("❌ No graphs available");
            return;
        }

        // Try graphs one by one until we find one with nodes
        TryNextGraph(0);
    }

    void TryNextGraph(int index)
    {
        if (index >= availableGraphs.Count)
        {
            Debug.LogError("❌ No graph with nodes found");
            return;
        }

        var graph = availableGraphs[index];
        Debug.Log($"🔄 Checking graph [{index}]: {graph.Name ?? "Unnamed"} (ID: {graph.Key})");

        var pipeline = new GraphDataManager.PipelineDefinition
        {
            Steps = new List<GraphDataManager.PipelineStep>()
        };

        // Temporary subscription for this specific check
        void OnCheckFetched(string fetchedGraphId, List<GraphDataManager.GraphData> nodes)
        {
            if (fetchedGraphId != graph.Key) return;

            graphManager.OnPipelineNodesFetched -= OnCheckFetched; // Unsubscribe

            if (nodes != null && nodes.Count > 0)
            {
                Debug.Log($"✅ Found valid graph: {graph.Name ?? graph.Key} ({nodes.Count} nodes)");
                // Visualize it (re-use the same method)
                VisualizeNodes(graph.Key, nodes);
            }
            else
            {
                Debug.Log($"⚠️ Graph empty, trying next...");
                TryNextGraph(index + 1);
            }
        }

        graphManager.OnPipelineNodesFetched += OnCheckFetched;
        graphManager.RunPipeline(graph.Key, pipeline);
    }

    void OnPipelineError(string graphId, string error)
    {
        Debug.LogError($"❌ Pipeline error for graph {graphId}: {error}");
    }

    void VisualizeNodes(string graphId, List<GraphDataManager.GraphData> nodes)
    {
        string graphName = graphId; // Fallback
        var metadata = availableGraphs.Find(g => g.Key == graphId);
        if (metadata != null) graphName = metadata.Name ?? graphId;

        Debug.Log($"\n🎨 === VISUALIZING GRAPH: {graphName} (ID: {graphId}) ===");
        Debug.Log($"📊 Total Nodes: {nodes?.Count ?? 0}");

        if (nodes == null || nodes.Count == 0)
        {
            Debug.LogWarning("⚠️ No nodes to visualize");
            return;
        }


        int createdCount = 0;
        foreach (var node in nodes)
        {
            GameObject nodeObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            nodeObj.name = node.Label ?? node.Key;

            // Random layout in a sphere
            Vector3 position = Random.insideUnitSphere * 10f;
            nodeObj.transform.position = position;

            nodeObj.transform.localScale = Vector3.one * 0.5f;

            var renderer = nodeObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = GetColorForNodeType(node.Type);
            }

            createdCount++;

            if (createdCount <= 10) // Log first few
            {
                Debug.Log($" [{createdCount}] {node.Label ?? node.Key} (Type: {node.Type})");
            }
        }

        if (nodes.Count > 10)
        {
            Debug.Log($" ... and {nodes.Count - 10} more nodes");
        }

        Debug.Log("✅ Node visualization complete!");
    }

    Color GetColorForNodeType(string nodeType)
    {
        if (string.IsNullOrEmpty(nodeType)) return Color.gray;

        return nodeType.ToUpper() switch
        {
            "DIAGRAM" => Color.magenta,
            "UML_LIFELINE" => Color.cyan,
            "UML_CLASS" => Color.yellow,
            "UML_ACTOR" => new Color(1f, 0.5f, 0f), // Orange
            "UML_USECASE" => new Color(0.5f, 0.5f, 1f), // Light blue
            _ => Color.gray
        };
    }
}