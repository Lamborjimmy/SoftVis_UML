using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks; // <-- Added
using Newtonsoft.Json;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;

public class GraphDataManager : MonoBehaviour
{
    [System.Serializable]
    public class InitialGraphData
    {
        [JsonProperty("_id")]
        public string Id;
        [JsonProperty("_key")]
        public string Key;
        [JsonProperty("_rev")]
        public string Rev;
        [JsonProperty("arangodb_graph")]
        public string ArrangoDB_Graph;
        [JsonProperty("edge_collections")]
        public List<string> EdgeCollections;
        [JsonProperty("name")]
        public string Name;
        [JsonProperty("updated_at")]
        public string UpdatedAt;
        [JsonProperty("vertex_collections")]
        public List<string> VertexCollections;
    }
    [System.Serializable]
    public class FullGraphData
    {
        [JsonProperty("vertices")]
        public List<NodeData> Vertices;
        [JsonProperty("edges")]
        public List<EdgeData> Edges;
    }
    [System.Serializable]
    public class EdgeData
    {
        [JsonProperty("_from")]
        public string From;
        [JsonProperty("_id")]
        public string Id;
        [JsonProperty("_key")]
        public string Key;
        [JsonProperty("_rev")]
        public string Rev;
        [JsonProperty("_to")]
        public string To;

        [JsonProperty("_graph_id")]
        public string GraphId;
        [JsonProperty("_properties")]
        public Dictionary<string, object> Propertieps;
        [JsonProperty("_type")]
        public string Type;
    }
    [System.Serializable]
    public class NodeData
    {
        [JsonProperty("_id")]
        public string Id;
        [JsonProperty("_key")]
        public string Key;
        [JsonProperty("_rev")]
        public string Rev;
        [JsonProperty("graph_id")]
        public string GraphId;
        [JsonProperty("label")]
        public string Label;
        [JsonProperty("properties")]
        public Dictionary<string, object> Properties;
        [JsonProperty("type")]
        public string Type;
    }

    [System.Serializable]
    public class PipelineDefinition
    {
        [JsonProperty("return_mode")]
        public string ReturnMode;
        [JsonProperty("steps")]
        public List<PipelineStep> Steps;
    }

    [System.Serializable]
    public class PipelineStep
    {
        [JsonProperty("type")]
        public string Type;
        [JsonProperty("params")]
        public Dictionary<string, object> Params;
    }
    // Events for visualization (or any other component) to subscribe to
    public event Action<string, List<NodeData>, List<EdgeData>> OnFullGraphFetched;
    public event Action<List<InitialGraphData>> OnGraphsListed;
    public event Action<string, List<NodeData>> OnPipelineNodesFetched; // graphId + nodes
    public event Action<string, string> OnPipelineError; // graphId + error message
    private string apiBaseUrl = "http://localhost:8081";
    private const string API_VERSION = "api/v1";

    #region Public Methods
    public void ListGraphs()
    {
        _ = ListGraphsAsync(); // Fire-and-forget
    }

    public void RunPipeline(string graphId, PipelineDefinition pipeline)
    {
        _ = RunDefinedPipelineAsync(graphId, pipeline); // Fire-and-forget
    }
    #endregion

