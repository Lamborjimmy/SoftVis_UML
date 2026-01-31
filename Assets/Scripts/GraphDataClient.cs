using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using System;

public class GraphDataClient : MonoBehaviour
{
    private string apiBaseUrl = "http://localhost:8081";
    private const string API_VERSION = "api/v1";
    
    [SerializeField] private bool debugLogging = true;

    [System.Serializable]
    public class GraphMetadata
    {
        [JsonProperty("_key")]
        public string Key;
        
        [JsonProperty("_id")]
        public string Id;
        
        [JsonProperty("name")]
        public string Name;
        
        [JsonProperty("vertex_collections")]
        public List<string> VertexCollections;
        
        [JsonProperty("edge_collections")]
        public List<string> EdgeCollections;
        
        [JsonProperty("updated_at")]
        public string UpdatedAt;
    }

    [System.Serializable]
    public class GraphNode
    {
        [JsonProperty("_key")]
        public string Key;
        
        [JsonProperty("_id")]
        public string Id;
        
        [JsonProperty("type")]
        public string Type;
        
        [JsonProperty("label")]
        public string Label;
        
        [JsonProperty("name")]
        public string Name;
        
        [JsonProperty("graph_id")]
        public string GraphId;
        
        [JsonProperty("properties")]
        public Dictionary<string, object> Properties;
    }

    [System.Serializable]
    public class GraphEdge
    {
        [JsonProperty("_key")]
        public string Key;
        
        [JsonProperty("_from")]
        public string From;
        
        [JsonProperty("_to")]
        public string To;
        
        [JsonProperty("type")]
        public string Type;
        
        [JsonProperty("label")]
        public string Label;
        
        [JsonProperty("graph_id")]
        public string GraphId;
    }

    [System.Serializable]
    public class GraphData
    {
        [JsonProperty("vertices")]
        public List<GraphNode> Vertices = new List<GraphNode>();
        
        [JsonProperty("edges")]
        public List<GraphEdge> Edges = new List<GraphEdge>();
        
        [JsonProperty("items")]
        public List<GraphNode> Items;
    }

    [System.Serializable]
    public class PipelineStep
    {
        [JsonProperty("type")]
        public string Type;
        
        [JsonProperty("params")]
        public Dictionary<string, object> Params;
    }

   [System.Serializable]
public class PipelineDefinition
{
    [JsonProperty("steps")]
    public List<PipelineStep> Steps;
    
    [JsonProperty("return_mode")]
    public string ReturnMode;  // ✅ ADD THIS
}

    // ==================== PUBLIC METHODS ====================

    public void ListGraphs(System.Action<List<GraphMetadata>> onSuccess, System.Action<string> onError)
    {
        StartCoroutine(ListGraphsCoroutine(onSuccess, onError));
    }

    public void GetGraph(string graphId, System.Action<GraphData> onSuccess, System.Action<string> onError)
    {
        var pipeline = new PipelineDefinition
        {
            Steps = new List<PipelineStep>()
        };
        RunPipeline(graphId, pipeline, onSuccess, onError);
    }

    public void RunPipeline(
        string graphId,
        PipelineDefinition pipeline,
        System.Action<GraphData> onSuccess,
        System.Action<string> onError)
    {
        StartCoroutine(RunPipelineCoroutine(graphId, pipeline, onSuccess, onError));
    }

    public void GetNodesByType(
        string graphId,
        string nodeType,
        System.Action<GraphData> onSuccess,
        System.Action<string> onError)
    {
        var pipeline = new PipelineDefinition
        {
            Steps = new List<PipelineStep>
            {
                new PipelineStep
                {
                    Type = "filter_nodes",
                    Params = new Dictionary<string, object>
                    {
                        { "key", "type" },
                        { "value", nodeType }
                    }
                }
            }
        };
        RunPipeline(graphId, pipeline, onSuccess, onError);
    }

    public void TraverseGraph(
        string graphId,
        string startNodeId,
        int maxDepth = 3,
        System.Action<GraphData> onSuccess = null,
        System.Action<string> onError = null)
    {
        var pipeline = new PipelineDefinition
        {
            Steps = new List<PipelineStep>
            {
                new PipelineStep
                {
                    Type = "filter_nodes",
                    Params = new Dictionary<string, object>
                    {
                        { "key", "_key" },
                        { "value", startNodeId }
                    }
                },
                new PipelineStep
                {
                    Type = "traverse",
                    Params = new Dictionary<string, object>
                    {
                        { "max_depth", maxDepth }
                    }
                }
            }
        };
        RunPipeline(graphId, pipeline, onSuccess, onError);
    }

    // ==================== PRIVATE COROUTINES ====================

