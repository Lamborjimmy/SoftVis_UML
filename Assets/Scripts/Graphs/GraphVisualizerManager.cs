using System;
using System.Collections.Generic;
using Assets.Scripts.Data;
using Assets.Scripts.Interfaces;
using Assets.Scripts.Visualizers;
using UnityEngine;

namespace Assets.Scripts.Graphs
{
    public class GraphVisualizerManager : MonoBehaviour
    {
        [Header("Node Type Prefabs")]
        [SerializeField] private GameObject actorPrefab;
        [SerializeField] private GameObject cubePrefab;
        [SerializeField] private GameObject packagePrefab;
        [SerializeField] private GameObject statePrefab;
        [SerializeField] private GameObject initialPrefab;
        [SerializeField] private GameObject finalPrefab;
        [SerializeField] private GameObject providedInterfacePrefab;
        [SerializeField] private GameObject requiredInterfacePrefab;
        [SerializeField] private GameObject useCasePrefab;
        [Header("Edge Type Ends Prefabs")]
        [SerializeField] private GameObject aggregationPrefab;
        [SerializeField] private GameObject compositionPrefab;
        [SerializeField] private GameObject generalizationPrefab;
        [SerializeField] private GameObject arrowPrefab;
        [SerializeField] private float verticalSpacing = 12f;
        [SerializeField] private GraphDataManager graphManager;
        private Dictionary<string, IGraphVisualizer> visualizersByType;
        private DefaultGraphVisualizer defaultGraphVisualizer;

        private readonly Dictionary<string, GameObject> graphContainers = new();
        private readonly List<string> stackOrder = new();

        private void Awake()
        {
            visualizersByType = new Dictionary<string, IGraphVisualizer>(StringComparer.OrdinalIgnoreCase)
            {
                [DiagramTypes.ACTIVITY_DIAGRAM] = new ActivityDiagramVisualizer(),
                [DiagramTypes.CLASS_DIAGRAM] = new ClassDiagramVisualizer(),
                [DiagramTypes.COMMUNICATION_DIAGRAM] = new CommunicationDiagramVisualizer(),
                [DiagramTypes.COMPONENT_DIAGRAM] = new ComponentDiagramVisualizer(),
                [DiagramTypes.DEPLOYTMENT_DIAGRAM] = new DeploymentDiagramVisualizer(),
                [DiagramTypes.PACKAGE_DIAGRAM] = new PackageDiagramVisualizer(),
                [DiagramTypes.STATE_DIAGRAM] = new StateDiagramVisualizer(),
                [DiagramTypes.USECASE_DIAGRAM] = new UseCaseDiagramVisualizer(),
            };
            foreach (var visualizer in visualizersByType.Values)
                visualizer.Initialize(InitializePrefabDictionary());
            defaultGraphVisualizer = new DefaultGraphVisualizer();
        }

        private void OnEnable()
        {
            graphManager.OnFullGraphFetched += VisualizeFullGraph;
        }

        private void OnDisable()
        {
            graphManager.OnFullGraphFetched -= VisualizeFullGraph;
        }

        private void VisualizeFullGraph(GraphMetadata graph, List<NodeData> nodes, List<EdgeData> edges)
        {
            if (graph == null || nodes?.Count == 0) return;
            if (graphContainers.ContainsKey(graph.Key)) return;

            string diagramType = (graph.GraphType ?? "unknown").ToLowerInvariant();

            IGraphVisualizer visualizer = GetVisualizer(diagramType);

            stackOrder.Add(graph.Key);

            float yOffset = (stackOrder.Count - 1) * verticalSpacing;

            var root = new GameObject($"Graph_{graph.Key}_{diagramType}");

            graphContainers[graph.Key] = root;

            visualizer.RenderGraph(graph, root, nodes, edges);
            root.transform.position = new Vector3(0, yOffset, 0);
            Debug.Log($"Stacked & rendered {graph.Key} ({diagramType}) at y = {yOffset}");
        }

        private IGraphVisualizer GetVisualizer(string diagramType)
        {
            if (visualizersByType.TryGetValue(diagramType, out var vis))
                return vis;

            Debug.LogWarning($"No visualizer found for type '{diagramType}' → using class visualizer as default");
            return defaultGraphVisualizer;
        }

        public void RemoveGraph(string graphId)
        {
            if (!graphContainers.TryGetValue(graphId, out var root)) return;

            int idx = stackOrder.IndexOf(graphId);
            stackOrder.Remove(graphId);
            Destroy(root);
            graphContainers.Remove(graphId);

            RealignStack(idx);
        }

        private void RealignStack(int fromIndex)
        {
            for (int i = fromIndex; i < stackOrder.Count; i++)
            {
                var id = stackOrder[i];
                if (graphContainers.TryGetValue(id, out var root))
                {
                    float y = i * verticalSpacing;
                    root.transform.position = new Vector3(root.transform.position.x, y, root.transform.position.z);
                }
            }
        }
        private Dictionary<string, GameObject> InitializePrefabDictionary()
        {
            Dictionary<string, GameObject> dict = new Dictionary<string, GameObject>();
            //Node prefabs
            dict[DiagramNodeTypes.DIAGRAM] = cubePrefab;
            dict[DiagramNodeTypes.ACTOR] = actorPrefab;
            dict[DiagramNodeTypes.CLASS] = cubePrefab;
            dict[DiagramNodeTypes.INTERFACE] = cubePrefab;
            dict[DiagramNodeTypes.ENUMERATION] = cubePrefab;
            dict[DiagramNodeTypes.LIFELINE] = cubePrefab;
            dict[DiagramNodeTypes.NODE] = cubePrefab;
            dict[DiagramNodeTypes.COMPONENT] = cubePrefab;
            dict[DiagramNodeTypes.PACKAGE] = packagePrefab;
            dict[DiagramNodeTypes.STATE] = statePrefab;
            dict[DiagramNodeTypes.INITIAL] = initialPrefab;
            dict[DiagramNodeTypes.FINAL] = finalPrefab;
            dict[DiagramNodeTypes.PROVIDED_INTERFACE] = providedInterfacePrefab;
            dict[DiagramNodeTypes.REQUIRED_INTERFACE] = requiredInterfacePrefab;
            dict[DiagramNodeTypes.PORT] = cubePrefab;
            dict[DiagramNodeTypes.ACTIVITY] = cubePrefab;
            dict[DiagramNodeTypes.ACTION] = cubePrefab;
            dict[DiagramNodeTypes.FORK] = cubePrefab;
            dict[DiagramNodeTypes.JOIN] = cubePrefab;
            dict[DiagramNodeTypes.DECISION] = cubePrefab;
            dict[DiagramNodeTypes.SWIMLANE] = cubePrefab;
            dict[DiagramNodeTypes.USECASE] = useCasePrefab;
            //Edge prefabs
            dict[DiagramEdgeTypes.AGGREGATES] = aggregationPrefab;
            dict[DiagramEdgeTypes.COMPOSES] = compositionPrefab;
            dict[DiagramEdgeTypes.GENERALIZES] = generalizationPrefab;
            dict[DiagramEdgeTypes.INCLUDES_UML] = arrowPrefab;
            dict[DiagramEdgeTypes.EXTENDS_UML] = arrowPrefab;
            dict[DiagramEdgeTypes.DEPENDENCY] = arrowPrefab;
            dict[DiagramEdgeTypes.TRANSITIONS_TO] = arrowPrefab;
            dict[DiagramEdgeTypes.FLOWS_TO] = arrowPrefab;
            dict[DiagramEdgeTypes.OBJECT_FLOW] = arrowPrefab;
            return dict;
        }
    }
}