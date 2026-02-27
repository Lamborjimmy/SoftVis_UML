using Assets.Scripts.Data;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;

namespace Assets.Scripts.Visualizers
{
    public class ClassDiagramVisualizer : BaseGraphVisualizer
    {
        private const float PADDING_Z = 1.5f;
        protected override void DrawDiagramContent(GameObject container, List<NodeData> nodes, List<EdgeData> edges)
        {
            GameObject nodesParent = new GameObject("Nodes");
            GameObject edgesParent = new GameObject("Edges");

            nodesParent.transform.SetParent(container.transform, false);
            edgesParent.transform.SetParent(container.transform, false);

            var nodeLookup = nodes.ToDictionary(n => n.Key);
            var nodeObjects = new Dictionary<string, GameObject>();

            var nestedChildrenKeys = new HashSet<string>();
            var classToMembersMap = new Dictionary<string, List<NodeData>>();

            // 1. Map Nested Relationships
            foreach (var edge in edges.Where(e => e.Type == DiagramEdgeTypes.NESTED))
            {
                string parentKey = ExtractKeyFromId(edge.From);
                string childKey = ExtractKeyFromId(edge.To);

                if (parentKey.EndsWith("_0")) continue; // Ignore nested inside the DIAGRAM

                nestedChildrenKeys.Add(childKey);

                if (!classToMembersMap.ContainsKey(parentKey))
                    classToMembersMap[parentKey] = new List<NodeData>();

                if (nodeLookup.TryGetValue(childKey, out var childNode))
                {
                    classToMembersMap[parentKey].Add(childNode);
                }
            }

            // 2. Spawn Nodes
            foreach (var node in nodes.Where(n => n.Type != DiagramNodeTypes.DIAGRAM))
            {
                if (nestedChildrenKeys.Contains(node.Key)) continue;

                if (node.Type == DiagramNodeTypes.CLASS || node.Type == DiagramNodeTypes.ENUMERATION || node.Type == DiagramNodeTypes.INTERFACE)
                {
                    classToMembersMap.TryGetValue(node.Key, out var members);
                    int memberCount = members != null ? members.Count : 0;

                    float maxTextWidth = MeasureText(node.Label ?? "", HEADER_FONT_SIZE, true);
                    float totalZ = 0.0f;

                    if (node.Type == DiagramNodeTypes.ENUMERATION || node.Type == DiagramNodeTypes.INTERFACE)
                    {
                        totalZ = (memberCount + 2) * LINE_HEIGHT + PADDING_Z;
                        if (maxTextWidth < "<<enumeration>>".Length)
                            maxTextWidth = MeasureText("<<enumeration>>", LABEL_FONT_SIZE, false);
                    }
                    else
                    {
                        totalZ = (memberCount + 1) * LINE_HEIGHT + PADDING_Z;
                    }

                    if (members != null)
                    {
                        foreach (var m in members)
                        {
                            string displayText = m.Type == DiagramNodeTypes.METHOD
                                ? "+ " + m.Label + "()"
                                : "- " + m.Label + " : " + m.Properties["type_name"];

                            float w = MeasureText(displayText, LABEL_FONT_SIZE, false);
                            if (w > maxTextWidth) maxTextWidth = w;
                        }
                    }

                    float totalX = Mathf.Max(maxTextWidth + 3f, 7f);

                    // A. Create Container
                    GameObject classContainer = new GameObject("Class_" + (node.Label ?? node.Key));
                    classContainer.transform.SetParent(nodesParent.transform, false);
                    classContainer.transform.localPosition = new Vector3(node.GetNodePosition().x, node.GetNodePosition().y + Y_ELEVATION, node.GetNodePosition().z);

                    // B. Create Cube Background
                    GameObject cubeObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cubeObj.name = "Background";
                    cubeObj.transform.SetParent(classContainer.transform, false);
                    cubeObj.transform.localPosition = Vector3.zero;
                    // Unified Thickness: Y = 0.2f
                    cubeObj.transform.localScale = new Vector3(totalX, 0.2f, totalZ);

                    if (cubeObj.TryGetComponent<Renderer>(out var rend))
                    {
                        rend.material = Resources.Load<Material>("Materials/DefaultMat");
                        rend.material.color = Color.bisque;
                    }

                    // C. Spawn Text Elements
                    float currentZ = (totalZ / 2f) - (PADDING_Z / 2f);
                    // Unified Text Elevation: Y = 0.11f
                    CreateTextLabel(classContainer.transform, node.Label, new Vector3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, currentZ), totalX, HEADER_FONT_SIZE, TextAlignmentOptions.Center, FontStyles.Bold);

                    if (node.Type == DiagramNodeTypes.INITIAL || node.Type == DiagramNodeTypes.ENUMERATION || node.Type == DiagramNodeTypes.INTERFACE)
                    {
                        currentZ -= LINE_HEIGHT;
                        string stereoType = node.Type == DiagramNodeTypes.INTERFACE ? "<<interface>>" : "<<enumeration>>";

                        GameObject text = CreateTextLabel(classContainer.transform, stereoType, new Vector3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, currentZ), totalX, LABEL_FONT_SIZE, TextAlignmentOptions.Center);
                    }

                    if (members != null)
                    {
                        foreach (var member in members)
                        {
                            currentZ -= LINE_HEIGHT;
                            string memberString = member.Type == DiagramNodeTypes.METHOD
                                ? "+ " + member.Label + "()"
                                : "- " + member.Label + ":" + member.Properties["type_name"];

                            CreateTextLabel(classContainer.transform, memberString, new Vector3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, currentZ), totalX, LABEL_FONT_SIZE, TextAlignmentOptions.Left);
                            nodeObjects[member.Key] = classContainer;
                        }
                    }

                    nodeObjects[node.Key] = classContainer;
                }
                else
                {
                    GameObject nodeObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    nodeObj.name = node.Label ?? node.Key;
                    nodeObj.transform.SetParent(nodesParent.transform, false);
                    nodeObj.transform.localPosition = new Vector3(node.GetNodePosition().x, node.GetNodePosition().y + Y_ELEVATION, node.GetNodePosition().z);
                    nodeObj.transform.localScale = Vector3.one * 1.5f;

                    if (nodeObj.TryGetComponent<Renderer>(out var rend))
                    {
                        rend.material = cachedNodeMaterial;
                        rend.material.color = Color.bisque;
                    }

                    nodeObjects[node.Key] = nodeObj;
                }
            }

            // 3. Draw Edges
            foreach (var edge in edges)
            {
                if (edge.Type == DiagramEdgeTypes.NESTED) continue;

                string fromKey = ExtractKeyFromId(edge.From);
                string toKey = ExtractKeyFromId(edge.To);

                if (nodeObjects.TryGetValue(fromKey, out var a) && nodeObjects.TryGetValue(toKey, out var b))
                {
                    DrawEdge(edgesParent, a, b, edge);
                }
            }
        }
    }
}