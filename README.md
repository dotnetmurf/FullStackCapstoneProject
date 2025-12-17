# SkillSnap

## Project Overview
This project was developed for the <a href="https://www.coursera.org/learn/full-stack-developer-capstone-project?specialization=microsoft-full-stack-developer" target="_blank">"Full-Stack Developer Capstone Project"</a>. The project is a comprehensive full-stack project, integrating front-end and back-end components, and optimizing performance. The course focuses on leveraging Microsoft Copilot for all phases of the development workflow. This capstone project is the final of twelve courses required for obtaining the <a href="https://www.coursera.org/professional-certificates/microsoft-full-stack-developer" target="_blank">"Microsoft Full-Stack Developer Professional Certificate"</a>.

---

## Application Overview
SkillSnap is a modern, full-stack portfolio and project tracker application. It enables users to create, manage, and showcase their professional profiles, skills, and projects. Built with .NET 8.0, Blazor WebAssembly, and ASP.NET Core Web API, SkillSnap is designed for maintainability, security, and a great user experience.

---

## Features
- User registration and login with secure JWT authentication
- Role-based access: Admin, User, and Anonymous sessions
- Create, edit, and delete portfolio profiles, projects, and skills
- Responsive, component-based UI with Blazor WebAssembly
- RESTful API with full CRUD support
- Data validation and error handling throughout
- Caching and performance monitoring on the backend

---

## Tech Stack
- **Frontend:** Blazor WebAssembly 8, Bootstrap 5, Razor Components
- **Backend:** ASP.NET Core 8 Web API, Entity Framework Core 8, SQLite
- **Authentication:** ASP.NET Identity, JWT tokens
- **Shared Models:** .NET 8 class library

---

## Getting Started

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download)
- Modern web browser (Chrome, Edge, Firefox)

### Running the Application
1. **Start the API**
   ```sh
   cd SkillSnap.Api
   dotnet run
   # API runs at http://localhost:5149
   ```
2. **Start the Client**
   ```sh
   cd SkillSnap.Client
   dotnet run
   # Client runs at http://localhost:5105
   ```
3. **Access the App**
   - Open http://localhost:5105 in your browser

### Default Admin Account
- **Email:** admin@skillsnap.com
- **Password:** Admin123!

---

## Project Structure
- `SkillSnap.Api/` - ASP.NET Core Web API (controllers, data, auth)
- `SkillSnap.Client/` - Blazor WebAssembly SPA (UI, services)
- `SkillSnap.Shared/` - Shared models and DTOs
- `.github/` - Copilot and AI agent instructions

---

## Developer Workflows
- **Migrations:**
  - Add: `dotnet ef migrations add <Name>`
  - Update: `dotnet ef database update`
- **Seeding:**
  - POST to `/api/seed` or use `DbSeeder.cs`
- **Testing:**
  - Manual via UI and API endpoints
- **Build:**
  - All projects must build and pass before commit

---

## Documentation
- [project.instructions.md](.github/instructions/project.instructions.md): Full technical documentation
- [user-manual.md](Docs/user-manual.md): Step-by-step user guide
- [architecture.md](Docs/architecture.md): Architecture and design
- [build-summary.md](Docs/build-summary.md): Phase-by-phase progress
- [copilot-instructions.md](.github/copilot-instructions.md): AI agent guidance

---

## License
This project is for educational and demonstration purposes. See your instructor or organization for licensing details.

---

## Credits
- Built with .NET 8, Blazor, and ASP.NET Core
- Bootstrap Icons by [Bootstrap](https://icons.getbootstrap.com/)

---

## Contact

For questions about this educational project, please refer to the course materials or contact the instructor at <a href="https://www.coursera.org/learn/full-stack-developer-capstone-project?specialization=microsoft-full-stack-developer" target="_blank">"Full-Stack Developer Capstone Project"</a>.

---

_Last updated: December 2025_
