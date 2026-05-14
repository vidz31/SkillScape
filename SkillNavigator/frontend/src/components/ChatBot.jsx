import React, { useState, useEffect, useRef } from 'react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card } from '@/components/ui/card';
import { ScrollArea } from '@/components/ui/scroll-area';
import { MessageCircle, X, Send, Loader2, Sparkles, Trash2 } from 'lucide-react';
import { useAuth } from '@/context/AuthContext';
import { api } from '@/services/api';
import { toast } from 'sonner';

const ChatBot = () => {
  const { user } = useAuth();
  const [isOpen, setIsOpen] = useState(false);
  const [messages, setMessages] = useState([]);
  const [input, setInput] = useState('');
  const [loading, setLoading] = useState(false);
  const scrollContainerRef = useRef(null);
  const messagesEndRef = useRef(null);

  // Scroll to bottom when new messages arrive
  const scrollToBottom = () => {
    setTimeout(() => {
      messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
    }, 100);
  };

  useEffect(() => {
    scrollToBottom();
  }, [messages, loading]);

  // Load conversation history on mount
  useEffect(() => {
    if (isOpen && user && messages.length === 0) {
      loadConversationHistory();
    }
  }, [isOpen, user]);

  const loadConversationHistory = async () => {
    try {
      const response = await api.get('/chatbot/history?limit=5');
      if (response.data && response.data.length > 0) {
        const formattedMessages = response.data.map(item => ({
          role: item.role,
          content: item.content,
          timestamp: new Date()
        }));
        setMessages(formattedMessages);
      } else {
        setInitialMessage();
      }
    } catch (error) {
      console.error('Error loading conversation history:', error);
      setInitialMessage();
    }
  };

  const setInitialMessage = () => {
    setMessages([{
      role: 'assistant',
      content: 'Welcome to Skill Navigator AI! 👋\n\nI\'m here to help you:\n✨ Discover careers that match your interests\n📚 Plan personalized learning paths\n🎯 Find required skills and resources\n🗺️ Create development roadmaps\n\nWhat would you like to explore today?',
      timestamp: new Date()
    }]);
  };

  const handleSendMessage = async (e) => {
    e.preventDefault();
    if (!input.trim() || loading) return;

    // Add user message to display
    const userMessage = {
      role: 'user',
      content: input,
      timestamp: new Date()
    };

    setMessages(prev => [...prev, userMessage]);
    setInput('');
    setLoading(true);

    try {
      // Filter conversation history to only include required properties
      const conversationHistoryForAPI = messages.map(msg => ({
        role: msg.role,
        content: msg.content
      }));

      const requestPayload = {
        message: input,
        conversationHistory: conversationHistoryForAPI
      };

      console.log('Sending request payload:', JSON.stringify(requestPayload, null, 2));
      
      const response = await api.post('/chatbot/message', requestPayload);

      console.log('Received response:', response.data);

      const chatResponse = response.data;

      // Create structured assistant message
      let assistantMessage = {
        role: 'assistant',
        content: chatResponse.message,
        timestamp: new Date(),
        careers: chatResponse.careers || [],
        skills: chatResponse.skills || [],
        roadmap: chatResponse.roadmap || null,
        confidenceScore: chatResponse.confidenceScore || 0
      };

      setMessages(prev => [...prev, assistantMessage]);
    } catch (error) {
      console.error('=== Chat Error Debug Info ===');
      console.error('Error detail:', error.response?.data || error.message);
      
      let errorMsg = 'I\'m having a temporary difficulty connecting to my AI brain. ';
      if (error.response?.status === 401) {
        errorMsg = 'Your session has expired. Please log in again to continue our conversation.';
      } else if (error.message === 'Network Error') {
        errorMsg = 'I can\'t reach the server. Please make sure the backend is running at http://localhost:5000.';
      } else {
        // Use the message from the backend if available, otherwise generic
        errorMsg = error.response?.data?.message || errorMsg;
      }
      
      const errorMessage = {
        role: 'assistant',
        content: errorMsg,
        timestamp: new Date()
      };
      setMessages(prev => [...prev, errorMessage]);
      toast.error('Chat error: ' + errorMsg);
    } finally {
      setLoading(false);
    }
  };

  const handleClearHistory = async () => {
    try {
      await api.delete('/chatbot/history');
      setInitialMessage();
      toast.success('Conversation cleared');
    } catch (error) {
      console.error('Error clearing history:', error);
      toast.error('Failed to clear conversation');
    }
  };

  const toggleOpen = () => {
    setIsOpen(!isOpen);
  };

  return (
    <div className="fixed bottom-6 right-6 z-50 flex flex-col items-end">
      {/* Chat Widget */}
      {isOpen && (
        <Card className="w-[420px] h-[calc(100vh-150px)] max-h-[600px] flex flex-col shadow-2xl border border-gray-300 bg-gradient-to-b from-white to-gray-50 rounded-xl overflow-hidden mb-4">
          {/* Header */}
          <div className="bg-gradient-to-r from-blue-600 via-blue-500 to-cyan-500 text-white px-6 py-4 flex items-center justify-between flex-shrink-0">
            <div className="flex items-center gap-3">
              <div className="bg-white/20 p-2 rounded-lg backdrop-blur-sm">
                <Sparkles size={20} className="text-white" />
              </div>
              <div>
                <h3 className="font-bold text-lg">Skill Navigator</h3>
                <p className="text-xs text-blue-100">AI Career Assistant</p>
              </div>
            </div>
            <div className="flex gap-1">
              <Button
                variant="ghost"
                size="sm"
                onClick={handleClearHistory}
                className="text-white hover:bg-white/20 h-8 w-8 p-0"
                title="Clear conversation"
              >
                <Trash2 size={16} />
              </Button>
              <Button
                variant="ghost"
                size="sm"
                onClick={toggleOpen}
                className="text-white hover:bg-white/20 h-8 w-8 p-0"
              >
                <X size={18} />
              </Button>
            </div>
          </div>

          {/* Messages Area */}
          <ScrollArea className="flex-1">
            <div className="space-y-4 px-4 py-4">
              {messages.map((msg, idx) => (
                <div
                  key={idx}
                  className={`flex ${msg.role === 'user' ? 'justify-end' : 'justify-start'}`}
                >
                  <div
                    className={`max-w-[85%] px-4 py-3 rounded-xl shadow-sm ${
                      msg.role === 'user'
                        ? 'bg-blue-600 text-white rounded-br-none'
                        : 'bg-gray-100 text-gray-900 rounded-bl-none border border-gray-200'
                    }`}
                  >
                    <p className="text-sm leading-relaxed whitespace-pre-wrap">{msg.content}</p>

                    {/* Display structured data if available */}
                    {msg.careers && msg.careers.length > 0 && (
                      <div className="mt-4 pt-3 border-t border-gray-300">
                        <p className="text-xs font-bold mb-2 text-gray-700">💼 Suggested Careers:</p>
                        <div className="space-y-2">
                          {msg.careers.map((career, i) => (
                            <div key={i} className="bg-white p-2 rounded-lg text-xs border border-gray-200">
                              <p className="font-semibold text-gray-900">{career.title}</p>
                              <div className="flex justify-between mt-1">
                                <span className="text-gray-600">Match: {(career.matchScore * 100).toFixed(0)}%</span>
                                {career.averageSalary && <span className="text-green-600 font-medium">{career.averageSalary}</span>}
                              </div>
                            </div>
                          ))}
                        </div>
                      </div>
                    )}

                    {/* Display skills if available */}
                    {msg.skills && msg.skills.length > 0 && (
                      <div className="mt-4 pt-3 border-t border-gray-300">
                        <p className="text-xs font-bold mb-2 text-gray-700">🎯 Required Skills:</p>
                        <div className="space-y-2">
                          {msg.skills.slice(0, 4).map((skill, i) => (
                            <div key={i} className="bg-white p-2 rounded-lg text-xs border border-gray-200">
                              <p className="font-semibold text-gray-900">{skill.name}</p>
                              <p className="text-gray-600 mt-1">⏱️ {skill.estimatedMonths} months</p>
                            </div>
                          ))}
                        </div>
                      </div>
                    )}

                    {/* Display roadmap if available */}
                    {msg.roadmap && (
                      <div className="mt-4 pt-3 border-t border-gray-300">
                        <p className="text-xs font-bold mb-2 text-gray-700">🗺️ Learning Roadmap:</p>
                        <div className="bg-white p-2 rounded-lg text-xs border border-gray-200">
                          <p className="font-semibold text-gray-900">{msg.roadmap.title}</p>
                          <p className="text-gray-600 mt-1">📅 {msg.roadmap.totalDurationMonths} months</p>
                          <p className="text-gray-600">📋 {msg.roadmap.phases?.length || 0} phases</p>
                        </div>
                      </div>
                    )}
                  </div>
                </div>
              ))}
              {loading && (
                <div className="flex justify-start">
                  <div className="bg-gray-100 text-gray-800 px-4 py-3 rounded-xl rounded-bl-none flex items-center gap-2">
                    <Loader2 size={16} className="animate-spin text-blue-600" />
                    <span className="text-sm">Thinking...</span>
                  </div>
                </div>
              )}
              <div ref={messagesEndRef} />
            </div>
          </ScrollArea>

          {/* Input Area */}
          <div className="border-t border-gray-200 bg-white px-4 py-3">
            <form onSubmit={handleSendMessage} className="flex gap-2">
              <Input
                value={input}
                onChange={(e) => setInput(e.target.value)}
                placeholder="Ask me about careers, skills, or learning paths..."
                className="text-sm flex-1 border-gray-300 placeholder:text-gray-400"
                disabled={loading}
              />
              <Button
                type="submit"
                size="sm"
                disabled={loading || !input.trim()}
                className="bg-blue-600 hover:bg-blue-700 text-white h-10 w-10 p-0 flex items-center justify-center"
              >
                <Send size={16} />
              </Button>
            </form>
            <p className="text-xs text-gray-400 mt-3 text-center flex items-center justify-center gap-1">
              <Sparkles size={10} className="text-blue-400" />
              Powered by Skill Navigator AI
            </p>
          </div>
        </Card>
      )}

      {/* Chat Button */}
      <button
        onClick={toggleOpen}
        className={`w-16 h-16 rounded-full shadow-lg flex items-center justify-center transition-all transform hover:scale-110 ${
          isOpen
            ? 'bg-gray-400 hover:bg-gray-500'
            : 'bg-gradient-to-br from-blue-600 to-blue-700 hover:from-blue-700 hover:to-blue-800 animate-bounce'
        } text-white font-semibold`}
        title={isOpen ? 'Close' : 'Open Skill Navigator AI'}
      >
        {isOpen ? <X size={24} /> : <MessageCircle size={24} />}
      </button>
    </div>
  );
};

export default ChatBot;
