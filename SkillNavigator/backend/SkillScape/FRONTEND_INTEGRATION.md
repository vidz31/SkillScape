# 🚀 Frontend Integration Guide

## Step-by-Step: Connect React Frontend to SkillScape Backend

---

## ✅ Prerequisites

✅ Backend is running on `https://localhost:5001`  
✅ React frontend is running on `http://localhost:5173`  
✅ Database is seeded with sample data  

---

## 🔧 Step 1: Update Frontend API Base URL

### File: `frontend/src/services/api/axios.js`

The base URL is already configured to use environment variable:

```javascript
const BASE_URL = import.meta.env.VITE_API_URL || '/api';
```

### Option A: Use Environment Variable (Recommended)

Create `.env.local` in frontend root:
```env
VITE_API_URL=https://localhost:5001/api
```

### Option B: Update Directly
```javascript
const BASE_URL = 'https://localhost:5001/api';
```

---

## 🔑 Step 2: Test Login Endpoint

### Using the API in your Auth Context

The `AuthContext.jsx` currently uses **mock authentication**. Let's update it to use real API:

### Current Mock Code (Lines ~85-105):
```javascript
const login = useCallback(async (credentials) => {
    dispatch({ type: 'AUTH_START' });
    try {
        const mockUser = {
            id: '1',
            email: credentials.email,
            name: credentials.email.split('@')[0],
            role: 'student',
            createdAt: new Date().toISOString(),
        };
        const mockToken = 'mock-jwt-token-' + Date.now();
        // ... rest of mock code
    }
}, []);
```

### Updated with Real API Call:
```javascript
const login = useCallback(async (credentials) => {
    dispatch({ type: 'AUTH_START' });
    try {
        // Call real API
        const response = await authApi.login({
            email: credentials.email,
            password: credentials.password
        });
        
        localStorage.setItem('token', response.token);
        localStorage.setItem('user', JSON.stringify({
            id: response.id,
            email: response.email,
            name: response.fullName,
            role: response.role.toLowerCase(),
            createdAt: new Date().toISOString(),
        }));
        
        setAuthToken(response.token);
        
        dispatch({ 
            type: 'AUTH_SUCCESS', 
            payload: { 
                user: { /* ... */ }, 
                token: response.token 
            } 
        });
    } catch (error) {
        dispatch({ type: 'AUTH_FAILURE' });
        throw error;
    }
}, []);
```

---

## 📚 Step 3: Update Dashboard to Use Real Data

### Current: Mock Data

```javascript
const recentSkills = [
    { id: '1', name: 'React Fundamentals', xp: 150, completedAt: '2 hours ago' },
    // ...
];
```

### Updated: Fetch from API

```javascript
import { useEffect, useState } from 'react';
import { useAuth } from '@/context/AuthContext';

const Dashboard = () => {
    const { user } = useAuth();
    const [stats, setStats] = useState(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);

    useEffect(() => {
        const fetchStats = async () => {
            try {
                const response = await apiClient.get('/progress/me');
                setStats(response.data.data);
            } catch (err) {
                setError(err.message);
            } finally {
                setLoading(false);
            }
        };

        if (user?.id) {
            fetchStats();
        }
    }, [user]);

    if (loading) return <div>Loading...</div>;
    if (error) return <div>Error: {error}</div>;

    return (
        <div>
            <h2>Level {stats.level}</h2>
            <p>Total XP: {stats.totalXP}</p>
            {/* Render actual stats */}
        </div>
    );
};
```

---

## 🎯 Step 4: Quiz Page Integration

### File: `frontend/src/pages/Quiz.jsx`

Currently uses mock questions. Update to fetch from API:

```javascript
import { useEffect, useState } from 'react';
import { apiClient } from '@/services/api/axios';

const Quiz = () => {
    const [questions, setQuestions] = useState([]);
    const [loading, setLoading] = useState(true);
    const [currentIndex, setCurrentIndex] = useState(0);
    const [selectedAnswers, setSelectedAnswers] = useState({});

    useEffect(() => {
        const fetchQuestions = async () => {
            try {
                const response = await apiClient.get('/quiz/questions');
                setQuestions(response.data.data);
            } catch (error) {
                console.error('Failed to load quiz', error);
            } finally {
                setLoading(false);
            }
        };

        fetchQuestions();
    }, []);

    const handleSubmitQuiz = async () => {
        try {
            // Convert selected answers to API format
            const responses = Object.entries(selectedAnswers).map(([qId, oId]) => ({
                questionId: qId,
                optionId: oId
            }));

            const result = await apiClient.post('/quiz/submit', { responses });
            
            // Handle result - show recommendation
            console.log('Recommended domain:', result.data.data.recommendedDomainName);
        } catch (error) {
            console.error('Failed to submit quiz', error);
        }
    };

    if (loading) return <div>Loading quiz...</div>;

    const question = questions[currentIndex];

    return (
        <div>
            <h2>{question?.text}</h2>
            {question?.options.map(option => (
                <button
                    key={option.id}
                    onClick={() => setSelectedAnswers({ ...selectedAnswers, [question.id]: option.id })}
                >
                    {option.text}
                </button>
            ))}
            {currentIndex < questions.length - 1 && (
                <button onClick={() => setCurrentIndex(currentIndex + 1)}>Next</button>
            )}
            {currentIndex === questions.length - 1 && (
                <button onClick={handleSubmitQuiz}>Submit Quiz</button>
            )}
        </div>
    );
};
```

---

## 🛣️ Step 5: Roadmap Page Integration

### File: `frontend/src/pages/Roadmap.jsx`

