using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Data;
using Assets.Scripts.Interfaces;
using UnityEngine;

namespace Assets.Scripts.Visualizers
{
    public abstract class BaseGraphVisualizer : IGraphVisualizer
    {
        protected abstract void DrawDiagramContent(GameObject container, List<NodeData> nodes, List<EdgeData> edges);

        public void RenderGraph(GraphMetadata graph, GameObject container, List<NodeData> nodes, List<EdgeData> edges)
        {
            if (nodes == null || nodes.Count == 0) return;

            VisualizeDiagramPlane(container, nodes);

            DrawDiagramContent(container, nodes, edges);
        }

        private void VisualizeDiagramPlane(GameObject container, List<NodeData> nodes)
        {
            var diagramNode = nodes.FirstOrDefault(n => n.Type == "DIAGRAM");
            var contentNodes = nodes.Where(n => n.Type != "DIAGRAM").ToList();

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

        protected string ExtractKeyFromId(string id)
        {
            if (string.IsNullOrEmpty(id)) return "";
            int slashIndex = id.LastIndexOf('/');
            return slashIndex >= 0 ? id.Substring(slashIndex + 1) : id;
        }
    }
}