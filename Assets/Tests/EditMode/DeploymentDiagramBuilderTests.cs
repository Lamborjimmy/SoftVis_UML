using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Assets.Scripts.Builders;
using Assets.Scripts.Data;
using Assets.Tests.EditMode.Helpers;
using static Assets.Tests.EditMode.Helpers.TestDataFactory;

namespace Assets.Tests.EditMode
{
    public class DeploymentDiagramBuilderTests
    {
        private DeploymentDiagramBuilder builder;

        [SetUp]
        public void SetUp()
        {
            builder = new DeploymentDiagramBuilder(new MockTextMeasurer());
        }

        [Test]
        public void Build_SetsDiagramType()
        {
            var meta = MakeMetadata("g1", "DeployDiag", DiagramTypes.DEPLOYMENT_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("n1", DiagramNodeTypes.NODE, x: 5, z: 5, name: "Server"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "n1", DiagramEdgeTypes.NESTED),
            };

            var diagram = builder.Build(meta, nodes, edges);
            Assert.AreEqual(DiagramTypes.DEPLOYMENT_DIAGRAM, diagram.DiagramType);
        }

        [Test]
        public void Build_ExcludesRootDiagram()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.DEPLOYMENT_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("n1", DiagramNodeTypes.NODE, x: 5, z: 5, name: "Server"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "n1", DiagramEdgeTypes.NESTED),
            };

