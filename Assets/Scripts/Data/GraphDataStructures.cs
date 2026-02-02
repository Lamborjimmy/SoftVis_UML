using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Assets.Scripts.Data
{
    [Serializable]
    public class GraphMetadata
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
    [Serializable]
    public class NodesAndEdgesData
    {
        [JsonProperty("vertices")]
        public List<NodeData> Vertices;
        [JsonProperty("edges")]
        public List<EdgeData> Edges;
    }
    [Serializable]
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
    [Serializable]
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
    [Serializable]
    public class Pipeline
    {
        [JsonProperty("return_mode")]
        public string ReturnMode = PipelineReturnModes.SUBGRAPH;
        [JsonProperty("steps")]
        public List<PipelineStep> Steps;
    }
    [Serializable]
    public class PipelineStep
    {
        [JsonProperty("type")]
        public string Type;
        [JsonProperty("params")]
        public Dictionary<string, object> Params;
    }
}