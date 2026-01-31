using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks; // <-- Added
using Newtonsoft.Json;
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
    public class GraphData
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
    public event Action<List<InitialGraphData>> OnGraphsListed;
    public event Action<string, List<GraphData>> OnPipelineNodesFetched; // graphId + nodes
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

                    if (json == "null" || string.IsNullOrWhiteSpace(json))
                    {
                        Debug.LogWarning($"API returned null/empty response. Status: {request.responseCode}");
                        Debug.LogWarning($"Response length: {json?.Length ?? 0}");
                        return;
                    }

                    Debug.Log($"Pipeline response length: {json.Length}");
                    Debug.Log($"Pipeline response (first 500 chars): {json.Substring(0, Math.Min(500, json.Length))}");

                    var graphData = JsonConvert.DeserializeObject<List<GraphData>>(json);
                    if (graphData == null)
                    {
                        graphData = new List<GraphData>();
                    }

                    Debug.Log($"Successfully fetched {graphData.Count} nodes");

                    foreach (var node in graphData)
                    {
                        Debug.Log("=== Node ===");
                        Debug.Log($"_id: {node.Id ?? "null"}");
                        Debug.Log($"_key: {node.Key ?? "null"}");
                        Debug.Log($"_rev: {node.Rev ?? "null"}");
                        Debug.Log($"graph_id: {node.GraphId ?? "null"}");
                        Debug.Log($"label: {node.Label ?? "null"}");
                        Debug.Log($"type: {node.Type ?? "null"}");

                        Debug.Log("Properties:");
                        if (node.Properties == null)
                        {
                            Debug.Log(" null");
                        }
                        else if (node.Properties.Count == 0)
                        {
                            Debug.Log(" (empty)");
                        }
                        else
                        {
                            foreach (var kvp in node.Properties)
                            {
                                string valueStr = kvp.Value?.ToString() ?? "null";
                                Debug.Log($" {kvp.Key}: {valueStr}");
                            }
                        }

                        Debug.Log("==============");
                    }
                    OnPipelineNodesFetched?.Invoke(graphId, graphData);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error parsing pipeline result: {e.Message}\n{e.StackTrace}");
                    OnPipelineError?.Invoke(graphId, request.error ?? "Unknown error");
                }
            }
        }
    }
    #endregion
}