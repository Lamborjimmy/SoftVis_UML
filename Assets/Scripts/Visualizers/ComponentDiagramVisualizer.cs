using Assets.Scripts.Data;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;

namespace Assets.Scripts.Visualizers
{
    public class ComponentDiagramVisualizer : BaseGraphVisualizer
    {
        protected override void DrawDiagramContent(GameObject container, List<NodeData> nodes, List<EdgeData> edges)
        {
            GameObject nodesParent = new GameObject("Nodes");
            GameObject edgesParent = new GameObject("Edges");

            nodesParent.transform.SetParent(container.transform, false);
            edgesParent.transform.SetParent(container.transform, false);

            var nodeObjects = new Dictionary<string, GameObject>();
            var nodeBounds = new Dictionary<string, Bounds>();

            // 1. Map Nesting Hierarchy
            var parentToChildren = new Dictionary<string, List<NodeData>>();
            var childToParent = new Dictionary<string, string>();
            var nestedChildKeys = new HashSet<string>();

            foreach (var edge in edges.Where(e => e.Type == DiagramEdgeTypes.NESTED || e.Type == "NESTED"))
            {
                string pKey = ExtractKeyFromId(edge.From);
                string cKey = ExtractKeyFromId(edge.To);

                nestedChildKeys.Add(cKey);
                childToParent[cKey] = pKey;

                if (!parentToChildren.ContainsKey(pKey))
                    parentToChildren[pKey] = new List<NodeData>();

                var childNode = nodes.FirstOrDefault(n => n.Key == cKey);
                if (childNode != null)
                    parentToChildren[pKey].Add(childNode);
            }

            var rootDiagram = nodes.FirstOrDefault(n => n.Type == DiagramNodeTypes.DIAGRAM || n.Type == "DIAGRAM");

            int GetDepth(string nodeKey)
            {
                int depth = 0;
                string current = nodeKey;
                while (childToParent.ContainsKey(current))
                {
                    current = childToParent[current];
                    if (rootDiagram != null && current != rootDiagram.Key)
                        depth++;
                }
                return depth;
            }

            // 2. Draw Nodes and Containers
            foreach (var node in nodes)
            {
                if (node == rootDiagram) continue;

                int depth = GetDepth(node.Key);

                float dampeningFactor = 0.6f;
                float currentElevation = Y_ELEVATION * (1f + (depth * dampeningFactor));

                bool isInterface = node.Type.Contains("INTERFACE");
                bool isPort = node.Type == DiagramNodeTypes.PORT || node.Type == "PORT" || node.Type == "UML_PORT";
                bool isComponent = node.Type == DiagramNodeTypes.COMPONENT || node.Type == "COMPONENT" || node.Type == "UML_COMPONENT";
                bool isActor = node.Type == DiagramNodeTypes.ACTOR;

                GameObject nodeContainer = new GameObject("Node_" + (node.GetNodeName() ?? node.Key));
                nodeContainer.transform.SetParent(nodesParent.transform, false);

                bool isContainer = parentToChildren.TryGetValue(node.Key, out var children) && children.Count > 0;

                float nodeWidth, nodeHeight;
                Vector3 position;

                if (isContainer)
                {
                    GetBounds(node.Key, parentToChildren, out float minX, out float maxX, out float minZ, out float maxZ);

                    float paddingX = 4.0f;
                    float paddingZ = 1.0f; // REDUCED from 4.5f

                    nodeWidth = (maxX - minX) + paddingX * 2;
                    nodeHeight = (maxZ - minZ) + paddingZ * 2;
                    float centerZ = (minZ + maxZ) / 2f;

                    position = new Vector3((minX + maxX) / 2f, node.GetNodePosition().y + currentElevation - (Y_ELEVATION / 2f), centerZ);
                }
                else if (isInterface)
                {
                    nodeWidth = 2.5f;
                    nodeHeight = 2.5f;
                    position = new Vector3(node.GetNodePosition().x, node.GetNodePosition().y + currentElevation, node.GetNodePosition().z);
                }
                else if (isPort)
                {
                    nodeWidth = 0.75f;
                    nodeHeight = 0.75f;
                    position = new Vector3(node.GetNodePosition().x, node.GetNodePosition().y + currentElevation, node.GetNodePosition().z);
                }
                else
                {
                    float textWidth = MeasureText(node.GetNodeName() ?? "", HEADER_FONT_SIZE, true);
                    nodeWidth = Mathf.Max(textWidth + 4f, 6f);
                    nodeHeight = 4f;

                    position = new Vector3(node.GetNodePosition().x, node.GetNodePosition().y + currentElevation, node.GetNodePosition().z);
                }

                nodeContainer.transform.localPosition = position;
                nodeBounds[node.Key] = new Bounds(position, new Vector3(nodeWidth, 0f, nodeHeight));

                GameObject visualObj;
                if (prefabsDictionary != null && prefabsDictionary.TryGetValue(node.Type, out GameObject prefab) && prefab != null)
                {
                    visualObj = Object.Instantiate(prefab, nodeContainer.transform);
                    visualObj.name = "Background";
                    visualObj.transform.localPosition = Vector3.zero;

                    if (isInterface || isActor)
                        visualObj.transform.localScale = Vector3.one;
                    else
                        visualObj.transform.localScale = new Vector3(nodeWidth, 0.2f, nodeHeight);

                    foreach (var txt in visualObj.GetComponentsInChildren<TextMeshPro>()) Object.Destroy(txt.gameObject);
                }
                else
                {
                    PrimitiveType prim = PrimitiveType.Cube;
                    if (isInterface) prim = PrimitiveType.Sphere;

                    visualObj = GameObject.CreatePrimitive(prim);
                    visualObj.transform.SetParent(nodeContainer.transform, false);
                    visualObj.name = "Background";
                    visualObj.transform.localPosition = Vector3.zero;

                    if (isInterface)
                        visualObj.transform.localScale = Vector3.one * nodeWidth;
                    else
                        visualObj.transform.localScale = new Vector3(nodeWidth, 0.2f, nodeHeight);

                    if (visualObj.TryGetComponent<Renderer>(out var rend))
                    {
                        rend.material = cachedNodeMaterial;

                        if (isInterface)
                            rend.material.color = new Color(0.9f, 0.8f, 0.9f);
                        else if (isPort)
                            rend.material.color = new Color(0.4f, 0.4f, 0.4f);
                        else
                            rend.material.color = GetLayerColor(depth, isContainer || isComponent);
                    }
                }

                string stereotype = "";
                if (isComponent) stereotype = "<<component>>";
                else if (isPort) stereotype = "<<port>>";

                if (isContainer)
                {
                    float textZ = (nodeHeight / 2f) - 1.5f;
                    CreateTextLabel(nodeContainer.transform, stereotype, new Vector3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, textZ), nodeWidth, LABEL_FONT_SIZE, TextAlignmentOptions.Top);
                    CreateTextLabel(nodeContainer.transform, node.GetNodeName(), new Vector3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, textZ - LINE_HEIGHT), nodeWidth, HEADER_FONT_SIZE, TextAlignmentOptions.Top, FontStyles.Bold);
                }
                else if (isInterface || isActor)
                {
                    CreateTextLabel(nodeContainer.transform, node.GetNodeName(), new Vector3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, -2.5f), Mathf.Max(nodeWidth, 8f), HEADER_FONT_SIZE, TextAlignmentOptions.Top, FontStyles.Bold);
                }
                else if (isPort)
                {
                    // Ports do not get floating text to keep diagram clean
                }
                else
                {
                    CreateTextLabel(nodeContainer.transform, stereotype, new Vector3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, 0.5f), nodeWidth, LABEL_FONT_SIZE, TextAlignmentOptions.Center);
                    CreateTextLabel(nodeContainer.transform, node.GetNodeName(), new Vector3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, 0.5f - LINE_HEIGHT), nodeWidth, HEADER_FONT_SIZE, TextAlignmentOptions.Center, FontStyles.Bold);
                }

                nodeObjects[node.Key] = nodeContainer;
            }

            // ------------------------------------------------------------------------
            // NEW: Step 2.4 - Pre-calculate external connections for ports
            // ------------------------------------------------------------------------
            var validEdges = edges.Where(e => e.Type != DiagramEdgeTypes.NESTED && e.Type != "NESTED").ToList();

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

            // ------------------------------------------------------------------------
            // NEW: Step 2.5 - Connection-Oriented Edge Snapping
            // ------------------------------------------------------------------------
            var parentToPorts = new Dictionary<string, List<string>>();

            foreach (var node in nodes)
            {
                bool isPort = node.Type == DiagramNodeTypes.PORT || node.Type == "PORT" || node.Type == "UML_PORT";

                if (isPort && childToParent.TryGetValue(node.Key, out string parentKey))
                {
                    if (nodeBounds.TryGetValue(parentKey, out Bounds parentBounds) &&
                        nodeObjects.TryGetValue(node.Key, out GameObject portObj))
                    {
                        // Register port to parent for separation in Step 2.6
                        if (!parentToPorts.ContainsKey(parentKey)) parentToPorts[parentKey] = new List<string>();
                        parentToPorts[parentKey].Add(node.Key);

                        Vector3 parentCenter = parentBounds.center;
                        Vector3 extents = parentBounds.extents;
                        Vector3 portPos = portObj.transform.localPosition;

                        Vector3 targetDir = Vector3.zero;
                        int connectionCount = 0;

                        if (portConnections.TryGetValue(node.Key, out List<string> connectedKeys))
                        {
                            foreach (string connectedKey in connectedKeys)
                            {
                                if (nodeObjects.TryGetValue(connectedKey, out GameObject connectedObj))
                                {
                                    targetDir += (connectedObj.transform.localPosition - parentCenter);
                                    connectionCount++;
                                }
                            }
                        }

                        Vector3 dir;
                        if (connectionCount > 0 && targetDir.sqrMagnitude > 0.0001f)
                            dir = targetDir / connectionCount;
                        else
                            dir = portPos - parentCenter;

                        dir.y = 0f;

                        if (dir.sqrMagnitude > 0.0001f)
                        {
                            float scaleX = extents.x / Mathf.Max(Mathf.Abs(dir.x), 0.0001f);
                            float scaleZ = extents.z / Mathf.Max(Mathf.Abs(dir.z), 0.0001f);
                            float snapScale = Mathf.Min(scaleX, scaleZ);

                            Vector3 snappedPos = parentCenter + (dir * snapScale);
                            snappedPos.y = portPos.y;
                            portObj.transform.localPosition = snappedPos;
                        }
                    }
                }
            }

            // ------------------------------------------------------------------------
            // NEW: Step 2.6 - Port Separation (Anti-Overlap)
            // ------------------------------------------------------------------------
            float minPortSpacing = 2.0f; // Minimum distance between ports
            int relaxationIterations = 10;

            for (int step = 0; step < relaxationIterations; step++)
            {
                foreach (var kvp in parentToPorts)
                {
                    string parentKey = kvp.Key;
                    List<string> ports = kvp.Value;

                    if (ports.Count < 2) continue; // Only push apart if there's 2 or more ports

                    Bounds parentBounds = nodeBounds[parentKey];
                    Vector3 parentCenter = parentBounds.center;
                    Vector3 extents = parentBounds.extents;

                    // 1. Repel ports from each other
                    for (int i = 0; i < ports.Count; i++)
                    {
                        for (int j = i + 1; j < ports.Count; j++)
                        {
                            GameObject p1 = nodeObjects[ports[i]];
                            GameObject p2 = nodeObjects[ports[j]];

                            Vector3 pos1 = p1.transform.localPosition;
                            Vector3 pos2 = p2.transform.localPosition;

                            Vector3 diff = pos1 - pos2;
                            diff.y = 0; // Ignore elevation
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

                    // 2. Re-snap to the perimeter
                    foreach (string portKey in ports)
                    {
                        GameObject portObj = nodeObjects[portKey];
                        Vector3 currentPos = portObj.transform.localPosition;
                        Vector3 dir = currentPos - parentCenter;
                        dir.y = 0;

                        if (dir.sqrMagnitude > 0.0001f)
                        {
                            float scaleX = extents.x / Mathf.Max(Mathf.Abs(dir.x), 0.0001f);
                            float scaleZ = extents.z / Mathf.Max(Mathf.Abs(dir.z), 0.0001f);
                            float snapScale = Mathf.Min(scaleX, scaleZ);

                            Vector3 snappedPos = parentCenter + (dir * snapScale);
                            snappedPos.y = currentPos.y;
                            portObj.transform.localPosition = snappedPos;
                        }
                    }
                }
            }

            // 3. Draw Hub Edges
            var selfLoops = validEdges.Where(e => ExtractKeyFromId(e.From) == ExtractKeyFromId(e.To));
            var normalEdges = validEdges.Where(e => ExtractKeyFromId(e.From) != ExtractKeyFromId(e.To));

            DrawDiagramEdges(selfLoops, nodeObjects, edgesParent, normalEdges);
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

        private void GetBounds(string parentKey, Dictionary<string, List<NodeData>> parentToChildren, out float minX, out float maxX, out float minZ, out float maxZ)
        {
            minX = minZ = float.MaxValue;
            maxX = maxZ = float.MinValue;

            if (!parentToChildren.ContainsKey(parentKey)) return;

            float paddingX = 2.0f;
            float paddingZ = 1.5f;

            foreach (var child in parentToChildren[parentKey])
            {
                float childMinX = child.GetNodePosition().x;
                float childMaxX = child.GetNodePosition().x;
                float childMinZ = child.GetNodePosition().z;
                float childMaxZ = child.GetNodePosition().z;

                if (parentToChildren.ContainsKey(child.Key) && parentToChildren[child.Key].Count > 0)
                {
                    GetBounds(child.Key, parentToChildren, out childMinX, out childMaxX, out childMinZ, out childMaxZ);

                    childMinX -= paddingX;
                    childMaxX += paddingX;
                    childMinZ -= paddingZ;
                    childMaxZ += paddingZ;
                }

                minX = Mathf.Min(minX, childMinX);
                maxX = Mathf.Max(maxX, childMaxX);
                minZ = Mathf.Min(minZ, childMinZ);
                maxZ = Mathf.Max(maxZ, childMaxZ);
            }
        }
    }
}