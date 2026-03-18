using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Data;
using Assets.Scripts.Interfaces;
using Assets.Scripts.Models;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.Renderers.Unity
{
    public class UnityDiagramRenderer : IDiagramRenderer
    {
        private readonly Dictionary<string, GameObject> prefabs;
        private Material lineMaterial;
        private Material dashedMaterial;
        private Material nodeMaterial;
        private MaterialPropertyBlock propertyBlock;

        private Dictionary<string, int> edgePairCounts;

        private static readonly int COLOR_PROPERTY_ID = Shader.PropertyToID("_BaseColor");

        public UnityDiagramRenderer(Dictionary<string, GameObject> prefabs)
        {
            this.prefabs = prefabs;
            propertyBlock = new MaterialPropertyBlock();
            edgePairCounts = new Dictionary<string, int>();

            InitializeMaterials();
        }

        private void InitializeMaterials()
        {
            if (lineMaterial == null)
                lineMaterial = new Material(Shader.Find("Sprites/Default"));

            if (nodeMaterial == null)
                nodeMaterial = Resources.Load<Material>("Materials/DefaultMat");

            if (dashedMaterial == null)
            {
                dashedMaterial = new Material(Shader.Find("Sprites/Default"));
                dashedMaterial.mainTexture = CreateDashedTexture();
                dashedMaterial.mainTextureScale = Vector2.one;
            }
        }

        public void Render(DiagramModel diagram, object container)
        {
            var parent = (GameObject)container;
            edgePairCounts.Clear();

            var nodesParent = new GameObject("Nodes");
            nodesParent.transform.SetParent(parent.transform, false);

            var edgesParent = new GameObject("Edges");
            edgesParent.transform.SetParent(parent.transform, false);

            var nodeGameObjects = new Dictionary<string, GameObject>();

            foreach (var nodeModel in diagram.Nodes)
            {
                var nodeGO = RenderNode(nodeModel, nodesParent.transform);
                nodeGameObjects[nodeModel.Id] = nodeGO;

                foreach (var childKey in nodeModel.ChildKeys)
                    nodeGameObjects[childKey] = nodeGO;
            }

            RenderEdges(diagram.Edges, edgesParent.transform, nodeGameObjects);

            RenderBasePlane(diagram, parent);
        }

        public void Clear(object container)
        {
            var go = (GameObject)container;
            foreach (Transform child in go.transform)
                Object.Destroy(child.gameObject);
        }

        #region Node Rendering
        private GameObject RenderNode(NodeModel model, Transform parent)
        {
            var nodeGO = new GameObject("Node_" + model.Label);
            nodeGO.transform.SetParent(parent, false);
            nodeGO.transform.localPosition = ToUnityVector(model.Position);

            var bgGroup = new GameObject("Background");
            bgGroup.transform.SetParent(nodeGO.transform, false);
            bgGroup.transform.localPosition = Vector3.zero;

            GameObject visuals = CreateNodeVisuals(model, bgGroup.transform);
            ApplyColorToHierarchy(visuals, model.BackgroundColor);

            foreach (var label in model.Labels)
                RenderTextLabel(label, bgGroup.transform);

            return nodeGO;
        }

        private GameObject CreateNodeVisuals(NodeModel model, Transform parent)
        {
            GameObject visualsObj;

            if (prefabs != null && prefabs.TryGetValue(model.NodeType, out GameObject prefab))
            {
                visualsObj = Object.Instantiate(prefab, parent);
                visualsObj.name = "Visuals";
                visualsObj.transform.localPosition = Vector3.zero;

                if (model.UseUniformScale)
                    visualsObj.transform.localScale = Vector3.one * model.Scale.X;
                else
                    visualsObj.transform.localScale = ToUnityVector(model.Scale);

                ConfigureSpecialPrefabs(model, visualsObj);
            }
            else
            {
                visualsObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visualsObj.name = "Visuals";
                visualsObj.transform.SetParent(parent, false);
                visualsObj.transform.localPosition = Vector3.zero;
                visualsObj.transform.localScale = ToUnityVector(model.Scale);
            }

            return visualsObj;
        }

        private void ConfigureSpecialPrefabs(NodeModel vm, GameObject visualsObj)
        {
            if (vm.NodeType == DiagramNodeTypes.PACKAGE)
            {
                float nodeWidth = vm.Scale.X;
                float totalHeight = vm.Scale.Z;
                float currentTabHeight = 1.5f;
                float nodeHeight = totalHeight - currentTabHeight;

                if (visualsObj.transform.childCount > 0)
                {
                    float tabWorldWidth = Mathf.Min(2.5f, nodeWidth * 0.5f);

                    Transform pkgBody = visualsObj.transform.Find("Package");
                    if (pkgBody == null) pkgBody = visualsObj.transform.GetChild(0);

                    if (pkgBody != null)
                    {
                        pkgBody.localScale = new Vector3(1f, 1f, nodeHeight / totalHeight);
                        pkgBody.localPosition = new Vector3(0f, 0f, -currentTabHeight / (2f * totalHeight));
                    }

                    Transform tabBody = visualsObj.transform.Find("Tab");
                    if (tabBody == null && visualsObj.transform.childCount > 1)
                        tabBody = visualsObj.transform.GetChild(1);

                    if (tabBody != null)
                    {
                        tabBody.localScale = new Vector3(tabWorldWidth / nodeWidth, 1f, currentTabHeight / totalHeight);
                        float tabLocalX = -0.5f + (tabWorldWidth / (2f * nodeWidth));
                        float tabLocalZ = 0.5f - (currentTabHeight / (2f * totalHeight));
                        tabBody.localPosition = new Vector3(tabLocalX, 0f, tabLocalZ);
                    }
                }
            }
        }

        private void RenderTextLabel(TextLabelModel label, Transform parent)
        {
            var textGO = new GameObject("Text_" + label.Text);
            textGO.transform.SetParent(parent, false);
            textGO.transform.localPosition = ToUnityVector(label.Position);
            textGO.transform.localRotation = Quaternion.Euler(90, 0, 0);

            var tmp = textGO.AddComponent<TextMeshPro>();
            tmp.text = label.Text;
            tmp.fontSize = label.FontSize;
            tmp.fontStyle = ToUnityFontStyle(label.Style);
            tmp.color = ToUnityColor(label.Color);
            tmp.alignment = ToUnityAlignment(label.Alignment);
            tmp.rectTransform.sizeDelta = new Vector2(label.Width, 2f);
        }
        #endregion

        #region Edge Rendering
        private void RenderEdges(List<EdgeModel> edges, Transform parent, Dictionary<string, GameObject> nodeGOs)
        {
            var selfLoops = edges.Where(e => e.IsSelfLoop);
            var normalEdges = edges.Where(e => !e.IsSelfLoop);

            foreach (var edge in selfLoops)
            {
                if (nodeGOs.TryGetValue(edge.FromId, out var nodeGO))
                    RenderSelfLoopEdge(parent, nodeGO, edge);
            }

            var edgeGroups = normalEdges.GroupBy(e =>
            {
                string directionSector = "Unknown";

                if (nodeGOs.TryGetValue(e.FromId, out GameObject src) && nodeGOs.TryGetValue(e.ToId, out GameObject tgt))
                    directionSector = GetApproachDirection(src.transform.position, tgt.transform.position);

                return $"{e.ToId}_{e.EdgeType}_{directionSector}";
            });

            foreach (var group in edgeGroups)
                RenderMergeHubEdges(parent, group.ToList(), nodeGOs);
        }

        private void RenderMergeHubEdges(Transform parent, List<EdgeModel> groupedEdges, Dictionary<string, GameObject> nodeGOs)
        {
            if (groupedEdges.Count == 0) return;

            if (groupedEdges.Count == 1)
            {
                var edge = groupedEdges[0];
                if (nodeGOs.TryGetValue(edge.FromId, out var fromGO) && nodeGOs.TryGetValue(edge.ToId, out var toGO))
                    RenderEdge(parent, fromGO, toGO, edge, true);
                return;
            }

            string toKey = groupedEdges[0].ToId;
            if (!nodeGOs.TryGetValue(toKey, out GameObject targetNode)) return;

            Vector3 averageSourcePos = CalculateAverageSourcePosition(groupedEdges, nodeGOs);
            Vector3 dirToSources = (averageSourcePos - targetNode.transform.position).normalized;
            if (dirToSources == Vector3.zero) dirToSources = Vector3.forward;

            Vector3 farTarget = targetNode.transform.position + (dirToSources * 1000f);
            Vector3 borderPoint = CalculateNodeBorderIntersection(targetNode, farTarget);
            float standoffDistance = 4.5f;
            Vector3 mergePoint = borderPoint + (dirToSources * standoffDistance);

            GameObject hubObj = new GameObject($"Hub_{toKey}_{groupedEdges[0].EdgeType}");
            hubObj.transform.position = mergePoint;
            hubObj.transform.SetParent(parent, false);

            foreach (var edge in groupedEdges)
            {
                if (nodeGOs.TryGetValue(edge.FromId, out GameObject src))
                    RenderEdge(hubObj.transform, src, hubObj, edge, false);
            }

            RenderEdge(hubObj.transform, hubObj, targetNode, groupedEdges[0], true);
        }

        private void RenderEdge(Transform parent, GameObject fromObj, GameObject toObj, EdgeModel edge, bool drawDecorator)
        {
            if (fromObj == toObj)
            {
                RenderSelfLoopEdge(parent, fromObj, edge);
                return;
            }

            Vector3 startPoint = CalculateNodeBorderIntersection(fromObj, toObj.transform.position);
            Vector3 endPoint = CalculateNodeBorderIntersection(toObj, fromObj.transform.position);
            OffsetParallelEdges(fromObj, toObj, ref startPoint, ref endPoint);

            GameObject edgeGO = CreateEdgeGameObject(edge, parent, startPoint, endPoint);
            Vector3 finalDirection = (endPoint - startPoint).normalized;

            if (drawDecorator)
            {
                if (edge.EndDecorator != DecoratorType.None)
                    AttachDecorator(edge.EndDecorator, edge.EdgeType, edgeGO.transform, endPoint, finalDirection);
                if (edge.StartDecorator != DecoratorType.None)
                    AttachDecorator(edge.StartDecorator, edge.EdgeType, edgeGO.transform, startPoint, -finalDirection);
            }
        }

        private void RenderSelfLoopEdge(Transform parent, GameObject nodeObj, EdgeModel edge)
        {
            Vector3 topTarget = nodeObj.transform.position + Vector3.forward * 10f;
            Vector3 rightTarget = nodeObj.transform.position + Vector3.right * 10f;

            Vector3 startPoint = CalculateNodeBorderIntersection(nodeObj, topTarget);
            Vector3 endPoint = CalculateNodeBorderIntersection(nodeObj, rightTarget);

            float loopScale = 3.5f;
            int curveSegments = 10;
            Vector3[] edgePoints = CalculateCubicBezierPoints(curveSegments, startPoint, endPoint, loopScale);

            GameObject edgeGO = CreateEdgeGameObject(edge, parent, startPoint, endPoint, edgePoints, curveSegments + 1);
            Vector3 finalDirection = (endPoint - edgePoints[curveSegments - 1]).normalized;

            if (edge.EndDecorator != DecoratorType.None)
                AttachDecorator(edge.EndDecorator, edge.EdgeType, edgeGO.transform, endPoint, finalDirection);
        }

        private GameObject CreateEdgeGameObject(EdgeModel edge, Transform parent, Vector3 startPoint, Vector3 endPoint, Vector3[] points = null, int posCount = 2)
        {
            var edgeGO = new GameObject($"Edge_{edge.EdgeType}");
            edgeGO.transform.SetParent(parent, false);
            edgeGO.transform.position = Vector3.zero;
            edgeGO.transform.rotation = Quaternion.identity;

            var lr = edgeGO.AddComponent<LineRenderer>();
            lr.positionCount = posCount;
            lr.useWorldSpace = false;
            lr.startWidth = lr.endWidth = edge.LineWidth;
            lr.startColor = lr.endColor = ToUnityColor(edge.LineColor);

            if (points != null)
                lr.SetPositions(points);
            else
            {
                lr.SetPosition(0, startPoint);
                lr.SetPosition(1, endPoint);
            }

            ApplyEdgeLineMaterial(lr, edge, startPoint, endPoint);

            return edgeGO;
        }

        private void ApplyEdgeLineMaterial(LineRenderer lr, EdgeModel edge, Vector3 startPoint, Vector3 endPoint)
        {
            if (edge.IsDashed)
            {
                lr.material = dashedMaterial;
                lr.textureMode = LineTextureMode.Tile;

                float lineLength = Vector3.Distance(startPoint, endPoint);
                float dashPeriodWorldUnits = 0.35f;

                Vector2 tiling = new Vector2(lineLength / dashPeriodWorldUnits, 1f);

                lr.GetPropertyBlock(propertyBlock);
                propertyBlock.SetVector("_MainTex_ST", new Vector4(tiling.x, tiling.y, 0f, 0f));
                lr.SetPropertyBlock(propertyBlock);
            }
            else
            {
                lr.sharedMaterial = lineMaterial;
                lr.textureMode = LineTextureMode.Stretch;
            }

            SetRendererColor(lr, Color.white);
        }

        private void AttachDecorator(DecoratorType decoratorType, string edgeType, Transform parent, Vector3 position, Vector3 direction)
        {
            string prefabKey = GetDecoratorPrefabKey(decoratorType, edgeType);

            if (prefabKey != null && prefabs != null && prefabs.TryGetValue(prefabKey, out var prefab))
            {
                float offset = GetDecoratorOffset(decoratorType, edgeType);
                var decorator = Object.Instantiate(prefab, parent);
                decorator.transform.position = position + (direction * offset);
                decorator.transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        private string GetDecoratorPrefabKey(DecoratorType type, string edgeType)
        {
            return type switch
            {
                DecoratorType.Arrow => DiagramEdgeTypes.INCLUDES_UML,
                DecoratorType.DiamondHollow => DiagramEdgeTypes.AGGREGATES,
                DecoratorType.DiamondFilled => DiagramEdgeTypes.COMPOSES,
                DecoratorType.Triangle => DiagramEdgeTypes.GENERALIZES,
                _ => null
            };
        }

        private float GetDecoratorOffset(DecoratorType type, string edgeType)
        {
            return type switch
            {
                DecoratorType.DiamondHollow => -1f,
                DecoratorType.DiamondFilled => -1f,
                DecoratorType.Triangle => -0.5f,
                DecoratorType.Arrow => edgeType == DiagramEdgeTypes.EXTENDS_UML ? -0.2f : -0.4f,
                _ => 0f
            };
        }
        #endregion

        #region Edge Calculation Helpers
        private Vector3 CalculateNodeBorderIntersection(GameObject nodeObj, Vector3 targetPosition)
        {
            Transform visualsContainer = nodeObj.transform.Find("Background");
            if (visualsContainer == null) return nodeObj.transform.position;

            Vector3 targetLocal = nodeObj.transform.InverseTransformPoint(targetPosition);
            Vector3 dir = targetLocal.normalized;
            if (dir == Vector3.zero) return nodeObj.transform.position;

            Renderer[] renderers = visualsContainer.GetComponentsInChildren<Renderer>();
            Bounds localBounds = CalculateNodeLocalBounds(renderers, nodeObj, visualsContainer);
            Vector3 localHit = dir * CalculateMinValue(localBounds, dir);

            float offset = 0.03f;
            localHit += localHit.normalized * offset;

            return nodeObj.transform.TransformPoint(localHit);
        }

        private Bounds CalculateNodeLocalBounds(Renderer[] renderers, GameObject nodeObj, Transform visualsContainer)
        {
            Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
            if (renderers.Length > 0)
            {
                bounds = new Bounds(nodeObj.transform.InverseTransformPoint(renderers[0].bounds.center), Vector3.zero);
                foreach (var r in renderers)
                {
                    if (r.GetComponent<TextMeshPro>() != null) continue;
                    bounds.Encapsulate(nodeObj.transform.InverseTransformPoint(r.bounds.min));
                    bounds.Encapsulate(nodeObj.transform.InverseTransformPoint(r.bounds.max));
                }
            }
            else
            {
                bounds = new Bounds(Vector3.zero, visualsContainer.localScale);
            }
            return bounds;
        }

        private float CalculateMinValue(Bounds bounds, Vector3 dir)
        {
            Vector3 minLocal = bounds.min;
            Vector3 maxLocal = bounds.max;
            float tMin = float.MaxValue;

            if (Mathf.Abs(dir.x) > 0.0001f)
            {
                float tx = (dir.x > 0 ? maxLocal.x : minLocal.x) / dir.x;
                if (tx > 0.001f) tMin = Mathf.Min(tMin, tx);
            }

            if (Mathf.Abs(dir.z) > 0.0001f)
            {
                float tz = (dir.z > 0 ? maxLocal.z : minLocal.z) / dir.z;
                if (tz > 0.001f) tMin = Mathf.Min(tMin, tz);
            }

            if (Mathf.Abs(dir.y) > 0.0001f)
            {
                float ty = (dir.y > 0 ? maxLocal.y : minLocal.y) / dir.y;
                if (ty > 0.001f) tMin = Mathf.Min(tMin, ty);
            }

            return tMin;
        }

        private void OffsetParallelEdges(GameObject fromObj, GameObject toObj, ref Vector3 startPoint, ref Vector3 endPoint)
        {
            string pairKey = fromObj.GetInstanceID() < toObj.GetInstanceID()
                ? $"{fromObj.GetInstanceID()}_{toObj.GetInstanceID()}"
                : $"{toObj.GetInstanceID()}_{fromObj.GetInstanceID()}";

            if (!edgePairCounts.ContainsKey(pairKey)) edgePairCounts[pairKey] = 0;
            int count = edgePairCounts[pairKey];
            edgePairCounts[pairKey]++;

            if (count > 0)
            {
                Vector3 dir = (endPoint - startPoint).normalized;
                Vector3 perp = Vector3.Cross(dir, Vector3.up).normalized;

                float offsetAmount = 0.8f * ((count + 1) / 2) * (count % 2 != 0 ? 1 : -1);

                startPoint += perp * offsetAmount;
                endPoint += perp * offsetAmount;
            }
        }

        private Vector3 CalculateAverageSourcePosition(List<EdgeModel> groupedEdges, Dictionary<string, GameObject> nodeGOs)
        {
            Vector3 averageSourcePos = Vector3.zero;
            int count = 0;

            foreach (var edge in groupedEdges)
            {
                if (nodeGOs.TryGetValue(edge.FromId, out GameObject srcEdge))
                {
                    averageSourcePos += srcEdge.transform.position;
                    count++;
                }
            }

            return count > 0 ? averageSourcePos / count : Vector3.zero;
        }

        private string GetApproachDirection(Vector3 sourcePos, Vector3 targetPos)
        {
            Vector3 dir = (sourcePos - targetPos).normalized;
            float angle = Mathf.Atan2(dir.z, dir.x) * Mathf.Rad2Deg;

            if (angle < 0) angle += 360f;

            int numOfSectors = 4;
            float degrees = 360 / numOfSectors;
            int sector = Mathf.RoundToInt(angle / degrees) % numOfSectors;

            return sector.ToString();
        }

        private Vector3[] CalculateCubicBezierPoints(int segments, Vector3 startPoint, Vector3 endPoint, float loopScale)
        {
            Vector3 controlPoint1 = startPoint + Vector3.forward * loopScale + Vector3.right * loopScale;
            Vector3 controlPoint2 = endPoint + Vector3.right * loopScale + Vector3.forward * loopScale;
            Vector3[] points = new Vector3[segments + 1];

            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                points[i] = CalculateCubicBezierPoint(t, startPoint, controlPoint1, controlPoint2, endPoint);
            }

            return points;
        }

        private Vector3 CalculateCubicBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            Vector3 q0 = Vector3.Lerp(p0, p1, t);
            Vector3 q1 = Vector3.Lerp(p1, p2, t);
            Vector3 q2 = Vector3.Lerp(p2, p3, t);

            Vector3 r0 = Vector3.Lerp(q0, q1, t);
            Vector3 r1 = Vector3.Lerp(q1, q2, t);

            return Vector3.Lerp(r0, r1, t);
        }
        #endregion

        #region Base Plane Rendering
        private void RenderBasePlane(DiagramModel diagram, GameObject container)

        {
            Renderer[] allRenderers = container.GetComponentsInChildren<Renderer>();
            if (allRenderers.Length == 0) return;

            Bounds totalBounds = new Bounds(allRenderers[0].bounds.center, Vector3.zero);

            foreach (var rend in allRenderers)
            {
                if (!rend.enabled) continue;
                totalBounds.Encapsulate(rend.bounds);
            }

            float padding = 2.5f;
            float width = totalBounds.size.x + (padding * 2);
            float height = totalBounds.size.z + (padding * 2);

            Vector3 center = new Vector3(totalBounds.center.x, -0.5f, totalBounds.center.z);

            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plane.transform.SetParent(container.transform, false);
            plane.name = diagram.Name ?? "Diagram_Base";
            plane.transform.localPosition = center;
            plane.transform.localRotation = Quaternion.Euler(90, 0, 0);
            plane.transform.localScale = new Vector3(width, height, 1);

            ApplyMaterialToSingle(plane, diagram.BasePlaneColor);

            Vector3 offset = new Vector3(-totalBounds.center.x, 0, -totalBounds.center.z);

            foreach (Transform child in container.transform)
                child.position += offset;
        }
        #endregion

        #region Material & Color Helpers
        private void ApplyMaterialToSingle(GameObject obj, RGBA color)
        {
            if (obj.TryGetComponent<Renderer>(out var rend))
            {
                rend.sharedMaterial = nodeMaterial;
                SetRendererColor(rend, ToUnityColor(color));
            }
        }

        private void ApplyColorToHierarchy(GameObject obj, RGBA color)
        {
            foreach (var rend in obj.GetComponentsInChildren<Renderer>())
                SetRendererColor(rend, ToUnityColor(color));
        }

        private void SetRendererColor(Renderer rend, Color color)
        {
            rend.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(COLOR_PROPERTY_ID, color);
            rend.SetPropertyBlock(propertyBlock);
        }

        private Texture2D CreateDashedTexture(int dashPixels = 24, int gapPixels = 12, int textureHeight = 8)
        {
            int width = dashPixels + gapPixels;
            var tex = new Texture2D(width, textureHeight, TextureFormat.RGBA32, false);

            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;

            Color dashColor = Color.white;
            Color transparent = new Color(0, 0, 0, 0);

            for (int x = 0; x < width; x++)
            {
                Color c = x < dashPixels ? dashColor : transparent;
                for (int y = 0; y < textureHeight; y++)
                    tex.SetPixel(x, y, c);
            }

            tex.Apply();
            return tex;
        }
        #endregion

        #region Conversion Helpers
        private Vector3 ToUnityVector(Vec3 v) => new Vector3(v.X, v.Y, v.Z);
        private Color ToUnityColor(RGBA c) => new Color(c.R, c.G, c.B, c.A);

        private TextAlignmentOptions ToUnityAlignment(Data.TextAlignment a) => a switch
        {
            Data.TextAlignment.Left => TextAlignmentOptions.Left,
            Data.TextAlignment.Right => TextAlignmentOptions.Right,
            Data.TextAlignment.Top => TextAlignmentOptions.Top,
            Data.TextAlignment.TopLeft => TextAlignmentOptions.TopLeft,
            _ => TextAlignmentOptions.Center
        };

        private FontStyles ToUnityFontStyle(Data.FontStyle s) => s switch
        {
            Data.FontStyle.Bold => FontStyles.Bold,
            Data.FontStyle.Italic => FontStyles.Italic,
            _ => FontStyles.Normal
        };
        #endregion
    }
}
