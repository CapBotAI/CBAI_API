# CBAI API System Diagrams

This directory contains class diagrams and sequence diagrams for the main flows in the CapBot AI API system.

## Diagram Structure

Each flow has its own directory containing:
- `class.mermaid` - Class diagram showing components and relationships
- `sequence.mermaid` - Sequence diagram showing interaction flows

## Available Flows

### 1. Authentication Flow (`01_Authentication/`)
**Purpose**: User registration and login functionality
- **Main Components**: AuthController, AuthService, User entity, JwtService
- **Key Operations**: Register new users, Login with credentials, JWT token generation
- **Actors**: Users (Students, Supervisors, Admins)

### 2. SemesterPhase Flow (`02_SemesterPhase/`)
**Purpose**: Academic semester and phase management
- **Main Components**: SemesterController, PhaseTypeController, PhaseController with respective services
- **Key Operations**: Create semesters, manage phase types, create phases within semesters
- **Actors**: Administrators
- **Relationships**: Phases belong to Semesters and have PhaseTypes

### 3. Evaluation Criteria Flow (`03_EvaluationCriteria/`)
**Purpose**: Managing evaluation criteria for topic assessments
- **Main Components**: EvaluationCriteriaController, EvaluationCriteriaService, EvaluationCriteria entity
- **Key Operations**: Create, update, delete, and query evaluation criteria
- **Actors**: Administrators and authorized users
- **Features**: Soft delete, pagination, active criteria filtering

### 4. Topic Flow (`04_Topic/`)
**Purpose**: Research topic lifecycle management
- **Main Components**: TopicController, TopicService, Topic entity, TopicVersion entity
- **Key Operations**: Create topics, update topics, approve topics, version management
- **Actors**: Supervisors (create/edit), Administrators (approve), Students (view)
- **Relationships**: Topics belong to Categories and Semesters, have multiple Versions

## Viewing the Diagrams

These diagrams are written in Mermaid format and can be viewed using:
- [Mermaid Live Editor](https://mermaid.live/)
- GitHub's built-in Mermaid rendering
- VS Code with Mermaid extension
- Any Mermaid-compatible viewer

## Architecture Notes

- **Clean Architecture**: Controllers → Interfaces → Services → Entities
- **Repository Pattern**: Services interact with entities through repositories
- **Dependency Injection**: Controllers depend on service interfaces
- **Soft Delete**: Most entities support soft deletion (DeletedAt field)
- **Audit Trail**: Entities track creation and modification timestamps
- **Authorization**: Role-based access control (Admin, Supervisor, Student roles)

## Diagram Conventions

- **Solid lines (→)**: Direct dependencies/associations
- **Dashed lines (..>)**: Usage/data transfer
- **Implementation (|..)**: Interface implementation
- **Composition (||--o{)**: One-to-many relationships
- **Actors**: External users interacting with the system
- **Participants**: Internal system components