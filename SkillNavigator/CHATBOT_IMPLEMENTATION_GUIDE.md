# Skill Navigator AI ChatBot - Complete Implementation Guide

## 📋 Overview

The Skill Navigator AI ChatBot is a domain-specific intelligent assistant that helps users discover careers, plan learning paths, and develop professional skills. The system integrates OpenAI's GPT-4 API with a structured knowledge base to provide contextual, personalized career guidance.

### Key Features
- ✅ **Context-Aware Responses**: Domain-specific guidance for careers and skills
- ✅ **Structured Output**: Returns careers, skills, and roadmaps alongside natural language responses
- ✅ **Conversation Memory**: Maintains conversation history for personalized interactions
- ✅ **Knowledge Base Integration**: References curated career and skill data
- ✅ **User Personalization**: Considers skill level and interests for better recommendations
- ✅ **Real-time Chat Widget**: Interactive chat UI in bottom-right corner of the app

---

## 🏗️ Architecture Overview

### Backend Stack
- **Framework**: ASP.NET Core 8.0
- **Architecture**: Clean Architecture (API → Application → Domain → Infrastructure)
- **AI Integration**: OpenAI API (GPT-4o-mini)
- **Data**: JSON-based Knowledge Base (careers.json, skills.json, roadmaps.json)
- **Language**: C#

### Frontend Stack
- **Framework**: React with Vite
- **UI Library**: shadcn/ui + Radix UI
- **HTTP Client**: Axios
- **State Management**: React Hooks
- **Styling**: Tailwind CSS

### Directory Structure

```
Backend:
├── SkillScape.API/
│   ├── Controllers/ChatBotController.cs
│   └── appsettings.json
├── SkillScape.Application/
│   ├── DTOs/
│   │   ├── ChatRequest.cs
│   │   └── ChatResponse.cs
│   └── Interfaces/
│       └── IChatBotService.cs
└── SkillScape.Infrastructure/
    ├── Services/
    │   ├── ChatBotService.cs
    │   ├── OpenAIService.cs
    │   └── KnowledgeBaseService.cs
    └── KnowledgeBase/
        ├── careers.json
        ├── skills.json
        ├── roadmaps.json
        └── system-prompts.json

Frontend:
└── src/
    └── components/
        ├── ChatBot.jsx
        └── layout/
            └── DashboardLayout.jsx
```

---

## 🚀 Setup & Configuration

### Backend Setup

#### 1. Prerequisites
- .NET 8.0 SDK or later
- Visual Studio 2022 or VS Code
- OpenAI API Key

#### 2. Install Dependencies
```bash
cd backend/SkillScape
dotnet restore
```

#### 3. Configure OpenAI API
Edit `SkillScape.API/appsettings.json`:
```json
{
  "OpenAI": {
    "ApiKey": "your-openai-api-key-here"
  }
}
```

**To get your OpenAI API Key:**
1. Visit https://platform.openai.com/account/api-keys
2. Create a new secret key
3. Copy and paste it in appsettings.json

#### 4. Run Migrations
```bash
dotnet ef database update
```

#### 5. Start Backend
```bash
dotnet run --project SkillScape.API
```
Backend will run at: `http://localhost:5000`

### Frontend Setup

#### 1. Prerequisites
- Node.js 16+
- npm or bun

#### 2. Install Dependencies
```bash
cd frontend
npm install
# or
bun install
```

#### 3. Configure API URL
Ensure `vite.config.js` has correct API URL:
```javascript
export default defineConfig({
  define: {
    'import.meta.env.VITE_API_URL': JSON.stringify('http://localhost:5000/api')
  }
})
```

#### 4. Start Development Server
```bash
npm run dev
# or
bun run dev
```
Frontend will run at: `http://localhost:5173`

---

## 📚 API Endpoints

### Base URL
```
http://localhost:5000/api/chatbot
```

### Authentication
All endpoints require JWT authentication. Include the token in the Authorization header:
```
Authorization: Bearer <token>
```

### Endpoints

#### 1. Send Message to ChatBot
**POST** `/api/chatbot/message`

