using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Data;
using Assets.Scripts.Models;

namespace Assets.Scripts.Builders
{
    public class ComponentDiagramBuilder : BaseDiagramBuilder
    {
        public ComponentDiagramBuilder(ITextMeasurer textMeasurer) : base(textMeasurer) { }

        public override DiagramModel Build(GraphMetadata metadata, List<NodeData> nodes, List<EdgeData> edges)
        {
            var diagram = new DiagramModel
            {
                Id = metadata.Key,
                Name = metadata.Name,
                DiagramType = DiagramTypes.COMPONENT_DIAGRAM
            };

            var nesting = BuildNestingHierarchy(nodes, edges);
            var nodeBoundsDict = new Dictionary<string, BoundsData>();
            var nodePositions = new Dictionary<string, Vec3>();

            foreach (var node in nodes)
            {
                if (node == nesting.RootDiagram || node.Type == DiagramNodeTypes.DIAGRAM) continue;

                int depth = nesting.GetDepth(node.Key);
                float dampeningFactor = 0.6f;
                float currentElevation = Y_ELEVATION * (1f + (depth * dampeningFactor));

                NodeModel nodeModel;

                if (nesting.IsContainer(node.Key))
                    nodeModel = BuildContainerNode(node, nesting.ParentToChildren, currentElevation, depth);
                else if (node.Type.Contains("INTERFACE"))
                    nodeModel = BuildInterfaceNode(node, currentElevation);
                else if (node.Type == DiagramNodeTypes.PORT)
                    nodeModel = BuildPortNode(node, currentElevation);
                else if (node.Type == DiagramNodeTypes.ACTOR)
                    nodeModel = BuildActorNode(node, currentElevation);
                else
                    nodeModel = BuildComponentNode(node, currentElevation, depth);

                nodeBoundsDict[node.Key] = nodeModel.Bounds;
                nodePositions[node.Key] = nodeModel.Position;
                diagram.Nodes.Add(nodeModel);
            }

            var validEdges = edges.Where(e => e.Type != DiagramEdgeTypes.NESTED).ToList();
            SnapPortsToParentPerimeter(diagram.Nodes, nesting.ChildToParent, nodeBoundsDict, nodePositions, validEdges);

            diagram.Edges = BuildEdgeModels(edges);

            return diagram;
        }
        private NodeModel BuildContainerNode(NodeData node, Dictionary<string, List<NodeData>> parentToChildren, float currentElevation, int depth)
        {
            GetRecursiveBounds(node.Key, parentToChildren, out float minX, out float maxX, out float minZ, out float maxZ);

            float paddingX = 4.0f;
            float paddingZ = 1.0f;

            float width = (maxX - minX) + (paddingX * 2);
            float height = (maxZ - minZ) + (paddingZ * 2);
            float centerZ = (minZ + maxZ) / 2f;

            Vec3 position = new Vec3((minX + maxX) / 2f, node.GetNodePosition().Y + currentElevation - (Y_ELEVATION / 2f), centerZ);

            var nodeModel = BuildNodeModel(node, position, width, height, GetLayerColor(depth), RGBA.Black, 0, false);

            float textZ = (height / 2f) - 1.5f;
            if (node.Type == DiagramNodeTypes.COMPONENT)
            {
                nodeModel.StereotypeLabel = "<<component>>";
                nodeModel.Labels.Add(CreateLabel(
                    "<<component>>",
                    new Vec3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, textZ),
                    width,
                    LABEL_FONT_SIZE,
                    nodeModel.TextColor,
                    TextAlignment.Top,
                    FontStyle.Normal
                ));
            }

            nodeModel.Labels.Add(CreateLabel(
                node.GetNodeName(),
                new Vec3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, textZ - LINE_HEIGHT),
                width,
                HEADER_FONT_SIZE,
                nodeModel.TextColor,
                TextAlignment.Top,
                FontStyle.Bold
            ));

