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
        // Events for visualization (or any other component) to subscribe to
        public event Action<string, List<NodeData>, List<EdgeData>> OnFullGraphFetched;
        public event Action<List<GraphMetadata>> OnGraphsListed;
        public event Action<string, string> OnPipelineError;

        #region Public Methods
        public void ListGraphs(bool shouldRunPipeline)
        {
            _ = ListGraphsAsync(shouldRunPipeline);
        }

        public void RunPipeline(string graphId, Pipeline pipeline)
        {
            _ = RunDefinedPipelineAsync(graphId, pipeline);
        }
        public void FetchFullGraph(string graphId)
        {
            Debug.Log($"🔄 Requesting full graph data for: {graphId}");

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
            RunPipeline(graphId, pipeline);
        }
        #endregion

        #region Private Async Methods
        private async Task ListGraphsAsync(bool shouldRunPipeline)
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
                        }
                        OnGraphsListed?.Invoke(graphs);
                        if (graphs.Count > 0 && shouldRunPipeline)
                            _ = RunPipelineOnAllGraphsAsync(graphs);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error parsing graph list: {e.Message}");
                    }
                }
            }
        }

        private async Task RunPipelineOnAllGraphsAsync(List<GraphMetadata> graphs)
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

                var pipeline = new Pipeline
                {
                    Steps = new List<PipelineStep>()
                };

                await RunDefinedPipelineAsync(graphId, pipeline);

                await Task.Delay(500);
            }

            Debug.Log("Finished running pipeline on all graphs.");
        }

        private async Task RunDefinedPipelineAsync(string graphId, Pipeline pipeline)
        {
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
                            OnFullGraphFetched?.Invoke(graphId, nodes, edges);
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
}