            var diagram = builder.Build(meta, nodes, edges);
            Assert.IsFalse(diagram.Nodes.Any(n => n.Id == "d1"));
        }

        [Test]
        public void Build_StandardNode_HasStereotypeAndNameLabels()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.DEPLOYMENT_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("n1", DiagramNodeTypes.NODE, x: 5, z: 5, name: "WebServer"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "n1", DiagramEdgeTypes.NESTED),
            };

            var diagram = builder.Build(meta, nodes, edges);
            var node = diagram.Nodes.First(n => n.Id == "n1");
            Assert.IsTrue(node.Labels.Any(l => l.Text == "<<node>>"));
            Assert.IsTrue(node.Labels.Any(l => l.Text == "WebServer"));
        }

        [Test]
        public void Build_StandardNode_BackgroundColor_IsLightBlue()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.DEPLOYMENT_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("n1", DiagramNodeTypes.NODE, x: 5, z: 5, name: "Server"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "n1", DiagramEdgeTypes.NESTED),
            };

            var diagram = builder.Build(meta, nodes, edges);
            var node = diagram.Nodes.First(n => n.Id == "n1");
            Assert.AreEqual(RGBA.LightBlue.R, node.BackgroundColor.R, 0.01f);
            Assert.AreEqual(RGBA.LightBlue.G, node.BackgroundColor.G, 0.01f);
        }

        [Test]
        public void Build_ComponentNode_HasComponentStereotype()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.DEPLOYMENT_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("comp1", DiagramNodeTypes.COMPONENT, x: 5, z: 5, name: "AppServer"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "comp1", DiagramEdgeTypes.NESTED),
            };

            var diagram = builder.Build(meta, nodes, edges);
            var comp = diagram.Nodes.First(n => n.Id == "comp1");
            Assert.IsTrue(comp.Labels.Any(l => l.Text == "<<component>>"));
        }

        [Test]
        public void Build_ArtifactNode_HasArtifactStereotype()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.DEPLOYMENT_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("art1", DiagramNodeTypes.ARTIFACT, x: 5, z: 5, name: "app.jar"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "art1", DiagramEdgeTypes.NESTED),
            };

            var diagram = builder.Build(meta, nodes, edges);
            var art = diagram.Nodes.First(n => n.Id == "art1");
            Assert.IsTrue(art.Labels.Any(l => l.Text == "<<artifact>>"));
            Assert.IsTrue(art.Labels.Any(l => l.Text == "app.jar"));
        }

        [Test]
        public void Build_InterfaceNode_UsesUniformScale()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.DEPLOYMENT_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("pi1", DiagramNodeTypes.PROVIDED_INTERFACE, x: 3, z: 3, name: "IService"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "pi1", DiagramEdgeTypes.NESTED),
            };

            var diagram = builder.Build(meta, nodes, edges);
            var iface = diagram.Nodes.First(n => n.Id == "pi1");
            Assert.IsTrue(iface.UseUniformScale);
            Assert.IsTrue(iface.Labels.Any(l => l.Text == "IService"));
        }

        [Test]
        public void Build_ContainerNode_HasStereotypeAndNameLabels()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.DEPLOYMENT_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("n1", DiagramNodeTypes.NODE, x: 0, z: 0, name: "Host"),
                MakeNode("art1", DiagramNodeTypes.ARTIFACT, x: 5, z: 5, name: "app.war"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "n1", DiagramEdgeTypes.NESTED),
                MakeEdge("e2", "n1", "art1", DiagramEdgeTypes.NESTED),
            };

            var diagram = builder.Build(meta, nodes, edges);
            var container = diagram.Nodes.First(n => n.Id == "n1");
            Assert.IsTrue(container.Labels.Any(l => l.Text == "<<node>>"));
            Assert.IsTrue(container.Labels.Any(l => l.Text == "Host"));
        }

        [Test]
        public void Build_DeployedOnEdge_CreatesEdge()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.DEPLOYMENT_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("n1", DiagramNodeTypes.NODE, x: 0, z: 0, name: "Server"),
                MakeNode("art1", DiagramNodeTypes.ARTIFACT, x: 10, z: 0, name: "app.jar"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "n1", DiagramEdgeTypes.NESTED),
                MakeEdge("e2", "d1", "art1", DiagramEdgeTypes.NESTED),
                MakeEdge("e3", "art1", "n1", DiagramEdgeTypes.DEPLOYED_ON),
            };

            var diagram = builder.Build(meta, nodes, edges);
            Assert.AreEqual(1, diagram.Edges.Count);
            Assert.AreEqual(DiagramEdgeTypes.DEPLOYED_ON, diagram.Edges[0].EdgeType);
        }

        [Test]
        public void Build_NestedDepth_AffectsPositionY()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.DEPLOYMENT_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("n1", DiagramNodeTypes.NODE, x: 0, z: 0, name: "Host"),
                MakeNode("n2", DiagramNodeTypes.NODE, x: 5, z: 5, name: "VM"),
                MakeNode("art1", DiagramNodeTypes.ARTIFACT, x: 10, z: 10, name: "app"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "n1", DiagramEdgeTypes.NESTED),
                MakeEdge("e2", "n1", "n2", DiagramEdgeTypes.NESTED),
                MakeEdge("e3", "n2", "art1", DiagramEdgeTypes.NESTED),
            };

            var diagram = builder.Build(meta, nodes, edges);

            var host = diagram.Nodes.First(n => n.Id == "n1");
            var vm = diagram.Nodes.First(n => n.Id == "n2");
            var art = diagram.Nodes.First(n => n.Id == "art1");

            Assert.Less(host.Position.Y, vm.Position.Y, "VM should have higher Y position than Host");
            Assert.Less(vm.Position.Y, art.Position.Y, "Artifact should have higher Y position than VM");
        }
        [Test]
        public void Build_StandardNode_VerifyExactElevation()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.DEPLOYMENT_DIAGRAM);
            var nodes = new List<NodeData> { MakeNode("d1", DiagramNodeTypes.DIAGRAM), MakeNode("n1", DiagramNodeTypes.NODE, y: 0) };
            var edges = new List<EdgeData> { MakeEdge("e1", "d1", "n1", DiagramEdgeTypes.NESTED) };

            var diagram = builder.Build(meta, nodes, edges);
            var node = diagram.Nodes.First(n => n.Id == "n1");

            Assert.AreEqual(0.2f, node.Position.Y, 0.001f);
        }
        [Test]
        public void Build_ContainerNode_AppliesMassivePadding()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.DEPLOYMENT_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("root", DiagramNodeTypes.DIAGRAM),
                MakeNode("parent", DiagramNodeTypes.NODE, name: "Server"),
                MakeNode("child", DiagramNodeTypes.ARTIFACT, x: 0, z: 0)
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "root", "parent", DiagramEdgeTypes.NESTED),
                MakeEdge("e2", "parent", "child", DiagramEdgeTypes.NESTED)
            };

            var diagram = builder.Build(meta, nodes, edges);
            var parent = diagram.Nodes.First(n => n.Id == "parent");

            Assert.AreEqual(12f, parent.Scale.X, 0.001f);
            Assert.AreEqual(8f, parent.Scale.Z, 0.001f);
        }
        [Test]
        public void Build_StandardNode_EnforcesScaleAndLabelOffsets()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.DEPLOYMENT_DIAGRAM);
            var nodes = new List<NodeData> { MakeNode("d1", DiagramNodeTypes.DIAGRAM), MakeNode("n1", DiagramNodeTypes.NODE, name: "PC") };
            var edges = new List<EdgeData> { MakeEdge("e1", "d1", "n1", DiagramEdgeTypes.NESTED) };

            var diagram = builder.Build(meta, nodes, edges);
            var node = diagram.Nodes.First(n => n.Id == "n1");

            Assert.AreEqual(6f, node.Scale.X, 0.001f);
            Assert.AreEqual(4f, node.Scale.Z, 0.001f);

            Assert.IsTrue(node.Labels.All(l => l.Position.Z <= 0.5f));
        }
        [Test]
        public void Build_InterfaceNode_IsFixedSquare()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.DEPLOYMENT_DIAGRAM);
            var nodes = new List<NodeData> { MakeNode("d1", DiagramNodeTypes.DIAGRAM), MakeNode("i1", DiagramNodeTypes.PROVIDED_INTERFACE) };
            var edges = new List<EdgeData> { MakeEdge("e1", "d1", "i1", DiagramEdgeTypes.NESTED) };

            var diagram = builder.Build(meta, nodes, edges);
            var iface = diagram.Nodes.First(n => n.Id == "i1");

            Assert.IsTrue(iface.UseUniformScale);
            Assert.AreEqual(1f, iface.Scale.X, 0.001f);
        }
    }
}
