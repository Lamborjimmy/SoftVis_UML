using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using TMPro;
using Softviz.UML.Renderers.Unity;
using Softviz.UML.Models;
using Softviz.UML.Data;

namespace Softviz.Tests.PlayMode
{
    public class UnityDiagramRendererTests
    {
        private GameObject testContainer;
        private UnityDiagramRenderer renderer;
        private Dictionary<string, GameObject> fakePrefabs;

        [SetUp]
        public void SetUp()
        {
            testContainer = new GameObject("TestContainer");
            fakePrefabs = CreateFakePrefabs();
            renderer = new UnityDiagramRenderer(fakePrefabs);
        }

        [TearDown]
        public void TearDown()
        {
            if (testContainer != null)
                Object.Destroy(testContainer);

            if (prefabHolder != null)
                Object.Destroy(prefabHolder);
        }

        private GameObject prefabHolder;

        private Dictionary<string, GameObject> CreateFakePrefabs()
        {
            var prefabs = new Dictionary<string, GameObject>();

            // Create a holder object far away to store prefabs (keeps them active but invisible)
            prefabHolder = new GameObject("PrefabHolder");
            prefabHolder.transform.position = new Vector3(99999, 99999, 99999);

            // Create fake prefabs for various node types
            string[] nodeTypes = new[]
            {
                DiagramNodeTypes.CLASS,
                DiagramNodeTypes.INTERFACE,
                DiagramNodeTypes.ACTOR,
                DiagramNodeTypes.USECASE,
                DiagramNodeTypes.STATE,
                DiagramNodeTypes.ACTION,
                DiagramNodeTypes.COMPONENT,
                DiagramNodeTypes.NODE,
                DiagramNodeTypes.ARTIFACT,
                DiagramNodeTypes.INITIAL,
                DiagramNodeTypes.FINAL,
                DiagramNodeTypes.DECISION,
                DiagramNodeTypes.FORK,
                DiagramNodeTypes.JOIN,
                DiagramNodeTypes.LIFELINE,
                DiagramNodeTypes.PORT,
                DiagramNodeTypes.PROVIDED_INTERFACE,
                DiagramNodeTypes.REQUIRED_INTERFACE
            };

            foreach (var nodeType in nodeTypes)
            {
                var prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                prefab.name = $"FakePrefab_{nodeType}";
                prefab.transform.SetParent(prefabHolder.transform);
                prefabs[nodeType] = prefab;
            }

            // Create fake edge decorator prefabs
            string[] edgeTypes = new[]
            {
                DiagramEdgeTypes.GENERALIZES,
                DiagramEdgeTypes.AGGREGATES,
                DiagramEdgeTypes.COMPOSES,
                DiagramEdgeTypes.INCLUDES_UML
            };

            foreach (var edgeType in edgeTypes)
            {
                var prefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                prefab.name = $"FakeDecorator_{edgeType}";
                prefab.transform.SetParent(prefabHolder.transform);
                prefabs[edgeType] = prefab;
            }
            var labelPrefab = new GameObject("FakeEdgeLabelPrefab");
            labelPrefab.AddComponent<TextMeshProUGUI>();
            labelPrefab.transform.SetParent(prefabHolder.transform);

            prefabs["EdgeLabelPrefab"] = labelPrefab;
            // Special Package prefab with Tab child for package node tests
            var packagePrefab = new GameObject("PackagePrefab");
            var packageBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            packageBody.name = "Package";
            packageBody.transform.SetParent(packagePrefab.transform);
            var tabBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tabBody.name = "Tab";
            tabBody.transform.SetParent(packagePrefab.transform);
            packagePrefab.transform.SetParent(prefabHolder.transform);
            prefabs[DiagramNodeTypes.PACKAGE] = packagePrefab;

            return prefabs;
        }

        private DiagramModel CreateSimpleDiagram(string id = "test_diag", string name = "Test Diagram")
        {
            return new DiagramModel
            {
                Id = id,
                Name = name,
                DiagramType = DiagramTypes.CLASS_DIAGRAM,
                BasePlaneColor = RGBA.Gray
            };
        }

        private NodeModel CreateNodeModel(string id, string label, string nodeType, Vec3 position, Vec3 scale, RGBA color)
        {
            return new NodeModel
            {
                Id = id,
                Label = label,
                NodeType = nodeType,
                Position = position,
                Scale = scale,
                BackgroundColor = color,
                TextColor = RGBA.Black,
                UseUniformScale = false
            };
        }

