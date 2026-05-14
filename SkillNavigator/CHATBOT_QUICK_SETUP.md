# ChatBot Quick Setup Guide

## ⚡ 5-Minute Quick Start

### Step 1: Configure OpenAI API Key
1. Get your API key from https://platform.openai.com/account/api-keys
2. Update `backend/SkillScape/SkillScape.API/appsettings.json`:
```json
{
  "OpenAI": {
    "ApiKey": "sk-your-key-here"
  }
}
```

### Step 2: Start Backend
```bash
cd backend/SkillScape
dotnet run --project SkillScape.API
# Server runs at http://localhost:5000
```

### Step 3: Start Frontend
```bash
cd frontend
npm install
npm run dev
# App runs at http://localhost:5173
```

### Step 4: Test the ChatBot
1. Navigate to http://localhost:5173
2. Log in to your account
3. Look for the blue ChatBot button in bottom-right corner
4. Click to open and start chatting!

---

## 🧪 Testing the ChatBot

### Example Queries to Test

#### Career Guidance
- "I like problem-solving and technology. What career is best for me?"
- "Which career should I choose?"
- "What jobs are available in data science?"

#### Skill Learning
- "What skills do I need for web development?"
- "How long does it take to learn Python?"
- "What is JavaScript used for?"

#### Learning Roadmap
- "Create a learning roadmap for cloud architecture"
- "What's the path to becoming a data scientist?"
- "Plan my learning journey for DevOps"

#### General Questions
- "Tell me about the tech industry"
- "What are trending technologies in 2026?"

---

## 🔧 Backend Architecture

```
ChatBot Request Flow:
  User Input (React)
     ↓
  ChatBot API (POST /api/chatbot/message)
     ↓
  ChatBotService (Intent Detection)
     ↓
  ├─→ HandleCareerGuidanceAsync → KnowledgeBaseService
  ├─→ HandleSkillRecommendationAsync → KnowledgeBaseService
  ├─→ HandleRoadmapRequestAsync → KnowledgeBaseService
  └─→ HandleGeneralQueryAsync (passes through)
     ↓
  OpenAIService (API Call to GPT-4)
     ↓
  Structured ChatResponse (JSON)
     ↓
  Frontend Display (React)
```

---

## 📦 Key Files Overview

### Backend
| File | Purpose |
|------|---------|
| `ChatBotController.cs` | REST API endpoints |
| `ChatBotService.cs` | Main logic and intent routing |
| `OpenAIService.cs` | OpenAI API integration |
| `KnowledgeBaseService.cs` | Knowledge base queries |
| `ChatRequest.cs` | DTO for incoming messages |
| `ChatResponse.cs` | DTO for responses |
| `careers.json` | Career database |
| `skills.json` | Skills database |
| `roadmaps.json` | Learning roadmaps |

### Frontend
| File | Purpose |
|------|---------|
| `ChatBot.jsx` | Chat widget component |
| `DashboardLayout.jsx` | Layout with integrated ChatBot |

---

## 🐛 Debugging Tips

### 1. Check Backend is Running
```bash
# Terminal should show:
# Now listening on: http://localhost:5000
```

### 2. Verify API Connection
Open browser console and check:
- Network tab: POST requests to `/api/chatbot/message` should return 200
- No CORS errors in console

### 3. Check OpenAI API Key
```csharp
// Add this to Program.cs to debug
var apiKey = builder.Configuration["OpenAI:ApiKey"];
Console.WriteLine($"API Key configured: {!string.IsNullOrEmpty(apiKey)}");
```

### 4. View API Logs
Check backend console output for errors:
```
[Information] ChatBot message processed. UserId: xxx, Intent: career_guidance
[Error] Error calling OpenAI API: ...
```

### 5. Check Browser DevTools
- Network tab: Verify request/response bodies
- Console: Check for JavaScript errors
- Application: Check localStorage for auth token

---

## 📊 API Response Structure

All ChatBot responses follow this structure:

```json
{
  "message": "Main text response",
  "careers": [
    {
      "title": "Career Name",
      "matchScore": 0.85,
      "keyResponsibilities": ["..."],
      "averageSalary": "...",
      "jobOutlook": "..."
    }
  ],
  "skills": [
    {
      "name": "Skill Name",
      "level": "Intermediate",
      "estimatedMonths": 3,
      "resources": ["..."]
    }
  ],
  "roadmap": {
    "title": "Roadmap Title",
    "totalDurationMonths": 12,
    "phases": [...]
  },
  "confidenceScore": 0.92
}
```

