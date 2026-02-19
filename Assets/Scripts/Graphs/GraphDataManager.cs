using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using Assets.Scripts.Data;
namespace Assets.Scripts.Graphs
{

    public class GraphDataManager : MonoBehaviour
    {
        public event Action<GraphMetadata, List<NodeData>, List<EdgeData>> OnFullGraphFetched;
        public event Action<List<GraphMetadata>> OnGraphsListed;
        public event Action<string, string> OnPipelineError;
        public event Action<GraphMetadata> OnGraphTypeSet;

        #region Public Methods
        public void ListGraphs()
        {
            _ = ListGraphsAsync();
        }

        public void RunPipeline(GraphMetadata graph, Pipeline pipeline)
        {
            _ = RunDefinedPipelineAsync(graph, pipeline);
        }
        public void FetchFullGraph(GraphMetadata graph)
        {
            Debug.Log($"🔄 Requesting full graph data for: {graph.Key}");

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

            // Re-use your existing generic pipeline runner
            RunPipeline(graph, pipeline);
        }
        #endregion
        #region Private Methods
        private void FetchDiagramTypeOnly(GraphMetadata graph)
        {
            Debug.Log($"🔄 Fetching diagram type only for graph: {graph.Key}");

            var pipeline = new Pipeline
            {
                ReturnMode = PipelineReturnModes.VERTICES,
                Steps = new List<PipelineStep>
                {
                    new PipelineStep
                    {
                        Type = PipelineStepTypes.LIMIT,
                        Params = new Dictionary<string, object>
                        {
                            { "count", 1 },
                            { "offset", 0 }
                        }
                    }
                }
            };

            _ = RunDiagramTypePipelineAsync(graph, pipeline);
        }

        #endregion

        #region Private Async Methods
        private async Task ListGraphsAsync()
        {
            string url = $"{APIDefinitions.API_BASE_URL}/{APIDefinitions.API_VERSION}/graphs";
            Debug.Log($"Getting graphs from {url}");

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
                    Debug.LogError($"HTTP {request.responseCode} : {request.error}");
                else
                {
                    try
                    {
                        string json = request.downloadHandler.text;
                        Debug.Log($"Response JSON: {json}");

                        var graphs = JsonConvert.DeserializeObject<List<GraphMetadata>>(json);
                        if (graphs == null)
                            graphs = new List<GraphMetadata>();

                        Debug.Log($"Successfully fetched {graphs.Count} graphs");

                        foreach (var graph in graphs)
                        {
                            Debug.Log("=== Graph ===");
                            Debug.Log($"_key: {graph.Key ?? "null"}");
                            Debug.Log($"name: {graph.Name ?? "null"}");
                            Debug.Log($"updated_at: {graph.UpdatedAt ?? "null"}");
                            Debug.Log("==============");
                            FetchDiagramTypeOnly(graph);
                        }
                        OnGraphsListed?.Invoke(graphs);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error parsing graph list: {e.Message}");
                    }
                }
            }
        }

        private async Task RunDefinedPipelineAsync(GraphMetadata graph, Pipeline pipeline)
        {
            string graphId = graph.Key;
            string url = $"{APIDefinitions.API_BASE_URL}/{APIDefinitions.API_VERSION}/graphs/{graphId}/pipeline";
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

                        if (pipeline.ReturnMode != null)
                        {
                            var fullResponse = JsonConvert.DeserializeObject<List<NodesAndEdgesData>>(json);

                            if (fullResponse == null || fullResponse.Count == 0)
                            {
                                Debug.LogWarning("Subgraph response was empty or invalid");
                                OnPipelineError?.Invoke(graphId, "Empty subgraph response");
                                return;
                            }

                            var item = fullResponse[0];
                            var nodes = item.Vertices ?? new List<NodeData>();
                            var edges = item.Edges ?? new List<EdgeData>();


                            Debug.Log($"Successfully fetched subgraph: {nodes.Count} nodes, {edges.Count} edges");
                            OnFullGraphFetched?.Invoke(graph, nodes, edges);
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
        private async Task RunDiagramTypePipelineAsync(GraphMetadata graph, Pipeline pipeline)
        {
            string graphId = graph.Key;
            string url = $"{APIDefinitions.API_BASE_URL}/{APIDefinitions.API_VERSION}/graphs/{graphId}/pipeline";

            Debug.Log($"Running minimal pipeline (first vertex only) on '{graphId}': {url}");

            string pipelineJson = JsonConvert.SerializeObject(pipeline, Formatting.Indented);
            Debug.Log($"Pipeline: {pipelineJson}");

            byte[] bodyRaw = Encoding.UTF8.GetBytes(pipelineJson);

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.ConnectionError ||
                    request.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"HTTP {request.responseCode}: {request.error}");
                    OnPipelineError?.Invoke(graphId, request.error ?? "Unknown error");
                    return;
                }

                try
                {
                    string json = request.downloadHandler.text;
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        Debug.LogWarning($"API returned null/empty response for diagram type. Status: {request.responseCode}");
                        return;
                    }

                    var vertices = JsonConvert.DeserializeObject<List<NodeData>>(json);
                    if (vertices == null || vertices.Count == 0)
                    {
                        Debug.LogWarning($"No vertices returned for graph {graphId} - cannot determine diagram type");
                        return;
                    }

                    var firstNode = vertices[0];

                    if (firstNode.Properties != null &&
                        firstNode.Properties.TryGetValue("diagram_type", out var diagramTypeObj) &&
                        diagramTypeObj != null)
                    {
                        string diagramType = diagramTypeObj.ToString();
                        Debug.Log($"Diagram type found: {diagramType}");

                        graph.GraphType = diagramType;
                        OnGraphTypeSet?.Invoke(graph);
                    }
                    else
                    {
                        Debug.LogWarning($"First node has no 'diagram_type' property in graph {graphId}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error parsing diagram type result for {graphId}: {e.Message}\n{e.StackTrace}");
                    OnPipelineError?.Invoke(graphId, e.Message);
                }
            }
        }
        #endregion
    }
}