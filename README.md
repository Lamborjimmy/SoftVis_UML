# UML Diagram Visualizer: Feature & Implementation Checklist

This document tracks the implementation status of UML elements, nodes, edges, and features across the supported diagram types within the Unity visualizer, based on standard UML specifications.

---

## 1. Supported Diagrams

### ✅ Implemented

- [x] **Class Diagram**
- [x] **Deployment Diagram**
- [x] **Package Diagram**
- [x] **State Machine Diagram**
- [x] **Use Case Diagram**
- [x] **Activity Diagram**
- [x] **Component Diagram**
- [x] **Communication Diagram**

### 📝 To Implement

- [ ] **Sequence Diagram**

---

## 2. Nodes & Elements

### ✅ Implemented

- [x] **Diagram Plane**: Base node representing the UML diagram used as a canvas.
- [x] **Class Node**: Node representing a class in a class diagram.
- [x] **Interface Node**: Node representing an interface in a class diagram.
- [x] **Enumeration Node**: Node representing an enumeration in a class diagram.
- [x] **Method Node**: Node representing methods/operations inside a class.
- [x] **Attribute Node**: Node representing attributes/properties inside a class.
- [x] **Actor Node**: Stick-figure node representing an actor in a use case or communication diagram.
- [x] **Use Case Node**: Elliptical node representing a use case.
- [x] **Use Case Extension Points**: Dynamic compartments in Use Case nodes displaying extension points.
- [x] **Package Node**: Folder-like node representing a package in a package diagram.
- [x] **State Node**: Node representing a standard behavioral state in a state diagram.
- [x] **Pseudostate Node (Basic)**: Nodes representing `initial` (start) or `final` (end) states.
- [x] **Deployment Node**: 3D node representing a hardware/execution environment in a deployment diagram.
- [x] **Component Node (Deployment)**: Node representing a software component residing on a deployment node.
- [x] **Artifact Node**: Node representing a deployable file/artifact (e.g., `.jar`, `.dll`).
- [x] **Provided Interface**: Node representing an exposed interface.
- [x] **Required Interface**: Node representing a required/consumed interface dependency.
- [x] **Component Node**: Node representing a component natively within a component/package diagram.
- [x] **Port Node**: Node representing an exposed connection point on a component boundary.
- [x] **Action Node**: Basic node representing an activity or action in an activity diagram.
- [x] **Swimlane Node**: Node representing a partition (swimlane) in an activity diagram.
- [x] **Advanced Pseudostates**: Explicit nodes for `decision`/`choice`, `fork`, and `join`.
- [x] **Lifeline Node**: Node representing an object or interacting participant in a communication diagram.
- [x] **State Internal Behaviors**: Dedicated compartments for `entry /`, `do /`, and `exit /` actions within a state node.

### 📝 To Implement

- [ ] **Parameter Node**: Sub-node representing an input parameter for a `Method Node`.
- [ ] **Composite State Node**: State node capable of containing nested sub-states or orthogonal regions.

---

## 3. Edges & Relationships

### ✅ Implemented

- [x] **Generalization**: Inheritance arrow.
- [x] **Association**: Standard communication link.
- [x] **Aggregation**: Hollow diamond arrow.
- [x] **Composition**: Filled diamond arrow.
- [x] **Dependency**: Standard dashed arrow.
- [x] **Include (`«include»`)**: Used in Use Case diagrams.
- [x] **Extend (`«extend»`)**: Used in Use Case diagrams.
- [x] **Transition**: Directional state change edge.
- [x] **Self-associated Edges**: Loops pointing back to the source node.
- [x] **Provides**: Connecting components to provided interfaces.
- [x] **Control Flow / Object Flow**: Directional flow arrows routing activity step-by-step or transferring objects in Activity Diagrams.
- [x] **Edge Labels**: General text labeling on edges for specific roles or names.
- [x] **Multiplicity**: Source and target instance notations (e.g., `1`, `0..*`) primarily for Class diagrams.
- [x] **Transition Labels**: Labels for state transitions specifying `[Guard]`, `Trigger`, and `/Effect`.
- [x] **Message Labels**: Sequential numbering and text labels for messages on communication diagram links.

### 📝 To Implement

- [ ] **Package Edge Notation**: Explicit stereotype labels like `«merge»`, `«import»`, `«access»`.
- [ ] **Deployment Edge Notation**: Explicit stereotype labels like `«manifest»`, `«deploy»`.

---

## 4. Global Features & Mechanics

### ✅ Implemented

- [x] **Hierarchical Nesting**: Recursive parsing of nested edges to visually embed elements inside containers.
- [x] **Node Stereotype Labels**: Explicitly rendering textual stereotypes (e.g. `«node»`, `«artifact»`) above node names automatically.
- [x] **Edge Hub**: Routing mechanism that intercepts and organizes edges before they enter a node to prevent visual clutter.
- [x] **Rank-Based Layout**: Automatic horizontal layout mapping that groups elements in steps from initial to final.
- [x] **Swimlane Boundaries**: Dynamic calculation of bounding boxes around partitions ensuring nested elements remain safely contained within their lanes.
- [x] **Port Snapping**: Algorithmic separation and snapping of port nodes to the physical perimeter of parent Component boundaries.
- [x] **Dynamic Text Measuring**: Utility measuring and properly scaling text width backgrounds dynamically (e.g. for Actor/Lifeline/Use Case text widths).
- [x] **Universal Node Prefabs**: Transitioning from procedural primitive generation to a fully Prefab-based system for all node types to unify aesthetics.
- [x] **Diagram Plane Scaling**: Automatic/default bounding box scaling when rendering multiple sub-graphs or disjointed graphs on the same plane.

### 📝 To Implement