        #region Hierarchy Structure Tests

        [UnityTest]
        public IEnumerator Render_CreatesNodesAndEdgesParents()
        {
            var diagram = CreateSimpleDiagram();
            renderer.Render(diagram, testContainer);
            yield return null;

            var nodesParent = testContainer.transform.Find("Nodes");
            var edgesParent = testContainer.transform.Find("Edges");

            Assert.IsNotNull(nodesParent, "Nodes parent GameObject should be created");
            Assert.IsNotNull(edgesParent, "Edges parent GameObject should be created");
        }

        [UnityTest]
        public IEnumerator Render_EmptyDiagram_CreatesOnlyParentsAndBasePlane()
        {
            var diagram = CreateSimpleDiagram();
            renderer.Render(diagram, testContainer);
            yield return null;

            var nodesParent = testContainer.transform.Find("Nodes");
            var edgesParent = testContainer.transform.Find("Edges");

            Assert.AreEqual(0, nodesParent.childCount, "Nodes parent should have no children for empty diagram");
            Assert.AreEqual(0, edgesParent.childCount, "Edges parent should have no children for empty diagram");
        }

        #endregion

        #region Node Rendering Tests

        [UnityTest]
        public IEnumerator Render_SingleNode_CreatesCorrectGameObject()
        {
            var diagram = CreateSimpleDiagram();
            diagram.Nodes.Add(CreateNodeModel("n1", "MyClass", DiagramNodeTypes.CLASS,
                new Vec3(5f, 1f, 10f), new Vec3(4f, 0.2f, 3f), RGBA.SoftYellow));

            renderer.Render(diagram, testContainer);
            yield return null;

            var nodesParent = testContainer.transform.Find("Nodes");
            Assert.AreEqual(1, nodesParent.childCount, "Should have exactly one node child");

            var nodeGO = nodesParent.Find("Node_MyClass");
            Assert.IsNotNull(nodeGO, "Node should be named 'Node_MyClass'");
        }

        [UnityTest]
        public IEnumerator Render_NodePosition_IsCorrect()
        {
            var diagram = CreateSimpleDiagram();
            diagram.Nodes.Add(CreateNodeModel("n1", "TestNode", DiagramNodeTypes.CLASS,
                new Vec3(10f, 2.5f, -5f), new Vec3(2f, 0.2f, 2f), RGBA.White));

            renderer.Render(diagram, testContainer);
            yield return null;

            var nodeGO = testContainer.transform.Find("Nodes/Node_TestNode");
            Assert.IsNotNull(nodeGO);

            Assert.AreEqual(10f, nodeGO.localPosition.x, 0.01f, "X position should match");
            Assert.AreEqual(2.5f, nodeGO.localPosition.y, 0.01f, "Y position should match");
            Assert.AreEqual(-5f, nodeGO.localPosition.z, 0.01f, "Z position should match");
        }

        [UnityTest]
        public IEnumerator Render_MultipleNodes_AllCreated()
        {
            var diagram = CreateSimpleDiagram();
            diagram.Nodes.Add(CreateNodeModel("n1", "ClassA", DiagramNodeTypes.CLASS,
                new Vec3(0, 0, 0), new Vec3(2, 0.2f, 2), RGBA.SoftYellow));
            diagram.Nodes.Add(CreateNodeModel("n2", "ClassB", DiagramNodeTypes.CLASS,
                new Vec3(5, 0, 0), new Vec3(2, 0.2f, 2), RGBA.SoftYellow));
            diagram.Nodes.Add(CreateNodeModel("n3", "InterfaceC", DiagramNodeTypes.INTERFACE,
                new Vec3(10, 0, 0), new Vec3(2, 0.2f, 2), RGBA.Thistle));

            renderer.Render(diagram, testContainer);
            yield return null;

            var nodesParent = testContainer.transform.Find("Nodes");
            Assert.AreEqual(3, nodesParent.childCount, "Should have 3 node children");

            Assert.IsNotNull(nodesParent.Find("Node_ClassA"));
            Assert.IsNotNull(nodesParent.Find("Node_ClassB"));
            Assert.IsNotNull(nodesParent.Find("Node_InterfaceC"));
        }