**Request:**
```json
{
  "message": "What career should I choose?",
  "skillLevel": "Beginner",
  "interests": ["Technology", "Web Development"],
  "conversationHistory": []
}
```

**Response:**
```json
{
  "message": "Based on your interest in web development...",
  "careers": [
    {
      "title": "Web Developer",
      "description": "Build responsive websites...",
      "matchScore": 0.95,
      "keyResponsibilities": [...],
      "averageSalary": "$80,000 - $120,000",
      "jobOutlook": "Growing 23% faster than average"
    }
  ],
  "skills": [
    {
      "name": "JavaScript",
      "description": "Learn JavaScript...",
      "level": "Intermediate",
      "estimatedMonths": 2,
      "resources": [...]
    }
  ],
  "roadmap": null,
  "resources": [],
  "generatedAt": "2026-05-04T10:30:00Z",
  "confidenceScore": 0.92
}
```

#### 2. Get Conversation History
**GET** `/api/chatbot/history?limit=10`

**Response:**
```json
[
  {
    "role": "user",
    "content": "What skills do I need for web development?"
  },
  {
    "role": "assistant",
    "content": "To become a web developer, you'll need..."
  }
]
```

#### 3. Clear Conversation History
**DELETE** `/api/chatbot/history`

**Response:**
```json
{
  "message": "Conversation history cleared successfully"
}
```

---

## 💡 Example Prompts & Expected Responses

### Example 1: Career Guidance

**User Input:**
```
"I'm interested in technology and enjoy problem-solving. Which career should I choose?"
```

**Expected Response:**
```
Based on your interest in technology and problem-solving, I recommend exploring these careers:

💼 **Web Developer**
- Match Score: 85%
- Average Salary: $80,000 - $120,000
- Job Outlook: Growing 23% faster than average
- Key Responsibilities: Design user interfaces, optimize performance, debug code

💼 **Software Engineer**
- Match Score: 90%
- Responsibilities include system design, algorithm development, and code optimization

🎯 **Next Steps:**
1. Learn the fundamentals of your chosen language (JavaScript, Python, or C#)
2. Build small projects to practice
3. Contribute to open-source projects
4. Start applying for entry-level positions

Would you like me to create a detailed roadmap for any of these careers?
```

### Example 2: Skill Recommendation

**User Input:**
```
"What skills are needed for data science?"
```

**Expected Response:**
```
To become a Data Scientist, you'll need the following core skills:

**Foundation Skills (Months 1-3):**
- Python Programming (1-2 months)
- Statistics & Mathematics (1-2 months)
- SQL & Databases (1 month)

**Intermediate Skills (Months 4-7):**
- Data Visualization (Matplotlib, Seaborn) - 1 month
- Machine Learning Fundamentals - 2 months
- Feature Engineering & Model Selection - 1 month

**Advanced Skills (Months 8-10):**
- Deep Learning (TensorFlow, PyTorch) - 2 months
- NLP & Computer Vision basics - 1 month

**Estimated Total Time:** 10-12 months with consistent daily practice

📚 **Recommended Resources:**
- Python: Codecademy Python Course
- Statistics: Khan Academy Statistics
- SQL: W3Schools SQL Tutorial
- ML: Coursera Machine Learning Specialization
```

### Example 3: Learning Roadmap

**User Input:**
```
"Create a roadmap for web development from beginner to professional"
```

**Expected Response:**
```
# Web Developer Learning Roadmap (12 months)

## Phase 1: Foundation (Months 1-3)
**Topics:** HTML5, CSS3, JavaScript Basics, DOM

**Projects:**
- Personal Portfolio Website
- Responsive Landing Page
- Interactive Calculator

**Milestones:**
- ✅ Create your first website
- ✅ Master CSS Grid and Flexbox
- ✅ Understand JavaScript event handling

---

## Phase 2: Frontend Framework (Months 4-6)
**Topics:** React, State Management, Routing, API Integration

**Projects:**
- Todo Application
- Weather App with API
- Social Media Dashboard

**Milestones:**
- ✅ Create reusable React components
- ✅ Manage state effectively
- ✅ Integrate third-party APIs

---

## Phase 3: Backend (Months 7-9)
**Topics:** Node.js, Express, SQL, Authentication

**Projects:**
- RESTful API
- Blog Platform with Auth
- Task Management App

---

## Phase 4: Deployment (Months 10-12)
**Topics:** Git, DevOps, Performance, Security

**Milestones:**
- ✅ Deploy to production
- ✅ Optimize performance
- ✅ Launch first portfolio project
```

