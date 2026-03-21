using System.Collections.Generic;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using Assets.Scripts.Builders;
using Assets.Scripts.Data;
using Assets.Scripts.Models;
using Assets.Tests.EditMode.Helpers;

namespace Assets.Tests.EditMode
{
    public class TestableDiagramBuilder : BaseDiagramBuilder
    {
        public TestableDiagramBuilder(ITextMeasurer textMeasurer) : base(textMeasurer) { }

        public override DiagramModel Build(GraphMetadata metadata, List<NodeData> nodes, List<EdgeData> edges)
        {
            return new DiagramModel { Id = metadata.Key, Name = metadata.Name };
        }

        public NestingContext TestBuildNestingHierarchy(List<NodeData> nodes, List<EdgeData> edges)
            => BuildNestingHierarchy(nodes, edges);

        public List<EdgeModel> TestBuildEdgeModels(List<EdgeData> edges)
            => BuildEdgeModels(edges);

        public NodeModel TestBuildNodeModel(NodeData node, Vec3 basePosition, float width, float height, RGBA color, RGBA textColor, float elevation, bool useUniformScale)
            => BuildNodeModel(node, basePosition, width, height, color, textColor, elevation, useUniformScale);

        public TextLabelModel TestCreateLabel(string text, Vec3 localPos, float width, float fontSize, RGBA color, TextAlignment alignment = TextAlignment.Center, FontStyle style = FontStyle.Normal)
            => CreateLabel(text, localPos, width, fontSize, color, alignment, style);

        public RGBA TestGetNodeColorByDepth(int depth)
            => GetNodeColorByDepth(depth);

        public float TestMeasureText(string text, float fontSize, bool isBold = false)
            => MeasureText(text, fontSize, isBold);
        public void TestGetRecursiveBounds(string parentKey, Dictionary<string, List<NodeData>> parentToChildren, out float minX, out float maxX, out float minZ, out float maxZ, Dictionary<string, Vec3> positions = null) => GetRecursiveBounds(parentKey, parentToChildren, out minX, out maxX, out minZ, out maxZ, positions);
    }

    public class NestingContextTests
    {
        private MockTextMeasurer mockMeasurer;
        private TestableDiagramBuilder builder;

        [SetUp]
        public void SetUp()
        {
            mockMeasurer = new MockTextMeasurer();
            builder = new TestableDiagramBuilder(mockMeasurer);
        }

        private NodeData MakeNode(string key, string type, float x = 0, float y = 0, float z = 0, string name = null)
        {
            var props = new Dictionary<string, object>();
            if (name != null) props["name"] = name;
            props["position"] = new JObject { ["x"] = x, ["y"] = y, ["z"] = z };
            return new NodeData { Key = key, Type = type, Properties = props };
        }

        private EdgeData MakeEdge(string key, string from, string to, string type)
        {
            return new EdgeData { Key = key, From = from, To = to, Type = type };
        }

        [Test]
        public void BuildNestingHierarchy_NoEdges_EmptyContext()
        {
            var nodes = new List<NodeData> { MakeNode("n1", DiagramNodeTypes.CLASS) };
            var edges = new List<EdgeData>();

            var ctx = builder.TestBuildNestingHierarchy(nodes, edges);

            Assert.AreEqual(0, ctx.ParentToChildren.Count);
            Assert.AreEqual(0, ctx.ChildToParent.Count);
            Assert.AreEqual(0, ctx.NestedChildKeys.Count);
        }

