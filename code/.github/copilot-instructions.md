# GitHub Copilot Instructions

This file contains general instructions for GitHub Copilot when interacting with this codebase.

## Project Context

**Project Name**: me-glipper
**Description**: A personal weight tracking application with Angular frontend and .NET backend

## Design Rules

### General

- Use Tailwind CSS for styling instead of custom CSS when possible
- Prefer standalone Angular components
- Follow a clean architecture approach in the .NET backend
- For components with many records, implement height constraints with scrolling
- Centralize HTTP API calls in services
- Use interfaces for data models

### Frontend (Angular)

- Use Angular's standalone component API
- Implement responsive design for all components
- Use Tailwind utility classes instead of custom CSS
- Keep component logic simple and focused on a single responsibility
- Use reactive forms for all form inputs
- Use TypeScript strict mode for Angular code
- Follow Angular best practices for component design
- Use RxJS patterns for asynchronous operations

#### Styling

- Use Tailwind utility classes directly in HTML
- Avoid custom CSS unless absolutely necessary
- Use a consistent color scheme based on Tailwind's blue palette
- Ensure all UI elements are accessible

### Backend (.NET)

- Use .NET 8+ features where appropriate
- Follow RESTful API design principles
- Implement proper error handling and validation
- Use dependency injection for all services
- Keep controllers thin, with business logic in services
- Include proper error handling in both frontend and backend

#### Database

- Use Azure Table Storage for persistence
- Define clear entity models with appropriate partitioning strategy
- Handle database operations asynchronously

### Security

- Use API key authentication for backend requests
- Store sensitive information securely
- Validate all user inputs

## Azure Guidelines

- Follow Azure best practices when generating Azure-related code
- Use managed identities where possible
- Follow least privilege principles

This file will be updated as preferences and standards evolve.