Update to fetch real domains and roadmap:

```javascript
useEffect(() => {
    const fetchRoadmap = async () => {
        try {
            const domainsResponse = await apiClient.get('/domains');
            setDomains(domainsResponse.data.data);

            // Fetch roadmap for first domain
            if (domainsResponse.data.data.length > 0) {
                const roadmapResponse = await apiClient.get(
                    `/domains/${domainsResponse.data.data[0].id}/roadmap`
                );
                setSelectedRoadmap(roadmapResponse.data.data);
            }
        } catch (error) {
            console.error('Failed to load roadmap', error);
        }
    };

    fetchRoadmap();
}, []);
```

---

## 👥 Step 6: Mentors Page Integration

### File: `frontend/src/pages/Mentors.jsx`

```javascript
import { useEffect, useState } from 'react';
import { apiClient } from '@/services/api/axios';
import { useAuth } from '@/context/AuthContext';

const Mentors = () => {
    const { user } = useAuth();
    const [mentors, setMentors] = useState([]);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        const fetchMentors = async () => {
            try {
                const response = await apiClient.get('/mentors');
                setMentors(response.data.data);
            } catch (error) {
                console.error('Failed to load mentors', error);
            } finally {
                setLoading(false);
            }
        };

        fetchMentors();
    }, []);

    const handleRequestMentor = async (mentorId, topic, message) => {
        try {
            const response = await apiClient.post('/mentors/request', {
                mentorId,
                topic,
                message
            });
            console.log('Request sent!', response.data.data);
        } catch (error) {
            console.error('Failed to request mentor', error);
        }
    };

    return (
        <div>
            {!loading && mentors.map(mentor => (
                <div key={mentor.id}>
                    <h3>{mentor.userName}</h3>
                    <p>Expertise: {mentor.expertise}</p>
                    <button onClick={() => handleRequestMentor(mentor.id, 'Topic', 'Message')}>
                        Request Session
                    </button>
                </div>
            ))}
        </div>
    );
};
```

---

## ✨ Step 7: Test Complete Flow

### Test Sequence:

1. **Test Login**
   - Go to Login page
   - Use email: `student@example.com` (password: anything)
   - Should receive JWT token
   - Redirect to Dashboard

2. **Test Dashboard**
   - Should show user level, XP, badges
   - Fetch from `/progress/me`

3. **Test Quiz**
   - Go to Quiz page
   - Answer questions
   - Submit → see recommendation
   - POST to `/quiz/submit`

4. **Test Roadmap**
   - Browse domains
   - See skills and learning paths
   - GET `/domains/{id}/roadmap`

5. **Test Mentors**
   - View all mentors
   - Request a session
   - POST `/mentors/request`

6. **Test Progress**
   - Complete a skill
   - POST `/progress/complete-skill`
   - Check badges update

---

## 🔐 Common Issues & Solutions

### **Issue: CORS Error**

**Error:** `Access to XMLHttpRequest blocked by CORS`

**Solution:** Backend already allows localhost:5173. Ensure:
1. Frontend is on `http://localhost:5173` (exact)
2. Backend CORS policy includes this origin
3. Check `Program.cs` CORS settings

### **Issue: 401 Unauthorized**

**Error:** `401 - Unauthorized`

**Solution:**
1. JWT token might be expired
2. Token not properly attached to headers
3. Check `Authorization` header in network tab
4. Should be: `Bearer {token}`

### **Issue: Database Empty**

**Error:** `No domains/skills found`

**Solution:**
```bash
# Re-run seeding:
dotnet ef database drop --project SkillScape.Infrastructure -f
dotnet ef database update --project SkillScape.Infrastructure
```

### **Issue: Port 5001 In Use**

**Solution:** Change in `launchSettings.json`:
```json
"applicationUrl": "https://localhost:5002"
```

---

## 📝 API Response Format

All endpoints return this format:

```json
{
    "success": true,
    "message": "Success message",
    "data": {
        // Actual response data
    }
}
```

Example login response:
```json
{
    "success": true,
    "message": "Login successful",
    "data": {
        "id": "user-id",
        "email": "student@example.com",
        "fullName": "Student Name",
        "role": "Student",
        "token": "eyJhbGciOiJIUzI1NiIs...",
        "expiresAt": "2026-02-23T12:30:00Z"
    }
}
```

---

## 🎵 Progress Tracking

As you integrate each feature:

- [ ] Update API base URL
- [ ] Test login with real API
- [ ] Update AuthContext to use real auth
- [ ] Integrate Dashboard (fetch stats)
- [ ] Integrate Quiz (fetch questions)
- [ ] Integrate Roadmap (fetch domains)
- [ ] Integrate Mentors (fetch list & request)
- [ ] Integrate Progress (complete skills)
- [ ] Test full flow end-to-end
- [ ] Handle errors gracefully
- [ ] Add loading states
- [ ] Test on production build

---

## 🚀 Deployment Checklist

Before going to production:

- [ ] Change API base URL to production URL
- [ ] Update JWT secret key
- [ ] Configure production database
- [ ] Enable HTTPS
- [ ] Update CORS origins
- [ ] Setup environment variables
- [ ] Configure error logging
- [ ] Setup monitoring
- [ ] Test all endpoints
- [ ] Performance optimization

---

## 📞 Need Help?

All API endpoints are documented in:
- Swagger: `https://localhost:5001/swagger`
- README.md in backend folder
- IMPLEMENTATION_COMPLETE.md
- API Controller comments

**You're all set! Integration should be straightforward! 🎉**
