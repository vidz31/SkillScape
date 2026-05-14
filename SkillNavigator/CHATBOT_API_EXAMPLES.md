# ChatBot API Examples & Sample Responses

## 📡 API Request/Response Examples

### Base Information
- **Base URL**: `http://localhost:5000/api/chatbot`
- **Authentication**: Required (JWT Bearer Token)
- **Content-Type**: `application/json`

---

## 1️⃣ Career Guidance Query

### Request
```bash
curl -X POST http://localhost:5000/api/chatbot/message \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "message": "I am interested in technology and enjoy solving problems. Which career path should I follow?",
    "skillLevel": "Beginner",
    "interests": ["Technology", "Problem Solving", "Web Development"],
    "conversationHistory": []
  }'
```

### Response (200 OK)
```json
{
  "message": "Based on your interest in technology and problem-solving, I have some excellent career recommendations for you:\n\n**🎯 Web Developer**\nAs a Web Developer, you would build responsive, interactive websites and web applications. This role is perfect for someone who enjoys problem-solving and creative development. The field is growing rapidly, with a projected growth of 23% faster than average.\n\n- Average Salary: $80,000 - $120,000 per year\n- Job Outlook: Growing 23% faster than average\n- Key Responsibilities: Design user-facing features, optimize applications, debug website issues, collaborate with designers\n\n**🎯 Software Engineer**\nSoftware Engineers develop and maintain software systems. This role requires strong problem-solving skills and offers significant growth opportunities in various industries.\n\n- Average Salary: $90,000 - $140,000 per year\n- Job Outlook: Growing 15% faster than average\n\n**🎯 Data Scientist**\nData Scientists extract insights from data using statistical analysis and machine learning. This requires strong analytical and problem-solving abilities.\n\n- Average Salary: $100,000 - $150,000 per year\n- Job Outlook: Growing 36% faster than average\n\n**Next Steps:**\n1. Choose a specialty that interests you most\n2. Learn the fundamental skills (see skill recommendations)\n3. Build projects to develop practical experience\n4. Apply for entry-level positions or internships\n\nWould you like me to create a detailed learning roadmap for any of these careers?",
  "careers": [
    {
      "title": "Web Developer",
      "description": "Build responsive, interactive websites and web applications using modern frameworks and technologies.",
      "matchScore": 0.95,
      "keyResponsibilities": [
        "Design and develop user-facing features",
        "Optimize applications for speed and scalability",
        "Maintain and improve existing code",
        "Collaborate with UX/UI designers",
        "Implement responsive design",
        "Debug and fix website bugs"
      ],
      "averageSalary": "$80,000 - $120,000",
      "jobOutlook": "Growing 23% faster than average"
    },
    {
      "title": "Software Engineer",
      "description": "Develop and maintain software systems using various programming languages and frameworks.",
      "matchScore": 0.90,
      "keyResponsibilities": [
        "Design software architecture",
        "Write clean, efficient code",
        "Test and debug applications",
        "Collaborate with team members",
        "Document code and processes",
        "Participate in code reviews"
      ],
      "averageSalary": "$90,000 - $140,000",
      "jobOutlook": "Growing 15% faster than average"
    },
    {
      "title": "Data Scientist",
      "description": "Extract insights from data using statistical analysis, machine learning, and programming.",
      "matchScore": 0.85,
      "keyResponsibilities": [
        "Analyze complex datasets",
        "Build predictive models",
        "Create data visualizations",
        "Develop algorithms",
        "Communicate insights to stakeholders",
        "Deploy machine learning models"
      ],
      "averageSalary": "$100,000 - $150,000",
      "jobOutlook": "Growing 36% faster than average"
    }
  ],
  "skills": [
    {
      "name": "HTML & CSS",
      "description": "Master the fundamentals of web markup and styling.",
      "level": "Beginner",
      "estimatedMonths": 1,
      "resources": [
        "HTML & CSS Crash Course",
        "MDN Web Docs - HTML"
      ]
    },
    {
      "name": "JavaScript",
      "description": "Learn the programming language of the web.",
      "level": "Beginner",
      "estimatedMonths": 2,
      "resources": [
        "JavaScript Basics",
        "You Don't Know JS"
      ]
    },
    {
      "name": "React",
      "description": "Build interactive user interfaces with React.",
      "level": "Intermediate",
      "estimatedMonths": 2,
      "resources": [
        "React Official Documentation",
        "React - The Complete Guide"
      ]
    }
  ],
  "roadmap": null,
  "resources": [],
  "generatedAt": "2026-05-04T14:30:00Z",
  "confidenceScore": 0.92
}
```