    private IEnumerator ListGraphsCoroutine(
        System.Action<List<GraphMetadata>> onSuccess,
        System.Action<string> onError)
    {
        string url = $"{apiBaseUrl}/{API_VERSION}/graphs";
        Log($"Fetching graphs from: {url}");

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Accept", "application/json");
            request.SetRequestHeader("Content-Type", "application/json");
            
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.ProtocolError)
            {
                string errorMsg = $"HTTP {request.responseCode}: {request.error}";
                Log(errorMsg, true);
                onError?.Invoke(errorMsg);
            }
            else
            {
                try
                {
                    string json = request.downloadHandler.text;
                    Log($"Response: {json}");

                    var graphs = JsonConvert.DeserializeObject<List<GraphMetadata>>(json);
                    if (graphs == null)
                    {
                        graphs = new List<GraphMetadata>();
                    }
                    Log($"Successfully fetched {graphs.Count} graphs");
                    onSuccess?.Invoke(graphs);
                }
                catch (System.Exception ex)
                {
                    string errorMsg = $"Error parsing graph list: {ex.Message}";
                    Log(errorMsg, true);
                    onError?.Invoke(errorMsg);
                }
            }
        }
    }

    private IEnumerator RunPipelineCoroutine(
    string graphId,
    PipelineDefinition pipeline,
    System.Action<GraphData> onSuccess,
    System.Action<string> onError)
{
    string url = $"{apiBaseUrl}/{API_VERSION}/graphs/{graphId}/pipeline";
    Log($"Running pipeline on graph '{graphId}': {url}");

    string pipelineJson = JsonConvert.SerializeObject(pipeline, Formatting.Indented);
    Log($"Pipeline: {pipelineJson}");

    byte[] bodyRaw = Encoding.UTF8.GetBytes(pipelineJson);

    using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
    {
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Accept", "application/json");

        yield return request.SendWebRequest();

        // ✅ NEW: Debug the full response
        Log($"HTTP Status Code: {request.responseCode}");
        Log($"HTTP Response Headers: {request.GetResponseHeader("Content-Type")}");

        if (request.result == UnityWebRequest.Result.ConnectionError ||
            request.result == UnityWebRequest.Result.ProtocolError)
        {
            string errorMsg = $"HTTP {request.responseCode}: {request.error}";
            Log(errorMsg, true);
            
            // ✅ NEW: Log the error response body
            if (!string.IsNullOrEmpty(request.downloadHandler.text))
            {
                Log($"Error response body: {request.downloadHandler.text}", true);
            }
            
            onError?.Invoke(errorMsg);
        }
        else
        {
            try
            {
                string json = request.downloadHandler.text;
                
                // ✅ NEW: Check if response is actually null or just "null" string
                if (json == "null" || string.IsNullOrWhiteSpace(json))
                {
                    Log($"API returned null/empty response. Status: {request.responseCode}", true);
                    Log($"Response length: {json?.Length ?? 0}", true);
                    onError?.Invoke("API returned empty response");
                    yield break;
                }

                Log($"Pipeline response length: {json.Length}");
                Log($"Pipeline response (first 500 chars): {json.Substring(0, Math.Min(500, json.Length))}");

                // ✅ FIX: Handle multiple response formats
                GraphData graphData = ParseGraphResponse(json);

                if (graphData == null)
                {
                    graphData = new GraphData();
                }

                // Ensure lists are never null
                if (graphData.Vertices == null)
                    graphData.Vertices = new List<GraphNode>();
                if (graphData.Edges == null)
                    graphData.Edges = new List<GraphEdge>();

                Log($"Successfully retrieved {graphData.Vertices.Count} vertices and {graphData.Edges.Count} edges");
                onSuccess?.Invoke(graphData);
            }
            catch (System.Exception ex)
            {
                string errorMsg = $"Error parsing pipeline result: {ex.Message}\n{ex.StackTrace}";
                Log(errorMsg, true);
                onError?.Invoke(errorMsg);
            }
        }
    }
}

    /// <summary>
