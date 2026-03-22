using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Softviz.UML.Builders;
using Softviz.UML.Data;
using Softviz.Tests.EditMode.Helpers;
using static Softviz.Tests.EditMode.Helpers.TestDataFactory;

namespace Softviz.Tests.EditMode
{
    public class StateDiagramBuilderTests
    {
        private StateDiagramBuilder builder;

        [SetUp]
        public void SetUp()
        {
            builder = new StateDiagramBuilder(new MockTextMeasurer());
        }

        [Test]
        public void Build_SetsDiagramType()
        {
            var meta = MakeMetadata("g1", "StateDiag", DiagramTypes.STATE_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("s1", DiagramNodeTypes.STATE, name: "Idle"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "s1", DiagramEdgeTypes.NESTED),
            };

            var diagram = builder.Build(meta, nodes, edges);
            Assert.AreEqual(DiagramTypes.STATE_DIAGRAM, diagram.DiagramType);
        }

        [Test]
        public void Build_StateNode_HasLabel()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.STATE_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("s1", DiagramNodeTypes.STATE, name: "Running"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "s1", DiagramEdgeTypes.NESTED),
            };

            var diagram = builder.Build(meta, nodes, edges);
            var state = diagram.Nodes.First(n => n.Id == "s1");
            Assert.IsTrue(state.Labels.Any(l => l.Text == "Running"));
        }

        [Test]
        public void Build_StateNode_BackgroundColor_IsMistyRose()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.STATE_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("s1", DiagramNodeTypes.STATE, name: "Active"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "s1", DiagramEdgeTypes.NESTED),
            };

            var diagram = builder.Build(meta, nodes, edges);
            var state = diagram.Nodes.First(n => n.Id == "s1");
            Assert.AreEqual(RGBA.MistyRose.R, state.BackgroundColor.R, 0.01f);
            Assert.AreEqual(RGBA.MistyRose.G, state.BackgroundColor.G, 0.01f);
        }

        [Test]
        public void Build_PseudostateInitial_MapsToInitialType()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.STATE_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("ps1", DiagramNodeTypes.PSEUDOSTATE, name: "initial"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "ps1", DiagramEdgeTypes.NESTED),
            };

            var diagram = builder.Build(meta, nodes, edges);
            var pseudo = diagram.Nodes.First(n => n.Id == "ps1");
            Assert.AreEqual(DiagramNodeTypes.INITIAL, pseudo.NodeType);
            Assert.IsTrue(pseudo.UseUniformScale);
        }

        [Test]
        public void Build_PseudostateFinal_MapsToFinalType()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.STATE_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("ps1", DiagramNodeTypes.PSEUDOSTATE, name: "final"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "ps1", DiagramEdgeTypes.NESTED),
            };

            var diagram = builder.Build(meta, nodes, edges);
            var pseudo = diagram.Nodes.First(n => n.Id == "ps1");
            Assert.AreEqual(DiagramNodeTypes.FINAL, pseudo.NodeType);
        }

        [Test]
        public void Build_TransitionsToEdge_HasArrowDecorator()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.STATE_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("s1", DiagramNodeTypes.STATE, name: "A"),
                MakeNode("s2", DiagramNodeTypes.STATE, name: "B"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "s1", DiagramEdgeTypes.NESTED),
                MakeEdge("e2", "d1", "s2", DiagramEdgeTypes.NESTED),
                MakeEdge("e3", "s1", "s2", DiagramEdgeTypes.TRANSITIONS_TO),
            };

            var diagram = builder.Build(meta, nodes, edges);
            Assert.AreEqual(1, diagram.Edges.Count);
            Assert.AreEqual(DecoratorType.Arrow, diagram.Edges[0].EndDecorator);
        }

        [Test]
        public void Build_StateWithBehaviors_CreatesLabelsForMembers()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.STATE_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("s1", DiagramNodeTypes.STATE, name: "Active"),
                MakeNode("b1", DiagramNodeTypes.ACTION, name: "doSomething",
                    extraProps: new Dictionary<string, object> { { "behavior_type", "entry" } }),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "s1", DiagramEdgeTypes.NESTED),
                MakeEdge("e2", "s1", "b1", DiagramEdgeTypes.NESTED),
            };

            var diagram = builder.Build(meta, nodes, edges);
            var state = diagram.Nodes.First(n => n.Id == "s1");
            Assert.IsTrue(state.Labels.Count >= 2, "State with behaviors should have header + member labels");
            Assert.IsTrue(state.Labels.Any(l => l.Text.Contains("doSomething")));
        }
        [Test]
        public void Build_CompositeState_ExpandsToFitChildren()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.STATE_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("root", DiagramNodeTypes.DIAGRAM),
                MakeNode("parent", DiagramNodeTypes.STATE, name: "Outer"),
                MakeNode("child", DiagramNodeTypes.STATE, x: 5, z: 5, name: "Inner")
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "root", "parent", DiagramEdgeTypes.NESTED),
                MakeEdge("e2", "parent", "child", DiagramEdgeTypes.NESTED)
            };

            var diagram = builder.Build(meta, nodes, edges);
            var parent = diagram.Nodes.First(n => n.Id == "parent");

            Assert.AreEqual(10f, parent.Scale.X, 0.001f);
            Assert.AreEqual(8f, parent.Scale.Z, 0.001f);
        }
        [Test]
        public void Build_FiltersInternalBehaviorsFromNodeList()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.STATE_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("root", DiagramNodeTypes.DIAGRAM),
                MakeNode("s1", DiagramNodeTypes.STATE, name: "State"),
                MakeNode("b1", DiagramNodeTypes.ACTION, name: "entryAction",
                    extraProps: new Dictionary<string, object> { { "behavior_type", "entry" } })
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "root", "s1", DiagramEdgeTypes.NESTED),
                MakeEdge("e2", "s1", "b1", DiagramEdgeTypes.NESTED)
            };

            var diagram = builder.Build(meta, nodes, edges);

            Assert.AreEqual(1, diagram.Nodes.Count);
            Assert.AreEqual("s1", diagram.Nodes[0].Id);
        }
        [Test]
        public void Build_StateNode_ScalesWithBehaviorCount()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.STATE_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM),
                MakeNode("s1", DiagramNodeTypes.STATE, name: "S1"),
                MakeNode("b1", DiagramNodeTypes.ACTION, name: "b1", extraProps: new Dictionary<string, object> {{"behavior_type", "entry"}}),
                MakeNode("b2", DiagramNodeTypes.ACTION, name: "b2", extraProps: new Dictionary<string, object> {{"behavior_type", "exit"}})
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "s1", DiagramEdgeTypes.NESTED),
                MakeEdge("e2", "s1", "b1", DiagramEdgeTypes.NESTED),
                MakeEdge("e3", "s1", "b2", DiagramEdgeTypes.NESTED)
            };

            var diagram = builder.Build(meta, nodes, edges);
            var state = diagram.Nodes.First(n => n.Id == "s1");

            Assert.AreEqual(3.4f, state.Scale.Z, 0.001f);
        }
        [Test]
        public void Build_StateNode_FormatsBehaviorLabelsCorrectly()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.STATE_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM),
                MakeNode("s1", DiagramNodeTypes.STATE, name: "PowerOn"),
                MakeNode("b1", DiagramNodeTypes.ACTION, name: "init",
                    extraProps: new Dictionary<string, object> { { "behavior_type", "entry" } })
            };
            var edges = new List<EdgeData> { MakeEdge("e1", "d1", "s1", DiagramEdgeTypes.NESTED), MakeEdge("e2", "s1", "b1", DiagramEdgeTypes.NESTED) };

            var diagram = builder.Build(meta, nodes, edges);
            var state = diagram.Nodes.First(n => n.Id == "s1");

            Assert.IsTrue(state.Labels.Any(l => l.Text == "entry / init"));
        }

    }
}