    #region Private Async Methods
    private async Task ListGraphsAsync()
    {
        string url = $"{apiBaseUrl}/{API_VERSION}/graphs";
        Debug.Log($"Getting graphs from {url}");

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            await request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"HTTP {request.responseCode} : {request.error}");
            }
            else
            {
                try
                {
                    string json = request.downloadHandler.text;
                    Debug.Log($"Response JSON: {json}");

                    var graphs = JsonConvert.DeserializeObject<List<InitialGraphData>>(json);
                    if (graphs == null)
                    {
                        graphs = new List<InitialGraphData>();
                    }

                    Debug.Log($"Successfully fetched {graphs.Count} graphs");

                    foreach (var graph in graphs)
                    {
                        Debug.Log("=== Graph ===");
                        Debug.Log($"_id: {graph.Id ?? "null"}");
                        Debug.Log($"_key: {graph.Key ?? "null"}");
                        Debug.Log($"_rev: {graph.Rev ?? "null"}");
                        Debug.Log($"arangodb_graph: {graph.ArrangoDB_Graph ?? "null"}");

                        string edgeCollectionsStr = graph.EdgeCollections == null
                            ? "null"
                            : (graph.EdgeCollections.Count == 0 ? "(empty)" : string.Join(", ", graph.EdgeCollections));
                        Debug.Log($"edge_collections: {edgeCollectionsStr}");

                        Debug.Log($"name: {graph.Name ?? "null"}");
                        Debug.Log($"updated_at: {graph.UpdatedAt ?? "null"}");

                        string vertexCollectionsStr = graph.VertexCollections == null
                            ? "null"
                            : (graph.VertexCollections.Count == 0 ? "(empty)" : string.Join(", ", graph.VertexCollections));
                        Debug.Log($"vertex_collections: {vertexCollectionsStr}");

                        Debug.Log("==============");
                    }
                    OnGraphsListed?.Invoke(graphs);
                    if (graphs.Count > 0)
                    {
                        _ = RunPipelineOnAllGraphsAsync(graphs); // Start processing all graphs (fire-and-forget, runs sequentially in background)
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error parsing graph list: {e.Message}");
                }
            }
        }
    }

    private async Task RunPipelineOnAllGraphsAsync(List<InitialGraphData> graphs)
    {
        Debug.Log($"Starting to run default pipeline on {graphs.Count} graphs...");

        foreach (var graph in graphs)
        {
            string graphId = graph.Key;

            if (string.IsNullOrEmpty(graphId))
            {
                Debug.LogWarning($"Skipping graph with null/empty _key");
                continue;
            }

            Debug.Log($"Running default pipeline on graph: {graph.Name ?? "Unnamed"} (ID: {graphId})");

            var pipeline = new PipelineDefinition
            {
                Steps = new List<PipelineStep>()
            };

            await RunDefinedPipelineAsync(graphId, pipeline);

            await Task.Delay(500); // Gentle delay between requests
        }

        Debug.Log("Finished running pipeline on all graphs.");
    }

    private async Task RunDefinedPipelineAsync(string graphId, PipelineDefinition pipeline)
    {
        string url = $"{apiBaseUrl}/{API_VERSION}/graphs/{graphId}/pipeline";
        Debug.Log($"Running pipeline on graph '{graphId}': {url}");

        string pipelineJson = JsonConvert.SerializeObject(pipeline, Formatting.Indented);
        Debug.Log($"Pipeline: {pipelineJson}");

        byte[] bodyRaw = Encoding.UTF8.GetBytes(pipelineJson);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            await request.SendWebRequest();

            Debug.Log($"HTTP Status Code: {request.responseCode}");
            Debug.Log($"HTTP Response Headers: {request.GetResponseHeader("Content-Type")}");

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"HTTP {request.responseCode}: {request.error}");
                if (!string.IsNullOrEmpty(request.downloadHandler.text))
                {
                    Debug.LogError($"Error response body: {request.downloadHandler.text}");
                    OnPipelineError?.Invoke(graphId, request.error ?? "Unknown error");
                }
            }
            else
            {
                try
                {
                    string json = request.downloadHandler.text;

                    if (string.IsNullOrWhiteSpace(json))
                    {
                        Debug.LogWarning($"API returned null/empty response. Status: {request.responseCode}");
                        return;
                    }

                    Debug.Log($"Pipeline response length: {json.Length}");
                    Debug.Log($"Pipeline response (first 500 chars): {json.Substring(0, Mathf.Min(500, json.Length))}");

                    // Detect if this is the new "subgraph" format (array with vertices + edges)
                    if (pipeline.ReturnMode != null && pipeline.ReturnMode.Equals("subgraph", StringComparison.OrdinalIgnoreCase))
                    {
                        var fullResponse = JsonConvert.DeserializeObject<List<FullGraphData>>(json);

                        if (fullResponse == null || fullResponse.Count == 0)
                        {
                            Debug.LogWarning("Subgraph response was empty or invalid");
                            OnPipelineError?.Invoke(graphId, "Empty subgraph response");
                            return;
                        }

                        var item = fullResponse[0]; // The API returns a single-item array
                        var nodes = item.Vertices ?? new List<NodeData>();
                        var edges = item.Edges ?? new List<EdgeData>();

                        Debug.Log($"Successfully fetched subgraph: {nodes.Count} nodes, {edges.Count} edges");

                        // Optional: keep printing nodes as before
                        // foreach (var node in nodes) { ... your existing node printing ... }

                        // Invoke the new event for visualization
                        OnFullGraphFetched?.Invoke(graphId, nodes, edges);
                    }
                    else
                    {
                        // Fallback for old format (just list of nodes)
                        var nodes = JsonConvert.DeserializeObject<List<NodeData>>(json);
                        if (nodes == null) nodes = new List<NodeData>();

                        Debug.Log($"Successfully fetched {nodes.Count} nodes (legacy mode)");

                        // Your existing node printing loop here...

                        OnPipelineNodesFetched?.Invoke(graphId, nodes); // if you still use this event
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error parsing pipeline result: {e.Message}\n{e.StackTrace}");
                    OnPipelineError?.Invoke(graphId, e.Message);
                }
            }
        }
    }
    #endregion
}