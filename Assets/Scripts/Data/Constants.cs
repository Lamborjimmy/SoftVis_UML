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
        public const string DEPLOYMENT_DIAGRAM = "deployment";
        public const string PACKAGE_DIAGRAM = "package";
        public const string STATE_DIAGRAM = "state";
        public const string USECASE_DIAGRAM = "usecase";
    }
    public class DiagramNodeTypes
    {
        public const string DIAGRAM = "DIAGRAM";
        public const string PACKAGE = "UML_PACKAGE";
        public const string CLASS = "UML_CLASS";
        public const string INTERFACE = "UML_INTERFACE";
        public const string ENUMERATION = "UML_ENUMERATION";
        public const string USECASE = "UML_USECASE";
        public const string ACTOR = "UML_ACTOR";
        public const string ACTIVITY = "UML_ACTIVITY";
        public const string SWIMLANE = "UML_SWIMLANE";
        public const string STATE = "UML_STATE";
        public const string PSEUDOSTATE = "UML_PSEUDOSTATE";
        public const string NODE = "UML_NODE";
        public const string COMPONENT = "UML_COMPONENT";
        public const string LIFELINE = "UML_LIFELINE";
        public const string ATTRIBUTE = "UML_ATTRIBUTE";
        public const string METHOD = "UML_METHOD";
        public const string PARAMETER = "UML_PARAMETER";
        public const string ACTION = "UML_ACTION";
        public const string EXTENSION_POINT = "UML_EXTENSION_POINT";
        public const string OBJECT_NODE = "UML_OBJECT_NODE";
        public const string ARTIFACT = "UML_ARTIFACT";
        public const string PORT = "UML_PORT";
        public const string INITIAL = "UML_INITIAL";
        public const string FINAL = "UML_FINAL";
        public const string FORK = "UML_FORK";
        public const string JOIN = "UML_JOIN";
        public const string DECISION = "UML_DECISION";
        public const string REQUIRED_INTERFACE = "UML_REQUIRED_INTERFACE";
        public const string PROVIDED_INTERFACE = "UML_PROVIDED_INTERFACE";
    }
    public class DiagramEdgeTypes
    {
        public const string NESTED = "NESTED";
        public const string COMPOSES = "COMPOSES";
        public const string AGGREGATES = "AGGREGATES";
        public const string ASSOCIATED_WITH = "ASSOCIATED_WITH";
        public const string GENERALIZES = "GENERALIZES";
        public const string DEPENDENCY = "DEPENDENCY";
        public const string REALIZES_UML = "REALIZES_UML";
        public const string FLOWS_TO = "FLOWS_TO";
        public const string OBJECT_FLOW = "OBJECT_FLOW";
        public const string TRANSITIONS_TO = "TRANSITIONS_TO";
        public const string MESSAGES = "MESSAGES";
        public const string REQUIRES = "REQUIRES";
        public const string PROVIDES = "PROVIDES";
        public const string DEPLOYED_ON = "DEPLOYED_ON";
        public const string REPRESENTS = "REPRESENTS";
        public const string INCLUDES_UML = "INCLUDES_UML";
        public const string EXTENDS_UML = "EXTENDS_UML";
    }
}