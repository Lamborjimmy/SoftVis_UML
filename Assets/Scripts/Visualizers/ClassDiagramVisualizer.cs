using Assets.Scripts.Data;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;

namespace Assets.Scripts.Visualizers
{
    public class ClassDiagramVisualizer : BaseGraphVisualizer
    {
        private TextMeshPro measurer;
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
            foreach (var edge in edges.Where(e => e.Type == "NESTED"))
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
            foreach (var node in nodes.Where(n => n.Type != "DIAGRAM"))
            {
                if (nestedChildrenKeys.Contains(node.Key)) continue;

                if (node.Type == "UML_CLASS" || node.Type == "UML_ENUMERATION" || node.Type == "UML_INTERFACE")
                {
                    // Fetch members early to calculate size
                    classToMembersMap.TryGetValue(node.Key, out var members);
                    int memberCount = members != null ? members.Count : 0;

                    // Calculate Dimensions dynamically
                    float lineHeight = 1.2f; // Space between each line of text
                    float paddingZ = 2.0f; // Top and bottom padding

                    // Estimate width based on string length
                    float maxTextWidth = MeasureText(node.Label ?? "", 14, true);

                    float totalZ = 0.0f;
                    if (node.Type == "UML_ENUMERATION" || node.Type == "UML_INTERFACE")
                    {
                        totalZ = (memberCount + 2) * lineHeight + paddingZ; // +2 for the header and <<>> notationon
                        if (maxTextWidth < "<<enumeration>>".Length)
                            maxTextWidth = MeasureText("<<enumeration>>", 10, false);
                    }
                    else
                        totalZ = (memberCount + 1) * lineHeight + paddingZ; // +1 for the header
                    if (members != null)
                    {
                        foreach (var m in members)
                        {
                            string displayText = "";
                            if (m.Type == "UML_METHOD")
                                displayText = "+ " + m.Label + "()";
                            else if (m.Type == "UML_ATTRIBUTE")
                                displayText = "- " + m.Label + " : " + m.Properties["type_name"];
                            float w = MeasureText(displayText, 10, false);
                            if (w > maxTextWidth) maxTextWidth = w;
                        }
                    }

                    float totalX = maxTextWidth + 3f;
                    totalX = Mathf.Max(totalX, 7f);

                    // A. Create the Root Container for this Class
                    GameObject classContainer = new GameObject("Class_" + (node.Label ?? node.Key));
                    classContainer.transform.SetParent(nodesParent.transform, false);
                    classContainer.transform.localPosition = new Vector3(node.GetNodePosition().x, node.GetNodePosition().y + 0.4f, node.GetNodePosition().z);

                    // B. Create the Cube Background
                    GameObject cubeObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cubeObj.name = "Background";
                    cubeObj.transform.SetParent(classContainer.transform, false);
                    cubeObj.transform.localPosition = Vector3.zero; // Center of container
                    cubeObj.transform.localScale = new Vector3(totalX, 0.4f, totalZ); // Apply dynamic size

                    if (cubeObj.TryGetComponent<Renderer>(out var rend))
                    {
                        rend.material = Resources.Load<Material>("Materials/DefaultMat");
                        rend.material.color = Color.bisque;
                    }

                    // C. Spawn Text Elements
                    // Start drawing text near the top edge of the Z-axis bounds
                    float currentZ = (totalZ / 2f) - (paddingZ / 2f);

                    // Header text placed slightly above the cube (Y = 0.21f to avoid clipping)
                    CreateTextLabel(classContainer.transform, node.Label, new Vector3(0, 0.21f, currentZ), true, totalX);
                    if (node.Type == "UML_INTERFACE" || node.Type == "UML_ENUMERATION")
                    {
                        currentZ -= lineHeight;

                        if (node.Type == "UML_INTERFACE")
                        {
                            GameObject text = CreateTextLabel(classContainer.transform, "<<interface>>", new Vector3(0, 0.21f, currentZ), false, totalX);
                            text.GetComponent<TextMeshPro>().alignment = TextAlignmentOptions.Center;
                        }
                        else if (node.Type == "UML_ENUMERATION")
                        {
                            GameObject text = CreateTextLabel(classContainer.transform, "<<enumeration>>", new Vector3(0, 0.21f, currentZ), false, totalX);
                            text.GetComponent<TextMeshPro>().alignment = TextAlignmentOptions.Center;

                        }
                    }


                    if (members != null)
                    {
                        foreach (var member in members)
                        {
                            currentZ -= lineHeight; // Move down for the next line
                            GameObject memberText;
                            if (member.Type == "UML_METHOD")
                                memberText = CreateTextLabel(classContainer.transform, "+ " + member.Label + "()", new Vector3(0, 0.21f, currentZ), false, totalX);
                            else if (member.Type == "UML_ATTRIBUTE")
                                memberText = CreateTextLabel(classContainer.transform, "- " + member.Label + ":" + member.Properties["type_name"], new Vector3(0, 0.21f, currentZ), false, totalX);
                            // Map the member key to the MAIN container so lines draw to the class box
                            nodeObjects[member.Key] = classContainer;
                        }
                    }

                    // Map the main class key to the container
                    nodeObjects[node.Key] = classContainer;
                }
                else
                {
                    // Fallback for floating nodes
                    GameObject nodeObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    nodeObj.name = node.Label ?? node.Key;
                    nodeObj.transform.SetParent(nodesParent.transform, false);
                    nodeObj.transform.localPosition = new Vector3(node.GetNodePosition().x, node.GetNodePosition().y + 0.4f, node.GetNodePosition().z);
                    nodeObj.transform.localScale = Vector3.one * 0.6f;

                    if (nodeObj.TryGetComponent<Renderer>(out var rend))
                    {
                        rend.material = Resources.Load<Material>("Materials/DefaultMat");
                        rend.material.color = Color.bisque;
                    }

                    nodeObjects[node.Key] = nodeObj;
                }
            }