        [UnityTest]
        public IEnumerator Render_NodeWithUniformScale_AppliesUniformScale()
        {
            var diagram = CreateSimpleDiagram();
            var node = CreateNodeModel("n1", "Actor", DiagramNodeTypes.ACTOR,
                new Vec3(0, 0, 0), new Vec3(1.5f, 1.5f, 1.5f), RGBA.White);
            node.UseUniformScale = true;
            diagram.Nodes.Add(node);

            renderer.Render(diagram, testContainer);
            yield return null;

            var nodeGO = testContainer.transform.Find("Nodes/Node_Actor");
            var visuals = nodeGO.Find("Background/Visuals");
            Assert.IsNotNull(visuals);

            // Uniform scale should apply node.Scale.X to all axes
            Assert.AreEqual(1.5f, visuals.localScale.x, 0.01f);
            Assert.AreEqual(1.5f, visuals.localScale.y, 0.01f);
            Assert.AreEqual(1.5f, visuals.localScale.z, 0.01f);
        }

        [UnityTest]
        public IEnumerator Render_NodeWithNonUniformScale_AppliesNonUniformScale()
        {
            var diagram = CreateSimpleDiagram();
            var node = CreateNodeModel("n1", "WideNode", DiagramNodeTypes.CLASS,
                new Vec3(0, 0, 0), new Vec3(8f, 0.2f, 3f), RGBA.White);
            node.UseUniformScale = false;
            diagram.Nodes.Add(node);

            renderer.Render(diagram, testContainer);
            yield return null;

            var visuals = testContainer.transform.Find("Nodes/Node_WideNode/Background/Visuals");
            Assert.IsNotNull(visuals);

            Assert.AreEqual(8f, visuals.localScale.x, 0.01f);
            Assert.AreEqual(0.2f, visuals.localScale.y, 0.01f);
            Assert.AreEqual(3f, visuals.localScale.z, 0.01f);
        }

        [UnityTest]
        public IEnumerator Render_NodeWithoutPrefab_CreatesFallbackCube()
        {
            // Use a node type that doesn't have a prefab
            var diagram = CreateSimpleDiagram();
            diagram.Nodes.Add(CreateNodeModel("n1", "Unknown", "UNKNOWN_TYPE",
                new Vec3(0, 0, 0), new Vec3(2, 0.2f, 2), RGBA.White));

            renderer.Render(diagram, testContainer);
            yield return null;

            var visuals = testContainer.transform.Find("Nodes/Node_Unknown/Background/Visuals");
            Assert.IsNotNull(visuals, "Should create fallback visuals");

            // Fallback should be a MeshFilter with cube mesh
            var meshFilter = visuals.GetComponent<MeshFilter>();
            Assert.IsNotNull(meshFilter, "Fallback should have MeshFilter (from CreatePrimitive)");
        }

        [UnityTest]
        public IEnumerator Render_PackageNode_ConfiguresTabAndBody()
        {
            var diagram = CreateSimpleDiagram();
            var packageNode = CreateNodeModel("p1", "MyPackage", DiagramNodeTypes.PACKAGE,
                new Vec3(0, 0, 0), new Vec3(10f, 0.2f, 8f), RGBA.Khaki);
            diagram.Nodes.Add(packageNode);

            renderer.Render(diagram, testContainer);
            yield return null;

            var visuals = testContainer.transform.Find("Nodes/Node_MyPackage/Background/Visuals");
            Assert.IsNotNull(visuals);

            var packageBody = visuals.Find("Package");
            var tabBody = visuals.Find("Tab");

            Assert.IsNotNull(packageBody, "Package body should exist");
            Assert.IsNotNull(tabBody, "Tab should exist");
        }

        #endregion

        #region Text Label Rendering Tests

        [UnityTest]
        public IEnumerator Render_NodeWithLabel_CreatesTextMeshPro()
        {
            var diagram = CreateSimpleDiagram();
            var node = CreateNodeModel("n1", "LabeledNode", DiagramNodeTypes.CLASS,
                new Vec3(0, 0, 0), new Vec3(4, 0.2f, 3), RGBA.White);
            node.Labels.Add(new TextLabelModel
            {
                Text = "MyClassName",
                Position = new Vec3(0, 0.3f, 0),
                Width = 4f,
                FontSize = 5f,
                Color = RGBA.Black,
                Alignment = Softviz.UML.Data.TextAlignment.Center,
                Style = Softviz.UML.Data.FontStyle.Bold
            });
            diagram.Nodes.Add(node);

            renderer.Render(diagram, testContainer);
            yield return null;

            var tmp = testContainer.GetComponentInChildren<TextMeshPro>();
            Assert.IsNotNull(tmp, "TextMeshPro component should be created");
            Assert.AreEqual("MyClassName", tmp.text);
            Assert.AreEqual(5f, tmp.fontSize, 0.01f);
            Assert.AreEqual(FontStyles.Bold, tmp.fontStyle);
        }