---

## 2️⃣ Skill Learning Query

### Request
```bash
curl -X POST http://localhost:5000/api/chatbot/message \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "message": "I want to learn machine learning. What skills do I need and how long will it take?",
    "skillLevel": "Intermediate",
    "interests": ["Data Science", "Machine Learning"],
    "conversationHistory": [
      {
        "role": "user",
        "content": "I am interested in technology and enjoy solving problems. Which career path should I follow?"
      },
      {
        "role": "assistant",
        "content": "Based on your interest in technology and problem-solving..."
      }
    ]
  }'
```

### Response (200 OK)
```json
{
  "message": "Great choice! Machine Learning is an exciting and in-demand field. Since you're at an intermediate level, you already have a head start. Here's what you'll need to master:\n\n**📚 Foundation Skills (If not already covered):**\n- Python Programming (already covered if intermediate)\n- Statistics & Probability\n- Linear Algebra\n- Data Manipulation with Pandas\n\n**🎯 Core ML Skills:**\n1. **Supervised Learning** (2-3 months)\n   - Regression algorithms\n   - Classification algorithms\n   - Model evaluation metrics\n\n2. **Unsupervised Learning** (1-2 months)\n   - Clustering algorithms\n   - Dimensionality reduction\n   - Feature extraction\n\n3. **Feature Engineering** (1 month)\n   - Feature selection\n   - Feature scaling\n   - Handling missing data\n\n4. **Model Optimization** (1 month)\n   - Hyperparameter tuning\n   - Cross-validation\n   - Ensemble methods\n\n**🚀 Advanced Topics (3-6 months):**\n- Deep Learning (Neural Networks)\n- Natural Language Processing\n- Computer Vision\n- Reinforcement Learning\n\n**⏱️ Timeline:**\n- Minimum: 3-4 months (focused study, 20+ hours/week)\n- Comfortable: 6-8 months (15 hours/week)\n- Thorough: 10-12 months (building projects)\n\n**💻 Recommended Projects:**\n1. Iris Dataset Classification\n2. Housing Price Prediction\n3. Customer Segmentation\n4. Sentiment Analysis\n5. Image Classification\n\n**📖 Top Resources:**\n- Andrew Ng's Machine Learning Course (Coursera)\n- Scikit-learn Documentation\n- StatQuest with Josh Starmer (YouTube)\n- Hands-On Machine Learning book\n\nWould you like me to create a detailed 6-month roadmap for machine learning?",
  "careers": [
    {
      "title": "Machine Learning Engineer",
      "description": "Build and deploy machine learning solutions for real-world problems.",
      "matchScore": 0.88,
      "keyResponsibilities": [
        "Design ML algorithms",
        "Prepare and analyze data",
        "Train and evaluate models",
        "Deploy models to production",
        "Monitor model performance",
        "Optimize for performance and cost"
      ],
      "averageSalary": "$120,000 - $180,000",
      "jobOutlook": "Growing 46% faster than average"
    },
    {
      "title": "Data Scientist",
      "description": "Extract insights from data using statistical analysis and machine learning.",
      "matchScore": 0.82,
      "keyResponsibilities": [
        "Analyze complex datasets",
        "Build predictive models",
        "Create visualizations",
        "Develop algorithms",
        "Communicate findings",
        "Deploy ML models"
      ],
      "averageSalary": "$100,000 - $150,000",
      "jobOutlook": "Growing 36% faster than average"
    }
  ],
  "skills": [
    {
      "name": "Machine Learning",
      "description": "Master machine learning algorithms and frameworks.",
      "level": "Advanced",
      "estimatedMonths": 3,
      "resources": [
        "Machine Learning Specialization - Coursera",
        "Scikit-learn Documentation",
        "Fast.ai - Practical Deep Learning"
      ]
    },
    {
      "name": "Python",
      "description": "Advanced Python for data science.",
      "level": "Intermediate",
      "estimatedMonths": 1,
      "resources": [
        "Python for Data Analysis",
        "Real Python Tutorials"
      ]
    }
  ],
  "roadmap": null,
  "resources": [],
  "generatedAt": "2026-05-04T14:35:00Z",
  "confidenceScore": 0.89
}
```

