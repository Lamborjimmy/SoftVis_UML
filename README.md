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

### 📝 To Implement

- [ ] **Activity Diagram**
- [ ] **Component Diagram**
- [ ] **Communication Diagram**
- [ ] **Sequence Diagram**

---

## 2. Nodes & Elements

### ✅ Implemented

- [x] **Diagram Plane**: Base node representing the UML diagram used as a canvas for drawing elements or hosting nested sub-diagrams.
- [x] **Class Node**: Node representing a class in a class diagram.
- [x] **Interface Node**: Node representing an interface in a class diagram.
- [x] **Enumeration Node**: Node representing an enumeration in a class diagram.
- [x] **Method Node**: Node representing methods/operations inside a class.
- [x] **Attribute Node**: Node representing attributes/properties inside a class.
- [x] **Actor Node**: Stick-figure node representing an actor in a use case diagram.
- [x] **Use Case Node**: Elliptical node representing a use case.
- [x] **Package Node**: Folder-like node representing a package in a package diagram.
- [x] **State Node**: Node representing a standard behavioral state in a state diagram.
- [x] **Pseudostate Node (Basic)**: Nodes representing `initial` (start) or `final` (end) states.
- [x] **Deployment Node**: 3D node representing a hardware/execution environment in a deployment diagram.
- [x] **Component Node (Deployment)**: Node representing a software component residing on a deployment node.
- [x] **Provided Interface**: Node representing an exposed interface in a deployment/component diagram.

### 📝 To Implement

- [ ] **Parameter Node**: Sub-node representing an input parameter for a `Method Node`.
- [ ] **Component Node (Package)**: Node representing a component natively within a package diagram.
- [ ] **Artifact Node**: Node representing a deployable file/artifact (e.g., `.jar`, `.dll`).
- [ ] **Composite State Node**: State node capable of containing nested sub-states or orthogonal regions.
- [ ] **Advanced Pseudostates**: Explicit nodes for `choice`, `fork`, `join`, `junction`, and `entry/exit points` in state diagrams.
- [ ] **State Internal Behaviors**: Dedicated compartments for `entry /`, `do /`, and `exit /` actions within a state node.

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

### 📝 To Implement

- [ ] **Edge Labels**: General text labeling on edges for specific roles or names.
- [ ] **Multiplicity**: Source and target instance notations (e.g., `1`, `0..*`) primarily for Class diagrams.
- [ ] **Transition Labels**: Labels for state transitions specifying `[Guard]`, `Trigger`, and `/Effect`.
- [ ] **Package Edge Notation**: Explicit stereotype labels like `«merge»`, `«import»`, `«access»`.
- [ ] **Deployment Edge Notation**: Explicit stereotype labels like `«manifest»`, `«deploy»`.

---

## 4. Global Features & Mechanics

### ✅ Implemented

- [x] **Hierarchical Nesting**: Recursive parsing of `DiagramEdgeTypes.NESTED` to visually embed elements inside containers (e.g., packages inside packages, components inside nodes).
- [x] **Edge Hub**: Routing mechanism that intercepts and organizes edges before they enter a node to prevent visual clutter.

### 📝 To Implement

- [ ] **Diagram Plane Scaling**: Automatic/default bounding box scaling when rendering multiple sub-graphs or disjointed graphs on the same plane.
- [ ] **Universal Node Prefabs**: Transitioning from procedural primitive generation (e.g., `GameObject.CreatePrimitive`) to a fully Prefab-based system for all node types to unify aesthetics.
