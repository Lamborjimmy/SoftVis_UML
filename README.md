# UML Diagram Visualizer: Feature Checklist

This document tracks the implementation status of UML elements and features across the supported diagram types, based on standard UML specifications.

## 1. Class Diagram

**Status:** Core structural nodes and primary relationships implemented. Internal class features and advanced edge notations are missing.

### Implemented

- [x] **Class Nodes**: Basic rendering of classes.
- [x] **Nesting / Inner Classes**: Hierarchical containment of nested classes.
- [x] **Basic Associations**: Standard solid-line connections and self-loops.
- [x] **Aggregation**: Directed relationships using the `AGGREGATES` prefab (hollow diamond).
- [x] **Composition**: Directed relationships using the `COMPOSES` prefab (filled diamond).
- [x] **Generalization**: Inheritance relationships using the `GENERALIZES` prefab (hollow triangle).

### Todo

- [ ] **Interfaces**: Distinct visual representation (e.g., `«interface»` stereotype or lollipop notation).
- [ ] **Abstract Classes**: Italicized class name labels to denote abstraction.
- [ ] **Attributes Compartment**: Displaying properties/fields inside the class node.
- [ ] **Operations Compartment**: Displaying methods/functions inside the class node.
- [ ] **Visibility Modifiers**: Prefixing attributes/methods with `+` (public), `-` (private), `#` (protected), or `~` (package).
- [ ] **Multiplicity**: Edge labels representing instance limits (e.g., `1`, `0..1`, `*`, `1..*`).
- [ ] **Edge Roles**: Text labels on edges to indicate how the associated classes interact.

---

## 2. Deployment Diagram

**Status:** Foundation for nodes and basic topology implemented. Specific artifact routing and hardware/software differentiation are missing.

### Implemented

- [x] **Deployment Nodes**: Basic 3D representations of nodes.
- [x] **Node Nesting**: Hierarchical rendering for nodes containing components/other nodes.
- [x] **Communication Paths**: Basic edges drawing connections between nodes.

### Todo

- [ ] **Execution Environments vs. Devices**: Distinct visual stereotypes to separate hardware (devices) from software containers (execution environments).
- [ ] **Artifacts**: Specific nodes representing physical files (e.g., `.jar`, `.dll`, scripts) using the document icon or `«artifact»`.
- [ ] **Manifestation Relationships**: Dashed lines (`«manifest»`) showing which artifact implements which component.
- [ ] **Deployment Relationships**: Dashed lines (`«deploy»`) specifying an artifact is deployed on a specific node.
- [ ] **Communication Protocol Labels**: Labels on communication paths defining the protocol used (e.g., `«TCP/IP»`, `«HTTP»`).
- [ ] **Deployment Specifications**: Nodes representing configuration files tied to a deployment.

---

## 3. Package Diagram

**Status:** Package containment and basic associations implemented. Explicit import/merge definitions and element visibility are missing.

### Implemented

- [x] **Package Nodes**: Folder-like representations using the package prefab.
- [x] **Nested Packages**: Recursive calculation allowing packages to contain sub-packages.
- [x] **Basic Dependencies**: Simple edges connecting different packages.

### Todo

- [ ] **Package Imports (`«import»`)**: Dashed dependencies indicating a public import of another package's contents.
- [ ] **Package Merges (`«merge»`)**: Dashed dependencies indicating that contents are combined/extended.
- [ ] **Package Access (`«access»`)**: Dashed dependencies indicating a private import.
- [ ] **Fully Qualified Names**: Text labels displaying the hierarchy (e.g., `Parent::Child::Class`).
- [ ] **Element Visibility**: Indicators showing if packaged elements are exported (public) or hidden (private) from importers.

---

## 4. State Machine Diagram

**Status:** Basic lifecycle (Start -> State -> End) implemented. Detailed transition logic, composite states, and complex routing are missing.

### Implemented

- [x] **State Nodes**: Basic visual representations of states.
- [x] **Initial State**: Support for the solid black circle start node (`INITIAL` prefab).
- [x] **Final State**: Support for the bullseye end node (`FINAL` prefab).
- [x] **Basic Transitions**: Edges routing state changes.

### Todo

- [ ] **Transition Triggers**: Labels on edges indicating the event that causes the state change.
- [ ] **Transition Guards**: Boolean conditions on edges evaluated before transitioning (e.g., `[condition]`).
- [ ] **Transition Effects**: Actions executed during the transition (e.g., `/ action`).
- [ ] **Internal Activities**: Compartment inside the state node for `entry /`, `do /`, and `exit /` behaviors.
- [ ] **Choice Pseudostates**: Diamond nodes for dynamic conditional branching.
- [ ] **Junction Pseudostates**: Filled circle nodes for static merging/branching.
- [ ] **Fork and Join**: Heavy black bars indicating the splitting or synchronizing of concurrent threads.
- [ ] **Composite States**: States that contain orthogonal regions or nested sub-states.
- [ ] **History States**: Support for shallow `[H]` and deep `[H*]` state memory pseudostates.

---

## 5. Use Case Diagram

**Status:** Core logic and associations implemented. System boundaries, general logic reuse, and actor multiplicities are missing.

### Implemented

- [x] **Actors**: Dedicated stick-figure nodes (`ACTOR` prefab).
- [x] **Use Cases**: Oval/elliptical nodes representing system functionalities.
- [x] **Include Relationships**: Dashed arrows pointing to included use cases (`«include»`).
- [x] **Extend Relationships**: Dashed arrows pointing to base use cases (`«extend»`), including extension point mapping.
- [x] **Associations**: Solid lines connecting actors to the use cases they participate in.

### Todo

- [ ] **System Boundary**: A visual, labeled rectangular container grouping related use cases, separating them from external actors.
- [ ] **Actor Generalization**: Inheritance relationships between actors (e.g., "Guest" generalizing to "Registered User").
- [ ] **Use Case Generalization**: Inheritance relationships between use cases.
- [ ] **Multiplicity on Associations**: Labels indicating how many instances of an actor can interact with the use case simultaneously.
- [ ] **Extension Point Compartments**: Visual lists inside the Use Case node explicitly declaring available extension points.