---

## 3️⃣ Learning Roadmap Query

### Request
```bash
curl -X POST http://localhost:5000/api/chatbot/message \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "message": "Create a comprehensive learning roadmap for web development. I am a beginner and have 12 months to dedicate to learning.",
    "skillLevel": "Beginner",
    "interests": ["Web Development", "Frontend"],
    "conversationHistory": []
  }'
```

### Response (200 OK)
```json
{
  "message": "# 🗺️ Complete Web Development Roadmap (12 Months)\n\nExcellent! With 12 months of dedicated learning, you can become a competent web developer. Here's your structured path:\n\n## **Phase 1: Foundation (Months 1-3)**\n**Goal**: Master HTML, CSS, and JavaScript basics\n\n**Topics to Learn:**\n- HTML5 semantic structure\n- CSS3 layouts (Flexbox, Grid)\n- JavaScript fundamentals\n- DOM manipulation\n- ES6+ features\n- Git & version control\n\n**Projects:**\n1. Personal Portfolio Website (1-2 weeks)\n2. Responsive Landing Page (2-3 weeks)\n3. Interactive Calculator/To-Do App (2-3 weeks)\n\n**Time Commitment:** 20-25 hours/week\n**Milestones:**\n- ✓ Create your first website\n- ✓ Master CSS Flexbox and Grid\n- ✓ Understand JavaScript event handling\n- ✓ Complete 3 projects\n\n---\n\n## **Phase 2: Frontend Framework (Months 4-6)**\n**Goal**: Master React and modern frontend development\n\n**Topics to Learn:**\n- React fundamentals\n- Components and JSX\n- Hooks (useState, useEffect, etc.)\n- State management (Redux/Context API)\n- React Router\n- API integration\n- Testing (Jest, React Testing Library)\n\n**Projects:**\n1. Weather App with Live API Integration (2-3 weeks)\n2. Social Media Dashboard (3-4 weeks)\n3. E-commerce Product Catalog (3-4 weeks)\n\n**Time Commitment:** 20-25 hours/week\n**Milestones:**\n- ✓ Build 5+ reusable React components\n- ✓ Implement state management\n- ✓ Integrate real APIs\n- ✓ Write unit tests\n\n---\n\n## **Phase 3: Backend & Databases (Months 7-9)**\n**Goal**: Understand backend development and databases\n\n**Topics to Learn:**\n- Node.js fundamentals\n- Express.js framework\n- RESTful API design\n- SQL & relational databases (PostgreSQL)\n- MongoDB & NoSQL\n- Authentication & authorization\n- Database design\n\n**Projects:**\n1. Build a Simple REST API (2-3 weeks)\n2. Blog Platform with Authentication (3-4 weeks)\n3. Task Management App (Full Stack) (3-4 weeks)\n\n**Time Commitment:** 20-25 hours/week\n**Milestones:**\n- ✓ Design & build your first API\n- ✓ Implement user authentication\n- ✓ Master database operations\n- ✓ Connect frontend to backend\n\n---\n\n## **Phase 4: Full Stack & Deployment (Months 10-12)**\n**Goal**: Deploy production-ready applications\n\n**Topics to Learn:**\n- Full stack integration\n- Performance optimization\n- Security best practices\n- Deployment (Vercel, Heroku, AWS)\n- Docker & containerization basics\n- Continuous Integration/Deployment\n- Code quality & linting\n\n**Projects:**\n1. Complete Full Stack Project (6-8 weeks)\n2. Open Source Contributions (2-4 weeks)\n3. Deploy to Production (ongoing)\n\n**Time Commitment:** 20-25 hours/week\n**Milestones:**\n- ✓ Deploy first app to production\n- ✓ Optimize performance\n- ✓ Implement security measures\n- ✓ Complete an end-to-end project\n\n---\n\n## **Learning Resources**\n\n**Free Resources:**\n- freeCodeCamp (YouTube)\n- MDN Web Docs\n- JavaScript.info\n- W3Schools\n\n**Paid Courses:**\n- The Complete JavaScript Course 2024 (Udemy)\n- React - The Complete Guide (Udemy)\n- The Web Developer Bootcamp (Udemy)\n\n**Practice Platforms:**\n- LeetCode\n- HackerRank\n- Frontend Masters\n- Codewars\n\n## **Daily Schedule (Example)**\n- **Monday-Friday**: 3-4 hours (evenings/weekends)\n- **Saturday**: 4-5 hours\n- **Sunday**: 3-4 hours\n- **Total**: 20-25 hours/week\n\n## **Success Tips**\n1. Build projects simultaneously with learning\n2. Join developer communities (Discord, Reddit)\n3. Code every single day\n4. Review and refactor old code\n5. Follow best practices from day 1\n6. Contribute to open source\n7. Network with other developers\n\nYou've got this! 🚀",
  "careers": [],
  "skills": [],
  "roadmap": {
    "title": "Web Developer Roadmap",
    "description": "Complete learning path to become a professional web developer",
    "totalDurationMonths": 12,
    "phases": [
      {
        "name": "Foundation (Months 1-3)",
        "durationMonths": 3,
        "topics": [
          "HTML5 semantics and structure",
          "CSS3 layouts and responsive design",
          "JavaScript fundamentals",
          "DOM manipulation",
          "ES6+ features"
        ],
        "projects": [
          "Personal portfolio website",
          "Responsive landing page",
          "Interactive calculator"
        ],
        "milestones": [
          "Create your first website",
          "Master CSS Grid and Flexbox",
          "Understand JavaScript event handling"
        ]
      },
      {
        "name": "Frontend Framework (Months 4-6)",
        "durationMonths": 3,
        "topics": [
          "React basics and components",
          "State management (Redux/Context)",
          "React Router",
          "API integration",
          "Testing with Jest and React Testing Library"
        ],
        "projects": [
          "Build a todo application",
          "Create a weather app using APIs",
          "Build a social media dashboard"
        ],
        "milestones": [
          "Create reusable React components",
          "Manage application state effectively",
          "Integrate external APIs"
        ]
      },
      {
        "name": "Backend & Databases (Months 7-9)",
        "durationMonths": 3,
        "topics": [
          "Node.js and Express",
          "RESTful API design",
          "SQL and relational databases",
          "Authentication and authorization",
          "MongoDB and NoSQL basics"
        ],
        "projects": [
          "Build a RESTful API",
          "Create a blog platform",
          "Develop a task management app with authentication"
        ],
        "milestones": [
          "Design and build your first API",
          "Implement user authentication",
          "Master database operations"
        ]
      },
      {
        "name": "Full Stack Integration & Deployment (Months 10-12)",
        "durationMonths": 3,
        "topics": [
          "Full stack development integration",
          "Git and version control",
          "Deployment (Heroku, Vercel, AWS)",
          "Performance optimization",
          "Security best practices"
        ],
        "projects": [
          "Build a complete full-stack application",
          "Deploy your projects to production",
          "Contribute to open source"
        ],
        "milestones": [
          "Deploy your first application",
          "Achieve good code quality and performance",
          "Complete a production-ready project"
        ]
      }
    ]
  },
  "resources": [],
  "generatedAt": "2026-05-04T14:40:00Z",
  "confidenceScore": 0.95
}
```

