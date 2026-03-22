using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Softviz.UML.Builders;
using Softviz.UML.Data;
using Softviz.Tests.EditMode.Helpers;
using static Softviz.Tests.EditMode.Helpers.TestDataFactory;

namespace Softviz.Tests.EditMode
{
    public class ComponentDiagramBuilderTests
    {
        private ComponentDiagramBuilder builder;

        [SetUp]
        public void SetUp()
        {
            builder = new ComponentDiagramBuilder(new MockTextMeasurer());
        }

        [Test]
        public void Build_SetsDiagramType()
        {
            var meta = MakeMetadata("g1", "CompDiag", DiagramTypes.COMPONENT_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("comp1", DiagramNodeTypes.COMPONENT, x: 5, z: 5, name: "Auth"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "comp1", DiagramEdgeTypes.NESTED),
            };

            var diagram = builder.Build(meta, nodes, edges);
            Assert.AreEqual(DiagramTypes.COMPONENT_DIAGRAM, diagram.DiagramType);
        }

        [Test]
        public void Build_ExcludesRootDiagramNode()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.COMPONENT_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("comp1", DiagramNodeTypes.COMPONENT, x: 5, z: 5, name: "Auth"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "comp1", DiagramEdgeTypes.NESTED),
            };

            var diagram = builder.Build(meta, nodes, edges);
            Assert.IsFalse(diagram.Nodes.Any(n => n.Id == "d1"));
        }

        [Test]
        public void Build_ComponentNode_HasStereotypeAndNameLabels()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.COMPONENT_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("comp1", DiagramNodeTypes.COMPONENT, x: 5, z: 5, name: "AuthService"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "comp1", DiagramEdgeTypes.NESTED),
            };

            var diagram = builder.Build(meta, nodes, edges);
            var comp = diagram.Nodes.First(n => n.Id == "comp1");
            Assert.IsTrue(comp.Labels.Any(l => l.Text == "<<component>>"));
            Assert.IsTrue(comp.Labels.Any(l => l.Text == "AuthService"));
        }

        [Test]
        public void Build_ComponentNode_BackgroundColor_IsMint()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.COMPONENT_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("comp1", DiagramNodeTypes.COMPONENT, x: 5, z: 5, name: "Svc"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "comp1", DiagramEdgeTypes.NESTED),
            };

            var diagram = builder.Build(meta, nodes, edges);
            var comp = diagram.Nodes.First(n => n.Id == "comp1");
            Assert.AreEqual(RGBA.Mint.R, comp.BackgroundColor.R, 0.01f);
            Assert.AreEqual(RGBA.Mint.G, comp.BackgroundColor.G, 0.01f);
        }

        [Test]
        public void Build_InterfaceNode_UsesUniformScale()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.COMPONENT_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("pi1", DiagramNodeTypes.PROVIDED_INTERFACE, x: 3, z: 3, name: "IAuth"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "pi1", DiagramEdgeTypes.NESTED),
            };

            var diagram = builder.Build(meta, nodes, edges);
            var iface = diagram.Nodes.First(n => n.Id == "pi1");
            Assert.IsTrue(iface.UseUniformScale);
            Assert.IsTrue(iface.Labels.Any(l => l.Text == "IAuth"));
        }

        [Test]
        public void Build_PortNode_IsSmall()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.COMPONENT_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("comp1", DiagramNodeTypes.COMPONENT, x: 0, z: 0, name: "Svc"),
                MakeNode("port1", DiagramNodeTypes.PORT, x: 3, z: 0, name: "p1"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "comp1", DiagramEdgeTypes.NESTED),
                MakeEdge("e2", "comp1", "port1", DiagramEdgeTypes.NESTED),
            };

            var diagram = builder.Build(meta, nodes, edges);
            var port = diagram.Nodes.First(n => n.Id == "port1");
            Assert.AreEqual(DiagramNodeTypes.PORT, port.NodeType);
            Assert.AreEqual(0, port.Labels.Count, "Port should have no labels");
        }

        [Test]
        public void Build_ContainerComponent_HasStereotypeLabel()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.COMPONENT_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("comp1", DiagramNodeTypes.COMPONENT, x: 0, z: 0, name: "Parent"),
                MakeNode("comp2", DiagramNodeTypes.COMPONENT, x: 5, z: 5, name: "Child"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "comp1", DiagramEdgeTypes.NESTED),
                MakeEdge("e2", "comp1", "comp2", DiagramEdgeTypes.NESTED),
            };

            var diagram = builder.Build(meta, nodes, edges);
            var container = diagram.Nodes.First(n => n.Id == "comp1");
            Assert.AreEqual("<<component>>", container.StereotypeLabel);
            Assert.IsTrue(container.Labels.Any(l => l.Text == "<<component>>"));
            Assert.IsTrue(container.Labels.Any(l => l.Text == "Parent"));
        }

        [Test]
        public void Build_ActorNode_UsesUniformScale()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.COMPONENT_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("a1", DiagramNodeTypes.ACTOR, x: 0, z: 0, name: "Admin"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "a1", DiagramEdgeTypes.NESTED),
            };

            var diagram = builder.Build(meta, nodes, edges);
            var actor = diagram.Nodes.First(n => n.Id == "a1");
            Assert.IsTrue(actor.UseUniformScale);
            Assert.IsTrue(actor.Labels.Any(l => l.Text == "Admin"));
        }

        [Test]
        public void Build_DependencyEdge_IsDashed()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.COMPONENT_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("comp1", DiagramNodeTypes.COMPONENT, x: 0, z: 0, name: "A"),
                MakeNode("comp2", DiagramNodeTypes.COMPONENT, x: 10, z: 0, name: "B"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "comp1", DiagramEdgeTypes.NESTED),
                MakeEdge("e2", "d1", "comp2", DiagramEdgeTypes.NESTED),
                MakeEdge("e3", "comp1", "comp2", DiagramEdgeTypes.DEPENDENCY),
            };

            var diagram = builder.Build(meta, nodes, edges);
            Assert.AreEqual(1, diagram.Edges.Count);
            Assert.IsTrue(diagram.Edges[0].IsDashed);
        }
        [Test]
        public void Build_Ports_SnapToParentPerimeter()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.COMPONENT_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("root", DiagramNodeTypes.DIAGRAM),
                MakeNode("comp", DiagramNodeTypes.COMPONENT, name: "Comp"),
                MakeNode("dummy", DiagramNodeTypes.COMPONENT, x: 0, z: 0),
                MakeNode("port", DiagramNodeTypes.PORT, x: 1, z: 1)
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "root", "comp", DiagramEdgeTypes.NESTED),
                MakeEdge("e2", "comp", "dummy", DiagramEdgeTypes.NESTED),
                MakeEdge("e3", "comp", "port", DiagramEdgeTypes.NESTED)
            };

            var diagram = builder.Build(meta, nodes, edges);
            var port = diagram.Nodes.First(n => n.Id == "port");
            var comp = diagram.Nodes.First(n => n.Id == "comp");

            float dx = System.Math.Abs(port.Position.X - comp.Bounds.Center.X);
            float dz = System.Math.Abs(port.Position.Z - comp.Bounds.Center.Z);

            bool isOnEdgeX = System.Math.Abs(dx - comp.Bounds.Extents.X) < 0.01f;
            bool isOnEdgeZ = System.Math.Abs(dz - comp.Bounds.Extents.Z) < 0.01f;

            Assert.IsTrue(isOnEdgeX || isOnEdgeZ, "Port did not snap to the perimeter of the component.");
        }
        [Test]
        public void Build_Ports_RepelEachOtherWhenOverlapping()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.COMPONENT_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("root", DiagramNodeTypes.DIAGRAM),
                MakeNode("comp", DiagramNodeTypes.COMPONENT, x: 0, z: 0),
                MakeNode("p1", DiagramNodeTypes.PORT, x: 5, z: 0),
                MakeNode("p2", DiagramNodeTypes.PORT, x: 5, z: 0)
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "root", "comp", DiagramEdgeTypes.NESTED),
                MakeEdge("e2", "comp", "p1", DiagramEdgeTypes.NESTED),
                MakeEdge("e3", "comp", "p2", DiagramEdgeTypes.NESTED)
            };

            var diagram = builder.Build(meta, nodes, edges);
            var p1 = diagram.Nodes.First(n => n.Id == "p1");
            var p2 = diagram.Nodes.First(n => n.Id == "p2");

            float distance = (p1.Position - p2.Position).Magnitude;
            Assert.Greater(distance, 0.5f, "Ports should be pushed apart by at least the minPortSpacing threshold.");
        }
        [Test]
        public void Build_ContainerNode_AppliesPaddingToChildren()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.COMPONENT_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("root", DiagramNodeTypes.DIAGRAM),
                MakeNode("container", DiagramNodeTypes.COMPONENT, name: "System"),
                MakeNode("child", DiagramNodeTypes.COMPONENT, x: 0, z: 0, name: "SubSys")
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "root", "container", DiagramEdgeTypes.NESTED),
                MakeEdge("e2", "container", "child", DiagramEdgeTypes.NESTED)
            };

            var diagram = builder.Build(meta, nodes, edges);
            var container = diagram.Nodes.First(n => n.Id == "container");
            var child = diagram.Nodes.First(n => n.Id == "child");

            float expectedMinWidth = child.Scale.X + 2.0f;
            Assert.GreaterOrEqual(container.Scale.X, expectedMinWidth);
        }
    }
}