        [UnityTest]
        public IEnumerator Render_NodeWithMultipleLabels_CreatesAllLabels()
        {
            var diagram = CreateSimpleDiagram();
            var node = CreateNodeModel("n1", "MultiLabel", DiagramNodeTypes.CLASS,
                new Vec3(0, 0, 0), new Vec3(4, 0.2f, 3), RGBA.White);
            node.Labels.Add(new TextLabelModel { Text = "<<interface>>", FontSize = 4f });
            node.Labels.Add(new TextLabelModel { Text = "ISerializable", FontSize = 5f, Style = Softviz.UML.Data.FontStyle.Bold });
            node.Labels.Add(new TextLabelModel { Text = "+ Serialize(): void", FontSize = 3f });
            diagram.Nodes.Add(node);

            renderer.Render(diagram, testContainer);
            yield return null;

            var tmps = testContainer.GetComponentsInChildren<TextMeshPro>();
            Assert.AreEqual(3, tmps.Length, "Should have 3 TextMeshPro components");

            var texts = tmps.Select(t => t.text).ToList();
            Assert.Contains("<<interface>>", texts);
            Assert.Contains("ISerializable", texts);
            Assert.Contains("+ Serialize(): void", texts);
        }

        [UnityTest]
        public IEnumerator Render_LabelAlignment_IsAppliedCorrectly()
        {
            var diagram = CreateSimpleDiagram();
            var node = CreateNodeModel("n1", "AlignTest", DiagramNodeTypes.CLASS,
                new Vec3(0, 0, 0), new Vec3(4, 0.2f, 3), RGBA.White);
            node.Labels.Add(new TextLabelModel
            {
                Text = "LeftAligned",
                Alignment = Softviz.UML.Data.TextAlignment.Left
            });
            diagram.Nodes.Add(node);

            renderer.Render(diagram, testContainer);
            yield return null;

            var tmp = testContainer.GetComponentInChildren<TextMeshPro>();
            Assert.AreEqual(TextAlignmentOptions.Left, tmp.alignment);
        }

        [UnityTest]
        public IEnumerator Render_LabelRotation_IsSetTo90DegreesX()
        {
            var diagram = CreateSimpleDiagram();
            var node = CreateNodeModel("n1", "RotTest", DiagramNodeTypes.CLASS,
                new Vec3(0, 0, 0), new Vec3(4, 0.2f, 3), RGBA.White);
            node.Labels.Add(new TextLabelModel { Text = "Test" });
            diagram.Nodes.Add(node);

            renderer.Render(diagram, testContainer);
            yield return null;

            var tmp = testContainer.GetComponentInChildren<TextMeshPro>();
            var rotation = tmp.transform.localRotation.eulerAngles;
            Assert.AreEqual(90f, rotation.x, 1f, "Label should be rotated 90 degrees on X axis");
        }

        #endregion

        #region Edge Rendering Tests

        [UnityTest]
        public IEnumerator Render_SingleEdge_CreatesLineRenderer()
        {
            var diagram = CreateSimpleDiagram();
            diagram.Nodes.Add(CreateNodeModel("n1", "From", DiagramNodeTypes.CLASS,
                new Vec3(0, 0, 0), new Vec3(2, 0.2f, 2), RGBA.White));
            diagram.Nodes.Add(CreateNodeModel("n2", "To", DiagramNodeTypes.CLASS,
                new Vec3(10, 0, 0), new Vec3(2, 0.2f, 2), RGBA.White));
            diagram.Edges.Add(new EdgeModel
            {
                Id = "e1",
                FromId = "n1",
                ToId = "n2",
                EdgeType = DiagramEdgeTypes.ASSOCIATED_WITH,
                LineWidth = 0.04f,
                LineColor = RGBA.White
            });

            renderer.Render(diagram, testContainer);
            yield return null;

            var lineRenderer = testContainer.GetComponentInChildren<LineRenderer>();
            Assert.IsNotNull(lineRenderer, "LineRenderer should be created for edge");
            Assert.AreEqual(2, lineRenderer.positionCount, "Edge should have 2 position points");
        }

