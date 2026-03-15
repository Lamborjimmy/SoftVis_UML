using Assets.Scripts.Data;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor.AnimatedValues;

namespace Assets.Scripts.Visualizers
{
    public class ComponentDiagramVisualizer : BaseGraphVisualizer
    {
        protected override Dictionary<string, GameObject> BuildDiagramNodes(GameObject nodesParent, List<NodeData> nodes, List<EdgeData> edges, NestingContext nesting)
        {
            var nodeBounds = new Dictionary<string, Bounds>();

            var nodeObjects = BuildNodes(nodesParent, nodes, nesting, nodeBounds);

            var validEdges = edges.Where(e => e.Type != DiagramEdgeTypes.NESTED).ToList();

            var parentToPorts = SnapPortsToParentPerimeter(nodes, nesting.ChildToParent, nodeObjects, nodeBounds, validEdges);
            RelaxOverlappingPorts(parentToPorts, nodeObjects, nodeBounds);

            return nodeObjects;
        }

        private Dictionary<string, GameObject> BuildNodes(GameObject nodesParent, List<NodeData> nodes, NestingContext nesting, Dictionary<string, Bounds> nodeBounds)
        {
            var nodeObjects = new Dictionary<string, GameObject>();

            foreach (var node in nodes)
            {
                if (node == nesting.RootDiagram || node.Type == DiagramNodeTypes.DIAGRAM) continue;

                int depth = nesting.GetDepth(node.Key);
                float dampeningFactor = 0.6f;
                float currentElevation = Y_ELEVATION * (1f + (depth * dampeningFactor));

                string nodeLabel = "Node_" + (node.GetNodeName() ?? node.Key);
                GameObject nodeContainer = CreateEmptyGameObject(nodesParent.transform, nodeLabel, Vector3.zero);

                bool isContainer = nesting.ParentToChildren.ContainsKey(node.Key) && nesting.ParentToChildren[node.Key].Count > 0;

                Bounds bounds;

                if (isContainer)
                    bounds = BuildContainerNode(nodeContainer, node, nesting.ParentToChildren, currentElevation, depth);
                else if (node.Type.Contains("INTERFACE"))
                    bounds = BuildInterfaceNode(nodeContainer, node, currentElevation);
                else if (node.Type == DiagramNodeTypes.PORT)
                    bounds = BuildPortNode(nodeContainer, node, currentElevation);
                else if (node.Type == DiagramNodeTypes.ACTOR)
                    bounds = BuildActorNode(nodeContainer, node, currentElevation);
                else
                    bounds = BuildComponentNode(nodeContainer, node, currentElevation, depth);

                nodeBounds[node.Key] = bounds;
                nodeObjects[node.Key] = nodeContainer;
            }

            return nodeObjects;
        }

        private Bounds BuildContainerNode(GameObject nodeContainer, NodeData node, Dictionary<string, List<NodeData>> parentToChildren, float currentElevation, int depth)
        {
            GetRecursiveBounds(node.Key, parentToChildren, out float minX, out float maxX, out float minZ, out float maxZ);

            float paddingX = 4.0f;
            float paddingZ = 1.0f;

            float width = (maxX - minX) + (paddingX * 2);
            float height = (maxZ - minZ) + (paddingZ * 2);
            float centerZ = (minZ + maxZ) / 2f;

            Vector3 position = new Vector3((minX + maxX) / 2f, node.GetNodePosition().y + currentElevation - (Y_ELEVATION / 2f), centerZ);
            nodeContainer.transform.localPosition = position;

            GameObject backgroundGroup = CreateEmptyGameObject(nodeContainer.transform, "Background", Vector3.zero);
            GameObject visualsObj = CreateNodeGameObject(node.Type, backgroundGroup.transform, width, height);

            ApplyColorToHierarchy(visualsObj, GetLayerColor(depth, true));


            float textZ = (height / 2f) - 1.5f;
            if (node.Type == DiagramNodeTypes.COMPONENT)
                CreateTextLabel(backgroundGroup.transform, "<<component>>", new Vector3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, textZ), width, LABEL_FONT_SIZE, TextAlignmentOptions.Top);