/// ✅ FIXED: Intelligently parse different response formats from the API
/// </summary>
private GraphData ParseGraphResponse(string json)
{
    if (string.IsNullOrEmpty(json))
    {
        Log("Empty response from API", true);
        return new GraphData();
    }

    try
    {
        // First, try to parse as direct GraphData object with vertices/edges
        var graphData = JsonConvert.DeserializeObject<GraphData>(json);
        if (graphData != null && (graphData.Vertices?.Count > 0 || graphData.Edges?.Count > 0))
        {
            Log($"Parsed as GraphData object: {graphData.Vertices?.Count ?? 0} vertices, {graphData.Edges?.Count ?? 0} edges");
            return graphData;
        }
    }
    catch { /* Continue to next format */ }

    try
    {
        // Try parsing as array of nodes/vertices directly
        var nodes = JsonConvert.DeserializeObject<List<GraphNode>>(json);
        if (nodes != null && nodes.Count > 0)
        {
            Log($"Parsed as array of {nodes.Count} nodes");
            return new GraphData 
            { 
                Vertices = nodes, 
                Edges = new List<GraphEdge>() 
            };
        }
    }
    catch (System.Exception ex)
    {
        Log($"Failed to parse as array of nodes: {ex.Message}");
    }

    try
    {
        // Try parsing as array of edges directly
        var edges = JsonConvert.DeserializeObject<List<GraphEdge>>(json);
        if (edges != null && edges.Count > 0)
        {
            Log($"Parsed as array of {edges.Count} edges");
            return new GraphData 
            { 
                Vertices = new List<GraphNode>(), 
                Edges = edges 
            };
        }
    }
    catch (System.Exception ex)
    {
        Log($"Failed to parse as array of edges: {ex.Message}");
    }

    try
    {
        // Try parsing as array of generic objects
        var objArray = JsonConvert.DeserializeObject<List<System.Collections.Generic.Dictionary<string, object>>>(json);
        if (objArray != null && objArray.Count > 0)
        {
            Log($"Detected array of {objArray.Count} generic objects");
            
            // Try to determine if these are nodes or edges
            var firstObj = objArray[0];
            
            // Check if it has _from and _to (edge properties)
            bool isEdges = firstObj.ContainsKey("_from") && firstObj.ContainsKey("_to");
            
            if (isEdges)
            {
                Log($"Parsed as array of {objArray.Count} edges");
                var edgesJson = JsonConvert.SerializeObject(objArray);
                var edgeList = JsonConvert.DeserializeObject<List<GraphEdge>>(edgesJson) ?? new List<GraphEdge>();
                return new GraphData 
                { 
                    Vertices = new List<GraphNode>(), 
                    Edges = edgeList 
                };
            }
            else
            {
                Log($"Parsed as array of {objArray.Count} nodes");
                var nodesJson = JsonConvert.SerializeObject(objArray);
                var nodeList = JsonConvert.DeserializeObject<List<GraphNode>>(nodesJson) ?? new List<GraphNode>();
                return new GraphData 
                { 
                    Vertices = nodeList, 
                    Edges = new List<GraphEdge>() 
                };
            }
        }
    }
    catch (System.Exception ex)
    {
        Log($"Failed to parse as array of objects: {ex.Message}");
    }

    try
    {
        // Try parsing as generic object and extract vertices/edges
        var obj = JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<string, object>>(json);
        if (obj != null)
        {
            var result = new GraphData();

            // Try to find vertices
            if (obj.ContainsKey("vertices"))
            {
                var verticesJson = JsonConvert.SerializeObject(obj["vertices"]);
                result.Vertices = JsonConvert.DeserializeObject<List<GraphNode>>(verticesJson) ?? new List<GraphNode>();
            }
            
            // Try to find edges
            if (obj.ContainsKey("edges"))
            {
                var edgesJson = JsonConvert.SerializeObject(obj["edges"]);
                result.Edges = JsonConvert.DeserializeObject<List<GraphEdge>>(edgesJson) ?? new List<GraphEdge>();
            }

            // Try to find items (alternative format)
            if (obj.ContainsKey("items") && result.Vertices.Count == 0)
            {
                var itemsJson = JsonConvert.SerializeObject(obj["items"]);
                result.Vertices = JsonConvert.DeserializeObject<List<GraphNode>>(itemsJson) ?? new List<GraphNode>();
            }

            if (result.Vertices.Count > 0 || result.Edges.Count > 0)
            {
                Log($"Parsed as generic object: {result.Vertices.Count} vertices, {result.Edges.Count} edges");
                return result;
            }
        }
    }
    catch (System.Exception ex)
    {
        Log($"Failed to parse as generic object: {ex.Message}");
    }

    Log($"Could not parse response in any format. Raw (first 200 chars): {json.Substring(0, Math.Min(200, json.Length))}", true);
    return new GraphData();
}

    public void SetApiBaseUrl(string url)
    {
        apiBaseUrl = url;
        Log($"API base URL set to: {apiBaseUrl}");
    }

    private void Log(string message, bool isError = false)
    {
        if (debugLogging)
        {
            if (isError)
                Debug.LogError($"[GraphDataClient] {message}");
            else
                Debug.Log($"[GraphDataClient] {message}");
        }
    }
}