---

## 4️⃣ Get Conversation History

### Request
```bash
curl -X GET "http://localhost:5000/api/chatbot/history?limit=5" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### Response (200 OK)
```json
[
  {
    "role": "user",
    "content": "What career should I choose?"
  },
  {
    "role": "assistant",
    "content": "Based on your interests..."
  },
  {
    "role": "user",
    "content": "Tell me more about web development"
  },
  {
    "role": "assistant",
    "content": "Web development is a great choice..."
  },
  {
    "role": "user",
    "content": "Create a learning roadmap"
  }
]
```

---

## 5️⃣ Clear Conversation History

### Request
```bash
curl -X DELETE http://localhost:5000/api/chatbot/history \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### Response (200 OK)
```json
{
  "message": "Conversation history cleared successfully"
}
```

---

## ❌ Error Responses

### 400 Bad Request
```json
{
  "message": "Message cannot be empty"
}
```

### 401 Unauthorized
```json
{
  "message": "User ID not found in token"
}
```

### 500 Internal Server Error
```json
{
  "message": "An error occurred while processing your request"
}
```

---

## 🧪 Testing with Postman

1. Import the following collection into Postman
2. Set the `{{bearer_token}}` variable with your JWT token
3. Run the requests

### Postman Collection
```json
{
  "info": {
    "name": "ChatBot API",
    "description": "Test ChatBot endpoints"
  },
  "item": [
    {
      "name": "Send Message",
      "request": {
        "method": "POST",
        "header": [
          {
            "key": "Authorization",
            "value": "Bearer {{bearer_token}}"
          },
          {
            "key": "Content-Type",
            "value": "application/json"
          }
        ],
        "body": {
          "mode": "raw",
          "raw": "{\n  \"message\": \"What career should I choose?\",\n  \"skillLevel\": \"Beginner\",\n  \"interests\": [\"Technology\"],\n  \"conversationHistory\": []\n}"
        },
        "url": {
          "raw": "http://localhost:5000/api/chatbot/message",
          "protocol": "http",
          "host": ["localhost"],
          "port": "5000",
          "path": ["api", "chatbot", "message"]
        }
      }
    },
    {
      "name": "Get History",
      "request": {
        "method": "GET",
        "header": [
          {
            "key": "Authorization",
            "value": "Bearer {{bearer_token}}"
          }
        ],
        "url": {
          "raw": "http://localhost:5000/api/chatbot/history?limit=10",
          "protocol": "http",
          "host": ["localhost"],
          "port": "5000",
          "path": ["api", "chatbot", "history"],
          "query": [{"key": "limit", "value": "10"}]
        }
      }
    },
    {
      "name": "Clear History",
      "request": {
        "method": "DELETE",
        "header": [
          {
            "key": "Authorization",
            "value": "Bearer {{bearer_token}}"
          }
        ],
        "url": {
          "raw": "http://localhost:5000/api/chatbot/history",
          "protocol": "http",
          "host": ["localhost"],
          "port": "5000",
          "path": ["api", "chatbot", "history"]
        }
      }
    }
  ]
}
```

---

## 📊 Response Field Documentation

### ChatResponse Object

| Field | Type | Description |
|-------|------|-------------|
| `message` | string | Main AI response text with markdown formatting |
| `careers` | CareerSuggestion[] | Array of relevant career suggestions |
| `skills` | SkillRequirement[] | Array of required skills |
| `roadmap` | Roadmap | Learning roadmap if applicable |
| `resources` | Resource[] | External resources and links |
| `generatedAt` | DateTime | Timestamp of response generation |
| `confidenceScore` | decimal | Confidence level (0.0 to 1.0) |

### CareerSuggestion Object

| Field | Type | Description |
|-------|------|-------------|
| `title` | string | Career job title |
| `description` | string | Career description |
| `matchScore` | decimal | Match score (0.0 to 1.0) |
| `keyResponsibilities` | string[] | Main job duties |
| `averageSalary` | string | Salary range |
| `jobOutlook` | string | Job market outlook |

### SkillRequirement Object

| Field | Type | Description |
|-------|------|-------------|
| `name` | string | Skill name |
| `description` | string | Skill description |
| `level` | string | Proficiency level |
| `estimatedMonths` | int | Time to learn |
| `resources` | string[] | Learning resources |

---

Enjoy exploring the ChatBot API! 🚀
