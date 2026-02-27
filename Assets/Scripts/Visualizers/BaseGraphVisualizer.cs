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
            VisualizeDiagramPlane(container, nodes);
            DrawDiagramContent(container, nodes, edges);
        }

        private void VisualizeDiagramPlane(GameObject container, List<NodeData> nodes)
        {
            var diagramNode = nodes.FirstOrDefault(n => n.Type == DiagramNodeTypes.DIAGRAM);
            var contentNodes = nodes.Where(n => n.Type != DiagramNodeTypes.DIAGRAM).ToList();

            if (contentNodes.Count == 0) return;

            float minX = float.MaxValue, maxX = float.MinValue;
            float minZ = float.MaxValue, maxZ = float.MinValue;

            foreach (var node in contentNodes)
            {
                Vector3 pos = node.GetNodePosition();
                if (pos.x < minX) minX = pos.x;
                if (pos.x > maxX) maxX = pos.x;
                if (pos.z < minZ) minZ = pos.z;
                if (pos.z > maxZ) maxZ = pos.z;
            }

            float padding = 5f;
            float width = (maxX - minX) + padding * 2;
            float height = (maxZ - minZ) + padding * 2;
            Vector3 center = new Vector3((minX + maxX) / 2, -0.5f, (minZ + maxZ) / 2);

            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plane.name = diagramNode?.Label ?? "Diagram_Base";
            plane.transform.SetParent(container.transform, false);

            plane.transform.localPosition = center;
            plane.transform.localRotation = Quaternion.Euler(90, 0, 0);

            plane.transform.localScale = new Vector3(width, height, 1);


            if (plane.TryGetComponent<Renderer>(out var rend))
            {
                rend.material = Resources.Load<Material>("Materials/DefaultMat");
                rend.material.color = new Color(0.2f, 0.2f, 0.2f, 1.0f);
            }
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
        protected void DrawEdge(GameObject parent, GameObject fromObj, GameObject toObj, EdgeData edge)
        {
            Vector3 startPoint = GetBorderPoint(fromObj, toObj.transform.position);
            Vector3 endPoint = GetBorderPoint(toObj, fromObj.transform.position);

            var edgeGo = new GameObject($"Edge_{edge.Key}");
            edgeGo.transform.SetParent(parent.transform, false);

            var lr = edgeGo.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.useWorldSpace = false;
            lr.SetPosition(0, startPoint);
            lr.SetPosition(1, endPoint);

            lr.startWidth = lr.endWidth = 0.04f;
            lr.startColor = lr.endColor = Color.white;

            bool isDashed = (edge.Type == DiagramEdgeTypes.INCLUDES_UML) || (edge.Type == DiagramEdgeTypes.EXTENDS_UML);

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

            Vector3 direction = (endPoint - startPoint).normalized;

            switch (edge.Type)
            {
                case DiagramEdgeTypes.AGGREGATES:
                    SpawnEdgeDecorator(DiagramEdgeTypes.AGGREGATES, parent.transform, startPoint, direction, 1f);
                    break;
                case DiagramEdgeTypes.COMPOSES:
                    SpawnEdgeDecorator(DiagramEdgeTypes.COMPOSES, parent.transform, startPoint, direction, 1f);
                    break;
                case DiagramEdgeTypes.GENERALIZES:
                    SpawnEdgeDecorator(DiagramEdgeTypes.GENERALIZES, parent.transform, endPoint, direction, -0.5f);
                    break;
                case DiagramEdgeTypes.INCLUDES_UML:
                    SpawnEdgeDecorator(DiagramEdgeTypes.INCLUDES_UML, parent.transform, endPoint, direction, -0.3f);
                    break;
                case DiagramEdgeTypes.EXTENDS_UML:
                    SpawnEdgeDecorator(DiagramEdgeTypes.INCLUDES_UML, parent.transform, endPoint, direction, -0.2f);
                    break;
            }
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
                // fallback for spheres or other shapes
                return classObj.transform.position;
            }

            // Work in local space of the class container
            Vector3 targetLocal = classObj.transform.InverseTransformPoint(targetPosition);
            Vector3 dir = targetLocal.normalized;           // direction from center → target
            if (dir == Vector3.zero) return classObj.transform.position;

            Vector3 half = background.localScale * 0.5f;    // (halfWidth, halfThicknessY, halfHeightZ)

            // Ray-AABB intersection from center → find the FIRST (nearest) hit
            float tMin = float.MaxValue;

            // X faces (left / right)
            if (Mathf.Abs(dir.x) > 0.0001f)
            {
                float tx = (dir.x > 0 ? half.x : -half.x) / dir.x;
                if (tx > 0.001f) tMin = Mathf.Min(tMin, tx);
            }

            // Z faces (top / bottom in diagram)
            if (Mathf.Abs(dir.z) > 0.0001f)
            {
                float tz = (dir.z > 0 ? half.z : -half.z) / dir.z;
                if (tz > 0.001f) tMin = Mathf.Min(tMin, tz);
            }

            // Y faces (thickness) - almost never hit in a 2D diagram, but keep for safety
            if (Mathf.Abs(dir.y) > 0.0001f)
            {
                float ty = (dir.y > 0 ? half.y : -half.y) / dir.y;
                if (ty > 0.001f) tMin = Mathf.Min(tMin, ty);
            }

            if (tMin == float.MaxValue || tMin <= 0.001f)
            {
                return classObj.transform.position; // fallback
            }

            // Hit point on the surface
            Vector3 localHit = dir * tMin;

            // Tiny outward offset so the line never clips into the box
            localHit += localHit.normalized * 0.03f;

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