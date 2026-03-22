using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Softviz.UML.Builders;
using Softviz.UML.Data;
using Softviz.Tests.EditMode.Helpers;
using static Softviz.Tests.EditMode.Helpers.TestDataFactory;

namespace Softviz.Tests.EditMode
{
    public class ActivityDiagramBuilderTests
    {
        private ActivityDiagramBuilder builder;

        [SetUp]
        public void SetUp()
        {
            builder = new ActivityDiagramBuilder(new MockTextMeasurer());
        }

        [Test]
        public void Build_SetsDiagramType()
        {
            var meta = MakeMetadata("g1", "ActDiag", DiagramTypes.ACTIVITY_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("i1", DiagramNodeTypes.INITIAL, x: 0, z: 0, name: "initial"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "i1", DiagramEdgeTypes.NESTED),
            };

            var diagram = builder.Build(meta, nodes, edges);
            Assert.AreEqual(DiagramTypes.ACTIVITY_DIAGRAM, diagram.DiagramType);
        }

        [Test]
        public void Build_ExcludesRootDiagram()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.ACTIVITY_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("a1", DiagramNodeTypes.ACTION, x: 0, z: 0, name: "DoStuff"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "a1", DiagramEdgeTypes.NESTED),
            };

            var diagram = builder.Build(meta, nodes, edges);
            Assert.IsFalse(diagram.Nodes.Any(n => n.NodeType == DiagramNodeTypes.DIAGRAM));
        }

        [Test]
        public void Build_InitialNode_UsesUniformScale()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.ACTIVITY_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("i1", DiagramNodeTypes.INITIAL, x: 0, z: 0, name: "start"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "i1", DiagramEdgeTypes.NESTED),
            };

            var diagram = builder.Build(meta, nodes, edges);
            var initial = diagram.Nodes.First(n => n.NodeType == DiagramNodeTypes.INITIAL);
            Assert.IsTrue(initial.UseUniformScale);
        }

        [Test]
        public void Build_FinalNode_UsesUniformScale()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.ACTIVITY_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("i1", DiagramNodeTypes.INITIAL, x: 0, z: 0, name: "start"),
                MakeNode("f1", DiagramNodeTypes.FINAL, x: 10, z: 0, name: "end"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "i1", DiagramEdgeTypes.NESTED),
                MakeEdge("e2", "d1", "f1", DiagramEdgeTypes.NESTED),
                MakeEdge("e3", "i1", "f1", DiagramEdgeTypes.FLOWS_TO),
            };

            var diagram = builder.Build(meta, nodes, edges);
            var final_ = diagram.Nodes.First(n => n.NodeType == DiagramNodeTypes.FINAL);
            Assert.IsTrue(final_.UseUniformScale);
        }

        [Test]
        public void Build_ActionNode_HasLabel()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.ACTIVITY_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("i1", DiagramNodeTypes.INITIAL, x: 0, z: 0, name: "start"),
                MakeNode("a1", DiagramNodeTypes.ACTION, x: 5, z: 0, name: "Process Order"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "i1", DiagramEdgeTypes.NESTED),
                MakeEdge("e2", "d1", "a1", DiagramEdgeTypes.NESTED),
                MakeEdge("e3", "i1", "a1", DiagramEdgeTypes.FLOWS_TO),
            };

            var diagram = builder.Build(meta, nodes, edges);
            var action = diagram.Nodes.First(n => n.Id == "a1");
            Assert.IsTrue(action.Labels.Any(l => l.Text == "Process Order"));
        }

        [Test]
        public void Build_ActionNode_BackgroundColor_IsLavender()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.ACTIVITY_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("i1", DiagramNodeTypes.INITIAL, x: 0, z: 0, name: "start"),
                MakeNode("a1", DiagramNodeTypes.ACTION, x: 5, z: 0, name: "DoIt"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "i1", DiagramEdgeTypes.NESTED),
                MakeEdge("e2", "d1", "a1", DiagramEdgeTypes.NESTED),
                MakeEdge("e3", "i1", "a1", DiagramEdgeTypes.FLOWS_TO),
            };

            var diagram = builder.Build(meta, nodes, edges);
            var action = diagram.Nodes.First(n => n.Id == "a1");
            Assert.AreEqual(RGBA.Lavender.R, action.BackgroundColor.R, 0.01f);
            Assert.AreEqual(RGBA.Lavender.G, action.BackgroundColor.G, 0.01f);
        }

        [Test]
        public void Build_DecisionNode_UsesCorrectScale()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.ACTIVITY_DIAGRAM);
            var nodes = new List<NodeData> { MakeNode("d1", DiagramNodeTypes.DIAGRAM), MakeNode("dec1", DiagramNodeTypes.DECISION) };
            var edges = new List<EdgeData> { MakeEdge("e1", "d1", "dec1", DiagramEdgeTypes.NESTED) };

            var diagram = builder.Build(meta, nodes, edges);
            var decision = diagram.Nodes.First(n => n.Id == "dec1");

            Assert.IsTrue(decision.UseUniformScale);
            Assert.AreEqual(1f, decision.Scale.X, 0.001f);
            Assert.AreEqual(1f, decision.Scale.Z, 0.001f);
        }

        [Test]
        public void Build_ForkNode_UsesCorrectScaleAndColor()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.ACTIVITY_DIAGRAM);
            var nodes = new List<NodeData> { MakeNode("d1", DiagramNodeTypes.DIAGRAM), MakeNode("f1", DiagramNodeTypes.FORK) };
            var edges = new List<EdgeData> { MakeEdge("e1", "d1", "f1", DiagramEdgeTypes.NESTED) };

            var diagram = builder.Build(meta, nodes, edges);
            var fork = diagram.Nodes.First(n => n.Id == "f1");

            Assert.IsFalse(fork.UseUniformScale);
            Assert.AreEqual(0.5f, fork.Scale.X, 0.001f);
            Assert.AreEqual(3f, fork.Scale.Z, 0.001f);
            Assert.AreEqual(RGBA.Black.R, fork.BackgroundColor.R);
            Assert.AreEqual(RGBA.Black.G, fork.BackgroundColor.G);
            Assert.AreEqual(RGBA.Black.B, fork.BackgroundColor.B);
        }

        [Test]
        public void Build_JoinNode_UsesCorrectScaleAndColor()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.ACTIVITY_DIAGRAM);
            var nodes = new List<NodeData> { MakeNode("d1", DiagramNodeTypes.DIAGRAM), MakeNode("j1", DiagramNodeTypes.JOIN) };
            var edges = new List<EdgeData> { MakeEdge("e1", "d1", "j1", DiagramEdgeTypes.NESTED) };

            var diagram = builder.Build(meta, nodes, edges);
            var joinNode = diagram.Nodes.First(n => n.Id == "j1");

            Assert.IsFalse(joinNode.UseUniformScale);
            Assert.AreEqual(0.5f, joinNode.Scale.X, 0.001f);
            Assert.AreEqual(3f, joinNode.Scale.Z, 0.001f);
            Assert.AreEqual(RGBA.Black.R, joinNode.BackgroundColor.R);
            Assert.AreEqual(RGBA.Black.G, joinNode.BackgroundColor.G);
            Assert.AreEqual(RGBA.Black.B, joinNode.BackgroundColor.B);
        }

        [Test]
        public void Build_FlowsToEdge_HasArrowDecorator()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.ACTIVITY_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("i1", DiagramNodeTypes.INITIAL, x: 0, z: 0, name: "start"),
                MakeNode("a1", DiagramNodeTypes.ACTION, x: 10, z: 0, name: "Act"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "i1", DiagramEdgeTypes.NESTED),
                MakeEdge("e2", "d1", "a1", DiagramEdgeTypes.NESTED),
                MakeEdge("e3", "i1", "a1", DiagramEdgeTypes.FLOWS_TO),
            };

            var diagram = builder.Build(meta, nodes, edges);
            Assert.AreEqual(1, diagram.Edges.Count);
            Assert.AreEqual(DecoratorType.Arrow, diagram.Edges[0].EndDecorator);
        }

        [Test]
        public void Build_SwimlaneNode_HasSwimlaneLabels()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.ACTIVITY_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("sw1", DiagramNodeTypes.SWIMLANE, x: 0, z: 0, name: "Lane1"),
                MakeNode("i1", DiagramNodeTypes.INITIAL, x: 0, z: 0, name: "start"),
                MakeNode("a1", DiagramNodeTypes.ACTION, x: 10, z: 0, name: "Act"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "sw1", DiagramEdgeTypes.NESTED),
                MakeEdge("e2", "sw1", "i1", DiagramEdgeTypes.NESTED),
                MakeEdge("e3", "sw1", "a1", DiagramEdgeTypes.NESTED),
                MakeEdge("e4", "i1", "a1", DiagramEdgeTypes.FLOWS_TO),
            };

            var diagram = builder.Build(meta, nodes, edges);
            var swimlane = diagram.Nodes.FirstOrDefault(n => n.Id == "sw1");
            Assert.IsNotNull(swimlane);
            Assert.IsTrue(swimlane.Labels.Any(l => l.Text == "<<swimlane>>"));
            Assert.IsTrue(swimlane.Labels.Any(l => l.Text == "Lane1"));
        }
        [Test]
        public void Build_ApplyRankBasedSpacing_CalculatesXPositionsCorrectly()
        {
            var meta = MakeMetadata("g1", "SpacingTest", DiagramTypes.ACTIVITY_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("root", DiagramNodeTypes.DIAGRAM),
                MakeNode("i1", DiagramNodeTypes.INITIAL, x: 0, z: 0),
                MakeNode("a1", DiagramNodeTypes.ACTION, x: 0, z: 0),
                MakeNode("f1", DiagramNodeTypes.FINAL, x: 0, z: 0)
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "root", "i1", DiagramEdgeTypes.NESTED),
                MakeEdge("e2", "root", "a1", DiagramEdgeTypes.NESTED),
                MakeEdge("e3", "root", "f1", DiagramEdgeTypes.NESTED),
                MakeEdge("flow1", "i1", "a1", DiagramEdgeTypes.FLOWS_TO),
                MakeEdge("flow2", "a1", "f1", DiagramEdgeTypes.FLOWS_TO)
            };

            var diagram = builder.Build(meta, nodes, edges);

            var initial = diagram.Nodes.First(n => n.Id == "i1");
            var action = diagram.Nodes.First(n => n.Id == "a1");
            var finalNode = diagram.Nodes.First(n => n.Id == "f1");

            Assert.AreEqual(0f, initial.Position.X, 0.001f);
            Assert.AreEqual(9f, action.Position.X, 0.001f);
            Assert.AreEqual(27f, finalNode.Position.X, 0.001f);
        }
        [Test]
        public void Build_ObjectFlowEdge_HasArrowDecorator()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.ACTIVITY_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("root", DiagramNodeTypes.DIAGRAM),
                MakeNode("n1", DiagramNodeTypes.ACTION),
                MakeNode("n2", DiagramNodeTypes.ACTION)
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "root", "n1", DiagramEdgeTypes.NESTED),
                MakeEdge("e2", "root", "n2", DiagramEdgeTypes.NESTED),
                MakeEdge("flow1", "n1", "n2", DiagramEdgeTypes.OBJECT_FLOW)
            };

            var diagram = builder.Build(meta, nodes, edges);

            Assert.AreEqual(1, diagram.Edges.Count);
            Assert.AreEqual(DecoratorType.Arrow, diagram.Edges[0].EndDecorator);
        }
        [Test]
        public void Build_ContainerNode_AddsPaddingAndTopLabel()
        {
            var meta = MakeMetadata("g1", "ContainerTest", DiagramTypes.ACTIVITY_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("root", DiagramNodeTypes.DIAGRAM),
                MakeNode("parentAction", DiagramNodeTypes.ACTION, name: "Outer Container"),
                MakeNode("childAction", DiagramNodeTypes.ACTION, x: 0, z: 0)
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "root", "parentAction", DiagramEdgeTypes.NESTED),
                MakeEdge("e2", "parentAction", "childAction", DiagramEdgeTypes.NESTED)
            };

            var diagram = builder.Build(meta, nodes, edges);
            var container = diagram.Nodes.First(n => n.Id == "parentAction");

            Assert.GreaterOrEqual(container.Scale.X, 4.0f);
            Assert.GreaterOrEqual(container.Scale.Z, 4.0f);

            var label = container.Labels.FirstOrDefault(l => l.Text == "Outer Container");
            Assert.IsNotNull(label);
            Assert.AreEqual(TextAlignment.Top, label.Alignment);
            Assert.AreEqual(FontStyle.Bold, label.Style);
        }
    }
}
