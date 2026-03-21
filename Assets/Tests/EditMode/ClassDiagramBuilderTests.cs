using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Assets.Scripts.Builders;
using Assets.Scripts.Data;
using Assets.Tests.EditMode.Helpers;
using static Assets.Tests.EditMode.Helpers.TestDataFactory;

namespace Assets.Tests.EditMode
{
    public class ClassDiagramBuilderTests
    {
        private ClassDiagramBuilder builder;
        private MockTextMeasurer mockMeasurer;

        [SetUp]
        public void SetUp()
        {
            mockMeasurer = new MockTextMeasurer();
            builder = new ClassDiagramBuilder(mockMeasurer);
        }

        [Test]
        public void Build_SetsDiagramMetadata()
        {
            var meta = MakeMetadata("g1", "ClassDiag", DiagramTypes.CLASS_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("c1", DiagramNodeTypes.CLASS, name: "MyClass"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "c1", DiagramEdgeTypes.NESTED),
            };

            var diagram = builder.Build(meta, nodes, edges);

            Assert.AreEqual("g1", diagram.Id);
            Assert.AreEqual("ClassDiag", diagram.Name);
            Assert.AreEqual(DiagramTypes.CLASS_DIAGRAM, diagram.DiagramType);
        }

        [Test]
        public void Build_ExcludesRootDiagramNode()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.CLASS_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("c1", DiagramNodeTypes.CLASS, name: "MyClass"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "c1", DiagramEdgeTypes.NESTED),
            };

            var diagram = builder.Build(meta, nodes, edges);

