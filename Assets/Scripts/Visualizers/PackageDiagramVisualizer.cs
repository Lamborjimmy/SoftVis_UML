using Assets.Scripts.Data;
using UnityEngine;
using System.Collections.Generic;
using TMPro;

namespace Assets.Scripts.Visualizers
{
    public class PackageDiagramVisualizer : BaseGraphVisualizer
    {
        protected override void DrawDiagramContent(GameObject container, List<NodeData> nodes, List<EdgeData> edges)
        {
            var (nodesParent, edgesParent) = CreateParentObjects(container);
            NestingContext nesting = BuildNestingHierarchy(nodes, edges);
            var nodeObjects = BuildNodes(nodesParent, nodes, nesting);
            FilterAndRenderEdges(edges, nodeObjects, edgesParent.transform);
        }

        private Dictionary<string, GameObject> BuildNodes(GameObject nodesParent, List<NodeData> nodes, NestingContext nesting)
        {
            var nodeObjects = new Dictionary<string, GameObject>();

            foreach (var node in nodes)
            {
                if (node == nesting.RootDiagram) continue;

                int depth = nesting.GetDepth(node.Key);
                float currentElevation = (depth + 1) * Y_ELEVATION;

                string nodeLabel = "Node_" + (node.GetNodeName() ?? node.Key);

                GameObject nodeContainer = CreateEmptyGameObject(nodesParent.transform, nodeLabel, Vector3.zero);

                if (nesting.IsContainer(node.Key))
                    BuildContainerNode(nodeContainer, node, nesting, currentElevation, depth);
                else if (node.Type == DiagramNodeTypes.PACKAGE)
                    BuildPackageNode(nodeContainer, node, currentElevation, depth);
                else
                    BuildStandardNode(nodeContainer, node, currentElevation, depth);

                nodeObjects[node.Key] = nodeContainer;
            }

            return nodeObjects;
        }

        private void BuildContainerNode(GameObject nodeContainer, NodeData node, NestingContext nesting, float currentElevation, int depth)
        {
            GetRecursiveBounds(node.Key, nesting.ParentToChildren, out float minX, out float maxX, out float minZ, out float maxZ);

            float paddingX = 4.0f;
            float paddingZ = 4.5f;
            float nodeWidth = (maxX - minX) + paddingX * 2;
            float nodeHeight = (maxZ - minZ) + paddingZ * 2;

            float currentTabHeight = 1.5f;
            float totalHeight = nodeHeight + currentTabHeight;
            float centerZ = (minZ + maxZ) / 2f;

            nodeContainer.transform.localPosition = new Vector3((minX + maxX) / 2f, node.GetNodePosition().y + currentElevation - (Y_ELEVATION / 2f), centerZ + (currentTabHeight / 2f));

            GameObject backgroundGroup = CreateEmptyGameObject(nodeContainer.transform, "Background", Vector3.zero);
            GameObject visualsObj = CreateNodeGameObject(node.Type, backgroundGroup.transform, nodeWidth, totalHeight);

            ConfigurePackagePrefabParts(visualsObj, nodeWidth, nodeHeight, totalHeight, currentTabHeight);
            ApplyMaterialToHierarchy(visualsObj, GetLayerColor(depth, true));

            float textZ = (nodeHeight / 2f) - (currentTabHeight / 2f) - 1.5f;
            CreateTextLabel(backgroundGroup.transform, node.GetNodeName(), new Vector3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, textZ), nodeWidth, HEADER_FONT_SIZE, TextAlignmentOptions.Top, FontStyles.Bold);
        }

