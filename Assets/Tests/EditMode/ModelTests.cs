using NUnit.Framework;
using Softviz.UML.Data;
using Softviz.UML.Models;

namespace Softviz.Tests.EditMode
{
    public class DiagramModelTests
    {
        [Test]
        public void Constructor_InitializesEmptyLists()
        {
            var diagram = new DiagramModel();
            Assert.IsNotNull(diagram.Nodes);
            Assert.IsNotNull(diagram.Edges);
            Assert.IsNotNull(diagram.EdgeHubs);
            Assert.AreEqual(0, diagram.Nodes.Count);
            Assert.AreEqual(0, diagram.Edges.Count);
            Assert.AreEqual(0, diagram.EdgeHubs.Count);
        }

        [Test]
        public void Constructor_DefaultBasePlaneColor_IsGray()
        {
            var diagram = new DiagramModel();
            Assert.AreEqual(RGBA.Gray.R, diagram.BasePlaneColor.R, 0.001f);
            Assert.AreEqual(RGBA.Gray.G, diagram.BasePlaneColor.G, 0.001f);
            Assert.AreEqual(RGBA.Gray.B, diagram.BasePlaneColor.B, 0.001f);
        }
    }

    public class NodeModelTests
    {
        [Test]
        public void Constructor_InitializesEmptyLists()
        {
            var node = new NodeModel();
            Assert.IsNotNull(node.ChildKeys);
            Assert.IsNotNull(node.Members);
            Assert.IsNotNull(node.Labels);
            Assert.AreEqual(0, node.ChildKeys.Count);
            Assert.AreEqual(0, node.Members.Count);
            Assert.AreEqual(0, node.Labels.Count);
        }

        [Test]
        public void Constructor_DefaultColors_AreBlack()
        {
            var node = new NodeModel();
            Assert.AreEqual(RGBA.Black.R, node.BackgroundColor.R);
            Assert.AreEqual(RGBA.Black.G, node.BackgroundColor.G);
            Assert.AreEqual(RGBA.Black.B, node.BackgroundColor.B);
            Assert.AreEqual(RGBA.Black.R, node.TextColor.R);
        }
    }

    public class EdgeModelTests
    {
        [Test]
        public void Constructor_InitializesDefaults()
        {
            var edge = new EdgeModel();
            Assert.IsNotNull(edge.Waypoints);
            Assert.AreEqual(0, edge.Waypoints.Count);
            Assert.AreEqual(0.04f, edge.LineWidth, 0.001f);
            Assert.AreEqual(DecoratorType.None, edge.StartDecorator);
            Assert.AreEqual(DecoratorType.None, edge.EndDecorator);
        }

        [Test]
        public void Constructor_DefaultLineColor_IsWhite()
        {
            var edge = new EdgeModel();
            Assert.AreEqual(RGBA.White.R, edge.LineColor.R);
            Assert.AreEqual(RGBA.White.G, edge.LineColor.G);
            Assert.AreEqual(RGBA.White.B, edge.LineColor.B);
        }
        [Test]
        public void Constructor_InitializesTextStringsToEmpty()
        {
            var edge = new EdgeModel();

            Assert.IsNotNull(edge.LabelText);
            Assert.AreEqual(string.Empty, edge.LabelText);

            Assert.IsNotNull(edge.MultiplicitySource);
            Assert.AreEqual(string.Empty, edge.MultiplicitySource);

            Assert.IsNotNull(edge.MultiplicityTarget);
            Assert.AreEqual(string.Empty, edge.MultiplicityTarget);
        }
    }

    public class TextLabelModelTests
    {
        [Test]
        public void Constructor_DefaultColor_IsBlack()
        {
            var label = new TextLabelModel();
            Assert.AreEqual(RGBA.Black.R, label.Color.R);
        }

        [Test]
        public void Constructor_DefaultStyle_IsNormal()
        {
            var label = new TextLabelModel();
            Assert.AreEqual(FontStyle.Normal, label.Style);
        }

        [Test]
        public void Constructor_DefaultAlignment_IsCenter()
        {
            var label = new TextLabelModel();
            Assert.AreEqual(TextAlignment.Center, label.Alignment);
        }
    }

    public class EdgeHubModelTests
    {
        [Test]
        public void Constructor_InitializesEmptyList()
        {
            var hub = new EdgeHubModel();
            Assert.IsNotNull(hub.IncomingEdgeKeys);
            Assert.AreEqual(0, hub.IncomingEdgeKeys.Count);
        }
    }
    public class MemberModelTests
    {
        [Test]
        public void MemberModel_CanStoreProperties()
        {
            var member = new MemberModel
            {
                Id = "m1",
                Text = "+ GetName(): string",
                MemberType = "Method",
                FontSize = 12f,
                Alignment = TextAlignment.Left
            };

            Assert.AreEqual("m1", member.Id);
            Assert.AreEqual("+ GetName(): string", member.Text);
            Assert.AreEqual("Method", member.MemberType);
            Assert.AreEqual(12f, member.FontSize);
            Assert.AreEqual(TextAlignment.Left, member.Alignment);
        }
    }
}