        [Test]
        public void BuildNestingHierarchy_FindsRootDiagram()
        {
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM, name: "Root"),
                MakeNode("c1", DiagramNodeTypes.CLASS),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "c1", DiagramEdgeTypes.NESTED)
            };

            var ctx = builder.TestBuildNestingHierarchy(nodes, edges);

            Assert.IsNotNull(ctx.RootDiagram);
            Assert.AreEqual("d1", ctx.RootDiagram.Key);
        }

        [Test]
        public void BuildNestingHierarchy_TracksParentChildRelations()
        {
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM),
                MakeNode("p1", DiagramNodeTypes.PACKAGE),
                MakeNode("c1", DiagramNodeTypes.CLASS),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "p1", DiagramEdgeTypes.NESTED),
                MakeEdge("e2", "p1", "c1", DiagramEdgeTypes.NESTED),
            };

            var ctx = builder.TestBuildNestingHierarchy(nodes, edges);

            Assert.IsTrue(ctx.ParentToChildren.ContainsKey("d1"));
            Assert.IsTrue(ctx.ParentToChildren.ContainsKey("p1"));
            Assert.AreEqual("p1", ctx.ParentToChildren["d1"][0].Key);
            Assert.AreEqual("c1", ctx.ParentToChildren["p1"][0].Key);
            Assert.IsTrue(ctx.NestedChildKeys.Contains("p1"));
            Assert.IsTrue(ctx.NestedChildKeys.Contains("c1"));
        }

        [Test]
        public void GetDepth_RootChild_ReturnsZero()
        {
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM),
                MakeNode("c1", DiagramNodeTypes.CLASS),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "c1", DiagramEdgeTypes.NESTED)
            };

            var ctx = builder.TestBuildNestingHierarchy(nodes, edges);
            Assert.AreEqual(0, ctx.GetDepth("c1"));
        }

        [Test]
        public void GetDepth_NestedChild_ReturnsCorrectDepth()
        {
            var nodes = new List<NodeData>
            {
                MakeNode("d1", DiagramNodeTypes.DIAGRAM),
                MakeNode("p1", DiagramNodeTypes.PACKAGE),
                MakeNode("c1", DiagramNodeTypes.CLASS),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "d1", "p1", DiagramEdgeTypes.NESTED),
                MakeEdge("e2", "p1", "c1", DiagramEdgeTypes.NESTED),
            };

            var ctx = builder.TestBuildNestingHierarchy(nodes, edges);
            Assert.AreEqual(1, ctx.GetDepth("c1"));
        }

        [Test]
        public void IsContainer_WithChildren_ReturnsTrue()
        {
            var nodes = new List<NodeData>
            {
                MakeNode("p1", DiagramNodeTypes.PACKAGE),
                MakeNode("c1", DiagramNodeTypes.CLASS),
            };
            var edges = new List<EdgeData>
            {
                MakeEdge("e1", "p1", "c1", DiagramEdgeTypes.NESTED)
            };

            var ctx = builder.TestBuildNestingHierarchy(nodes, edges);
            Assert.IsTrue(ctx.IsContainer("p1"));
        }

        [Test]
        public void IsContainer_WithNoChildren_ReturnsFalse()
        {
            var nodes = new List<NodeData>
            {
                MakeNode("c1", DiagramNodeTypes.CLASS),
            };
            var edges = new List<EdgeData>();

            var ctx = builder.TestBuildNestingHierarchy(nodes, edges);
            Assert.IsFalse(ctx.IsContainer("c1"));
        }
    }

    public class EdgeBuildingTests
    {
        private TestableDiagramBuilder builder;

        [SetUp]
        public void SetUp()
        {
            builder = new TestableDiagramBuilder(new MockTextMeasurer());
        }

        [Test]
        public void BuildEdgeModels_ExcludesNestedEdges()
        {
            var edges = new List<EdgeData>
            {
                new EdgeData { Key = "e1", From = "n1", To = "n2", Type = DiagramEdgeTypes.NESTED },
                new EdgeData { Key = "e2", From = "n1", To = "n3", Type = DiagramEdgeTypes.GENERALIZES },
            };

            var result = builder.TestBuildEdgeModels(edges);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("e2", result[0].Id);
        }

        [Test]
        public void BuildEdgeModels_SetsSelfLoop_WhenFromEqualsTo()
        {
            var edges = new List<EdgeData>
            {
                new EdgeData { Key = "e1", From = "n1", To = "n1", Type = DiagramEdgeTypes.ASSOCIATED_WITH },
            };

            var result = builder.TestBuildEdgeModels(edges);
            Assert.IsTrue(result[0].IsSelfLoop);
        }

        [Test]
        public void BuildEdgeModels_NotSelfLoop_WhenFromDiffersTo()
        {
            var edges = new List<EdgeData>
            {
                new EdgeData { Key = "e1", From = "n1", To = "n2", Type = DiagramEdgeTypes.ASSOCIATED_WITH },
            };

            var result = builder.TestBuildEdgeModels(edges);
            Assert.IsFalse(result[0].IsSelfLoop);
        }

        [Test]
        public void BuildEdgeModels_DashedEdge_ForIncludesUml()
        {
            var edges = new List<EdgeData>
            {
                new EdgeData { Key = "e1", From = "n1", To = "n2", Type = DiagramEdgeTypes.INCLUDES_UML },
            };
            var result = builder.TestBuildEdgeModels(edges);
            Assert.IsTrue(result[0].IsDashed);
        }

        [Test]
        public void BuildEdgeModels_DashedEdge_ForExtendsUml()
        {
            var edges = new List<EdgeData>
            {
                new EdgeData { Key = "e1", From = "n1", To = "n2", Type = DiagramEdgeTypes.EXTENDS_UML },
            };
            var result = builder.TestBuildEdgeModels(edges);
            Assert.IsTrue(result[0].IsDashed);
        }

        [Test]
        public void BuildEdgeModels_DashedEdge_ForDependency()
        {
            var edges = new List<EdgeData>
            {
                new EdgeData { Key = "e1", From = "n1", To = "n2", Type = DiagramEdgeTypes.DEPENDENCY },
            };
            var result = builder.TestBuildEdgeModels(edges);
            Assert.IsTrue(result[0].IsDashed);
        }

        [Test]
        public void BuildEdgeModels_NotDashed_ForGeneralizes()
        {
            var edges = new List<EdgeData>
            {
                new EdgeData { Key = "e1", From = "n1", To = "n2", Type = DiagramEdgeTypes.GENERALIZES },
            };
            var result = builder.TestBuildEdgeModels(edges);
            Assert.IsFalse(result[0].IsDashed);
        }

        [Test]
        public void BuildEdgeModels_StartDecorator_DiamondHollow_ForAggregates()
        {
            var edges = new List<EdgeData>
            {
                new EdgeData { Key = "e1", From = "n1", To = "n2", Type = DiagramEdgeTypes.AGGREGATES },
            };
            var result = builder.TestBuildEdgeModels(edges);
            Assert.AreEqual(DecoratorType.DiamondHollow, result[0].StartDecorator);
        }

        [Test]
        public void BuildEdgeModels_StartDecorator_DiamondFilled_ForComposes()
        {
            var edges = new List<EdgeData>
            {
                new EdgeData { Key = "e1", From = "n1", To = "n2", Type = DiagramEdgeTypes.COMPOSES },
            };
            var result = builder.TestBuildEdgeModels(edges);
            Assert.AreEqual(DecoratorType.DiamondFilled, result[0].StartDecorator);
        }

        [Test]
        public void BuildEdgeModels_EndDecorator_Triangle_ForGeneralizes()
        {
            var edges = new List<EdgeData>
            {
                new EdgeData { Key = "e1", From = "n1", To = "n2", Type = DiagramEdgeTypes.GENERALIZES },
            };
            var result = builder.TestBuildEdgeModels(edges);
            Assert.AreEqual(DecoratorType.Triangle, result[0].EndDecorator);
        }

        [Test]
        public void BuildEdgeModels_EndDecorator_Arrow_ForFlowsTo()
        {
            var edges = new List<EdgeData>
            {
                new EdgeData { Key = "e1", From = "n1", To = "n2", Type = DiagramEdgeTypes.FLOWS_TO },
            };
            var result = builder.TestBuildEdgeModels(edges);
            Assert.AreEqual(DecoratorType.Arrow, result[0].EndDecorator);
        }

        [Test]
        public void BuildEdgeModels_EndDecorator_Arrow_ForTransitionsTo()
        {
            var edges = new List<EdgeData>
            {
                new EdgeData { Key = "e1", From = "n1", To = "n2", Type = DiagramEdgeTypes.TRANSITIONS_TO },
            };
            var result = builder.TestBuildEdgeModels(edges);
            Assert.AreEqual(DecoratorType.Arrow, result[0].EndDecorator);
        }

        [Test]
        public void BuildEdgeModels_NoDecorators_ForAssociatedWith()
        {
            var edges = new List<EdgeData>
            {
                new EdgeData { Key = "e1", From = "n1", To = "n2", Type = DiagramEdgeTypes.ASSOCIATED_WITH },
            };
            var result = builder.TestBuildEdgeModels(edges);
            Assert.AreEqual(DecoratorType.None, result[0].StartDecorator);
            Assert.AreEqual(DecoratorType.None, result[0].EndDecorator);
        }

        [Test]
        public void BuildEdgeModels_EmptyList_ReturnsEmpty()
        {
            var result = builder.TestBuildEdgeModels(new List<EdgeData>());
            Assert.AreEqual(0, result.Count);
        }
        [Test]
        public void BuildEdgeModels_ParsesProperties_LabelAndMultiplicity()
        {
            var edges = new List<EdgeData>
        {
            new EdgeData
            {
                Key = "e1", From = "n1", To = "n2", Type = DiagramEdgeTypes.ASSOCIATED_WITH,
                Properties = new Dictionary<string, object>
                {
                    { "label", "creates" },
                    { "multiplicity_source", "1" },
                    { "multiplicity_target", "0..*" }
                }
            }
        };
            var result = builder.TestBuildEdgeModels(edges);

            Assert.AreEqual("creates", result[0].LabelText);
            Assert.AreEqual("1", result[0].MultiplicitySource);
            Assert.AreEqual("0..*", result[0].MultiplicityTarget);
        }

        [Test]
        public void BuildEdgeModels_EndDecorator_Arrow_ForObjectFlow()
        {
            var edges = new List<EdgeData>
        {
            new EdgeData { Key = "e1", From = "n1", To = "n2", Type = DiagramEdgeTypes.OBJECT_FLOW },
        };
            var result = builder.TestBuildEdgeModels(edges);

            Assert.AreEqual(DecoratorType.Arrow, result[0].EndDecorator);
        }
    }

    public class NodeBuildingHelperTests
    {
        private TestableDiagramBuilder builder;

        [SetUp]
        public void SetUp()
        {
            builder = new TestableDiagramBuilder(new MockTextMeasurer());
        }

        [Test]
        public void BuildNodeModel_SetsIdFromNodeKey()
        {
            var node = new NodeData
            {
                Key = "n1",
                Type = DiagramNodeTypes.CLASS,
                Properties = new Dictionary<string, object> { { "name", "MyClass" } }
            };

            var model = builder.TestBuildNodeModel(node, Vec3.Zero, 5f, 3f, RGBA.White, RGBA.Black, 0.1f, false);
            Assert.AreEqual("n1", model.Id);
        }

        [Test]
        public void BuildNodeModel_SetsLabelFromNodeName()
        {
            var node = new NodeData
            {
                Key = "n1",
                Type = DiagramNodeTypes.CLASS,
                Properties = new Dictionary<string, object> { { "name", "MyClass" } }
            };

            var model = builder.TestBuildNodeModel(node, Vec3.Zero, 5f, 3f, RGBA.White, RGBA.Black, 0.1f, false);
            Assert.AreEqual("MyClass", model.Label);
        }

        [Test]
        public void BuildNodeModel_AddsElevationToY()
        {
            var node = new NodeData
            {
                Key = "n1",
                Type = DiagramNodeTypes.CLASS,
                Properties = new Dictionary<string, object> { { "name", "Test" } }
            };

            var basePos = new Vec3(1f, 2f, 3f);
            var model = builder.TestBuildNodeModel(node, basePos, 5f, 3f, RGBA.White, RGBA.Black, 0.5f, false);
            Assert.AreEqual(2.5f, model.Position.Y, 0.001f);
        }

        [Test]
        public void BuildNodeModel_SetsScaleCorrectly()
        {
            var node = new NodeData
            {
                Key = "n1",
                Type = DiagramNodeTypes.CLASS,
                Properties = new Dictionary<string, object> { { "name", "Test" } }
            };

            var model = builder.TestBuildNodeModel(node, Vec3.Zero, 10f, 6f, RGBA.White, RGBA.Black, 0f, false);
            Assert.AreEqual(10f, model.Scale.X, 0.001f);
            Assert.AreEqual(0.2f, model.Scale.Y, 0.001f);
            Assert.AreEqual(6f, model.Scale.Z, 0.001f);
        }

        [Test]
        public void BuildNodeModel_SetsBounds()
        {
            var node = new NodeData
            {
                Key = "n1",
                Type = DiagramNodeTypes.CLASS,
                Properties = new Dictionary<string, object> { { "name", "Test" } }
            };

            var model = builder.TestBuildNodeModel(node, new Vec3(5f, 0f, 3f), 10f, 6f, RGBA.White, RGBA.Black, 0f, false);
            Assert.IsNotNull(model.Bounds);
            Assert.AreEqual(10f, model.Bounds.Size.X, 0.001f);
        }
        [Test]
        public void BuildNodeModel_LabelFallsBackToKey_WhenNameIsNull()
        {
            var node = new NodeData
            {
                Key = "fallbackKey",
                Type = DiagramNodeTypes.CLASS,
                Properties = new Dictionary<string, object>()
            };

            var model = builder.TestBuildNodeModel(node, Vec3.Zero, 5f, 3f, RGBA.White, RGBA.Black, 0.1f, false);

            Assert.AreEqual(string.Empty, model.Label);
        }
    }

    public class CreateLabelTests
    {
        private TestableDiagramBuilder builder;

        [SetUp]
        public void SetUp()
        {
            builder = new TestableDiagramBuilder(new MockTextMeasurer());
        }

        [Test]
        public void CreateLabel_SetsAllProperties()
        {
            var label = builder.TestCreateLabel("Hello", new Vec3(1f, 2f, 3f), 10f, 5f, RGBA.Black, TextAlignment.Left, FontStyle.Bold);
            Assert.AreEqual("Hello", label.Text);
            Assert.AreEqual(1f, label.Position.X, 0.001f);
            Assert.AreEqual(10f, label.Width, 0.001f);
            Assert.AreEqual(5f, label.FontSize, 0.001f);
            Assert.AreEqual(TextAlignment.Left, label.Alignment);
            Assert.AreEqual(FontStyle.Bold, label.Style);
        }
    }

    public class GetNodeColorByDepthTests
    {
        private TestableDiagramBuilder builder;

        [SetUp]
        public void SetUp()
        {
            builder = new TestableDiagramBuilder(new MockTextMeasurer());
        }

        [Test]
        public void GetNodeColorByDepth_Depth0_ReturnsLayer0()
        {
            var color = builder.TestGetNodeColorByDepth(0);
            Assert.AreEqual(RGBA.Layer0.R, color.R, 0.001f);
            Assert.AreEqual(RGBA.Layer0.G, color.G, 0.001f);
            Assert.AreEqual(RGBA.Layer0.B, color.B, 0.001f);
        }

        [Test]
        public void GetNodeColorByDepth_WrapsAround()
        {
            int wrapDepth = RGBA.NestingPalette.Length;

            var colorWrapped = builder.TestGetNodeColorByDepth(wrapDepth);
            var color0 = builder.TestGetNodeColorByDepth(0);

            Assert.AreEqual(color0.R, colorWrapped.R, 0.001f);
            Assert.AreEqual(color0.G, colorWrapped.G, 0.001f);
            Assert.AreEqual(color0.B, colorWrapped.B, 0.001f);
        }

        [Test]
        public void GetNodeColorByDepth_NegativeDepth_ReturnsLayer0()
        {
            var color = builder.TestGetNodeColorByDepth(-1);
            Assert.AreEqual(RGBA.Layer0.R, color.R, 0.001f);
        }
    }

    public class MeasureTextTests
    {
        [Test]
        public void MeasureText_UsesInjectedMeasurer()
        {
            var measurer = new MockTextMeasurer { CharWidth = 1f };
            var builder = new TestableDiagramBuilder(measurer);
            float result = builder.TestMeasureText("Hi", 10f, false);
            Assert.Greater(result, 0f);
        }

        [Test]
        public void MeasureText_EmptyString_ReturnsZero()
        {
            var measurer = new MockTextMeasurer();
            var builder = new TestableDiagramBuilder(measurer);
            float result = builder.TestMeasureText("", 10f, false);
            Assert.AreEqual(0f, result);
        }
    }
    public class GetRecursiveBoundsTests
    {
        private TestableDiagramBuilder builder;

        [SetUp]
        public void SetUp()
        {
            builder = new TestableDiagramBuilder(new MockTextMeasurer());
        }

        [Test]
        public void GetRecursiveBounds_SingleChild_CalculatesBoundsCorrectly()
        {
            var parentKey = "parent";
            var child1 = new NodeData { Key = "child1" };

            var parentToChildren = new Dictionary<string, List<NodeData>>
            {
                { parentKey, new List<NodeData> { child1 } }
            };

            var positions = new Dictionary<string, Vec3>
            {
                { "child1", new Vec3(5f, 0f, 10f) }
            };

            builder.TestGetRecursiveBounds(parentKey, parentToChildren, out float minX, out float maxX, out float minZ, out float maxZ, positions);

            // Child h
            Assert.AreEqual(5f, minX, 0.001f);
            Assert.AreEqual(5f, maxX, 0.001f);
            Assert.AreEqual(10f, minZ, 0.001f);
            Assert.AreEqual(10f, maxZ, 0.001f);
        }

        [Test]
        public void GetRecursiveBounds_DeepNesting_AddsPaddingForContainers()
        {
            var parentKey = "root";
            var child = new NodeData { Key = "child" };
            var grandchild = new NodeData { Key = "grandchild" };

            var parentToChildren = new Dictionary<string, List<NodeData>>
            {
                { "root", new List<NodeData> { child } },
                { "child", new List<NodeData> { grandchild } }
            };

            var positions = new Dictionary<string, Vec3>
            {
                { "child", new Vec3(0f, 0f, 0f) },
                { "grandchild", new Vec3(5f, 0f, 10f) }
            };

            builder.TestGetRecursiveBounds(parentKey, parentToChildren, out float minX, out float maxX, out float minZ, out float maxZ, positions);

            Assert.AreEqual(4f, minX, 0.001f);
            Assert.AreEqual(6f, maxX, 0.001f);
            Assert.AreEqual(9f, minZ, 0.001f);
            Assert.AreEqual(11f, maxZ, 0.001f);
        }
    }
}
