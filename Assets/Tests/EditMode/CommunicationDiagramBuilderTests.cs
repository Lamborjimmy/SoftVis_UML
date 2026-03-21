using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Assets.Scripts.Builders;
using Assets.Scripts.Data;
using Assets.Tests.EditMode.Helpers;
using static Assets.Tests.EditMode.Helpers.TestDataFactory;

namespace Assets.Tests.EditMode
{
    public class CommunicationDiagramBuilderTests
    {
        private CommunicationDiagramBuilder builder;

        [SetUp]
        public void SetUp()
        {
            builder = new CommunicationDiagramBuilder(new MockTextMeasurer());
        }

        [Test]
        public void Build_SetsDiagramType()
        {
            var meta = MakeMetadata("g1", "CommDiag", DiagramTypes.COMMUNICATION_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("l1", DiagramNodeTypes.LIFELINE, name: "obj1"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "l1", DiagramEdgeTypes.NESTED),
            };

            var diagram = builder.Build(meta, nodes, edges);
            Assert.AreEqual(DiagramTypes.COMMUNICATION_DIAGRAM, diagram.DiagramType);
        }

        [Test]
        public void Build_ActorNode_UsesUniformScale()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.COMMUNICATION_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("a1", DiagramNodeTypes.ACTOR, name: "User"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "a1", DiagramEdgeTypes.NESTED),
            };

            var diagram = builder.Build(meta, nodes, edges);
            var actor = diagram.Nodes.First(n => n.Id == "a1");
            Assert.IsTrue(actor.UseUniformScale);
            Assert.IsTrue(actor.Labels.Any(l => l.Text == "User"));
        }

        [Test]
        public void Build_LifelineNode_HasLabel()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.COMMUNICATION_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("l1", DiagramNodeTypes.LIFELINE, name: "myObject"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "l1", DiagramEdgeTypes.NESTED),
            };

            var diagram = builder.Build(meta, nodes, edges);
            var lifeline = diagram.Nodes.First(n => n.Id == "l1");
            Assert.IsTrue(lifeline.Labels.Any(l => l.Text == "myObject"));
        }

        [Test]
        public void Build_LifelineNode_BackgroundColor_IsPeach()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.COMMUNICATION_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("l1", DiagramNodeTypes.LIFELINE, name: "obj"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "l1", DiagramEdgeTypes.NESTED),
            };

            var diagram = builder.Build(meta, nodes, edges);
            var lifeline = diagram.Nodes.First(n => n.Id == "l1");
            Assert.AreEqual(RGBA.Peach.R, lifeline.BackgroundColor.R, 0.01f);
            Assert.AreEqual(RGBA.Peach.G, lifeline.BackgroundColor.G, 0.01f);
        }

        [Test]
        public void Build_MessagesEdge_CreatesEdgeModel()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.COMMUNICATION_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("l1", DiagramNodeTypes.LIFELINE, name: "A"),
                MakeNode("l2", DiagramNodeTypes.LIFELINE, name: "B"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "l1", DiagramEdgeTypes.NESTED),
                MakeEdge("e2", "d1", "l2", DiagramEdgeTypes.NESTED),
                MakeEdge("e3", "l1", "l2", DiagramEdgeTypes.MESSAGES),
            };

            var diagram = builder.Build(meta, nodes, edges);
            Assert.AreEqual(1, diagram.Edges.Count);
            Assert.AreEqual("l1", diagram.Edges[0].FromId);
            Assert.AreEqual("l2", diagram.Edges[0].ToId);
        }

        [Test]
        public void Build_ExcludesRootDiagram()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.COMMUNICATION_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("l1", DiagramNodeTypes.LIFELINE, name: "obj"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "l1", DiagramEdgeTypes.NESTED),
            };

            var diagram = builder.Build(meta, nodes, edges);
            Assert.IsFalse(diagram.Nodes.Any(n => n.Id == "d1"));
        }
        [Test]
        public void Build_Nodes_ApplyDepthBasedElevation()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.COMMUNICATION_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("l1", DiagramNodeTypes.LIFELINE, y: 0, name: "TopLevel"),
            };
            var edges = new List<EdgeData> { MakeEdge("e1", "d1", "l1", DiagramEdgeTypes.NESTED) };

            var diagram = builder.Build(meta, nodes, edges);
            var lifeline = diagram.Nodes.First(n => n.Id == "l1");

            Assert.AreEqual(0.1f, lifeline.Position.Y, 0.001f);
        }
        [Test]
        public void Build_LifelineNode_EnforcesMinimumWidthAndExpands()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.COMMUNICATION_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("short", DiagramNodeTypes.LIFELINE, name: "A"),
                MakeNode("long", DiagramNodeTypes.LIFELINE, name: "VeryLongLifelineNameThatForcesExpansion"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "short", DiagramEdgeTypes.NESTED),
                MakeEdge("e2", "d1", "long", DiagramEdgeTypes.NESTED),
            };

            var diagram = builder.Build(meta, nodes, edges);
            var shortLifeline = diagram.Nodes.First(n => n.Id == "short");
            var longLifeline = diagram.Nodes.First(n => n.Id == "long");

            Assert.AreEqual(6f, shortLifeline.Scale.X, 0.001f);
            Assert.AreEqual(3f, shortLifeline.Scale.Z, 0.001f);

            Assert.Greater(longLifeline.Scale.X, 6f);
            Assert.AreEqual(3f, longLifeline.Scale.Z, 0.001f);
        }
        [Test]
        public void Build_ActorNode_SetsFixedScaleAndOffsetsLabel()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.COMMUNICATION_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("a1", DiagramNodeTypes.ACTOR, name: "User"),
            };
            var edges = new List<EdgeData> { MakeEdge("e1", "d1", "a1", DiagramEdgeTypes.NESTED) };

            var diagram = builder.Build(meta, nodes, edges);
            var actor = diagram.Nodes.First(n => n.Id == "a1");

            Assert.AreEqual(1f, actor.Scale.X, 0.001f);
            Assert.AreEqual(1f, actor.Scale.Z, 0.001f);

            var label = actor.Labels.First(l => l.Text == "User");
            Assert.AreEqual(2f, label.Position.Z, 0.001f);
        }
    }
}
