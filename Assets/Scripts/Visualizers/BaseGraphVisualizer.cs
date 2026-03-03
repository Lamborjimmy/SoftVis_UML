using System.Collections.Generic;
using System.Linq;
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
        protected Material cachedNodeMaterial;
        protected Dictionary<string, GameObject> prefabsDictionary;
        private Dictionary<string, int> edgePairCounts = new Dictionary<string, int>();

        protected abstract void DrawDiagramContent(GameObject container, List<NodeData> nodes, List<EdgeData> edges);

        [Header("Constants")]
        protected const float LABEL_FONT_SIZE = 4f;
        protected const float HEADER_FONT_SIZE = 5f;
        protected const float LINE_HEIGHT = 0.8f;
        protected const float Y_ELEVATION = 0.1f;
        protected const float Y_ELEVATION_TEXT_OFFSET = 0.05f;

        public void Initialize(Dictionary<string, GameObject> prefabs)
        {
            prefabsDictionary = prefabs;
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
            if (nodes == null || nodes.Count == 0) return;

            edgePairCounts.Clear();

            DrawDiagramContent(container, nodes, edges);

            VisualizeDiagramPlane(container, nodes);
        }

        private void VisualizeDiagramPlane(GameObject container, List<NodeData> nodes)
        {
            var diagramNode = nodes.FirstOrDefault(n => n.Type == DiagramNodeTypes.DIAGRAM);

            // Grab all the 3D models we just generated inside the container
            Renderer[] allRenderers = container.GetComponentsInChildren<Renderer>();

            if (allRenderers.Length == 0) return;

            // Initialize the bounding box using the first valid renderer
            Bounds totalBounds = new Bounds(allRenderers[0].bounds.center, Vector3.zero);

            // Encapsulate every visible object into our mathematical bounding box
            foreach (var rend in allRenderers)
            {
                // Skip disabled renderers (like the invisible edge-routing "Background" boxes)
                if (!rend.enabled) continue;
                totalBounds.Encapsulate(rend.bounds);
            }

            float padding = 5f;
            float width = totalBounds.size.x + (padding * 2);
            float height = totalBounds.size.z + (padding * 2);

            // Find the true center of the rendered objects in world space
            Vector3 center = new Vector3(totalBounds.center.x, -0.5f, totalBounds.center.z);

            // Generate the Plane
            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plane.name = diagramNode?.Label ?? "Diagram_Base";
            plane.transform.SetParent(container.transform, false);

            plane.transform.position = center;
            plane.transform.localRotation = Quaternion.Euler(90, 0, 0);
            plane.transform.localScale = new Vector3(width, height, 1);

            if (plane.TryGetComponent<Renderer>(out var rendMat))
            {
                rendMat.material = cachedNodeMaterial;
                rendMat.material.color = new Color(0.2f, 0.2f, 0.2f, 1.0f);
            }

            Vector3 offset = new Vector3(-totalBounds.center.x, 0, -totalBounds.center.z);

            foreach (Transform child in container.transform)
                child.position += offset;
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

        protected string ExtractKeyFromId(string id)
        {
            if (string.IsNullOrEmpty(id)) return "";
            int slashIndex = id.LastIndexOf('/');
            return slashIndex >= 0 ? id.Substring(slashIndex + 1) : id;
        }

        protected void DrawEdge(GameObject parent, GameObject fromObj, GameObject toObj, EdgeData edge, bool drawDecorator = true)
        {
            if (fromObj == toObj)
            {
                DrawSelfLoop(parent, fromObj, edge);
                return;
            }
            Vector3 startPoint = GetBorderPoint(fromObj, toObj.transform.position);
            Vector3 endPoint = GetBorderPoint(toObj, fromObj.transform.position);

            // --- PARALLEL EDGE OFFSET LOGIC ---
            // Create a unique key for this pair of nodes regardless of direction
            string pairKey = fromObj.GetInstanceID() < toObj.GetInstanceID()
                ? $"{fromObj.GetInstanceID()}_{toObj.GetInstanceID()}"
                : $"{toObj.GetInstanceID()}_{fromObj.GetInstanceID()}";

            if (!edgePairCounts.ContainsKey(pairKey))
                edgePairCounts[pairKey] = 0;

            int count = edgePairCounts[pairKey];
            edgePairCounts[pairKey]++;

            // If there are multiple edges, push them perpendicularly apart
            if (count > 0)
            {
                Vector3 dir = (endPoint - startPoint).normalized;
                Vector3 perp = Vector3.Cross(dir, Vector3.up).normalized;

                float offsetAmount = 0.8f * ((count + 1) / 2) * (count % 2 != 0 ? 1 : -1);

                startPoint += perp * offsetAmount;
                endPoint += perp * offsetAmount;
            }

            var edgeGo = new GameObject($"Edge_{edge.Type}");
            edgeGo.transform.SetParent(parent.transform, false);

            var lr = edgeGo.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.useWorldSpace = false;
            lr.SetPosition(0, startPoint);
            lr.SetPosition(1, endPoint);

            lr.startWidth = lr.endWidth = 0.04f;
            lr.startColor = lr.endColor = Color.white;

            bool isDashed = (edge.Type == DiagramEdgeTypes.INCLUDES_UML) || (edge.Type == DiagramEdgeTypes.EXTENDS_UML) || (edge.Type == DiagramEdgeTypes.DEPENDENCY);

            if (isDashed)
            {
                lr.material = cachedDashedMaterial;
                lr.textureMode = LineTextureMode.Tile;

                float lineLength = Vector3.Distance(startPoint, endPoint);
                float dashPeriodWorldUnits = 0.35f;   // ← tweak this to change dash spacing

                Vector2 tiling = new Vector2(lineLength / dashPeriodWorldUnits, 1f);

                var block = new MaterialPropertyBlock();
                block.SetVector("_MainTex_ST", new Vector4(tiling.x, tiling.y, 0f, 0f));
                lr.SetPropertyBlock(block);
            }
            else
            {
                lr.material = cachedLineMaterial;
                lr.textureMode = LineTextureMode.Stretch;
            }

            Vector3 finalDirection = (endPoint - startPoint).normalized;

            if (drawDecorator)
            {
                switch (edge.Type)
                {
                    case DiagramEdgeTypes.AGGREGATES:
                        SpawnEdgeDecorator(DiagramEdgeTypes.AGGREGATES, parent.transform, startPoint, finalDirection, 1f);
                        break;
                    case DiagramEdgeTypes.COMPOSES:
                        SpawnEdgeDecorator(DiagramEdgeTypes.COMPOSES, parent.transform, startPoint, finalDirection, 1f);
                        break;
                    case DiagramEdgeTypes.GENERALIZES:
                        SpawnEdgeDecorator(DiagramEdgeTypes.GENERALIZES, parent.transform, endPoint, finalDirection, -0.5f);
                        break;
                    case DiagramEdgeTypes.INCLUDES_UML:
                        SpawnEdgeDecorator(DiagramEdgeTypes.INCLUDES_UML, parent.transform, endPoint, finalDirection, -0.3f);
                        break;
                    case DiagramEdgeTypes.EXTENDS_UML:
                        SpawnEdgeDecorator(DiagramEdgeTypes.INCLUDES_UML, parent.transform, endPoint, finalDirection, -0.2f);
                        break;
                    case DiagramEdgeTypes.DEPENDENCY:
                        SpawnEdgeDecorator(DiagramEdgeTypes.INCLUDES_UML, parent.transform, endPoint, finalDirection, -0.4f);
                        break;
                    case DiagramEdgeTypes.TRANSITIONS_TO:
                        SpawnEdgeDecorator(DiagramEdgeTypes.INCLUDES_UML, parent.transform, endPoint, finalDirection, -0.4f);
                        break;
                }
            }
        }
        protected void DrawBundledEdges(GameObject parent, List<EdgeData> groupedEdges, Dictionary<string, GameObject> nodeObjects)
        {
            if (groupedEdges.Count == 0) return;

            // If only 1 edge in the group, draw it normally
            if (groupedEdges.Count == 1)
            {
                var edge = groupedEdges[0];
                if (nodeObjects.TryGetValue(ExtractKeyFromId(edge.From), out var a) && nodeObjects.TryGetValue(ExtractKeyFromId(edge.To), out var b))
                {
                    DrawEdge(parent, a, b, edge);
                }
                return;
            }

            // --- Multiple Edges: Create a Merge Hub ---
            string toKey = ExtractKeyFromId(groupedEdges[0].To);
            if (!nodeObjects.TryGetValue(toKey, out GameObject targetObj)) return;

            // 1. Calculate the average position of all source nodes
            Vector3 averageSourcePos = Vector3.zero;
            List<GameObject> sourceObjs = new List<GameObject>();

            foreach (var edge in groupedEdges)
            {
                if (nodeObjects.TryGetValue(ExtractKeyFromId(edge.From), out GameObject src))
                {
                    sourceObjs.Add(src);
                    averageSourcePos += src.transform.position;
                }
            }

            if (sourceObjs.Count == 0) return;
            averageSourcePos /= sourceObjs.Count;

            // 2. Determine the direction the edges are approaching from
            Vector3 dirToSources = (averageSourcePos - targetObj.transform.position).normalized;
            if (dirToSources == Vector3.zero) dirToSources = Vector3.forward;

            // 3. Find the exact outer edge of the target node facing the incoming lines
            Vector3 farTarget = targetObj.transform.position + (dirToSources * 1000f);
            Vector3 borderPoint = GetBorderPoint(targetObj, farTarget);

            // 4. Place the hub safely OUTSIDE the visual bounds of the node
            float standoffDistance = 4.5f; // Hub will be exactly 2.5 units outside the border
            Vector3 mergePoint = borderPoint + (dirToSources * standoffDistance);

            // 5. Create a temporary invisible Hub object to act as a routing node
            GameObject hubObj = new GameObject($"Hub_{toKey}_{groupedEdges[0].Type}");
            hubObj.transform.position = mergePoint;
            hubObj.transform.SetParent(parent.transform, false);

            // 6. Draw lines from all sources to the Hub (drawDecorator = false)
            foreach (var edge in groupedEdges)
            {
                if (nodeObjects.TryGetValue(ExtractKeyFromId(edge.From), out GameObject src))
                {
                    DrawEdge(parent, src, hubObj, edge, false);
                }
            }

            // 7. Draw one final line from the Hub to the Target (drawDecorator = true)
            DrawEdge(parent, hubObj, targetObj, groupedEdges[0], true);
        }
        protected void DrawDiagramEdges(IEnumerable<EdgeData> selfLoops, Dictionary<string, GameObject> nodeObjects, GameObject edgesParent, IEnumerable<EdgeData> normalEdges)
        {
            foreach (var edge in selfLoops)
            {
                if (nodeObjects.TryGetValue(ExtractKeyFromId(edge.From), out var a))
                {
                    DrawEdge(edgesParent, a, a, edge);
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
            {
                DrawBundledEdges(edgesParent, group.ToList(), nodeObjects);
            }
        }
        protected string GetApproachDirection(Vector3 sourcePos, Vector3 targetPos)
        {
            Vector3 dir = (sourcePos - targetPos).normalized;

            float angle = Mathf.Atan2(dir.z, dir.x) * Mathf.Rad2Deg;

            if (angle < 0) angle += 360f;

            int numOfSectors = 4;
            float degrees = 360 / numOfSectors;
            Debug.Log(degrees);
            int sector = Mathf.RoundToInt(angle / degrees) % numOfSectors;

            return sector.ToString();
        }
        private void DrawSelfLoop(GameObject parent, GameObject nodeObj, EdgeData edge)
        {

            // 1.Create fake targets to start drawing from edges of the node
            Vector3 topTarget = nodeObj.transform.position + Vector3.forward * 10f;
            Vector3 rightTarget = nodeObj.transform.position + Vector3.right * 10f;

            Vector3 startPoint = GetBorderPoint(nodeObj, topTarget);
            Vector3 endPoint = GetBorderPoint(nodeObj, rightTarget);

            // 2. Define Control Points 
            float loopScale = 3.5f; // Increase this if the loop is too tight
            Vector3 cp1 = startPoint + Vector3.forward * loopScale + Vector3.right * (loopScale * 0.5f);
            Vector3 cp2 = endPoint + Vector3.right * loopScale + Vector3.forward * (loopScale * 0.5f);

            // 3. Generate Bezier curve points
            int curveSegments = 10;
            Vector3[] points = new Vector3[curveSegments + 1];
            for (int i = 0; i <= curveSegments; i++)
            {
                float t = i / (float)curveSegments;
                points[i] = CalculateCubicBezierPoint(t, startPoint, cp1, cp2, endPoint);
            }

            // 4. Create and configure the LineRenderer
            var edgeGo = new GameObject($"Edge_SelfLoop_{edge.Type}");
            edgeGo.transform.SetParent(parent.transform, false);

            var lr = edgeGo.AddComponent<LineRenderer>();
            lr.positionCount = curveSegments + 1;
            lr.SetPositions(points);
            lr.useWorldSpace = false;
            lr.startWidth = lr.endWidth = 0.04f;
            lr.startColor = lr.endColor = Color.white;

            // Apply Materials
            bool isDashed = (edge.Type == DiagramEdgeTypes.INCLUDES_UML) || (edge.Type == DiagramEdgeTypes.EXTENDS_UML) || (edge.Type == DiagramEdgeTypes.DEPENDENCY);
            if (isDashed)
            {
                lr.material = cachedDashedMaterial;
                lr.textureMode = LineTextureMode.Tile;

                float approxLength = Vector3.Distance(startPoint, cp1) + Vector3.Distance(cp1, cp2) + Vector3.Distance(cp2, endPoint);
                Vector2 tiling = new Vector2(approxLength / 0.35f, 1f);

                var block = new MaterialPropertyBlock();
                block.SetVector("_MainTex_ST", new Vector4(tiling.x, tiling.y, 0f, 0f));
                lr.SetPropertyBlock(block);
            }
            else
            {
                lr.material = cachedLineMaterial;
                lr.textureMode = LineTextureMode.Stretch;
            }

            // 5. Spawn the Decorator
            Vector3 finalDirection = (endPoint - points[curveSegments - 1]).normalized;
            switch (edge.Type)
            {
                case DiagramEdgeTypes.AGGREGATES:
                    SpawnEdgeDecorator(DiagramEdgeTypes.AGGREGATES, parent.transform, endPoint, finalDirection, 1f);
                    break;
                case DiagramEdgeTypes.COMPOSES:
                    SpawnEdgeDecorator(DiagramEdgeTypes.COMPOSES, parent.transform, endPoint, finalDirection, 1f);
                    break;
                case DiagramEdgeTypes.GENERALIZES:
                    SpawnEdgeDecorator(DiagramEdgeTypes.GENERALIZES, parent.transform, endPoint, finalDirection, -0.5f);
                    break;
                case DiagramEdgeTypes.INCLUDES_UML:
                case DiagramEdgeTypes.EXTENDS_UML:
                case DiagramEdgeTypes.DEPENDENCY:
                case DiagramEdgeTypes.TRANSITIONS_TO:
                    SpawnEdgeDecorator(DiagramEdgeTypes.INCLUDES_UML, parent.transform, endPoint, finalDirection, -0.4f);
                    break;
            }
        }

        private Vector3 CalculateCubicBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            Vector3 q0 = Vector3.Lerp(p0, p1, t);
            Vector3 q1 = Vector3.Lerp(p1, p2, t);
            Vector3 q2 = Vector3.Lerp(p2, p3, t);

            Vector3 r0 = Vector3.Lerp(q0, q1, t);
            Vector3 r1 = Vector3.Lerp(q1, q2, t);

            return Vector3.Lerp(r0, r1, t); ;
        }
        private void SpawnEdgeDecorator(string edgeType, Transform parent, Vector3 basePosition, Vector3 direction, float offset)
        {
            if (prefabsDictionary != null && prefabsDictionary.TryGetValue(edgeType, out GameObject prefab))
            {
                GameObject obj = Object.Instantiate(prefab, parent);
                obj.transform.localPosition = basePosition + (direction * offset);
                obj.transform.localRotation = Quaternion.LookRotation(direction);
            }
        }
        private Vector3 GetBorderPoint(GameObject classObj, Vector3 targetPosition)
        {
            Transform background = classObj.transform.Find("Background");
            if (background == null)
            {
                return classObj.transform.position;
            }

            Vector3 targetLocal = classObj.transform.InverseTransformPoint(targetPosition);
            Vector3 dir = targetLocal.normalized;
            if (dir == Vector3.zero) return classObj.transform.position;

            // 1. Calculate the true visual bounds using Renderers instead of just localScale
            Renderer[] renderers = background.GetComponentsInChildren<Renderer>();
            Bounds localBounds = new Bounds(Vector3.zero, Vector3.zero);

            if (renderers.Length > 0)
            {
                // Initialize bounds with the first renderer's true local position
                localBounds = new Bounds(classObj.transform.InverseTransformPoint(renderers[0].bounds.center), Vector3.zero);
                foreach (var r in renderers)
                {
                    // Encapsulate true world bounds converted to local space
                    localBounds.Encapsulate(classObj.transform.InverseTransformPoint(r.bounds.min));
                    localBounds.Encapsulate(classObj.transform.InverseTransformPoint(r.bounds.max));
                }
            }
            else
            {
                // Fallback for empty objects without renderers
                localBounds = new Bounds(Vector3.zero, background.localScale);
            }

            Vector3 minLocal = localBounds.min;
            Vector3 maxLocal = localBounds.max;

            float tMin = float.MaxValue;

            // 2. Intersect ray (from center 0,0,0) with the true local AABB
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

            if (tMin == float.MaxValue || tMin <= 0.001f)
            {
                return classObj.transform.position;
            }

            Vector3 localHit = dir * tMin;
            localHit += localHit.normalized * 0.03f; // Slight offset outward to prevent clipping

            return classObj.transform.TransformPoint(localHit);
        }
        protected GameObject CreateTextLabel(Transform parent, string text, Vector3 localPos, float width, float fontSize, TextAlignmentOptions textAlignment = TextAlignmentOptions.Center, FontStyles fontStyle = FontStyles.Normal)
        {
            GameObject textObj = new GameObject("Text_" + text);
            textObj.transform.SetParent(parent, false);
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
    }
}