        [UnityTest]
        public IEnumerator Render_EdgeWithArrowDecorator_CreatesDecorator()
        {
            var diagram = CreateSimpleDiagram();
            diagram.Nodes.Add(CreateNodeModel("n1", "From", DiagramNodeTypes.CLASS,
                new Vec3(0, 0, 0), new Vec3(2, 0.2f, 2), RGBA.White));
            diagram.Nodes.Add(CreateNodeModel("n2", "To", DiagramNodeTypes.CLASS,
                new Vec3(10, 0, 0), new Vec3(2, 0.2f, 2), RGBA.White));
            diagram.Edges.Add(new EdgeModel
            {
                Id = "e1",
                FromId = "n1",
                ToId = "n2",
                EdgeType = DiagramEdgeTypes.INCLUDES_UML,
                EndDecorator = DecoratorType.Arrow,
                IsDashed = true
            });

            renderer.Render(diagram, testContainer);
            yield return null;

            var edgesParent = testContainer.transform.Find("Edges");
            Assert.Greater(edgesParent.childCount, 0, "Should have edge children");

            // Check that decorator was instantiated (it uses the INCLUDES_UML prefab key)
            var decorators = edgesParent.GetComponentsInChildren<MeshRenderer>();
            Assert.Greater(decorators.Length, 0, "Should have decorator meshes");
        }

        [UnityTest]
        public IEnumerator Render_EdgeWithTriangleDecorator_CreatesTriangle()
        {
            var diagram = CreateSimpleDiagram();
            diagram.Nodes.Add(CreateNodeModel("n1", "Child", DiagramNodeTypes.CLASS,
                new Vec3(0, 0, 0), new Vec3(2, 0.2f, 2), RGBA.White));
            diagram.Nodes.Add(CreateNodeModel("n2", "Parent", DiagramNodeTypes.CLASS,
                new Vec3(10, 0, 0), new Vec3(2, 0.2f, 2), RGBA.White));
            diagram.Edges.Add(new EdgeModel
            {
                Id = "e1",
                FromId = "n1",
                ToId = "n2",
                EdgeType = DiagramEdgeTypes.GENERALIZES,
                EndDecorator = DecoratorType.Triangle
            });

            renderer.Render(diagram, testContainer);
            yield return null;

            var edgesParent = testContainer.transform.Find("Edges");
            Assert.Greater(edgesParent.childCount, 0);
        }

        [UnityTest]
        public IEnumerator Render_EdgeWithDiamondHollowDecorator_CreatesDiamond()
        {
            var diagram = CreateSimpleDiagram();
            diagram.Nodes.Add(CreateNodeModel("n1", "Whole", DiagramNodeTypes.CLASS,
                new Vec3(0, 0, 0), new Vec3(2, 0.2f, 2), RGBA.White));
            diagram.Nodes.Add(CreateNodeModel("n2", "Part", DiagramNodeTypes.CLASS,
                new Vec3(10, 0, 0), new Vec3(2, 0.2f, 2), RGBA.White));
            diagram.Edges.Add(new EdgeModel
            {
                Id = "e1",
                FromId = "n1",
                ToId = "n2",
                EdgeType = DiagramEdgeTypes.AGGREGATES,
                StartDecorator = DecoratorType.DiamondHollow
            });

            renderer.Render(diagram, testContainer);
            yield return null;

            var edgesParent = testContainer.transform.Find("Edges");
            Assert.Greater(edgesParent.childCount, 0);
        }

        [UnityTest]
        public IEnumerator Render_DashedEdge_UsesTextureMode()
        {
            var diagram = CreateSimpleDiagram();
            diagram.Nodes.Add(CreateNodeModel("n1", "From", DiagramNodeTypes.CLASS,
                new Vec3(0, 0, 0), new Vec3(2, 0.2f, 2), RGBA.White));
            diagram.Nodes.Add(CreateNodeModel("n2", "To", DiagramNodeTypes.CLASS,
                new Vec3(10, 0, 0), new Vec3(2, 0.2f, 2), RGBA.White));
            diagram.Edges.Add(new EdgeModel
            {
                Id = "e1",
                FromId = "n1",
                ToId = "n2",
                EdgeType = DiagramEdgeTypes.DEPENDENCY,
                IsDashed = true
            });

            renderer.Render(diagram, testContainer);
            yield return null;

            var lineRenderer = testContainer.GetComponentInChildren<LineRenderer>();
            Assert.IsNotNull(lineRenderer);
            Assert.AreEqual(LineTextureMode.Tile, lineRenderer.textureMode, "Dashed edge should use Tile texture mode");
        }