            return nodeModel;
        }
        private NodeModel BuildComponentNode(NodeData node, float currentElevation, int depth)
        {
            float textWidth = MeasureText(node.GetNodeName() ?? "", HEADER_FONT_SIZE, true);
            float width = Math.Max(textWidth + 4f, 6f);
            float height = 3f;
            var pos = new Vec3(node.GetNodePosition().X, node.GetNodePosition().Y + currentElevation, node.GetNodePosition().Z);
            var nodeModel = BuildNodeModel(node, pos, width, height, GetLayerColor(depth), RGBA.Black, currentElevation, false);

            nodeModel.Labels.Add(CreateLabel(
                "<<component>>",
                new Vec3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, 0.5f),
                width,
                LABEL_FONT_SIZE,
                nodeModel.TextColor,
                TextAlignment.Center,
                FontStyle.Normal
            ));

            nodeModel.Labels.Add(CreateLabel(
                node.GetNodeName(),
                new Vec3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, 0.5f - LINE_HEIGHT),
                width,
                HEADER_FONT_SIZE,
                nodeModel.TextColor,
                TextAlignment.Center,
                FontStyle.Bold
            ));

            return nodeModel;
        }
        private NodeModel BuildInterfaceNode(NodeData node, float currentElevation)
        {
            float width = 1.0f;
            var pos = new Vec3(node.GetNodePosition().X, node.GetNodePosition().Y + currentElevation, node.GetNodePosition().Z);
            var nodeModel = BuildNodeModel(node, pos, width, width, RGBA.White, RGBA.Black, currentElevation, true);

            nodeModel.Labels.Add(CreateLabel(
                node.GetNodeName(),
                new Vec3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, -2.5f),
                Math.Max(width, 8f),
                HEADER_FONT_SIZE,
                nodeModel.TextColor,
                TextAlignment.Top,
                FontStyle.Bold
            ));

            return nodeModel;
        }

        private NodeModel BuildPortNode(NodeData node, float currentElevation)
        {
            float width = 0.75f;
            var pos = new Vec3(node.GetNodePosition().X, node.GetNodePosition().Y + currentElevation, node.GetNodePosition().Z);
            var nodeModel = BuildNodeModel(node, pos, width, width, RGBA.Gray, RGBA.Black, currentElevation, false);
            return nodeModel;
        }

        private NodeModel BuildActorNode(NodeData node, float currentElevation)
        {
            float textWidth = MeasureText(node.GetNodeName() ?? "", HEADER_FONT_SIZE, true);
            float width = 1f;
            var pos = new Vec3(node.GetNodePosition().X, node.GetNodePosition().Y + currentElevation, node.GetNodePosition().Z);
            var nodeModel = BuildNodeModel(node, pos, width, width, RGBA.White, RGBA.Black, currentElevation, true);

            nodeModel.Labels.Add(CreateLabel(
                node.GetNodeName(),
                new Vec3(0, Y_ELEVATION + Y_ELEVATION_TEXT_OFFSET, -2.5f),
                Math.Max(textWidth + 3f, 8f),
                HEADER_FONT_SIZE,
                nodeModel.TextColor,
                TextAlignment.Top,
                FontStyle.Bold
            ));

            return nodeModel;
        }
        private RGBA GetLayerColor(int depth)
        {
            RGBA[] palette = new RGBA[]
            {
                new RGBA(0.40f, 0.50f, 0.65f, 1f),
                new RGBA(0.45f, 0.60f, 0.50f, 1f),
                new RGBA(0.60f, 0.45f, 0.55f, 1f),
                new RGBA(0.65f, 0.55f, 0.40f, 1f),
                new RGBA(0.45f, 0.55f, 0.60f, 1f)
            };

            RGBA c = palette[depth % palette.Length];
            return c;
        }


        private void SnapPortsToParentPerimeter(List<NodeModel> nodeModels, Dictionary<string, string> childToParent, Dictionary<string, BoundsData> nodeBoundsDict, Dictionary<string, Vec3> nodePositions, List<EdgeData> validEdges)
        {
            var portConnections = BuildPortConnections(validEdges);
            var nodeModelDict = nodeModels.ToDictionary(n => n.Id);

            foreach (var nodeModel in nodeModels)
            {
                bool isPort = nodeModel.NodeType == DiagramNodeTypes.PORT;
                if (!isPort || !childToParent.TryGetValue(nodeModel.Id, out string parentKey)) continue;
                if (!nodeBoundsDict.TryGetValue(parentKey, out BoundsData parentBounds)) continue;

                Vec3 parentCenter = parentBounds.Center;
                Vec3 extents = parentBounds.Extents;
                Vec3 portPos = nodeModel.Position;

                Vec3 dir = ComputePortDirection(nodeModel.Id, parentCenter, portPos, portConnections, nodePositions);

                if (dir.SqrMagnitude > 0.0001f)
                {
                    nodeModel.Position = SnapToPerimeter(parentCenter, extents, dir, portPos.Y);
                    nodeModel.Bounds = new BoundsData(nodeModel.Position, nodeModel.Scale);
                }
            }

            RelaxOverlappingPorts(nodeModels, childToParent, nodeBoundsDict);
        }

        private Dictionary<string, List<string>> BuildPortConnections(List<EdgeData> validEdges)
        {
            var portConnections = new Dictionary<string, List<string>>();
            foreach (var edge in validEdges)
            {
                string fromKey = edge.From;
                string toKey = edge.To;

                if (!portConnections.ContainsKey(fromKey)) portConnections[fromKey] = new List<string>();
                portConnections[fromKey].Add(toKey);

                if (!portConnections.ContainsKey(toKey)) portConnections[toKey] = new List<string>();
                portConnections[toKey].Add(fromKey);
            }
            return portConnections;
        }

        private Vec3 ComputePortDirection(string portKey, Vec3 parentCenter, Vec3 portPos, Dictionary<string, List<string>> portConnections, Dictionary<string, Vec3> nodePositions)
        {
            Vec3 targetDir = Vec3.Zero;
            int connectionCount = 0;

            if (portConnections.TryGetValue(portKey, out List<string> connectedKeys))
            {
                foreach (string connectedKey in connectedKeys)
                {
                    if (nodePositions.TryGetValue(connectedKey, out Vec3 connectedPos))
                    {
                        targetDir = targetDir + (connectedPos - parentCenter);
                        connectionCount++;
                    }
                }
            }

            Vec3 dir = (connectionCount > 0 && targetDir.SqrMagnitude > 0.0001f) ? targetDir / connectionCount : portPos - parentCenter;
            dir.Y = 0f;
            return dir;
        }

        private Vec3 SnapToPerimeter(Vec3 parentCenter, Vec3 extents, Vec3 dir, float yPos)
        {
            float scaleX = extents.X / Math.Max(Math.Abs(dir.X), 0.0001f);
            float scaleZ = extents.Z / Math.Max(Math.Abs(dir.Z), 0.0001f);
            float snapScale = Math.Min(scaleX, scaleZ);

            Vec3 snappedPos = parentCenter + (dir * snapScale);
            snappedPos.Y = yPos;
            return snappedPos;
        }

        private void RelaxOverlappingPorts(List<NodeModel> nodeModels, Dictionary<string, string> childToParent, Dictionary<string, BoundsData> nodeBoundsDict)
        {
            float minPortSpacing = 2.0f;
            int relaxationIterations = 10;

            var parentToPorts = new Dictionary<string, List<NodeModel>>();
            foreach (var nodeModel in nodeModels)
            {
                if (nodeModel.NodeType != DiagramNodeTypes.PORT) continue;
                if (!childToParent.TryGetValue(nodeModel.Id, out string parentKey)) continue;

                if (!parentToPorts.ContainsKey(parentKey)) parentToPorts[parentKey] = new List<NodeModel>();
                parentToPorts[parentKey].Add(nodeModel);
            }

            for (int step = 0; step < relaxationIterations; step++)
            {
                foreach (var kvp in parentToPorts)
                {
                    string parentKey = kvp.Key;
                    List<NodeModel> ports = kvp.Value;

                    if (ports.Count < 2) continue;
                    if (!nodeBoundsDict.TryGetValue(parentKey, out BoundsData parentBounds)) continue;

                    for (int i = 0; i < ports.Count; i++)
                    {
                        for (int j = i + 1; j < ports.Count; j++)
                        {
                            Vec3 diff = ports[i].Position - ports[j].Position;
                            diff.Y = 0;
                            float dist = diff.Magnitude;

                            if (dist < minPortSpacing)
                            {
                                if (dist < 0.001f)
                                {
                                    var rand = new Random();
                                    diff = new Vec3((float)(rand.NextDouble() - 0.5), 0, (float)(rand.NextDouble() - 0.5)).Normalized;
                                }

                                Vec3 push = diff.Normalized * ((minPortSpacing - dist) * 0.5f);
                                ports[i].Position = ports[i].Position + push;
                                ports[j].Position = ports[j].Position - push;
                            }
                        }
                    }

                    foreach (var port in ports)
                    {
                        Vec3 currentPos = port.Position;
                        Vec3 dir = currentPos - parentBounds.Center;
                        dir.Y = 0;

                        if (dir.SqrMagnitude > 0.0001f)
                        {
                            port.Position = SnapToPerimeter(parentBounds.Center, parentBounds.Extents, dir, currentPos.Y);
                            port.Bounds = new BoundsData(port.Position, port.Scale);
                        }
                    }
                }
            }
        }
    }
}