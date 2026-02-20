namespace Assets.Scripts.Data
{
    public class APIDefinitions
    {
        public const string API_BASE_URL = "http://localhost:18080";
        public const string API_VERSION = "api/v1";
    }
    public class PipelineReturnModes
    {
        public const string SUBGRAPH = "subgraph";
        public const string VERTICES = "vertices";
        public const string EDGES = "edges";
        public const string PATH = "paths";
        public const string COUNT = "count";
    }
    public class PipelineStepTypes
    {
        // Filter & Logical
        /// <summary>Filters vertices based on attributes</summary>
        public const string FILTER_NODES = "filter_nodes";
        /// <summary>Filters edges based on attributes</summary>
        public const string FILTER_EDGES = "filter_edges";
        /// <summary>Filters using boolean expressions (AND/OR/NOT)</summary>
        public const string WHERE = "where";

        // Stream & Set Management
        /// <summary>Materializes the current stream into a named set (array)</summary>
        public const string LET = "let";
        /// <summary>Resets the current stream back to the base vertex set</summary>
        public const string RESET = "reset";
        /// <summary>Sets the current stream to a named set</summary>
        public const string USE_SET = "use_set";
        /// <summary>Performs set union (distinct)</summary>
        public const string UNION = "union";
        /// <summary>Performs set intersection</summary>
        public const string INTERSECT = "intersect";
        /// <summary>Performs set subtraction (left minus right)</summary>
        public const string EXCEPT = "except";
        /// <summary>Performs an inner join between two sets</summary>
        public const string JOIN = "join";

        // Graph Traversal & Algorithms
        /// <summary>Performs a shortest-path query from the current vertex to a target vertex</summary>
        public const string SHORTEST_PATH = "shortest_path";
        /// <summary>Expands the k-hop neighborhood from the current vertices</summary>
        public const string K_HOP = "k_hop";
        /// <summary>Returns the reachability set from the current vertices (BFS traversal)</summary>
        public const string REACHABLE = "reachable";
        /// <summary>Computes a representative component identifier for each current vertex</summary>
        public const string COMPONENT_ID = "component_id";
        /// <summary>Groups the current vertices into connected components</summary>
        public const string CONNECTED_COMPONENTS = "connected_components";
        /// <summary>Traverses the graph</summary>
        public const string TRAVERSE = "traverse";

        // Results & Aggregation
        /// <summary>Limits the number of results</summary>
        public const string LIMIT = "limit";
        /// <summary>Projects specific fields</summary>
        public const string PROJECT = "project";
        /// <summary>Sorts results by specified fields</summary>
        public const string SORT = "sort";
        /// <summary>Returns count of matching documents</summary>
        public const string COUNT = "count";
        /// <summary>Removes duplicate results</summary>
        public const string DISTINCT = "distinct";
        /// <summary>Groups results and applies aggregations</summary>
        public const string GROUP_BY = "group_by";
        /// <summary>Returns vertices and edges as subgraph</summary>
        public const string COLLECT_SUBGRAPH = "collect_subgraph";

        // Hierarchy Expansion
        /// <summary>Expands to include ancestor nodes</summary>
        public const string INCLUDE_ANCESTORS = "include_ancestors";
        /// <summary>Expands to include descendant nodes</summary>
        public const string INCLUDE_DESCENDANTS = "include_descendants";
    }
    public class DiagramTypes
    {
        public const string ACTIVITY_DIAGRAM = "activity";
        public const string CLASS_DIAGRAM = "class";
        public const string COMMUNICATION_DIAGRAM = "communication";
        public const string COMPONENT_DIAGRAM = "component";
        public const string DEPLOYTMENT_DIAGRAM = "deployment";
        public const string PACKAGE_DIAGRAM = "package";
        public const string STATE_DIAGRAM = "state";
        public const string USECASE_DIAGRAM = "usecase";
    }
}