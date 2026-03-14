using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using Assets.Scripts.Data;
using Assets.Scripts.Interfaces;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.Visualizers
{
    public abstract class BaseGraphVisualizer : IGraphVisualizer
    {
        private Material cachedLineMaterial;
        private Material cachedDashedMaterial;
        private TextMeshPro measurer;
        private MaterialPropertyBlock propertyBlock;
        protected Material cachedNodeMaterial;
        protected Dictionary<string, GameObject> prefabsDictionary;
        private Dictionary<string, int> edgePairCounts = new Dictionary<string, int>();

        [Header("Constants")]
        protected const float LABEL_FONT_SIZE = 4f;
        protected const float HEADER_FONT_SIZE = 5f;
        protected const float LINE_HEIGHT = 0.8f;
        protected const float Y_ELEVATION = 0.1f;
        protected const float Y_ELEVATION_TEXT_OFFSET = 0.05f;
        private static readonly int COLOR_PROPERTY_ID = Shader.PropertyToID("_BaseColor");

        protected abstract void DrawDiagramContent(GameObject container, List<NodeData> nodes, List<EdgeData> edges);//TODO rename to CreateDiagramContent
        #region Nesting Context
        protected class NestingContext
        {
            public Dictionary<string, List<NodeData>> ParentToChildren { get; }
            public Dictionary<string, string> ChildToParent { get; }
            public HashSet<string> NestedChildKeys { get; }
            public NodeData RootDiagram { get; }

            public NestingContext(
                Dictionary<string, List<NodeData>> parentToChildren,
                Dictionary<string, string> childToParent,
                HashSet<string> nestedChildKeys,
                NodeData rootDiagram)
            {
                ParentToChildren = parentToChildren;
                ChildToParent = childToParent;
                NestedChildKeys = nestedChildKeys;
                RootDiagram = rootDiagram;
            }

            public int GetDepth(string nodeKey)
            {
                int depth = 0;
                string current = nodeKey;
                while (ChildToParent.ContainsKey(current))
                {
                    current = ChildToParent[current];
                    if (RootDiagram != null && current != RootDiagram.Key)
                        depth++;
                }
                return depth;
            }

            public bool IsContainer(string nodeKey)
            {
                return ParentToChildren.ContainsKey(nodeKey) && ParentToChildren[nodeKey].Count > 0;
            }
        }

        protected NestingContext BuildNestingHierarchy(List<NodeData> nodes, List<EdgeData> edges)
        {
            var parentToChildren = new Dictionary<string, List<NodeData>>();
            var childToParent = new Dictionary<string, string>();
            var nestedChildKeys = new HashSet<string>();
            var nodeLookup = nodes.ToDictionary(n => n.Key);

            foreach (var edge in edges.Where(e => e.Type == DiagramEdgeTypes.NESTED))
            {
                string parentKey = ExtractKeyFromId(edge.From);
                string childKey = ExtractKeyFromId(edge.To);

                nestedChildKeys.Add(childKey);
                childToParent[childKey] = parentKey;

                if (!parentToChildren.ContainsKey(parentKey))
                    parentToChildren[parentKey] = new List<NodeData>();

                if (nodeLookup.TryGetValue(childKey, out var childNode))
                    parentToChildren[parentKey].Add(childNode);
            }

            var rootDiagram = nodes.FirstOrDefault(n => n.Type == DiagramNodeTypes.DIAGRAM && !nestedChildKeys.Contains(n.Key));

            return new NestingContext(parentToChildren, childToParent, nestedChildKeys, rootDiagram);
        }
        #endregion

        protected (GameObject nodesParent, GameObject edgesParent) CreateParentObjects(GameObject container)
        {
            GameObject nodesParent = new GameObject("Nodes");
            GameObject edgesParent = new GameObject("Edges");
            nodesParent.transform.SetParent(container.transform, false);
            edgesParent.transform.SetParent(container.transform, false);
            return (nodesParent, edgesParent);
        }

        protected void FilterAndRenderEdges(List<EdgeData> edges, Dictionary<string, GameObject> nodeObjects, Transform edgesParent)
        {
            var validEdges = edges.Where(e => e.Type != DiagramEdgeTypes.NESTED).ToList();
            var selfLoops = validEdges.Where(e => ExtractKeyFromId(e.From) == ExtractKeyFromId(e.To));
            var normalEdges = validEdges.Where(e => ExtractKeyFromId(e.From) != ExtractKeyFromId(e.To));
            DrawDiagramEdges(selfLoops, nodeObjects, edgesParent.gameObject, normalEdges);
        }
        public void Initialize(Dictionary<string, GameObject> prefabs)
        {
            prefabsDictionary = prefabs;
            propertyBlock = new MaterialPropertyBlock();
            if (cachedLineMaterial == null)
                cachedLineMaterial = new Material(Shader.Find("Sprites/Default"));
            if (cachedNodeMaterial == null)
                cachedNodeMaterial = Resources.Load<Material>("Materials/DefaultMat");
            if (cachedDashedMaterial == null)
            {
                cachedDashedMaterial = new Material(Shader.Find("Sprites/Default"));
                cachedDashedMaterial.mainTexture = CreateDashedTexture();
                cachedDashedMaterial.mainTextureScale = Vector2.one;
            }
        }

        public void RenderGraph(GraphMetadata graph, GameObject container, List<NodeData> nodes, List<EdgeData> edges)
        {
            if (nodes == null || nodes.Count == 0)
            {
                Debug.LogWarning("[BaseVisualizer] No nodes to visualize.");
                return;
            }
            edgePairCounts.Clear();
            DrawDiagramContent(container, nodes, edges);
            RenderDiagramBasePlane(container, nodes);
        }

        private void RenderDiagramBasePlane(GameObject container, List<NodeData> nodes)
        {
            var diagramNode = nodes.FirstOrDefault(n => n.Type == DiagramNodeTypes.DIAGRAM);

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

            GameObject plane = CreatePrimitive(PrimitiveType.Cube, container.transform, diagramNode?.GetNodeName() ?? "Diagram_Base", center, Quaternion.Euler(90, 0, 0), new Vector3(width, height, 1));

            ApplyMaterialToSingle(plane, new Color(0.2f, 0.2f, 0.2f, 1.0f));

            Vector3 offset = new Vector3(-totalBounds.center.x, 0, -totalBounds.center.z);

            foreach (Transform child in container.transform)
                child.position += offset;
        }

        #region Material Applying
        protected void ApplyMaterialToSingle(GameObject obj, Color color)
        {
            if (obj.TryGetComponent<Renderer>(out var rend))
            {
                rend.sharedMaterial = cachedNodeMaterial;
                SetRendererColor(rend, color);
            }
        }
        protected void ApplyColorToHierarchy(GameObject obj, Color color)
        {
            foreach (var rend in obj.GetComponentsInChildren<Renderer>())
            {
                SetRendererColor(rend, color);
            }
        }
        private void SetRendererColor(Renderer rend, Color color)
        {
            rend.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(COLOR_PROPERTY_ID, color);
            rend.SetPropertyBlock(propertyBlock);
        }
        private void ApplyEdgeLineMaterial(LineRenderer lr, string edgeType, Vector3 startPoint, Vector3 endPoint)
        {
            bool isDashed = edgeType == DiagramEdgeTypes.INCLUDES_UML
                         || edgeType == DiagramEdgeTypes.EXTENDS_UML
                         || edgeType == DiagramEdgeTypes.DEPENDENCY;

            if (isDashed)
            {
                lr.material = cachedDashedMaterial;
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
                lr.sharedMaterial = cachedLineMaterial;
                lr.textureMode = LineTextureMode.Stretch;
            }
        }
        #endregion

        #region Helpers
        protected string ExtractKeyFromId(string id)//TODO rename to ExtractNodeKey
        {
            if (string.IsNullOrEmpty(id)) return "";
            int slashIndex = id.LastIndexOf('/');
            return slashIndex >= 0 ? id.Substring(slashIndex + 1) : id;
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

        #region Node Rendering
        protected GameObject CreateEmptyGameObject(Transform parentTransform, string name, Vector3 position)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(parentTransform);
            gameObject.transform.localPosition = position;
            return gameObject;
        }
        protected GameObject CreateBackgroundGameObject(Transform parentTransform)
        {
            GameObject background = new GameObject("Background");
            background.transform.SetParent(parentTransform, false);
            background.transform.localPosition = Vector3.zero;
            background.transform.localScale = Vector3.one;
            return background;
        }
        protected GameObject CreatePrimitive(PrimitiveType primitiveType, Transform parentTransform, string objectName, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            GameObject obj = GameObject.CreatePrimitive(primitiveType);
            obj.transform.SetParent(parentTransform, false);
            obj.name = objectName;
            obj.transform.localPosition = position;
            obj.transform.localRotation = rotation;
            obj.transform.localScale = scale;
            return obj;
        }
        protected void GetRecursiveBounds(
            string parentKey,
            Dictionary<string, List<NodeData>> parentToChildren,
            out float minX, out float maxX, out float minZ, out float maxZ,
            float childPaddingX = 0f, float childPaddingZ = 0f)
        {
            minX = minZ = float.MaxValue;
            maxX = maxZ = float.MinValue;

            if (!parentToChildren.ContainsKey(parentKey)) return;

            foreach (var child in parentToChildren[parentKey])
            {
                Vector3 pos = child.GetNodePosition();
                float childMinX = pos.x;
                float childMaxX = pos.x;
                float childMinZ = pos.z;
                float childMaxZ = pos.z;

                if (parentToChildren.ContainsKey(child.Key) && parentToChildren[child.Key].Count > 0)
                {
                    GetRecursiveBounds(child.Key, parentToChildren, out float nestedMinX, out float nestedMaxX, out float nestedMinZ, out float nestedMaxZ, childPaddingX, childPaddingZ);

                    if (nestedMinX != float.MaxValue)
                    {
                        childMinX = Mathf.Min(childMinX, nestedMinX - childPaddingX);
                        childMaxX = Mathf.Max(childMaxX, nestedMaxX + childPaddingX);
                        childMinZ = Mathf.Min(childMinZ, nestedMinZ - childPaddingZ);
                        childMaxZ = Mathf.Max(childMaxZ, nestedMaxZ + childPaddingZ);
                    }
                }

                minX = Mathf.Min(minX, childMinX);
                maxX = Mathf.Max(maxX, childMaxX);
                minZ = Mathf.Min(minZ, childMinZ);
                maxZ = Mathf.Max(maxZ, childMaxZ);
            }
        }
        protected GameObject CreateNodeGameObject(string nodeType, Transform parentTransform, float width, float height, bool useUniformScale = false)
        {
            GameObject visualObject;
            if (prefabsDictionary != null && prefabsDictionary.TryGetValue(nodeType, out GameObject prefab))
            {
                visualObject = Object.Instantiate(prefab, parentTransform);
                visualObject.name = "Visuals";
                visualObject.transform.localPosition = Vector3.zero;
                if (useUniformScale)
                    visualObject.transform.localScale = Vector3.one * width;
                else
                    visualObject.transform.localScale = new Vector3(width, 0.2f, height);
            }
            else
            {
                visualObject = CreatePrimitive(PrimitiveType.Cube, parentTransform, "Background", Vector3.zero, Quaternion.identity, new Vector3(width, 0.2f, height));
            }
            return visualObject;
        }
        #endregion

        #region Edge Rendering
        protected void DrawDiagramEdges(IEnumerable<EdgeData> selfLoops, Dictionary<string, GameObject> nodeObjects, GameObject edgesParent, IEnumerable<EdgeData> normalEdges)//TODO rename to RenderDiagramEdges and change edgesParent to Transform
        {
            foreach (var edge in selfLoops)
            {
                if (nodeObjects.TryGetValue(ExtractKeyFromId(edge.From), out var a))
                {
                    RenderEdge(edgesParent.transform, a, a, edge);
                }
            }

            var edgeGroups = normalEdges.GroupBy(e =>
            {
                string toKey = ExtractKeyFromId(e.To);
                string fromKey = ExtractKeyFromId(e.From);
                string directionSector = "Unknown";

                if (nodeObjects.TryGetValue(fromKey, out GameObject src) && nodeObjects.TryGetValue(toKey, out GameObject tgt))
                {
                    directionSector = GetApproachDirection(src.transform.position, tgt.transform.position);
                }

                return $"{toKey}_{e.Type}_{directionSector}";
            });

            foreach (var group in edgeGroups)
                RenderMergeHubEdges(edgesParent.transform, group.ToList(), nodeObjects);
        }

        private void RenderMergeHubEdges(Transform parent, List<EdgeData> groupedEdges, Dictionary<string, GameObject> nodeObjects)
        {
            if (groupedEdges.Count == 0) return;
            if (groupedEdges.Count == 1)
            {
                var edge = groupedEdges[0];
                if (nodeObjects.TryGetValue(ExtractKeyFromId(edge.From), out var a) && nodeObjects.TryGetValue(ExtractKeyFromId(edge.To), out var b))
                    RenderEdge(parent, a, b, edge);
                return;
            }

            string toKey = ExtractKeyFromId(groupedEdges[0].To);
            if (!nodeObjects.TryGetValue(toKey, out GameObject targetNode)) return;

            Vector3 averageSourcePos = CalculateAverageSourceEdgesPosition(groupedEdges, nodeObjects);
            Vector3 dirToSources = (averageSourcePos - targetNode.transform.position).normalized;
            if (dirToSources == Vector3.zero) dirToSources = Vector3.forward;

            Vector3 farTarget = targetNode.transform.position + (dirToSources * 1000f);
            Vector3 borderPoint = CalculateNodeBorderIntersection(targetNode, farTarget);
            float standoffDistance = 4.5f;
            Vector3 mergePoint = borderPoint + (dirToSources * standoffDistance);

            CreateEdgeMergeHubGameObject(toKey, parent, groupedEdges, nodeObjects, targetNode, mergePoint);
        }

        private void RenderEdge(Transform parent, GameObject fromObj, GameObject toObj, EdgeData edge, bool drawDecorator = true)
        {
            if (fromObj == toObj)
            {
                RenderSelfLoopEdge(parent, fromObj, edge, drawDecorator);
                return;
            }
            Vector3 startPoint = CalculateNodeBorderIntersection(fromObj, toObj.transform.position);
            Vector3 endPoint = CalculateNodeBorderIntersection(toObj, fromObj.transform.position);
            OffsetParallelEdges(fromObj, toObj, ref startPoint, ref endPoint);

            GameObject edgeGo = CreateEdgeGameObject(edge.Type, parent, startPoint, endPoint);
            Vector3 finalDirection = (endPoint - startPoint).normalized;
            if (drawDecorator) AttachEdgeDecorator(edge.Type, edgeGo.transform, startPoint, endPoint, finalDirection);
        }

        private void RenderSelfLoopEdge(Transform parent, GameObject nodeObj, EdgeData edge, bool drawDecorator)
        {
            Vector3 topTarget = nodeObj.transform.position + Vector3.forward * 10f;
            Vector3 rightTarget = nodeObj.transform.position + Vector3.right * 10f;

            Vector3 startPoint = CalculateNodeBorderIntersection(nodeObj, topTarget);
            Vector3 endPoint = CalculateNodeBorderIntersection(nodeObj, rightTarget);
            float loopScale = 3.5f;
            int curveSegments = 10;
            Vector3[] edgePoints = CalculateCubicBezierPoints(curveSegments, startPoint, endPoint, loopScale);

            GameObject edgeGo = CreateEdgeGameObject(edge.Type, parent, startPoint, endPoint, edgePoints, curveSegments + 1);
            Vector3 finalDirection = (endPoint - edgePoints[curveSegments - 1]).normalized;
            if (drawDecorator) AttachEdgeDecorator(edge.Type, edgeGo.transform, startPoint, endPoint, finalDirection);
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

        private void CreateEdgeMergeHubGameObject(string toKey, Transform parent, List<EdgeData> groupedEdges, Dictionary<string, GameObject> nodeObjects, GameObject targetNode, Vector3 mergePoint)
        {
            GameObject hubObj = new GameObject($"Hub_{toKey}_{groupedEdges[0].Type}");
            hubObj.transform.position = mergePoint;
            hubObj.transform.SetParent(parent.transform, false);

            foreach (var edge in groupedEdges)
            {
                if (nodeObjects.TryGetValue(ExtractKeyFromId(edge.From), out GameObject src))
                {
                    RenderEdge(hubObj.transform, src, hubObj, edge, false);
                }
            }
            RenderEdge(hubObj.transform, hubObj, targetNode, groupedEdges[0], true);
        }

        private Vector3 CalculateAverageSourceEdgesPosition(List<EdgeData> groupedEdges, Dictionary<string, GameObject> nodeObjects)
        {
            Vector3 averageSourcePos = Vector3.zero;
            List<GameObject> sourceNodeEdges = new List<GameObject>();
            foreach (var edge in groupedEdges)
            {
                if (nodeObjects.TryGetValue(ExtractKeyFromId(edge.From), out GameObject srcEdge))
                {
                    sourceNodeEdges.Add(srcEdge);
                    averageSourcePos += srcEdge.transform.position;
                }
            }
            return averageSourcePos / sourceNodeEdges.Count;
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

        private GameObject CreateEdgeGameObject(string edgeType, Transform parent, Vector3 startPoint, Vector3 endPoint, Vector3[] points = null, int posCount = 2)
        {
            var edgeGo = new GameObject($"Edge_{edgeType}");
            edgeGo.transform.SetParent(parent, false);

            edgeGo.transform.position = Vector3.zero;
            edgeGo.transform.rotation = Quaternion.identity;

            var lr = edgeGo.AddComponent<LineRenderer>();
            lr.positionCount = posCount;
            lr.useWorldSpace = false;

            lr.startWidth = lr.endWidth = 0.04f;
            lr.startColor = lr.endColor = Color.white;
            if (points != null)
                lr.SetPositions(points);
            else
            {
                lr.SetPosition(0, startPoint);
                lr.SetPosition(1, endPoint);
            }
            ApplyEdgeLineMaterial(lr, edgeType, startPoint, endPoint);
            SetRendererColor(lr, Color.white);

            return edgeGo;
        }

        private void AttachEdgeDecorator(string edgeType, Transform parent, Vector3 startPoint, Vector3 endPoint, Vector3 direction)
        {
            switch (edgeType)
            {
                case DiagramEdgeTypes.AGGREGATES:
                    SpawnDecoratorPrefab(DiagramEdgeTypes.AGGREGATES, parent, startPoint, direction, 1f);
                    break;
                case DiagramEdgeTypes.COMPOSES:
                    SpawnDecoratorPrefab(DiagramEdgeTypes.COMPOSES, parent, startPoint, direction, 1f);
                    break;
                case DiagramEdgeTypes.GENERALIZES:
                    SpawnDecoratorPrefab(DiagramEdgeTypes.GENERALIZES, parent, endPoint, direction, -0.5f);
                    break;
                case DiagramEdgeTypes.INCLUDES_UML:
                case DiagramEdgeTypes.EXTENDS_UML:
                    float extOffset = edgeType == DiagramEdgeTypes.EXTENDS_UML ? -0.2f : -0.3f;
                    SpawnDecoratorPrefab(DiagramEdgeTypes.INCLUDES_UML, parent, endPoint, direction, extOffset);
                    break;

                case DiagramEdgeTypes.DEPENDENCY:
                case DiagramEdgeTypes.TRANSITIONS_TO:
                case DiagramEdgeTypes.FLOWS_TO:
                case DiagramEdgeTypes.OBJECT_FLOW:
                    SpawnDecoratorPrefab(DiagramEdgeTypes.INCLUDES_UML, parent, endPoint, direction, -0.4f);
                    break;
            }
        }

        private void SpawnDecoratorPrefab(string edgeType, Transform parent, Vector3 basePosition, Vector3 direction, float offset)
        {
            if (prefabsDictionary != null && prefabsDictionary.TryGetValue(edgeType, out GameObject prefab))
            {
                GameObject obj = Object.Instantiate(prefab, parent);
                obj.transform.position = basePosition + (direction * offset);
                obj.transform.rotation = Quaternion.LookRotation(direction);
            }
        }
        #endregion

        #region Text Rendering (Original Methods Preserved)
        protected GameObject CreateTextLabel(Transform parent, string text, Vector3 localPos, float width, float fontSize, TextAlignmentOptions textAlignment = TextAlignmentOptions.Center, FontStyles fontStyle = FontStyles.Normal)
        {
            var textObj = new GameObject("Text_" + text);
            textObj.transform.SetParent(parent.transform, false);
            textObj.transform.localPosition = localPos;
            textObj.transform.localRotation = Quaternion.Euler(90, 0, 0);

            var tmp = textObj.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = fontSize;

            tmp.fontStyle = fontStyle;
            tmp.color = Color.black;
            tmp.alignment = textAlignment;

            tmp.rectTransform.sizeDelta = new Vector2(width, 2f);

            return textObj;
        }

        private void EnsureMeasurerExists()
        {
            if (measurer != null) return;

            var go = new GameObject("TextMeasurer");

            measurer = go.AddComponent<TextMeshPro>();
            measurer.enableAutoSizing = false;
            measurer.fontSize = 10;
            measurer.fontStyle = FontStyles.Normal;
            measurer.alignment = TextAlignmentOptions.Left;
            measurer.overflowMode = TextOverflowModes.Overflow;
            measurer.gameObject.SetActive(false);
        }

        protected float MeasureText(string content, float fontSize, bool isBold = false)
        {
            if (string.IsNullOrEmpty(content)) return 0f;
            EnsureMeasurerExists();
            measurer.fontSize = fontSize;
            measurer.fontStyle = isBold ? FontStyles.Bold : FontStyles.Normal;
            measurer.text = content;
            measurer.ForceMeshUpdate(true);
            float width = measurer.preferredWidth;
            return width;
        }
        #endregion
    }
}