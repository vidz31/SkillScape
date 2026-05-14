# 🚀 Quick Start - Groq ChatBot Testing Guide

## ⚡ 5-Minute Setup & Test

### Prerequisites
- [ ] .env file has both API keys
- [ ] Node.js installed (`npm --version`)
- [ ] .NET SDK installed (`dotnet --version`)

---

## Step 1: Start Backend (3 minutes)

```bash
# Navigate to backend
cd backend/SkillScape

# Restore dependencies (first time only)
dotnet restore

# Run the API
dotnet run --project SkillScape.API
```

**Expected Output:**
```
info: Program[0] Applying Entity Framework Core migrations...
info: SkillScape.API[0]
Now listening on: http://localhost:5000
info: ChatBot Service initialized with Groq (primary) and OpenAI (fallback)
```

✅ **Backend ready!** Keep this terminal open.

---

## Step 2: Start Frontend (2 minutes)

**In a new terminal:**

```bash
# Navigate to frontend
cd frontend

# Install dependencies (if not done)
npm install

# Start dev server
npm run dev
```

**Expected Output:**
```
  VITE v4.4.x
  ➜  Local:   http://localhost:5173/
  ➜  press h to show help
```

✅ **Frontend ready!** Open browser to http://localhost:5173

---

## Step 3: Login & Test ChatBot (0 minutes)

### Login
1. Go to http://localhost:5173
2. Click "Login" or "Sign In"
3. Use test credentials:
   - Email: `test@example.com`
   - Password: `Test@123`
   - (Or create new account)

### Find ChatBot Widget
- Look for **blue button** in **bottom-right corner**
- Shows message icon + "Chat with us"
- Click to expand

### Send Your First Message

**Test Query 1: Career Guidance**
```
Type: "I like technology and problem-solving. What career should I choose?"
Expected: 
- Instant response (within 300ms!)
- Career suggestions
- Match scores
- Skill requirements
```

**Test Query 2: Skill Learning**
```
Type: "What skills do I need to become a web developer?"
Expected:
- Skill list with timelines
- Learning resources
- Effort estimates
```

**Test Query 3: Roadmap**
```
Type: "Show me a 12-month roadmap for machine learning"
Expected:
- 4 phases over 12 months
- Topics per phase
- Project suggestions
- Milestone markers
```

**Test Query 4: General Question**
```
Type: "What's the job market like for cloud engineers?"
Expected:
- Career outlook
- Salary information
- Market insights
```

---

## 📊 Verification Checklist

### Response Speed
- [ ] Response appears in <1 second
- [ ] Feels instant (no noticeable wait)
- [ ] UI doesn't freeze

### Response Quality
- [ ] Message makes sense
- [ ] Addresses your question
- [ ] Professional tone
- [ ] Structured data shows correctly

### Groq in Action
Check backend terminal for:
```
"Attempting to get response from Groq (primary provider)"
"Successfully received response from Groq API"
```

If you see:
```
"Groq service failed, falling back to OpenAI"
```
→ Groq had an issue, fallback is working! ✅

### UI/UX
- [ ] Message input field works
- [ ] Send button responsive
- [ ] Skill level dropdown functional
- [ ] Clear history button works
- [ ] Chat scrolls properly
- [ ] Mobile responsive

---

## 🐛 Troubleshooting

### Issue: "Connection refused" on frontend
```
Solution:
1. Check backend is running (step 1)
2. Check backend is on port 5000
3. Check firewall allows localhost:5000
```

### Issue: "Unauthorized" error
```
Solution:
1. Clear browser cache
2. Login again
3. Check JWT token generation
```

### Issue: "API key not configured"
```
Solution:
1. Check .env file exists: backend/SkillScape/.env
2. Check GROQ_API_KEY line is there
3. Check key format (starts with gsk_)
4. Restart backend service
5. Check logs for actual error
```

### Issue: Slow responses (3+ seconds)
```
Likely: OpenAI fallback being used instead of Groq

Check:
1. Is "Groq service failed" in logs?
2. Is Groq API key valid?
3. Check Groq dashboard for rate limits
4. Check network connectivity
5. Restart backend service
```

### Issue: ChatBot icon not visible
```
Solution:
1. Make sure you're logged in
2. Navigate to authenticated page (Dashboard)
3. Hard refresh: Ctrl+Shift+R (Windows) or Cmd+Shift+R (Mac)
4. Check console for JavaScript errors
5. Check browser DevTools for CSS issues
```

---

## 📈 Performance Metrics to Note

### Expected Timings (Groq)
- **Time to Response**: 200-500ms
- **Response Display**: <100ms
- **Total UX Time**: <1s

### Groq vs OpenAI Speed
- Groq: 300ms
- OpenAI: 3000ms
- **Difference**: 10x faster with Groq!

### Tokens Generated
- Career guidance: 150-250 tokens
- Skill recommendations: 100-200 tokens
- Roadmap: 200-400 tokens
- General query: 100-300 tokens

---

## 🧪 Advanced Testing

### Test Conversation History
1. Send multiple messages
2. Scroll up to see previous messages
3. Click "Clear History"
4. Verify history is cleared

### Test Different Skill Levels
1. Select "Beginner" in dropdown
2. Ask: "What should I learn?"
3. Select "Intermediate"
4. Ask same question
5. Compare responses

### Test Intent Detection
- Say "career" → Career guidance
- Say "skill" → Skill recommendation
- Say "roadmap" → Learning path
- Say "general question" → General response

