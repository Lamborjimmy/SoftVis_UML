using System.Collections.Generic;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using Assets.Scripts.Data;

namespace Assets.Tests.EditMode
{
    public class NodeDataTests
    {
        [Test]
        public void GetNodeName_WithNameProperty_ReturnsName()
        {
            var node = new NodeData
            {
                Key = "n1",
                Type = DiagramNodeTypes.CLASS,
                Properties = new Dictionary<string, object> { { "name", "MyClass" } }
            };
            Assert.AreEqual("MyClass", node.GetNodeName());
        }

        [Test]
        public void GetNodeName_WithNullProperties_ReturnsEmpty()
        {
            var node = new NodeData { Key = "n1", Properties = null };
            Assert.AreEqual(string.Empty, node.GetNodeName());
        }

        [Test]
        public void GetNodeName_WithMissingNameKey_ReturnsEmpty()
        {
            var node = new NodeData
            {
                Key = "n1",
                Properties = new Dictionary<string, object> { { "type", "SomeType" } }
            };
            Assert.AreEqual(string.Empty, node.GetNodeName());
        }

        [Test]
        public void GetNodeName_WithNullNameValue_ReturnsEmpty()
        {
            var node = new NodeData
            {
                Key = "n1",
                Properties = new Dictionary<string, object> { { "name", null } }
            };
            Assert.AreEqual(string.Empty, node.GetNodeName());
        }

        [Test]
        public void GetNodePosition_WithValidPosition_ReturnsVec3()
        {
            var posObj = new JObject
            {
                ["x"] = 1.5f,
                ["y"] = 2.5f,
                ["z"] = 3.5f
            };
            var node = new NodeData
            {
                Key = "n1",
                Properties = new Dictionary<string, object> { { "position", posObj } }
            };

            var pos = node.GetNodePosition();
            Assert.AreEqual(1.5f, pos.X, 0.001f);
            Assert.AreEqual(2.5f, pos.Y, 0.001f);
            Assert.AreEqual(3.5f, pos.Z, 0.001f);
        }

        [Test]
        public void GetNodePosition_WithNullProperties_ReturnsZero()
        {
            var node = new NodeData { Key = "n1", Properties = null };
            var pos = node.GetNodePosition();
            Assert.AreEqual(0f, pos.X);
            Assert.AreEqual(0f, pos.Y);
            Assert.AreEqual(0f, pos.Z);
        }

        [Test]
        public void GetNodePosition_WithMissingPositionKey_ReturnsZero()
        {
            var node = new NodeData
            {
                Key = "n1",
                Properties = new Dictionary<string, object> { { "name", "Test" } }
            };
            var pos = node.GetNodePosition();
            Assert.AreEqual(0f, pos.X);
            Assert.AreEqual(0f, pos.Y);
            Assert.AreEqual(0f, pos.Z);
        }

        [Test]
        public void GetNodePosition_WithNonJObjectPosition_ReturnsZero()
        {
            var node = new NodeData
            {
                Key = "n1",
                Properties = new Dictionary<string, object> { { "position", "not_a_jobject" } }
            };
            var pos = node.GetNodePosition();
            Assert.AreEqual(0f, pos.X);
            Assert.AreEqual(0f, pos.Y);
            Assert.AreEqual(0f, pos.Z);
        }
    }

    public class EdgeDataTests
    {
        [Test]
        public void EdgeData_CanStoreFromAndTo()
        {
            var edge = new EdgeData
            {
                Key = "e1",
                From = "n1",
                To = "n2",
                Type = DiagramEdgeTypes.GENERALIZES
            };
            Assert.AreEqual("e1", edge.Key);
            Assert.AreEqual("n1", edge.From);
            Assert.AreEqual("n2", edge.To);
            Assert.AreEqual(DiagramEdgeTypes.GENERALIZES, edge.Type);
        }

        [Test]
        public void EdgeData_SelfLoop_HasSameFromAndTo()
        {
            var edge = new EdgeData
            {
                Key = "e1",
                From = "n1",
                To = "n1",
                Type = DiagramEdgeTypes.ASSOCIATED_WITH
            };
            Assert.AreEqual(edge.From, edge.To);
        }
    }

    public class GraphMetadataTests
    {
        [Test]
        public void GraphMetadata_CanStoreAllFields()
        {
            var meta = new GraphMetadata
            {
                Key = "g1",
                Name = "Test Graph",
                UpdatedAt = "2025-01-01",
                GraphType = DiagramTypes.CLASS_DIAGRAM
            };
            Assert.AreEqual("g1", meta.Key);
            Assert.AreEqual("Test Graph", meta.Name);
            Assert.AreEqual("2025-01-01", meta.UpdatedAt);
            Assert.AreEqual(DiagramTypes.CLASS_DIAGRAM, meta.GraphType);
        }
    }
    public class PipelineTests
    {
        [Test]
        public void Constructor_DefaultReturnMode_IsAssigned()
        {
            var pipeline = new Pipeline();

            Assert.IsNotNull(pipeline.ReturnMode);
            Assert.AreEqual(PipelineReturnModes.SUBGRAPH, pipeline.ReturnMode);
        }

        [Test]
        public void Pipeline_CanStoreSteps()
        {
            var pipeline = new Pipeline
            {
                Steps = new List<PipelineStep> { new PipelineStep { Type = "Filter" } }
            };

            Assert.IsNotNull(pipeline.Steps);
            Assert.AreEqual(1, pipeline.Steps.Count);
            Assert.AreEqual("Filter", pipeline.Steps[0].Type);
        }
    }

    public class PipelineStepTests
    {
        [Test]
        public void PipelineStep_CanStoreTypeAndParams()
        {
            var step = new PipelineStep
            {
                Type = "Traversal",
                Params = new Dictionary<string, object> { { "depth", 2 }, { "direction", "outbound" } }
            };

            Assert.AreEqual("Traversal", step.Type);
            Assert.IsNotNull(step.Params);
            Assert.AreEqual(2, step.Params["depth"]);
            Assert.AreEqual("outbound", step.Params["direction"]);
        }
    }
}