---

## 🎯 Customization Examples

### Change System Prompt
Edit in `ChatBotService.cs`:
```csharp
private void LoadSystemPrompt()
{
    _systemPrompt = @"Your custom prompt here...";
}
```

### Add New Career to Database
Edit `careers.json`:
```json
{
  "id": "new-career",
  "title": "New Career Title",
  "description": "...",
  "category": "Technology",
  "averageSalary": "$...",
  "keyResponsibilities": ["..."],
  "requiredSkillsIds": ["skill-id"],
  "roadmapId": "roadmap-id"
}
```

### Customize UI Colors
Edit `ChatBot.jsx`:
```jsx
// Change header color from blue to purple
<div className="bg-gradient-to-r from-purple-600 to-purple-700">
```

---

## 🚀 Production Deployment

### Before Going Live

1. **Security**
   - [ ] Change OpenAI API key to production key
   - [ ] Update JWT secret key
   - [ ] Enable HTTPS
   - [ ] Configure CORS for production domain
   - [ ] Add rate limiting

2. **Performance**
   - [ ] Enable response compression
   - [ ] Cache static assets
   - [ ] Monitor API response times
   - [ ] Set up error tracking (Sentry, etc.)

3. **Monitoring**
   - [ ] Set up logging (Application Insights, ELK)
   - [ ] Monitor API quota usage
   - [ ] Track error rates
   - [ ] Monitor cost metrics

### Environment Variables
```bash
# Backend (.env or appsettings.Production.json)
OPENAI_API_KEY=sk-prod-key-here
JWT_SECRET_KEY=your-secret-key
DATABASE_CONNECTION=production-connection-string

# Frontend (.env.production)
VITE_API_URL=https://api.skillnavigator.com/api
```

---

## 💡 Tips & Tricks

1. **Improve Response Quality**
   - Update system prompts based on user feedback
   - Refine intent detection for edge cases
   - Add more examples to knowledge base

2. **Reduce API Costs**
   - Cache frequent queries
   - Limit conversation history to last 5 messages
   - Use cheaper models for simple queries

3. **Better Personalization**
   - Save user interests and skill level in database
   - Reference previous conversations
   - Tailor responses to user level

4. **Troubleshoot Intent Detection**
   - Add logging to DetermineIntent() method
   - Test with various phrasings
   - Update keyword matching logic

---

## 📞 Common Issues & Solutions

| Issue | Solution |
|-------|----------|
| API Key errors | Regenerate key at platform.openai.com |
| Slow responses | Check OpenAI API status, increase timeout |
| CORS errors | Update allowed origins in Program.cs |
| Chat not appearing | Clear cache, check z-index: 50 |
| Empty responses | Increase conversation history limit |
| 401 Unauthorized | Verify JWT token in request header |

---

## 🎓 Next Steps

1. **Read Full Documentation**: See `CHATBOT_IMPLEMENTATION_GUIDE.md`
2. **Customize Knowledge Base**: Update careers, skills, roadmaps
3. **Fine-tune Prompts**: Update system prompts for better responses
4. **Add Analytics**: Track usage and user satisfaction
5. **Integrate with User Profile**: Store preferences per user

---

## 📚 File Locations

```
Project Root/
├── backend/SkillScape/
│   ├── SkillScape.API/
│   │   ├── Controllers/ChatBotController.cs ⭐
│   │   └── appsettings.json ⭐
│   ├── SkillScape.Application/
│   │   ├── DTOs/ChatRequest.cs ⭐
│   │   ├── DTOs/ChatResponse.cs ⭐
│   │   └── Interfaces/IChatBotService.cs ⭐
│   └── SkillScape.Infrastructure/
│       ├── Services/ChatBotService.cs ⭐
│       ├── Services/OpenAIService.cs ⭐
│       ├── Services/KnowledgeBaseService.cs ⭐
│       └── KnowledgeBase/
│           ├── careers.json ⭐
│           ├── skills.json ⭐
│           ├── roadmaps.json ⭐
│           └── system-prompts.json
├── frontend/
│   └── src/components/
│       ├── ChatBot.jsx ⭐
│       └── layout/DashboardLayout.jsx ⭐
└── CHATBOT_IMPLEMENTATION_GUIDE.md ⭐
   ⭐ = Key files to understand the implementation
```

---

Good luck! Feel free to reach out if you have any questions. 🚀