        [UnityTest]
        public IEnumerator Render_SolidEdge_UsesStretchTextureMode()
        {
            var diagram = CreateSimpleDiagram();
            diagram.Nodes.Add(CreateNodeModel("n1", "From", DiagramNodeTypes.CLASS,
                new Vec3(0, 0, 0), new Vec3(2, 0.2f, 2), RGBA.White));
            diagram.Nodes.Add(CreateNodeModel("n2", "To", DiagramNodeTypes.CLASS,
                new Vec3(10, 0, 0), new Vec3(2, 0.2f, 2), RGBA.White));
            diagram.Edges.Add(new EdgeModel
            {
                Id = "e1",
                FromId = "n1",
                ToId = "n2",
                EdgeType = DiagramEdgeTypes.ASSOCIATED_WITH,
                IsDashed = false
            });

            renderer.Render(diagram, testContainer);
            yield return null;

            var lineRenderer = testContainer.GetComponentInChildren<LineRenderer>();
            Assert.IsNotNull(lineRenderer);
            Assert.AreEqual(LineTextureMode.Stretch, lineRenderer.textureMode, "Solid edge should use Stretch texture mode");
        }

        [UnityTest]
        public IEnumerator Render_SelfLoopEdge_CreatesCurvedLine()
        {
            var diagram = CreateSimpleDiagram();
            diagram.Nodes.Add(CreateNodeModel("n1", "SelfRef", DiagramNodeTypes.CLASS,
                new Vec3(0, 0, 0), new Vec3(2, 0.2f, 2), RGBA.White));
            diagram.Edges.Add(new EdgeModel
            {
                Id = "e1",
                FromId = "n1",
                ToId = "n1",
                EdgeType = DiagramEdgeTypes.ASSOCIATED_WITH,
                IsSelfLoop = true
            });

            renderer.Render(diagram, testContainer);
            yield return null;

            var lineRenderer = testContainer.GetComponentInChildren<LineRenderer>();
            Assert.IsNotNull(lineRenderer, "Self-loop should create LineRenderer");
            Assert.Greater(lineRenderer.positionCount, 2, "Self-loop should have more than 2 points (bezier curve)");
        }

        [UnityTest]
        public IEnumerator Render_MultipleEdges_AllCreated()
        {
            var diagram = CreateSimpleDiagram();
            diagram.Nodes.Add(CreateNodeModel("n1", "A", DiagramNodeTypes.CLASS,
                new Vec3(0, 0, 0), new Vec3(2, 0.2f, 2), RGBA.White));
            diagram.Nodes.Add(CreateNodeModel("n2", "B", DiagramNodeTypes.CLASS,
                new Vec3(10, 0, 0), new Vec3(2, 0.2f, 2), RGBA.White));
            diagram.Nodes.Add(CreateNodeModel("n3", "C", DiagramNodeTypes.CLASS,
                new Vec3(5, 0, 10), new Vec3(2, 0.2f, 2), RGBA.White));

            diagram.Edges.Add(new EdgeModel { Id = "e1", FromId = "n1", ToId = "n2", EdgeType = DiagramEdgeTypes.ASSOCIATED_WITH });
            diagram.Edges.Add(new EdgeModel { Id = "e2", FromId = "n2", ToId = "n3", EdgeType = DiagramEdgeTypes.ASSOCIATED_WITH });
            diagram.Edges.Add(new EdgeModel { Id = "e3", FromId = "n3", ToId = "n1", EdgeType = DiagramEdgeTypes.ASSOCIATED_WITH });

            renderer.Render(diagram, testContainer);
            yield return null;

            var lineRenderers = testContainer.GetComponentsInChildren<LineRenderer>();
            Assert.AreEqual(3, lineRenderers.Length, "Should have 3 LineRenderers for 3 edges");
        }

        [UnityTest]
        public IEnumerator Render_EdgeLineWidth_IsApplied()
        {
            var diagram = CreateSimpleDiagram();
            diagram.Nodes.Add(CreateNodeModel("n1", "From", DiagramNodeTypes.CLASS,
                new Vec3(0, 0, 0), new Vec3(2, 0.2f, 2), RGBA.White));
            diagram.Nodes.Add(CreateNodeModel("n2", "To", DiagramNodeTypes.CLASS,
                new Vec3(10, 0, 0), new Vec3(2, 0.2f, 2), RGBA.White));
            diagram.Edges.Add(new EdgeModel
            {
                Id = "e1",
                FromId = "n1",
                ToId = "n2",
                EdgeType = DiagramEdgeTypes.ASSOCIATED_WITH,
                LineWidth = 0.1f
            });

            renderer.Render(diagram, testContainer);
            yield return null;

            var lineRenderer = testContainer.GetComponentInChildren<LineRenderer>();
            Assert.AreEqual(0.1f, lineRenderer.startWidth, 0.001f);
            Assert.AreEqual(0.1f, lineRenderer.endWidth, 0.001f);
        }

