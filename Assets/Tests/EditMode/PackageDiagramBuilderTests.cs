using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Softviz.UML.Builders;
using Softviz.UML.Data;
using Softviz.Tests.EditMode.Helpers;
using static Softviz.Tests.EditMode.Helpers.TestDataFactory;

namespace Softviz.Tests.EditMode
{
    public class PackageDiagramBuilderTests
    {
        private PackageDiagramBuilder builder;

        [SetUp]
        public void SetUp()
        {
            builder = new PackageDiagramBuilder(new MockTextMeasurer());
        }

        [Test]
        public void Build_SetsDiagramType()
        {
            var meta = MakeMetadata("g1", "PkgDiag", DiagramTypes.PACKAGE_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("p1", DiagramNodeTypes.PACKAGE, x: 0, z: 0, name: "core"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "p1", DiagramEdgeTypes.NESTED),
            };

            var diagram = builder.Build(meta, nodes, edges);
            Assert.AreEqual(DiagramTypes.PACKAGE_DIAGRAM, diagram.DiagramType);
        }

        [Test]
        public void Build_ExcludesRootDiagram()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.PACKAGE_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("p1", DiagramNodeTypes.PACKAGE, name: "core"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "p1", DiagramEdgeTypes.NESTED),
            };

            var diagram = builder.Build(meta, nodes, edges);
            Assert.IsFalse(diagram.Nodes.Any(n => n.Id == "d1"));
        }

        [Test]
        public void Build_ContainerPackage_HasLabel()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.PACKAGE_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("p1", DiagramNodeTypes.PACKAGE, x: 0, z: 0, name: "parent"),
                MakeNode("c1", DiagramNodeTypes.CLASS, x: 5, z: 5, name: "Child"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "p1", DiagramEdgeTypes.NESTED),
                MakeEdge("e2", "p1", "c1", DiagramEdgeTypes.NESTED),
            };

            var diagram = builder.Build(meta, nodes, edges);

            var container = diagram.Nodes.First(n => n.Id == "p1");
            Assert.IsTrue(container.Labels.Count > 0);
            Assert.AreEqual("parent", container.Labels[0].Text);
        }

        [Test]
        public void Build_LeafPackage_HasLabel()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.PACKAGE_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("p1", DiagramNodeTypes.PACKAGE, name: "leaf"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "p1", DiagramEdgeTypes.NESTED),
            };

            var diagram = builder.Build(meta, nodes, edges);
            var pkg = diagram.Nodes.First(n => n.Id == "p1");
            Assert.IsTrue(pkg.Labels.Any(l => l.Text == "leaf"));
        }

        [Test]
        public void Build_NestedDepth_AffectsPositionY()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.PACKAGE_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("p1", DiagramNodeTypes.PACKAGE, x: 0, z: 0, name: "outer"),
                MakeNode("p2", DiagramNodeTypes.PACKAGE, x: 5, z: 5, name: "inner"),
                MakeNode("c1", DiagramNodeTypes.CLASS, x: 10, z: 10, name: "Leaf"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "p1", DiagramEdgeTypes.NESTED),
                MakeEdge("e2", "p1", "p2", DiagramEdgeTypes.NESTED),
                MakeEdge("e3", "p2", "c1", DiagramEdgeTypes.NESTED),
            };

            var diagram = builder.Build(meta, nodes, edges);

            var outer = diagram.Nodes.First(n => n.Id == "p1");
            var inner = diagram.Nodes.First(n => n.Id == "p2");
            var leaf = diagram.Nodes.First(n => n.Id == "c1");

            Assert.Less(outer.Position.Y, inner.Position.Y, "Inner package should have higher Y position than outer");
            Assert.Less(inner.Position.Y, leaf.Position.Y, "Leaf should have higher Y position than inner");
        }

        [Test]
        public void Build_DependencyEdge_BuildsCorrectly()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.PACKAGE_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("p1", DiagramNodeTypes.PACKAGE, name: "A"),
                MakeNode("p2", DiagramNodeTypes.PACKAGE, name: "B"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "p1", DiagramEdgeTypes.NESTED),
                MakeEdge("e2", "d1", "p2", DiagramEdgeTypes.NESTED),
                MakeEdge("e3", "p1", "p2", DiagramEdgeTypes.DEPENDENCY),
            };

            var diagram = builder.Build(meta, nodes, edges);

            Assert.AreEqual(1, diagram.Edges.Count);
            Assert.IsTrue(diagram.Edges[0].IsDashed);
            Assert.AreEqual(DecoratorType.Arrow, diagram.Edges[0].EndDecorator);
        }
        [Test]
        public void Build_StandardNode_VerifyElevationIsCalculatedExactly()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.PACKAGE_DIAGRAM);
            var nodes = new List<NodeData> { MakeNode("d1", DiagramNodeTypes.DIAGRAM), MakeNode("n1", "CLASS", y: 0) };
            var edges = new List<EdgeData> { MakeEdge("e1", "d1", "n1", DiagramEdgeTypes.NESTED) };

            var diagram = builder.Build(meta, nodes, edges);
            var node = diagram.Nodes.First(n => n.Id == "n1");

            Assert.AreEqual(0.2f, node.Position.Y, 0.001f);
        }
        [Test]
        public void Build_PackageNode_IncludesTabInScale()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.PACKAGE_DIAGRAM);
            var nodes = new List<NodeData> { MakeNode("d1", DiagramNodeTypes.DIAGRAM), MakeNode("p1", DiagramNodeTypes.PACKAGE, name: "pkg") };
            var edges = new List<EdgeData> { MakeEdge("e1", "d1", "p1", DiagramEdgeTypes.NESTED) };

            var diagram = builder.Build(meta, nodes, edges);
            var pkg = diagram.Nodes.First(n => n.Id == "p1");

            Assert.AreEqual(4.5f, pkg.Scale.Z, 0.001f);

            var label = pkg.Labels.First();
            Assert.AreEqual(-0.75f, label.Position.Z, 0.001f);
        }
        [Test]
        public void Build_ContainerPackage_AppliesPaddingToChildren()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.PACKAGE_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("root", DiagramNodeTypes.DIAGRAM),
                MakeNode("parent", DiagramNodeTypes.PACKAGE, name: "Outer"),
                MakeNode("child", DiagramNodeTypes.PACKAGE, x: 0, z: 0, name: "Inner")
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "root", "parent", DiagramEdgeTypes.NESTED),
                MakeEdge("e2", "parent", "child", DiagramEdgeTypes.NESTED)
            };

            var diagram = builder.Build(meta, nodes, edges);
            var parent = diagram.Nodes.First(n => n.Id == "parent");
            var child = diagram.Nodes.First(n => n.Id == "child");

            Assert.AreEqual(8.0f, parent.Scale.X, 0.001f);

            Assert.AreEqual(10.5f, parent.Scale.Z, 0.001f);
        }
    }
}