            Assert.IsFalse(diagram.Nodes.Any(n => n.Id == "d1"));
            Assert.IsTrue(diagram.Nodes.Any(n => n.Id == "c1"));
        }

        [Test]
        public void Build_SimpleClass_CreatesNodeWithLabel()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.CLASS_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("c1", DiagramNodeTypes.CLASS, x: 5, z: 10, name: "Person"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "c1", DiagramEdgeTypes.NESTED),
            };

            var diagram = builder.Build(meta, nodes, edges);

            var node = diagram.Nodes.First(n => n.Id == "c1");
            Assert.AreEqual("Person", node.Label);
            Assert.AreEqual(DiagramNodeTypes.CLASS, node.NodeType);
            Assert.IsTrue(node.Labels.Count > 0);
            Assert.AreEqual("Person", node.Labels[0].Text);
        }

        [Test]
        public void Build_ClassWithMembers_AddsMembers()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.CLASS_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("c1", DiagramNodeTypes.CLASS, name: "Person"),
                MakeNode("a1", DiagramNodeTypes.ATTRIBUTE, name: "age",
                    extraProps: new Dictionary<string, object> { { "type_name", "int" } }),
                MakeNode("m1", DiagramNodeTypes.METHOD, name: "getName"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "c1", DiagramEdgeTypes.NESTED),
                MakeEdge("e2", "c1", "a1", DiagramEdgeTypes.NESTED),
                MakeEdge("e3", "c1", "m1", DiagramEdgeTypes.NESTED),
            };

            var diagram = builder.Build(meta, nodes, edges);

            var classNode = diagram.Nodes.First(n => n.Id == "c1");
            Assert.AreEqual(2, classNode.Members.Count);

            var attr = classNode.Members.First(m => m.MemberType == "attribute");
            Assert.IsTrue(attr.Text.Contains("age"));
            Assert.IsTrue(attr.Text.Contains("int"));

            var method = classNode.Members.First(m => m.MemberType == "method");
            Assert.IsTrue(method.Text.Contains("getName"));
            Assert.IsTrue(method.Text.Contains("()"));
        }

        [Test]
        public void Build_Interface_SetsStereotypeLabelAndCreates3DLabel()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.CLASS_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("i1", DiagramNodeTypes.INTERFACE, name: "ISerializable"),
            };
            var edges = new List<EdgeData> { MakeEdge("e1", "d1", "i1", DiagramEdgeTypes.NESTED) };

            var diagram = builder.Build(meta, nodes, edges);
            var iface = diagram.Nodes.First(n => n.Id == "i1");

            Assert.AreEqual("<<interface>>", iface.StereotypeLabel);

            Assert.IsTrue(iface.Labels.Any(l => l.Text == "<<interface>>"));
        }

        [Test]
        public void Build_Enumeration_SetsStereotypeLabelAndCreates3DLabel()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.CLASS_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("en1", DiagramNodeTypes.ENUMERATION, name: "Color"),
            };
            var edges = new List<EdgeData> { MakeEdge("e1", "d1", "en1", DiagramEdgeTypes.NESTED) };

            var diagram = builder.Build(meta, nodes, edges);
            var enumNode = diagram.Nodes.First(n => n.Id == "en1");

            Assert.AreEqual("<<enumeration>>", enumNode.StereotypeLabel);

            Assert.IsTrue(enumNode.Labels.Any(l => l.Text == "<<enumeration>>"));
        }

        [Test]
        public void Build_GeneralizationEdge_ProducesCorrectEdgeModel()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.CLASS_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("c1", DiagramNodeTypes.CLASS, name: "Animal"),
                MakeNode("c2", DiagramNodeTypes.CLASS, name: "Dog"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "c1", DiagramEdgeTypes.NESTED),
                MakeEdge("e2", "d1", "c2", DiagramEdgeTypes.NESTED),
                MakeEdge("e3", "c2", "c1", DiagramEdgeTypes.GENERALIZES),
            };

            var diagram = builder.Build(meta, nodes, edges);

            Assert.AreEqual(1, diagram.Edges.Count);
            var edge = diagram.Edges[0];
            Assert.AreEqual("c2", edge.FromId);
            Assert.AreEqual("c1", edge.ToId);
            Assert.AreEqual(DecoratorType.Triangle, edge.EndDecorator);
            Assert.IsFalse(edge.IsDashed);
        }

        [Test]
        public void Build_MultipleClasses_CreatesAllNodes()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.CLASS_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("c1", DiagramNodeTypes.CLASS, name: "A"),
                MakeNode("c2", DiagramNodeTypes.CLASS, name: "B"),
                MakeNode("c3", DiagramNodeTypes.CLASS, name: "C"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "c1", DiagramEdgeTypes.NESTED),
                MakeEdge("e2", "d1", "c2", DiagramEdgeTypes.NESTED),
                MakeEdge("e3", "d1", "c3", DiagramEdgeTypes.NESTED),
            };

            var diagram = builder.Build(meta, nodes, edges);

            Assert.AreEqual(3, diagram.Nodes.Count);
        }

        [Test]
        public void Build_NodePosition_IncludesElevation()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.CLASS_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("c1", DiagramNodeTypes.CLASS, x: 5, y: 0, z: 10, name: "Cls"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "c1", DiagramEdgeTypes.NESTED),
            };

            var diagram = builder.Build(meta, nodes, edges);
            var node = diagram.Nodes[0];
            Assert.Greater(node.Position.Y, 0f, "Y should include elevation offset");
        }

        [Test]
        public void Build_ClassNode_BackgroundColor_IsSoftYellow()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.CLASS_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("c1", DiagramNodeTypes.CLASS, name: "Cls"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "c1", DiagramEdgeTypes.NESTED),
            };

            var diagram = builder.Build(meta, nodes, edges);
            var node = diagram.Nodes[0];
            Assert.AreEqual(RGBA.SoftYellow.R, node.BackgroundColor.R, 0.01f);
            Assert.AreEqual(RGBA.SoftYellow.G, node.BackgroundColor.G, 0.01f);
            Assert.AreEqual(RGBA.SoftYellow.B, node.BackgroundColor.B, 0.01f);
        }
        [Test]
        public void Build_ExcludesNodesNestedInsideClasses()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.CLASS_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("c1", DiagramNodeTypes.CLASS, name: "ParentClass"),
                MakeNode("c2", DiagramNodeTypes.CLASS, name: "NestedClass"),
                MakeNode("m1", DiagramNodeTypes.METHOD, name: "DoWork")
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "c1", DiagramEdgeTypes.NESTED),
                MakeEdge("e2", "c1", "c2", DiagramEdgeTypes.NESTED),
                MakeEdge("e3", "c1", "m1", DiagramEdgeTypes.NESTED)
            };

            var diagram = builder.Build(meta, nodes, edges);

            Assert.AreEqual(1, diagram.Nodes.Count);
            Assert.AreEqual("c1", diagram.Nodes[0].Id);
        }
        [Test]
        public void Build_CalculateClassDimensions_ScalesWithMembersAndStereotypes()
        {
            var meta = MakeMetadata("g1", "Test", DiagramTypes.CLASS_DIAGRAM);
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("c1", DiagramNodeTypes.CLASS, name: "Short"),
                MakeNode("c2", DiagramNodeTypes.CLASS, name: "VeryLongClassNameThatExpandsWidth"),
                MakeNode("i1", DiagramNodeTypes.INTERFACE, name: "Short"),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "c1", DiagramEdgeTypes.NESTED),
                MakeEdge("e2", "d1", "c2", DiagramEdgeTypes.NESTED),
                MakeEdge("e3", "d1", "i1", DiagramEdgeTypes.NESTED),
            };

            var diagram = builder.Build(meta, nodes, edges);

            var standardClass = diagram.Nodes.First(n => n.Id == "c1");
            var wideClass = diagram.Nodes.First(n => n.Id == "c2");
            var interfaceClass = diagram.Nodes.First(n => n.Id == "i1");

            Assert.Greater(wideClass.Scale.X, standardClass.Scale.X);

            Assert.Greater(interfaceClass.Scale.Z, standardClass.Scale.Z);
        }
    }
}
