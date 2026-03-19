using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Assets.Scripts.Data
{
    [Serializable]
    public class GraphMetadata
    {
        [JsonProperty("_key")]
        public string Key;
        [JsonProperty("name")]
        public string Name;
        [JsonProperty("updated_at")]
        public string UpdatedAt;
        public string GraphType;
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
        [JsonProperty("_key")]
        public string Key;
        [JsonProperty("_to")]
        public string To;

        [JsonProperty("properties")]
        public Dictionary<string, object> Propertieps;
        [JsonProperty("type")]
        public string Type;
    }
    [Serializable]
    public class NodeData
    {
        [JsonProperty("_key")]
        public string Key;
        [JsonProperty("properties")]
        public Dictionary<string, object> Properties;
        [JsonProperty("type")]
        public string Type;
        public Vec3 GetNodePosition()
        {
            if (Properties != null && Properties.ContainsKey("position"))
            {
                var posObj = Properties["position"] as JObject;
                if (posObj != null)
                {
                    return new Vec3(
                        posObj.Value<float>("x"),
                        posObj.Value<float>("y"),
                        posObj.Value<float>("z")
                    );
                }
            }
            return Vec3.Zero;
        }
        public string GetNodeName()
        {
            if (Properties != null && Properties.TryGetValue("name", out object nameObj))
                return nameObj?.ToString() ?? string.Empty;
            return string.Empty;
        }
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