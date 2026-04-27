# SkillScape Backend - ASP.NET Core Web API

## Solution Architecture

The solution follows **Clean Architecture** principles with 4 projects:

```
SkillScape/
├── SkillScape.API           (Web API entry point)
├── SkillScape.Application   (Business logic, DTOs, Interfaces)
├── SkillScape.Domain        (Core entities, enums)
└── SkillScape.Infrastructure (Database, EF Core, DbContext)
```

### Project Dependencies
```
API → Application → Domain
API → Infrastructure → Domain & Application
```

---

## 🚀 Getting Started

### Prerequisites
- **.NET 8.0 SDK** or later
- **Visual Studio 2022** (or VS Code)
- **SQL Server LocalDB** (included with Visual Studio)

### Setup Steps

#### 1. Open Solution
```bash
cd SkillScape
# Open SkillScape.sln in Visual Studio
```

#### 2. Restore NuGet Packages
```bash
dotnet restore
```

#### 3. Configure Database Connection
Edit `SkillScape.API/appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=SkillScapeDb;Trusted_Connection=true;"
}
```

#### 4. Create Database with Migrations
```bash
# Open Package Manager Console in Visual Studio
# Or use CLI:
dotnet ef database update --project SkillScape.Infrastructure --startup-project SkillScape.API
```

#### 5. Run the API
```bash
dotnet run --project SkillScape.API
```

The API will start on: `https://localhost:5001`

---

## 📚 Project Structure

### SkillScape.Domain
Contains **core entities** and **enums**:
- `Entities/`: User, CareerDomain, Skill, Quiz, Badge, Mentor, etc.
- `Enums/`: UserRole, DifficultyLevel, BadgeRarity, etc.

**Key Principle**: Zero dependencies on other projects

### SkillScape.Application
Contains **business logic interfaces** and **DTOs**:
- `DTOs/`: Data Transfer Objects for API requests/responses
- `Interfaces/`: IAuthService, IDomainService, IQuizService, etc.
- `Services/`: Service implementations (to be created)

### SkillScape.Infrastructure
Contains **database access** and **EF Core configuration**:
- `Data/ApplicationDbContext.cs`: Database configuration
- `Repositories/`: Repository implementations (to be created)

### SkillScape.API
Contains **controllers** and **HTTP endpoints**:
- `Controllers/`: REST API endpoints
- `Program.cs`: Dependency injection, middleware setup
- `appsettings.json`: Configuration

---

## 🔑 Key Features Implemented

### Database Design
✅ 14+ entities with proper relationships
✅ Foreign key constraints
✅ Unique indexes where needed
✅ Cascade delete policies

### Authentication (To Be Implemented)
- [ ] ASP.NET Identity user management
- [ ] JWT token generation
- [ ] Role-based authorization (Student, Mentor, Admin)

### Entities
- ✅ Users with roles and XP system
- ✅ Career Domains (Frontend, Backend, etc.)
- ✅ Skills with difficulty levels
- ✅ User progress tracking
- ✅ Quiz system with weighted scoring
- ✅ Badge/Achievement system
- ✅ Roadmap with learning paths
- ✅ Mentor hub with requests

---

## 📋 API Endpoints (To Be Created)

### Authentication
```
POST   /api/auth/register
POST   /api/auth/login
GET    /api/auth/me
PATCH  /api/auth/profile
POST   /api/auth/change-password
```

### Career Domains
```
GET    /api/domains
GET    /api/domains/{id}
GET    /api/domains/{id}/roadmap
```

### Quiz
```
GET    /api/quiz/questions
POST   /api/quiz/submit
GET    /api/quiz/result
```

### Progress
```
GET    /api/progress/me
POST   /api/progress/complete-skill
GET    /api/progress/badges
```

### Mentors
```
GET    /api/mentors
POST   /api/mentor/request
GET    /api/mentor/requests
```

---

## 🔧 Next Steps

1. **Implement Services** in Application layer
2. **Implement Repositories** in Infrastructure layer
3. **Create Controllers** in API layer
4. **Implement JWT Authentication**
5. **Seed sample data** into database
6. **Test with Swagger/Postman**
7. **Connect React frontend**

---

## 🧪 Testing the API

### Using Swagger (Built-in)
Once running, visit: `https://localhost:5001/swagger`

### Using Postman
1. Create new requests for each endpoint
2. Set Authorization header: `Bearer {your-jwt-token}`

### Using Visual Studio
Use the built-in API testing tools

---

## 📝 Notes

- **JWT Secret Key**: Change `JwtSettings.SecretKey` in production
- **CORS**: Currently allows `http://localhost:5173` (Vite) and `http://localhost:3000` (React)
- **Database**: Uses SQL Server LocalDB by default
- **Entity Framework**: Code-First migrations approach

---

## 🚨 Important: Before First Run

Ensure these packages are installed:
```bash
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package Swashbuckle.AspNetCore
```

---

## 📞 Troubleshooting

### Database connection failed?
- Check SQL Server is running
- Verify connection string in `appsettings.json`
- Ensure LocalDB instance exists

### Port already in use?
- Edit `launchSettings.json` to use different port

### Missing migrations?
```bash
dotnet ef migrations add InitialCreate --project SkillScape.Infrastructure
dotnet ef database update --project SkillScape.Infrastructure
```

---

**Let's build something amazing! 🚀**