            CreateTextLabel(backgroundGroup.transform, node.GetNodeName(), new Vector3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, textZ - LINE_HEIGHT), width, HEADER_FONT_SIZE, TextAlignmentOptions.Top, FontStyles.Bold);

            return new Bounds(position, new Vector3(width, 0f, height));
        }

        private Bounds BuildComponentNode(GameObject nodeContainer, NodeData node, float currentElevation, int depth)
        {
            float textWidth = MeasureText(node.GetNodeName() ?? "", HEADER_FONT_SIZE, true);
            float width = Mathf.Max(textWidth + 4f, 6f);
            float height = 3f;

            var (bounds, background) = BuildNode(nodeContainer, node, currentElevation, node.GetNodePosition(), width, height, GetLayerColor(depth, true), false);

            CreateTextLabel(background.transform, "<<component>>", new Vector3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, 0.5f), width, LABEL_FONT_SIZE, TextAlignmentOptions.Center);
            CreateTextLabel(background.transform, node.GetNodeName(), new Vector3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, 0.5f - LINE_HEIGHT), width, HEADER_FONT_SIZE, TextAlignmentOptions.Center, FontStyles.Bold);

            return bounds;
        }

        private Bounds BuildInterfaceNode(GameObject nodeContainer, NodeData node, float currentElevation)
        {
            float width = 1.0f;

            var (bounds, background) = BuildNode(nodeContainer, node, currentElevation, node.GetNodePosition(), width, width, Color.white, true);

            CreateTextLabel(background.transform, node.GetNodeName(), new Vector3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, -2.5f), Mathf.Max(width, 8f), HEADER_FONT_SIZE, TextAlignmentOptions.Top, FontStyles.Bold);

            return bounds;
        }

        private Bounds BuildPortNode(GameObject nodeContainer, NodeData node, float currentElevation)
        {
            float width = 0.75f;

            var (bounds, background) = BuildNode(nodeContainer, node, currentElevation, node.GetNodePosition(), width, width, Color.gray, true);
            return bounds;
        }

        private Bounds BuildActorNode(GameObject nodeContainer, NodeData node, float currentElevation)
        {
            float textWidth = MeasureText(node.GetNodeName() ?? "", HEADER_FONT_SIZE, true);
            float width = 1f;

            var (bounds, background) = BuildNode(nodeContainer, node, currentElevation, node.GetNodePosition(), width, width, Color.white, true);

            CreateTextLabel(background.transform, node.GetNodeName(), new Vector3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, -2.5f), Mathf.Max(textWidth + 3f, 8f), HEADER_FONT_SIZE, TextAlignmentOptions.Top, FontStyles.Bold);

            return bounds;
        }

        private Color GetLayerColor(int depth, bool isContainer)
        {
            Color[] palette = new Color[]
            {
                new Color(0.40f, 0.50f, 0.65f),
                new Color(0.45f, 0.60f, 0.50f),
                new Color(0.60f, 0.45f, 0.55f),
                new Color(0.65f, 0.55f, 0.40f),
                new Color(0.45f, 0.55f, 0.60f)
            };

            Color c = palette[depth % palette.Length];
            c.a = isContainer ? 0.85f : 1f;
            return c;
        }

        private Dictionary<string, List<string>> SnapPortsToParentPerimeter(List<NodeData> nodes, Dictionary<string, string> childToParent, Dictionary<string, GameObject> nodeObjects, Dictionary<string, Bounds> nodeBounds, List<EdgeData> validEdges)
        {
            var portConnections = BuildPortConnections(validEdges);
            var parentToPorts = new Dictionary<string, List<string>>();

            foreach (var node in nodes)
            {
                bool isPort = node.Type == DiagramNodeTypes.PORT;
                if (!isPort || !childToParent.TryGetValue(node.Key, out string parentKey)) continue;
                if (!nodeBounds.TryGetValue(parentKey, out Bounds parentBounds) || !nodeObjects.TryGetValue(node.Key, out GameObject portObj)) continue;

                if (!parentToPorts.ContainsKey(parentKey)) parentToPorts[parentKey] = new List<string>();
                parentToPorts[parentKey].Add(node.Key);

                Vector3 parentCenter = parentBounds.center;
                Vector3 extents = parentBounds.extents;
                Vector3 portPos = portObj.transform.localPosition;

                Vector3 dir = ComputePortDirection(node.Key, parentCenter, portPos, portConnections, nodeObjects);

                if (dir.sqrMagnitude > 0.0001f)
                    portObj.transform.localPosition = SnapToPerimeter(parentCenter, extents, dir, portPos.y);
            }

            return parentToPorts;
        }

        private Dictionary<string, List<string>> BuildPortConnections(List<EdgeData> validEdges)
        {
            var portConnections = new Dictionary<string, List<string>>();
            foreach (var edge in validEdges)
            {
                string fromKey = ExtractKeyFromId(edge.From);
                string toKey = ExtractKeyFromId(edge.To);

                if (!portConnections.ContainsKey(fromKey)) portConnections[fromKey] = new List<string>();
                portConnections[fromKey].Add(toKey);

                if (!portConnections.ContainsKey(toKey)) portConnections[toKey] = new List<string>();
                portConnections[toKey].Add(fromKey);
            }
            return portConnections;
        }

        private Vector3 ComputePortDirection(string portKey, Vector3 parentCenter, Vector3 portPos, Dictionary<string, List<string>> portConnections, Dictionary<string, GameObject> nodeObjects)
        {
            Vector3 targetDir = Vector3.zero;
            int connectionCount = 0;

            if (portConnections.TryGetValue(portKey, out List<string> connectedKeys))
            {
                foreach (string connectedKey in connectedKeys)
                {
                    if (nodeObjects.TryGetValue(connectedKey, out GameObject connectedObj))
                    {
                        targetDir += connectedObj.transform.localPosition - parentCenter;
                        connectionCount++;
                    }
                }
            }

            Vector3 dir = (connectionCount > 0 && targetDir.sqrMagnitude > 0.0001f) ? targetDir / connectionCount : portPos - parentCenter;
            dir.y = 0f;
            return dir;
        }

        private Vector3 SnapToPerimeter(Vector3 parentCenter, Vector3 extents, Vector3 dir, float yPos)
        {
            float scaleX = extents.x / Mathf.Max(Mathf.Abs(dir.x), 0.0001f);
            float scaleZ = extents.z / Mathf.Max(Mathf.Abs(dir.z), 0.0001f);
            float snapScale = Mathf.Min(scaleX, scaleZ);

            Vector3 snappedPos = parentCenter + (dir * snapScale);
            snappedPos.y = yPos;
            return snappedPos;
        }

        private void RelaxOverlappingPorts(Dictionary<string, List<string>> parentToPorts, Dictionary<string, GameObject> nodeObjects, Dictionary<string, Bounds> nodeBounds)
        {
            float minPortSpacing = 2.0f;
            int relaxationIterations = 10;

            for (int step = 0; step < relaxationIterations; step++)
            {
                foreach (var kvp in parentToPorts)
                {
                    string parentKey = kvp.Key;
                    List<string> ports = kvp.Value;

                    if (ports.Count < 2) continue;

                    Bounds parentBounds = nodeBounds[parentKey];

                    for (int i = 0; i < ports.Count; i++)
                    {
                        for (int j = i + 1; j < ports.Count; j++)
                        {
                            GameObject p1 = nodeObjects[ports[i]];
                            GameObject p2 = nodeObjects[ports[j]];

                            Vector3 diff = p1.transform.localPosition - p2.transform.localPosition;
                            diff.y = 0; ;
                            float dist = diff.magnitude;

                            if (dist < minPortSpacing)
                            {
                                if (dist < 0.001f) diff = new Vector3(Random.value - 0.5f, 0, Random.value - 0.5f).normalized;

                                Vector3 push = diff.normalized * ((minPortSpacing - dist) * 0.5f);
                                p1.transform.localPosition += push;
                                p2.transform.localPosition -= push;
                            }
                        }
                    }

                    foreach (string portKey in ports)
                    {
                        GameObject portObj = nodeObjects[portKey];
                        Vector3 currentPos = portObj.transform.localPosition;
                        Vector3 dir = currentPos - parentBounds.center;
                        dir.y = 0;

                        if (dir.sqrMagnitude > 0.0001f)
                            portObj.transform.localPosition = SnapToPerimeter(parentBounds.center, parentBounds.extents, dir, currentPos.y);
                    }
                }
            }
        }
    }
}