---

## 🎯 Chatbot Intent Detection

The chatbot automatically detects user intent and routes to the appropriate handler:

### Intent Types

| Intent | Keywords | Handler | Example |
|--------|----------|---------|---------|
| `career_guidance` | career, job, profession, "which", "should" | HandleCareerGuidanceAsync | "Which career suits me?" |
| `skill_recommendation` | skill, learn, required, "what skills" | HandleSkillRecommendationAsync | "What skills do I need?" |
| `roadmap_request` | roadmap, plan, path, "create a plan" | HandleRoadmapRequestAsync | "Create a learning roadmap" |
| `general` | anything else | HandleGeneralQueryAsync | "Tell me about AI" |

---

## 🔧 System Prompts

The chatbot uses specialized system prompts to guide AI responses:

### Default System Prompt
```
You are an AI career assistant for the Skill Navigator platform. Your role is to provide 
structured, practical, and step-by-step guidance to help users discover careers, plan 
learning paths, and develop their professional skills.

Guidelines:
1. Be context-aware and domain-specific
2. Provide personalized recommendations
3. Always suggest specific, actionable steps
4. Be encouraging but realistic about timelines
5. Ask clarifying questions when needed
6. Format responses with clear sections
7. Include relevant resources
```

### Custom Prompts by Category
- **Career Guidance**: Focus on job market, responsibilities, salary
- **Skill Development**: Focus on learning paths, resources, timeline
- **Roadmap Planning**: Focus on phases, milestones, projects
- **Technical Skills**: Focus on industry trends, best practices, technologies

---

## 🧠 Knowledge Base Structure

### Careers Database (`careers.json`)
```json
{
  "id": "web-dev",
  "title": "Web Developer",
  "category": "Technology",
  "averageSalary": "$80,000 - $120,000",
  "jobOutlook": "Growing 23% faster",
  "keyResponsibilities": [...],
  "requiredSkillsIds": ["web-dev-skills"],
  "roadmapId": "web-dev-roadmap"
}
```

### Skills Database (`skills.json`)
```json
{
  "id": "javascript",
  "name": "JavaScript",
  "category": "Web Development",
  "level": "Beginner",
  "estimatedMonths": 2,
  "resources": [...]
}
```

### Roadmaps Database (`roadmaps.json`)
```json
{
  "id": "web-dev-roadmap",
  "title": "Web Developer Roadmap",
  "totalDurationMonths": 12,
  "phases": [
    {
      "name": "Foundation",
      "durationMonths": 3,
      "topics": [...],
      "projects": [...],
      "milestones": [...]
    }
  ]
}
```

---

## 🎨 Frontend Component Usage

### Basic Usage
The ChatBot widget is automatically included in the DashboardLayout:

```jsx
import ChatBot from '@/components/ChatBot';

// Already integrated in DashboardLayout
export const DashboardLayout = () => {
  return (
    <div>
      {/* Other components */}
      <ChatBot />
    </div>
  );
};
```

### ChatBot Component Props (if needed in future)
```jsx
<ChatBot 
  apiUrl="http://localhost:5000/api"
  initialMessage="Hello! How can I help you?"
  onMessageSent={(message) => console.log(message)}
/>
```

---

## 🔐 Security Considerations

### 1. API Key Management
- **Never** commit API keys to version control
- Use environment variables in production
- Rotate API keys regularly
- Monitor API usage for unusual patterns

### 2. Rate Limiting
Add rate limiting to protect API:
```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter(policyName: "fixed", configure: options =>
    {
        options.PermitLimit = 100;
        options.Window = TimeSpan.FromMinutes(1);
    });
});
```

### 3. Input Validation
- Validate message length (max 2000 characters)
- Sanitize user input
- Validate interests against known categories
- Check skill level against allowed values