            // 3. Draw Edges
            foreach (var edge in edges)
            {
                if (edge.Type == "NESTED") continue;

                string fromKey = ExtractKeyFromId(edge.From);
                string toKey = ExtractKeyFromId(edge.To);

                if (nodeObjects.TryGetValue(fromKey, out var a) && nodeObjects.TryGetValue(toKey, out var b))
                {
                    DrawEdge(edgesParent, a, b, edge.Key);
                }
            }

            Debug.Log($"Rendered organized Class Diagram inside {container.name}");
        }

        // Updated with 'width' parameter to ensure long text doesn't wrap awkwardly
        private GameObject CreateTextLabel(Transform parent, string text, Vector3 localPos, bool isHeader, float width)
        {
            GameObject textObj = new GameObject("Text_" + text);
            textObj.transform.SetParent(parent, false);
            textObj.transform.localPosition = localPos;
            textObj.transform.localRotation = Quaternion.Euler(90, 0, 0);

            var tmp = textObj.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = isHeader ? 14 : 10;
            if (isHeader) tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.black;

            // Left-align members, center-align the header
            tmp.alignment = isHeader ? TextAlignmentOptions.Center : TextAlignmentOptions.Left;

            // Set the rect width dynamically so it matches the box width
            tmp.rectTransform.sizeDelta = new Vector2(width - 1.0f, 2);

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
        private float MeasureText(string content, float fontSize, bool isBold = false)
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
        private void DrawEdge(GameObject parent, GameObject fromObj, GameObject toObj, string edgeKey)
        {
            Vector3 startPoint = GetBorderPoint(fromObj, toObj.transform.position);
            Vector3 endPoint = GetBorderPoint(toObj, fromObj.transform.position);

            var edgeGo = new GameObject($"Edge_{edgeKey}");
            edgeGo.transform.SetParent(parent.transform, false);

            var lr = edgeGo.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.useWorldSpace = false;
            lr.SetPosition(0, startPoint);
            lr.SetPosition(1, endPoint);
            lr.startWidth = lr.endWidth = 0.04f;
            lr.material = Resources.Load<Material>("Materials/DefaultMat");
            lr.startColor = lr.endColor = Color.black;
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
    }
}