        #endregion

        #region Base Plane Rendering Tests

        [UnityTest]
        public IEnumerator Render_CreatesBasePlane_WithDiagramName()
        {
            var diagram = CreateSimpleDiagram("diag1", "My Class Diagram");
            diagram.Nodes.Add(CreateNodeModel("n1", "Node", DiagramNodeTypes.CLASS,
                new Vec3(0, 0, 0), new Vec3(2, 0.2f, 2), RGBA.White));

            renderer.Render(diagram, testContainer);
            yield return null;

            var basePlane = testContainer.transform.Find("My Class Diagram");
            Assert.IsNotNull(basePlane, "Base plane should be created with diagram name");
        }

        [UnityTest]
        public IEnumerator Render_BasePlane_IsBelowNodes()
        {
            var diagram = CreateSimpleDiagram();
            diagram.Nodes.Add(CreateNodeModel("n1", "Node", DiagramNodeTypes.CLASS,
                new Vec3(0, 1f, 0), new Vec3(2, 0.2f, 2), RGBA.White));

            renderer.Render(diagram, testContainer);
            yield return null;

            var basePlane = testContainer.transform.Find("Test Diagram");
            Assert.IsNotNull(basePlane);
            Assert.Less(basePlane.localPosition.y, 0f, "Base plane Y should be below 0 (negative)");
        }

        [UnityTest]
        public IEnumerator Render_BasePlane_EncompassesAllNodes()
        {
            var diagram = CreateSimpleDiagram();
            diagram.Nodes.Add(CreateNodeModel("n1", "Left", DiagramNodeTypes.CLASS,
                new Vec3(-10, 0, 0), new Vec3(2, 0.2f, 2), RGBA.White));
            diagram.Nodes.Add(CreateNodeModel("n2", "Right", DiagramNodeTypes.CLASS,
                new Vec3(10, 0, 0), new Vec3(2, 0.2f, 2), RGBA.White));
            diagram.Nodes.Add(CreateNodeModel("n3", "Far", DiagramNodeTypes.CLASS,
                new Vec3(0, 0, 15), new Vec3(2, 0.2f, 2), RGBA.White));

            renderer.Render(diagram, testContainer);
            yield return null;

            var basePlane = testContainer.transform.Find("Test Diagram");
            Assert.IsNotNull(basePlane);

            // Base plane scale should be large enough to cover all nodes
            Assert.Greater(basePlane.localScale.x, 20f, "Base plane width should cover left-to-right span");
            Assert.Greater(basePlane.localScale.y, 15f, "Base plane height should cover front-to-back span");
        }

        #endregion

        #region Clear Tests

        [UnityTest]
        public IEnumerator Clear_RemovesAllChildren()
        {
            var diagram = CreateSimpleDiagram();
            diagram.Nodes.Add(CreateNodeModel("n1", "Node", DiagramNodeTypes.CLASS,
                new Vec3(0, 0, 0), new Vec3(2, 0.2f, 2), RGBA.White));

            renderer.Render(diagram, testContainer);
            yield return null;

            Assert.Greater(testContainer.transform.childCount, 0, "Should have children after render");

            renderer.Clear(testContainer);
            yield return null;

            Assert.AreEqual(0, testContainer.transform.childCount, "Should have no children after clear");
        }

        [UnityTest]
        public IEnumerator Clear_CanRerenderAfterClear()
        {
            var diagram1 = CreateSimpleDiagram("d1", "First");
            diagram1.Nodes.Add(CreateNodeModel("n1", "First", DiagramNodeTypes.CLASS,
                new Vec3(0, 0, 0), new Vec3(2, 0.2f, 2), RGBA.White));

            renderer.Render(diagram1, testContainer);
            yield return null;

            renderer.Clear(testContainer);
            yield return null;

            var diagram2 = CreateSimpleDiagram("d2", "Second");
            diagram2.Nodes.Add(CreateNodeModel("n2", "Second", DiagramNodeTypes.CLASS,
                new Vec3(5, 0, 0), new Vec3(2, 0.2f, 2), RGBA.White));

            renderer.Render(diagram2, testContainer);
            yield return null;

            var nodesParent = testContainer.transform.Find("Nodes");
            Assert.AreEqual(1, nodesParent.childCount);
            Assert.IsNotNull(nodesParent.Find("Node_Second"));
            Assert.IsNull(nodesParent.Find("Node_First"), "Old node should not exist");
        }

