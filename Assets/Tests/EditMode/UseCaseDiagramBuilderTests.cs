using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Softviz.UML.Builders;
using Softviz.UML.Data;
using Softviz.Tests.EditMode.Helpers;
using static Softviz.Tests.EditMode.Helpers.TestDataFactory;

namespace Assets.Tests.EditMode
{
    public class UseCaseDiagramBuilderTests
    {
        private UseCaseDiagramBuilder builder;

        [SetUp]
        public void SetUp()
        {
            builder = new UseCaseDiagramBuilder(new MockTextMeasurer());
        }

        [Test]
        public void Build_SetsDiagramType()
        {
            var meta = MakeMetadata("g1", "UCDiag", DiagramTypes.USECASE_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("uc1", DiagramNodeTypes.USECASE, name: "Login"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "uc1", DiagramEdgeTypes.NESTED),
            };

            var diagram = builder.Build(meta, nodes, edges);
            Assert.AreEqual(DiagramTypes.USECASE_DIAGRAM, diagram.DiagramType);
        }

        [Test]
        public void Build_ActorNode_UsesUniformScale()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.USECASE_DIAGRAM);
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
            Assert.AreEqual(DiagramNodeTypes.ACTOR, actor.NodeType);
        }

        [Test]
        public void Build_ActorNode_HasLabel()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.USECASE_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("a1", DiagramNodeTypes.ACTOR, name: "Admin"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "a1", DiagramEdgeTypes.NESTED),
            };

            var diagram = builder.Build(meta, nodes, edges);
            var actor = diagram.Nodes.First(n => n.Id == "a1");
            Assert.IsTrue(actor.Labels.Any(l => l.Text == "Admin"));
        }

        [Test]
        public void Build_UseCaseNode_HasLabel()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.USECASE_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("uc1", DiagramNodeTypes.USECASE, name: "Login"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "uc1", DiagramEdgeTypes.NESTED),
            };

            var diagram = builder.Build(meta, nodes, edges);
            var uc = diagram.Nodes.First(n => n.Id == "uc1");
            Assert.IsTrue(uc.Labels.Count > 0);
        }

        [Test]
        public void Build_UseCaseNode_BackgroundColor_IsThistle()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.USECASE_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("uc1", DiagramNodeTypes.USECASE, name: "Login"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "uc1", DiagramEdgeTypes.NESTED),
            };

            var diagram = builder.Build(meta, nodes, edges);
            var uc = diagram.Nodes.First(n => n.Id == "uc1");
            Assert.AreEqual(RGBA.Thistle.R, uc.BackgroundColor.R, 0.01f);
        }

        [Test]
        public void Build_ContainerNode_HasLabel()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.USECASE_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("sys", DiagramNodeTypes.PACKAGE, x: 0, z: 0, name: "System"),
                MakeNode("uc1", DiagramNodeTypes.USECASE, x: 5, z: 5, name: "Login"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "sys", DiagramEdgeTypes.NESTED),
                MakeEdge("e2", "sys", "uc1", DiagramEdgeTypes.NESTED),
            };

            var diagram = builder.Build(meta, nodes, edges);
            var container = diagram.Nodes.First(n => n.Id == "sys");
            Assert.IsTrue(container.Labels.Any(l => l.Text == "System"));
        }

        [Test]
        public void Build_IncludesUmlEdge_IsDashedWithArrow()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.USECASE_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("uc1", DiagramNodeTypes.USECASE, name: "A"),
                MakeNode("uc2", DiagramNodeTypes.USECASE, name: "B"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "uc1", DiagramEdgeTypes.NESTED),
                MakeEdge("e2", "d1", "uc2", DiagramEdgeTypes.NESTED),
                MakeEdge("e3", "uc1", "uc2", DiagramEdgeTypes.INCLUDES_UML),
            };

            var diagram = builder.Build(meta, nodes, edges);
            Assert.AreEqual(1, diagram.Edges.Count);
            Assert.IsTrue(diagram.Edges[0].IsDashed);
            Assert.AreEqual(DecoratorType.Arrow, diagram.Edges[0].EndDecorator);
        }

        [Test]
        public void Build_ExtendsUmlEdge_IsDashedWithArrow()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.USECASE_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("uc1", DiagramNodeTypes.USECASE, name: "Base"),
                MakeNode("uc2", DiagramNodeTypes.USECASE, name: "Extended"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "uc1", DiagramEdgeTypes.NESTED),
                MakeEdge("e2", "d1", "uc2", DiagramEdgeTypes.NESTED),
                MakeEdge("e3", "uc2", "uc1", DiagramEdgeTypes.EXTENDS_UML),
            };

            var diagram = builder.Build(meta, nodes, edges);
            Assert.AreEqual(1, diagram.Edges.Count);
            Assert.IsTrue(diagram.Edges[0].IsDashed);
        }

        [Test]
        public void Build_SkipsNonActorNonUseCaseLeafNodes()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.USECASE_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("c1", DiagramNodeTypes.CLASS, name: "ShouldBeSkipped"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "c1", DiagramEdgeTypes.NESTED),
            };

            var diagram = builder.Build(meta, nodes, edges);
            Assert.AreEqual(0, diagram.Nodes.Count);
        }
        [Test]
        public void Build_UseCaseNode_FormatsExtensionPointsAndExpandsScale()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.USECASE_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("root", DiagramNodeTypes.DIAGRAM),
                MakeNode("base", DiagramNodeTypes.USECASE, name: "Login"),
                MakeNode("ext1", DiagramNodeTypes.USECASE, name: "TwoFactor"),
                MakeNode("ext2", DiagramNodeTypes.USECASE, name: "SMS"),
                MakeNode("ext3", DiagramNodeTypes.USECASE, name: "Email")
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "root", "base", DiagramEdgeTypes.NESTED),
                MakeEdge("e2", "ext1", "base", DiagramEdgeTypes.EXTENDS_UML),
                MakeEdge("e3", "ext2", "base", DiagramEdgeTypes.EXTENDS_UML),
                MakeEdge("e4", "ext3", "base", DiagramEdgeTypes.EXTENDS_UML)
            };

            var diagram = builder.Build(meta, nodes, edges);
            var baseUseCase = diagram.Nodes.First(n => n.Id == "base");

            var label = baseUseCase.Labels.First();
            Assert.IsTrue(label.Text.Contains("TwoFactor"));
            Assert.IsTrue(label.Text.Contains("SMS"));
            Assert.IsTrue(label.Text.Contains("Email"));

            Assert.AreEqual(4.0f, baseUseCase.Scale.Z, 0.001f);
        }
        [Test]
        public void Build_SystemBoundary_AppliesMassivePadding()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.USECASE_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("root", DiagramNodeTypes.DIAGRAM),
                MakeNode("system", DiagramNodeTypes.NODE, name: "ATM"),
                MakeNode("uc1", DiagramNodeTypes.USECASE, x: 0, z: 0)
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "root", "system", DiagramEdgeTypes.NESTED),
                MakeEdge("e2", "system", "uc1", DiagramEdgeTypes.NESTED)
            };

            var diagram = builder.Build(meta, nodes, edges);
            var system = diagram.Nodes.First(n => n.Id == "system");

            Assert.AreEqual(12.0f, system.Scale.X, 0.001f);
        }
        [Test]
        public void Build_ActorNode_HasSpecificLabelOffset()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.USECASE_DIAGRAM);
            var nodes = new List<NodeData> { MakeNode("d1", DiagramNodeTypes.DIAGRAM), MakeNode("a1", DiagramNodeTypes.ACTOR, name: "User") };
            var edges = new List<EdgeData> { MakeEdge("e1", "d1", "a1", DiagramEdgeTypes.NESTED) };

            var diagram = builder.Build(meta, nodes, edges);
            var actor = diagram.Nodes.First(n => n.Id == "a1");

            var label = actor.Labels.First();
            Assert.AreEqual(2.0f, label.Position.Z, 0.001f);
        }
    }
}
