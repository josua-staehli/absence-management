# Mini Absence Management

## Description

As part of the onboarding, a small web application for managing absence requests is to be implemented.

The goal is to gain a first hands-on introduction to the technologies and architectural principles we use. The focus is not on a fully production-ready application, but on a clean, comprehensible vertical slice through backend, database and frontend.

The application should consist of a **.NET/C# backend**, simple persistence using **Entity Framework Core**, and a **React frontend**. The backend should use a simple, DDD-oriented architecture.

---

## Goal of the Task

Using a small, manageable project, the employee should demonstrate and practise how they:

- structure a backend with .NET and C#,
- use Entity Framework Core for database access,
- model simple domain logic following DDD principles,
- provide REST endpoints,
- build a React frontend,
- display and edit backend data in the frontend,
- handle validations and error cases cleanly,
- structure and document their code in an understandable way.

---

## Functional Requirements

A small application for managing absence requests is to be created.

An absence request consists of:

- employee
- absence type
- start date
- end date
- status
- optional comment

### Absence Types

At least the following types should be supported:

- vacation
- sickness
- training
- other

### Status

A request should have one of the following statuses:

- Open
- Approved
- Rejected

---

## Business Rules

The following rules should be implemented in the backend:

1. The start date must not be after the end date.
2. An absence request must not be created without an employee.
3. An absence request must have an absence type.
4. A request is initially created with the status `Open`.
5. Only open requests may be approved or rejected.
6. Approved or rejected requests must not be approved or rejected again.
7. Absence requests for the same employee must not overlap in time.
8. An open request may be edited.
9. An approved or rejected request may no longer be edited.

---

## Technical Requirements: Backend

The backend should be implemented with the following technologies:

- **.NET**
- **C#**
- **ASP.NET Core Web API**
- **Entity Framework Core**
- **SQLite, SQL Server LocalDB or in-memory database**
- **DDD-oriented architecture**

### Expected Backend Structure

The solution should be split sensibly into projects or folders, for example:

```text
AbsenceManagement.Api
AbsenceManagement.Application
AbsenceManagement.Domain
AbsenceManagement.Infrastructure
```

A slightly simplified structure is also acceptable, as long as the responsibilities are clearly separated.

### Responsibilities

#### Domain

Contains the business objects and rules.

Examples:

```text
Employee
AbsenceRequest
AbsenceType
AbsenceStatus
DateRange
```

The domain should not depend directly on Entity Framework, API controllers or React.

#### Application

Contains use cases or application services.

Examples:

```text
CreateAbsenceRequest
ApproveAbsenceRequest
RejectAbsenceRequest
UpdateAbsenceRequest
GetAbsenceRequests
```

#### Infrastructure

Contains the technical implementations.

Examples:

```text
DbContext
Entity Framework configurations
Repository implementations
Database migrations
```

#### Api

Contains the REST endpoints.

Examples:

```text
AbsenceRequestsController
EmployeesController
DTOs / request models / response models
```

---

## Expected API Endpoints

At least the following endpoints should be implemented:

### `GET /api/absence-requests`

Returns all absence requests.

### `GET /api/absence-requests/{id}`

Returns a single absence request.

### `POST /api/absence-requests`

Creates a new absence request.

### `PUT /api/absence-requests/{id}`

Edits an open absence request.

### `POST /api/absence-requests/{id}/approve`

Approves an open request.

### `POST /api/absence-requests/{id}/reject`

Rejects an open request.

Optional:

### `DELETE /api/absence-requests/{id}`

Deletes an open request.

---

## Technical Requirements: Frontend

The frontend should be implemented with the following technologies:

- **React**
- ideally **TypeScript**
- Fetch API or Axios for communication with the backend
- simple component structure
- simple form validation

### Expected Frontend Features

The frontend should contain at least the following features:

1. Overview of all absence requests
2. Form for creating a new absence request
3. Detail view or editing capability for open requests
4. Ability to approve a request
5. Ability to reject a request
6. Display of validation and error messages
7. Display of the current status of a request

---

## Proposed UI Structure

The user interface can deliberately be kept simple.

Possible pages or components:

```text
AbsenceRequestList
AbsenceRequestForm
AbsenceRequestDetails
StatusBadge
ApiErrorMessage
```

Example view:

- table with all absence requests
- "Create new request" button
- form with:
  - employee
  - absence type
  - start date
  - end date
  - comment
- actions per request:
  - edit
  - approve
  - reject

---

## Proposed Data Model

### Employee

```text
Id
FirstName
LastName
Email
```

### AbsenceRequest

```text
Id
EmployeeId
AbsenceType
StartDate
EndDate
Status
Comment
CreatedAt
UpdatedAt
```

### AbsenceType

```text
Vacation
Sickness
Training
Other
```

### AbsenceStatus

```text
Open
Approved
Rejected
```

---

## Tests

At least a few meaningful tests should be created.

### Expected Tests

At minimum, domain or application tests for the following rules:

- A request with a start date after the end date is invalid.
- A new request initially has the status `Open`.
- An open request can be approved.
- An approved request cannot be rejected afterwards.
- Overlapping absences for the same employee are not allowed.

Optional:

- integration test for an API endpoint
- tests for repository or application service

---

## Out of Scope

The following topics are deliberately not part of the task:

- login / authentication
- role and permission system
- production-ready UI design
- complex calendar view
- email notifications
- public holiday logic
- half working days
- multi-stage approval process
- production deployment pipeline

---

## Acceptance Criteria

The task is considered complete when:

- a running backend with .NET/C# exists,
- Entity Framework Core is used for persistence,
- a simple DDD-oriented structure is recognisable,
- the business rules are implemented in the backend,
- the most important REST endpoints exist,
- a React frontend exists,
- absence requests can be displayed in the frontend,
- new absence requests can be created via the frontend,
- open requests can be approved or rejected,
- validation errors are displayed comprehensibly,
- at least a few meaningful tests exist,
- a short README exists,
- the application can be started locally.

---

## Expected README Contents

The README should contain at least the following information:

- short description of the application
- technologies used
- project structure
- instructions for starting the backend
- instructions for starting the frontend
- instructions for running the tests
- short description of the most important business rules
- known limitations or open points

---

## Review Criteria

During the final review, the following points in particular should be discussed:

- How was the domain modelled?
- Where does the business logic live?
- How were validations implemented?
- How is the separation between API, Application, Domain and Infrastructure solved?
- How is Entity Framework used?
- How does the frontend communicate with the backend?
- How are errors handled?
- Which tests were written?
- What would they improve given more time?

---

## Optional Additional Scope

If there is time left after implementing the core scope, the following points can be added:

- filter by employee
- filter by status
- sorting by start date
- seed data for employees
- pagination
- simple loading indicator in the frontend
- simple visual status display
- integration test for the API
- Docker Compose for backend and database

---

## Notes

The task should be implemented pragmatically. What matters is not building as many features as possible, but creating a small, clean and comprehensible solution.

The focus is on:

- clean structure,
- understandable code,
- comprehensible business logic,
- sensible separation of responsibilities,
- ease of use,
- learning progress with .NET, Entity Framework, DDD and React.
