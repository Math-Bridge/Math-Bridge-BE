# Math-Bridge MCP Server

## Overview

Yes, Math-Bridge has MCP (Model Context Protocol) server capabilities! This document describes how the Math-Bridge backend API can be used as an MCP server to provide AI assistants with access to tutoring management services.

## What is MCP?

Model Context Protocol (MCP) is an open protocol that standardizes how applications provide context to AI assistants. It enables secure, controlled connections between AI tools and various data sources and services.

## Math-Bridge MCP Server Capabilities

The Math-Bridge backend API serves as an MCP server by exposing a comprehensive REST API for tutoring management operations. The server provides access to:

### Resources

1. **User Management** - Authentication, registration, and user profiles
2. **Tutor Management** - Tutor profiles, schedules, and verification
3. **Session Management** - Tutoring session scheduling and management
4. **Student Management** - Child/student profiles and progress tracking
5. **Contract Management** - Agreements between parents and tutors
6. **Package Management** - Service packages and pricing
7. **Center Management** - Tutoring center information
8. **School Management** - School profiles and data
9. **Curriculum Management** - Educational content and structure
10. **Unit Management** - Educational units and modules
11. **Notifications** - Push notifications and alerts
12. **Feedback & Reports** - Session feedback and daily reports
13. **Test Results** - Student assessment tracking
14. **Wallet & Transactions** - Financial management
15. **Payments** - SePay payment processing
16. **Statistics** - Analytics and reporting
17. **Video Conferencing** - Google Meet and Zoom integration
18. **Location Services** - Google Maps integration

### Tools

- `send_notification` - Send push notifications via Firebase
- `schedule_session` - Schedule tutoring sessions
- `process_payment` - Handle payment transactions
- `create_video_meeting` - Create video conference meetings
- `send_email` - Send email notifications
- `calculate_distance` - Calculate distances using Google Maps

## Configuration

The MCP server configuration is defined in `mcp-server.json`. This file contains:

- Base URL and endpoints
- Authentication requirements (JWT Bearer)
- Available resources and operations
- Tool descriptions
- Technology stack information

## Authentication

The Math-Bridge MCP server uses JWT (JSON Web Token) authentication:

1. Obtain a JWT token by authenticating via `/api/auth/login` or `/api/auth/google-login`
2. Include the token in the `Authorization` header as `Bearer {token}`
3. All API requests require valid authentication

## API Documentation

Interactive API documentation is available via Swagger UI:

- **Development**: `http://localhost:5000/swagger`
- **Production**: `https://api.vibe88.tech/swagger`

## Integration Examples

### Using with AI Assistants

AI assistants can use the Math-Bridge MCP server to:

1. **Query tutoring data**: Retrieve information about tutors, sessions, students
2. **Schedule sessions**: Create and manage tutoring appointments
3. **Send notifications**: Alert users about important events
4. **Process payments**: Handle financial transactions
5. **Generate reports**: Create analytics and statistics
6. **Manage video meetings**: Set up virtual tutoring sessions

### Example: Scheduling a Session

```
AI Assistant → Math-Bridge MCP Server
POST /api/session
{
  "tutorId": 123,
  "childId": 456,
  "startTime": "2025-11-15T10:00:00Z",
  "duration": 60
}
```

### Example: Sending a Notification

```
AI Assistant → Math-Bridge MCP Server
POST /api/notification
{
  "userId": 789,
  "message": "Your tutoring session starts in 15 minutes",
  "type": "session_reminder"
}
```

## Technology Stack

- **Framework**: ASP.NET Core 6.0+
- **Database**: SQL Server with Entity Framework Core
- **Authentication**: JWT Bearer tokens
- **Cloud Services**: 
  - Firebase (Push notifications)
  - Google Cloud Pub/Sub (Event messaging)
  - Google Maps API (Location services)
- **Video Conferencing**: Google Meet and Zoom integration
- **Payment Gateway**: SePay
- **API Documentation**: Swagger/OpenAPI

## Environment Configuration

### Development
- Base URL: `http://localhost:5000`
- Swagger UI: `http://localhost:5000/swagger`

### Production
- Base URL: `https://api.vibe88.tech`
- Swagger UI: `https://api.vibe88.tech/swagger`

## CORS Configuration

The server is configured to accept requests from:
- `https://web.vibe88.tech`
- `https://api.vibe88.tech`
- `http://localhost:5173` (development)

## Security Features

- JWT-based authentication
- Role-based authorization
- HTTPS/TLS encryption in production
- SQL Server retry policies for resilience
- Firebase credential validation
- Token expiration enforcement

## How to Connect

To connect an MCP client (like an AI assistant) to the Math-Bridge server:

1. Configure the MCP client with the base URL
2. Set up JWT authentication flow
3. Reference the `mcp-server.json` for available endpoints
4. Use the Swagger documentation for detailed request/response schemas

## Support

For questions or issues related to the MCP server capabilities:
- Review the Swagger documentation for detailed API specs
- Check the `mcp-server.json` configuration file
- Refer to the controller implementations in the `Controllers` directory

## Future Enhancements

Potential MCP server enhancements under consideration:
- GraphQL endpoint for more flexible queries
- WebSocket support for real-time updates
- Enhanced filtering and search capabilities
- Batch operations support
- Advanced analytics and reporting tools

---

**Last Updated**: November 2025
**API Version**: v1.0.0