### 4. Authentication
- All endpoints require JWT tokens
- Validate tokens before processing
- Store user ID from token claims
- Implement session management

---

## 📊 Monitoring & Analytics

### Key Metrics to Track
1. **Usage**: Message count, daily active users, average messages per session
2. **Performance**: API response time, model inference time, error rate
3. **Quality**: User satisfaction, task completion rate, clarification requests
4. **Costs**: API usage costs, token consumption, cost per session

### Sample Monitoring Code
```csharp
_logger.LogInformation("ChatBot message processed. " +
    "UserId: {UserId}, Intent: {Intent}, Confidence: {Confidence}, " +
    "Duration: {DurationMs}ms",
    userId, intent, confidenceScore, stopwatch.ElapsedMilliseconds);
```

---

## 🐛 Troubleshooting

### Common Issues

#### 1. "OpenAI API key is not configured"
**Solution**: Add API key to appsettings.json
```json
{
  "OpenAI": {
    "ApiKey": "sk-..."
  }
}
```

#### 2. CORS errors when calling API
**Solution**: Check CORS configuration in Program.cs
```csharp
policy.WithOrigins("http://localhost:5173")
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowCredentials();
```

#### 3. "User ID not found in token"
**Solution**: Ensure token contains 'sub' or nameidentifier claim

#### 4. Chatbot not appearing
**Solution**: 
- Clear browser cache
- Check if ChatBot.jsx is properly imported
- Verify z-index: 50 is not being overridden

#### 5. Slow API responses
**Solution**:
- Reduce conversation history limit from 10 to 5
- Check OpenAI API status
- Consider caching common queries

---

## 🚀 Performance Optimization

### 1. Conversation History Caching
```csharp
private Dictionary<string, List<ConversationContext>> _conversationHistory;
// Limit to last 10 messages to reduce token usage
var recentHistory = conversationHistory.TakeLast(10).ToList();
```

### 2. Knowledge Base Caching
```csharp
// Cache loaded JSON files to avoid repeated file I/O
private Dictionary<string, object>? _careersCache;
```

### 3. API Response Compression
```csharp
builder.Services.AddResponseCompression(options =>
{
    options.Providers.Add<GzipCompressionProvider>();
});
```

---

## 📚 Extended Features (Future Enhancements)

1. **Multi-language Support**: Translate responses to multiple languages
2. **User Preferences**: Save user interests and skill level
3. **Progress Tracking**: Track user progress through learning paths
4. **Integration with Quiz System**: Recommend quizzes based on conversation
5. **Integration with Mentorship**: Connect users with mentors based on career goals
6. **Analytics Dashboard**: Show chatbot usage statistics and insights
7. **Custom Training Data**: Fine-tune model with internal career data
8. **Offline Support**: Cache responses for offline access

---

## 📞 Support & Maintenance

### Regular Maintenance Tasks
- ✅ Update OpenAI API calls as new models release
- ✅ Review and update knowledge base quarterly
- ✅ Monitor API costs and usage
- ✅ Update system prompts based on user feedback
- ✅ Review and improve intent detection

### Update Knowledge Base
Edit JSON files in `SkillScape.Infrastructure/KnowledgeBase/`:
1. careers.json - Update job market data
2. skills.json - Add new technologies
3. roadmaps.json - Refine learning paths
4. system-prompts.json - Improve response quality

---

## 🎓 Learning Resources

- [OpenAI API Documentation](https://platform.openai.com/docs)
- [GPT-4 Best Practices](https://platform.openai.com/docs/guides/gpt-best-practices)
- [ASP.NET Core Documentation](https://learn.microsoft.com/en-us/aspnet/core/)
- [React Documentation](https://react.dev)
- [Prompt Engineering Guide](https://platform.openai.com/docs/guides/prompt-engineering)

---

## 📝 License & Credits

This chatbot implementation is part of the Skill Navigator platform. All career and skill data is curated for educational purposes.

**Built with:**
- OpenAI GPT-4 API
- ASP.NET Core
- React + Vite
- Tailwind CSS