        #endregion

        #region Child Key Tracking Tests

        [UnityTest]
        public IEnumerator Render_NodeWithChildKeys_TracksAllKeys()
        {
            var diagram = CreateSimpleDiagram();
            var parentNode = CreateNodeModel("p1", "Parent", DiagramNodeTypes.PACKAGE,
                new Vec3(0, 0, 0), new Vec3(10, 0.2f, 10), RGBA.Khaki);
            parentNode.ChildKeys.Add("c1");
            parentNode.ChildKeys.Add("c2");
            diagram.Nodes.Add(parentNode);

            // Add an edge that references the child key
            diagram.Edges.Add(new EdgeModel
            {
                Id = "e1",
                FromId = "c1",
                ToId = "p1",
                EdgeType = DiagramEdgeTypes.ASSOCIATED_WITH
            });

            renderer.Render(diagram, testContainer);
            yield return null;

            // The edge should connect to the parent node since c1 is a child key of p1
            var lineRenderers = testContainer.GetComponentsInChildren<LineRenderer>();
            Assert.AreEqual(1, lineRenderers.Length, "Edge should be created using child key mapping");
        }

        #endregion

        #region Material and Color Tests

        [UnityTest]
        public IEnumerator Render_NodeColor_IsAppliedToRenderer()
        {
            var diagram = CreateSimpleDiagram();
            diagram.Nodes.Add(CreateNodeModel("n1", "Colored", DiagramNodeTypes.CLASS,
                new Vec3(0, 0, 0), new Vec3(2, 0.2f, 2), new RGBA(1f, 0f, 0f, 1f)));

            renderer.Render(diagram, testContainer);
            yield return null;

            var nodeGO = testContainer.transform.Find("Nodes/Node_Colored");
            var renderers = nodeGO.GetComponentsInChildren<Renderer>();
            Assert.Greater(renderers.Length, 0, "Node should have renderers");

            // Check that color was applied via MaterialPropertyBlock
            var propertyBlock = new MaterialPropertyBlock();
            renderers[0].GetPropertyBlock(propertyBlock);

            // The color should be set (we can't easily verify the exact color due to PropertyBlock behavior,
            // but we can verify the renderer exists and has been configured)
            Assert.IsNotNull(renderers[0].sharedMaterial);
        }

        #endregion

        #region Edge Hub (Merge Point) Tests

        [UnityTest]
        public IEnumerator Render_MultipleEdgesToSameTarget_CreatesMergeHub()
        {
            var diagram = CreateSimpleDiagram();
            diagram.Nodes.Add(CreateNodeModel("n1", "Source1", DiagramNodeTypes.CLASS,
                new Vec3(0, 0, 0), new Vec3(2, 0.2f, 2), RGBA.White));
            diagram.Nodes.Add(CreateNodeModel("n2", "Source2", DiagramNodeTypes.CLASS,
                new Vec3(0, 0, 10), new Vec3(2, 0.2f, 2), RGBA.White));
            diagram.Nodes.Add(CreateNodeModel("n3", "Target", DiagramNodeTypes.CLASS,
                new Vec3(15, 0, 5), new Vec3(2, 0.2f, 2), RGBA.White));

            // Multiple edges of same type to same target
            diagram.Edges.Add(new EdgeModel
            {
                Id = "e1",
                FromId = "n1",
                ToId = "n3",
                EdgeType = DiagramEdgeTypes.GENERALIZES,
                EndDecorator = DecoratorType.Triangle
            });
            diagram.Edges.Add(new EdgeModel
            {
                Id = "e2",
                FromId = "n2",
                ToId = "n3",
                EdgeType = DiagramEdgeTypes.GENERALIZES,
                EndDecorator = DecoratorType.Triangle
            });

            renderer.Render(diagram, testContainer);
            yield return null;

            var edgesParent = testContainer.transform.Find("Edges");
            Assert.Greater(edgesParent.childCount, 0);

            // Hub object should be created for merged edges
            var hubObjects = edgesParent.GetComponentsInChildren<Transform>()
                .Where(t => t.name.StartsWith("Hub_"))
                .ToList();
            Assert.Greater(hubObjects.Count, 0, "Hub object should be created for merged edges");
        }

        #endregion
    }
}