        private void BuildPackageNode(GameObject nodeContainer, NodeData node, float currentElevation, int depth)
        {
            float textWidth = MeasureText(node.GetNodeName(), HEADER_FONT_SIZE, true);
            float nodeWidth = Mathf.Max(textWidth + 3f, 6f);
            float nodeHeight = 4f;
            float currentTabHeight = 1.5f;
            float totalHeight = nodeHeight + currentTabHeight;

            nodeContainer.transform.localPosition = new Vector3(node.GetNodePosition().x, node.GetNodePosition().y + currentElevation, node.GetNodePosition().z + (currentTabHeight / 2f));

            GameObject backgroundGroup = CreateEmptyGameObject(nodeContainer.transform, "Background", Vector3.zero);
            GameObject visualsObj = CreateNodeGameObject(node.Type, backgroundGroup.transform, nodeWidth, totalHeight);

            ConfigurePackagePrefabParts(visualsObj, nodeWidth, nodeHeight, totalHeight, currentTabHeight);
            ApplyMaterialToHierarchy(visualsObj, GetLayerColor(depth, false));

            float textZ = (nodeHeight / 2f) - (currentTabHeight / 2f) - 1.5f;
            CreateTextLabel(backgroundGroup.transform, node.GetNodeName(), new Vector3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, textZ), nodeWidth, HEADER_FONT_SIZE, TextAlignmentOptions.Top, FontStyles.Bold);
        }

        private void BuildStandardNode(GameObject nodeContainer, NodeData node, float currentElevation, int depth)
        {
            float textWidth = MeasureText(node.GetNodeName(), HEADER_FONT_SIZE, true);
            float nodeWidth = Mathf.Max(textWidth + 3f, 6f);
            float nodeHeight = 4f;

            nodeContainer.transform.localPosition = new Vector3(node.GetNodePosition().x, node.GetNodePosition().y + currentElevation, node.GetNodePosition().z);

            GameObject backgroundGroup = CreateEmptyGameObject(nodeContainer.transform, "Background", Vector3.zero);
            GameObject visualsObj = CreateNodeGameObject(node.Type, backgroundGroup.transform, nodeWidth, nodeHeight);

            ApplyMaterialToHierarchy(visualsObj, GetLayerColor(depth, false));

            CreateTextLabel(backgroundGroup.transform, node.GetNodeName(), new Vector3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, 0), nodeWidth, HEADER_FONT_SIZE, TextAlignmentOptions.Center, FontStyles.Bold);
        }

        private void ConfigurePackagePrefabParts(GameObject visualObj, float nodeWidth, float nodeHeight, float totalHeight, float currentTabHeight)
        {
            if (visualObj.transform.childCount == 0) return;

            float tabWorldWidth = Mathf.Min(2.5f, nodeWidth * 0.5f);
            Transform pkgBody = visualObj.transform.Find("Package");
            if (pkgBody == null) pkgBody = visualObj.transform.GetChild(0);

            if (pkgBody != null)
            {
                pkgBody.localScale = new Vector3(1f, 1f, nodeHeight / totalHeight);
                pkgBody.localPosition = new Vector3(0f, 0f, -currentTabHeight / (2f * totalHeight));
            }

            Transform tabBody = visualObj.transform.Find("Tab");
            if (tabBody == null && visualObj.transform.childCount > 1) tabBody = visualObj.transform.GetChild(1);

            if (tabBody != null)
            {
                tabBody.localScale = new Vector3(tabWorldWidth / nodeWidth, 1f, currentTabHeight / totalHeight);
                float tabLocalX = -0.5f + (tabWorldWidth / (2f * nodeWidth));
                float tabLocalZ = 0.5f - (currentTabHeight / (2f * totalHeight));
                tabBody.localPosition = new Vector3(tabLocalX, 0f, tabLocalZ);
            }
        }

        private Color GetLayerColor(int depth, bool isContainer)
        {
            Color[] palette = new Color[]
            {
                new Color(0.40f, 0.50f, 0.65f), // Slate Blue
                new Color(0.45f, 0.60f, 0.50f), // Muted Green
                new Color(0.60f, 0.45f, 0.55f), // Dusty Purple
                new Color(0.65f, 0.55f, 0.40f), // Earthy Orange/Brown
                new Color(0.45f, 0.55f, 0.60f)  // Teal Grey
            };

            Color c = palette[depth % palette.Length];
            c.a = isContainer ? 0.85f : 1f;
            return c;
        }

    }
}