### Test with Long Queries
1. Type very long question
2. Verify it's processed correctly
3. Check response quality doesn't degrade
4. Monitor token usage

### Test Error Handling
1. Send empty message
2. Send special characters
3. Send very long message
4. Verify graceful error handling

---

## 📊 Monitoring During Test

### Backend Console
Watch for:
```
✅ Good signs:
- "Attempting to get response from Groq (primary provider)"
- "Successfully received response from Groq API"
- No error messages

⚠️ Warning signs:
- Repeated "Groq service failed" messages
- "Both Groq and OpenAI failed"
- Authentication errors

🔥 Critical signs:
- Crashes or exceptions
- Database connection errors
- Missing configuration
```

### Browser DevTools
1. Open: F12 (or right-click → Inspect)
2. Go to: Network tab
3. Send message
4. Check request:
   - URL: http://localhost:5000/api/chatbot/message
   - Status: 200 (success)
   - Response time: <1s
5. Check response body:
   - Has "message" field
   - Has "careers" or "skills" if applicable
   - Has "confidenceScore"

---

## ✅ Test Summary Checklist

After testing, verify:

- [ ] Backend starts without errors
- [ ] Frontend loads in browser
- [ ] Can login successfully
- [ ] ChatBot widget appears
- [ ] Can send message
- [ ] Get response in <1s
- [ ] Response is relevant
- [ ] Structured data displays
- [ ] Multiple queries work
- [ ] History persists
- [ ] Clear history works
- [ ] Logs show Groq is used
- [ ] No error messages
- [ ] UI is responsive
- [ ] Mobile looks good

---

## 🎯 What You're Testing

### Groq Integration
✅ API key configured correctly
✅ Service receives request
✅ Groq processes message
✅ Response comes back
✅ Timing is fast (<1s)

### Fallback Mechanism
✅ If Groq fails, OpenAI takes over
✅ User still gets response
✅ Logs show fallback occurred
✅ Service continues working

### Knowledge Base
✅ Career data loads
✅ Skill data loads
✅ Roadmap data loads
✅ System prompts work

### Frontend Integration
✅ Component renders
✅ Input/output works
✅ Styling is correct
✅ Responsive design works

### End-to-End Flow
✅ User → Frontend → Backend → Groq → Response → Frontend → User
✅ All steps work correctly
✅ No broken links in chain
✅ Performance is excellent

---

## 🚀 Common Test Queries

### Career Discovery
- "I'm interested in tech. What career fits me?"
- "I want a job that pays well. Suggestions?"
- "What's the job market for developers?"

### Skill Learning
- "What skills do I need for web dev?"
- "How long does it take to learn Python?"
- "What's the roadmap for cloud architecture?"

### General Questions
- "Tell me about the AI/ML industry"
- "What certifications matter?"
- "How do I transition to tech?"

### Edge Cases
- "..." (empty or minimal)
- "abcdef" (random text)
- Very long question (1000+ chars)
- Special characters

---

## ⏱️ Timing Expectations

```
User sends message at: 0ms
├─ Request travels to backend: +10ms (total: 10ms)
├─ Backend processes intent: +20ms (total: 30ms)
├─ Groq API processes: +250ms (total: 280ms)
├─ Response travels back: +10ms (total: 290ms)
└─ Frontend displays: +10ms (total: 300ms)

Total time: ~300ms
Perceived by user: "Instant!"
```

---

## 📞 Support

If something doesn't work:

1. **Check logs first**
   - Backend console output
   - Browser DevTools console
   - Network tab in DevTools

2. **Try these fixes**
   - Restart backend: Ctrl+C, then run again
   - Clear frontend cache: Ctrl+Shift+R
   - Re-login if auth error
   - Check .env file exists

3. **Verify configuration**
   - .env has GROQ_API_KEY
   - Key format is valid (starts with gsk_)
   - OpenAI key is also present
   - AI_PROVIDER=groq

4. **Check Groq Dashboard**
   - https://console.groq.com
   - Verify API key is active
   - Check usage and rate limits
   - Look for recent errors

---

## 🎉 Success Indicators

When everything works:

✅ Backend console shows:
```
"ChatBot Service initialized with Groq (primary) and OpenAI (fallback)"
"Attempting to get response from Groq (primary provider)"
"Successfully received response from Groq API"
```

✅ Frontend shows:
```
Message input field with skill level selector
Send button
Clear history option
Instant responses
```

✅ User experience:
```
Type question → Instant response
Read structured data
Continue conversation
Clear history when done
```

---

## 🎯 Next Steps After Testing

1. **If all works**: Celebrate! 🎉
   - Your Groq integration is successful
   - Fast, cost-efficient chatbot ready
   - Ready for production

2. **If you find issues**: Debug using the guide above
   - Check logs
   - Verify configuration
   - Check Groq dashboard

3. **Ready for production**:
   - Deploy to hosting
   - Configure environment variables
   - Monitor Groq usage
   - Optimize as needed

---

**Happy Testing!** 🚀

Your Skill Navigator ChatBot with Groq integration is ready to impress users with instant, intelligent career guidance!

---

For detailed information, see:
- `GROQ_IMPLEMENTATION_SUMMARY.md` - Complete implementation
- `GROQ_INTEGRATION_GUIDE.md` - Technical guide
- `WHY_GROQ_DECISION.md` - Why Groq was chosen
