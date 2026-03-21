using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Assets.Scripts.Data;

namespace Assets.Tests.EditMode.Helpers
{
    public static class TestDataFactory
    {
        public static NodeData MakeNode(string key, string type, float x = 0, float y = 0, float z = 0, string name = null, Dictionary<string, object> extraProps = null)
        {
            var props = new Dictionary<string, object>();
            if (name != null) props["name"] = name;
            props["position"] = new JObject { ["x"] = x, ["y"] = y, ["z"] = z };
            if (extraProps != null)
            {
                foreach (var kvp in extraProps)
                    props[kvp.Key] = kvp.Value;
            }
            return new NodeData { Key = key, Type = type, Properties = props };
        }

        public static EdgeData MakeEdge(string key, string from, string to, string type)
        {
            return new EdgeData { Key = key, From = from, To = to, Type = type };
        }

        public static GraphMetadata MakeMetadata(string key, string name, string graphType)
        {
            return new GraphMetadata { Key = key, Name = name, GraphType = graphType };
        }
    }
}
