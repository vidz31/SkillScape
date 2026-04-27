import React, { useEffect, useMemo, useState } from 'react';
import { useLocation } from 'react-router-dom';
import { adminApi, domainsApi } from '@/services/api';
import { toast } from 'sonner';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Tabs, TabsContent } from '@/components/ui/tabs';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Badge } from '@/components/ui/badge';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import {
  Loader2,
  Users,
  UserCheck,
  CalendarClock,
  BarChart3,
  BellRing,
  ListChecks,
  Map,
  Star,
} from 'lucide-react';

const StatCard = ({ label, value, icon: Icon }) => (
  <Card>
    <CardContent className="p-5 flex items-center justify-between">
      <div>
        <p className="text-sm text-muted-foreground">{label}</p>
        <p className="text-2xl font-bold mt-1">{value ?? 0}</p>
      </div>
      <div className="h-10 w-10 rounded-full bg-secondary flex items-center justify-center">
        <Icon className="h-5 w-5" />
      </div>
    </CardContent>
  </Card>
);

const emptyMentorForm = {
  email: '',
  fullName: '',
  expertiseArea: '',
  yearsOfExperience: 1,
  sessionPrice: 0,
  currentCompany: '',
  bio: '',
  skills: '',
  linkedInUrl: '',
  availabilitySchedule: '',
};

const emptyQuizForm = {
  id: null,
  careerDomainId: '',
  text: '',
  category: '',
  displayOrder: 1,
  isActive: true,
  options: ['', ''],
};

const emptyRoadmapForm = {
  id: null,
  careerDomainId: '',
  skillId: '',
  title: '',
  description: '',
  stepNumber: 1,
  estimatedHours: 8,
  isActive: true,
  topics: [''],
};

const AdminPage = () => {
  const [loading, setLoading] = useState(true);

  const [stats, setStats] = useState(null);
  const [analytics, setAnalytics] = useState(null);
  const [users, setUsers] = useState([]);
  const [allMentors, setAllMentors] = useState([]);
  const [pendingMentors, setPendingMentors] = useState([]);
  const [sessions, setSessions] = useState([]);
  const [quizQuestions, setQuizQuestions] = useState([]);
  const [roadmapModules, setRoadmapModules] = useState([]);
  const [progressSnapshots, setProgressSnapshots] = useState([]);

  const [domains, setDomains] = useState([]);
  const [skillsByDomain, setSkillsByDomain] = useState({});

  const [announcement, setAnnouncement] = useState('');
  const [rejectReasons, setRejectReasons] = useState({});
  const [roadmapFilterDomain, setRoadmapFilterDomain] = useState('');
  const [roadmapSearchText, setRoadmapSearchText] = useState('');
  const [sessionStatusFilter, setSessionStatusFilter] = useState('');
  const [sessionStudentFilter, setSessionStudentFilter] = useState('');
  const [sessionMentorFilter, setSessionMentorFilter] = useState('');

  const [mentorForm, setMentorForm] = useState(emptyMentorForm);
  const [quizForm, setQuizForm] = useState(emptyQuizForm);
  const [roadmapForm, setRoadmapForm] = useState(emptyRoadmapForm);
  const location = useLocation();

  const activeSection = useMemo(() => {
    const parts = location.pathname.split('/').filter(Boolean);
    if (parts.length < 2) return 'dashboard';
    const section = parts[1];
    const validSections = ['dashboard', 'users', 'mentors', 'questions', 'roadmap', 'progress', 'sessions', 'analytics', 'announcement'];
    return validSections.includes(section) ? section : 'dashboard';
  }, [location.pathname]);

  const loadAdminData = async () => {
    try {
      setLoading(true);
      const [
        statsData,
        usersData,
        mentorsData,
        pendingData,
        sessionsData,
        analyticsData,
        questionsData,
        roadmapData,
        progressData,
      ] = await Promise.all([
        adminApi.getDashboard(),
        adminApi.getUsers(),
        adminApi.getMentors(),
        adminApi.getPendingMentors(),
        adminApi.getSessions(),
        adminApi.getAnalytics(),
        adminApi.getQuizQuestions(),
        adminApi.getRoadmapModules(),
        adminApi.getProgressSnapshot(),
      ]);

      setStats(statsData);
      setUsers(usersData || []);
      setAllMentors(mentorsData || []);
      setPendingMentors(pendingData || []);
      setSessions(sessionsData || []);
      setAnalytics(analyticsData);
      setQuizQuestions(questionsData || []);
      setRoadmapModules(roadmapData || []);
      setProgressSnapshots(progressData || []);
    } catch (error) {
      console.error('Admin data load error:', error);
      toast.error(error.message || 'Failed to load admin data');
    } finally {
      setLoading(false);
    }
  };

  const loadDomainData = async () => {
    try {
      const domainRes = await domainsApi.getAll();
      const domainList = domainRes?.data?.data || [];
      console.log('Loaded domains:', domainList);
      setDomains(domainList);

      const details = await Promise.all(domainList.map((domain) => domainsApi.getById(domain.id)));
      const map = {};
      details.forEach((detail) => {
        const domain = detail?.data?.data;
        if (domain?.id) map[domain.id] = domain.skills || [];
      });
      setSkillsByDomain(map);
    } catch (error) {
      console.error('Domain load error:', error);
      toast.error(error.message || 'Failed to load domains/skills');
    }
  };

  useEffect(() => {
    loadAdminData();
    loadDomainData();
  }, []);

  const roleCounts = useMemo(() => {
    return users.reduce(
      (acc, user) => {
        acc[user.role] = (acc[user.role] || 0) + 1;
        return acc;
      },
      { Student: 0, Mentor: 0, Admin: 0 }
    );
  }, [users]);

  const mentorSkills = useMemo(() => {
    return mentorForm.skills
      .split(',')
      .map((skill) => skill.trim())
      .filter(Boolean);
  }, [mentorForm.skills]);

  const roadmapSkillOptions = skillsByDomain[roadmapForm.careerDomainId] || [];

  // Get unique domains from roadmap modules
  const uniqueDomainsFromRoadmaps = useMemo(() => {
    const domainNames = new Set();
    roadmapModules.forEach((module) => {
      const domainName = module.careerDomainName || module.CareerDomainName;
      if (domainName) {
        domainNames.add(domainName);
      }
    });
    return Array.from(domainNames).sort();
  }, [roadmapModules]);

  // Filter roadmap modules
  const filteredRoadmapModules = useMemo(() => {
    return roadmapModules.filter((module) => {
      // Handle both camelCase and PascalCase field names from backend
      const domainName = module.careerDomainName || module.CareerDomainName || '';
      const skillName = module.skillName || module.SkillName || '';
      const title = module.title || module.Title || '';
      const description = module.description || module.Description || '';
      
      const matchesDomain = !roadmapFilterDomain || domainName === roadmapFilterDomain;
      const matchesSearch = !roadmapSearchText || 
        title.toLowerCase().includes(roadmapSearchText.toLowerCase()) ||
        description.toLowerCase().includes(roadmapSearchText.toLowerCase()) ||
        skillName.toLowerCase().includes(roadmapSearchText.toLowerCase()) ||
        domainName.toLowerCase().includes(roadmapSearchText.toLowerCase());
      return matchesDomain && matchesSearch;
    });
  }, [roadmapModules, roadmapFilterDomain, roadmapSearchText]);

  const updateRoadmapTopic = (index, value) => {
    setRoadmapForm((prev) => ({
      ...prev,
      topics: prev.topics.map((topic, topicIndex) => (topicIndex === index ? value : topic)),
    }));
  };

  const addRoadmapTopic = () => {
    setRoadmapForm((prev) => ({
      ...prev,
      topics: [...prev.topics, ''],
    }));
  };

  const removeRoadmapTopic = (index) => {
    setRoadmapForm((prev) => {
      const nextTopics = prev.topics.filter((_, topicIndex) => topicIndex !== index);
      return {
        ...prev,
        topics: nextTopics.length > 0 ? nextTopics : [''],
      };
    });
  };

  const resetQuizForm = () => setQuizForm(emptyQuizForm);
  const resetRoadmapForm = () => setRoadmapForm(emptyRoadmapForm);

  const toggleBlockUser = async (user) => {
    try {
      await adminApi.blockUser({
        userId: user.id,
        isBlocked: !user.isBlocked,
        reason: !user.isBlocked ? 'Blocked by admin action' : null,
      });
      toast.success(user.isBlocked ? 'User unblocked' : 'User blocked');
      await loadAdminData();
    } catch (error) {
      toast.error(error.message || 'Failed to update user');
    }
  };

  const approveMentor = async (mentorId) => {
    try {
      await adminApi.approveMentor(mentorId);
      toast.success('Mentor approved');
      await loadAdminData();
    } catch (error) {
      toast.error(error.message || 'Failed to approve mentor');
    }
  };

  const rejectMentor = async (mentorId) => {
    try {
      await adminApi.rejectMentor(mentorId, rejectReasons[mentorId] || 'Rejected by admin');
      toast.success('Mentor rejected');
      await loadAdminData();
    } catch (error) {
      toast.error(error.message || 'Failed to reject mentor');
    }
  };

  const createMentor = async () => {
    try {
      await adminApi.createMentor({
        ...mentorForm,
        yearsOfExperience: Number(mentorForm.yearsOfExperience),
        sessionPrice: Number(mentorForm.sessionPrice),
        skills: mentorSkills,
      });
      toast.success('Mentor created');
      setMentorForm(emptyMentorForm);
      await loadAdminData();
    } catch (error) {
      toast.error(error.message || 'Failed to create mentor');
    }
  };

  const toggleBlockMentor = async (mentor) => {
    try {
      await adminApi.blockMentor(mentor.id, {
        isBlocked: !mentor.isBlocked,
        reason: !mentor.isBlocked ? 'Blocked by admin' : null,
      });
      toast.success(mentor.isBlocked ? 'Mentor unblocked' : 'Mentor blocked');
      await loadAdminData();
    } catch (error) {
      toast.error(error.message || 'Failed to update mentor block status');
    }
  };

  const deleteMentor = async (mentorId) => {
    try {
      await adminApi.deleteMentor(mentorId);
      toast.success('Mentor removed');
      await loadAdminData();
    } catch (error) {
      toast.error(error.message || 'Failed to remove mentor');
    }
  };

  const submitQuizQuestion = async () => {
    if (!quizForm.careerDomainId || !quizForm.text.trim() || !quizForm.category.trim()) {
      toast.error('Career domain, question text, and category are required');
      return;
    }

    const cleanedOptions = quizForm.options.map((text) => text.trim()).filter(Boolean);
    if (cleanedOptions.length < 2) {
      toast.error('At least 2 options are required');
      return;
    }

    const payload = {
      careerDomainId: quizForm.careerDomainId,
      text: quizForm.text,
      category: quizForm.category,
      displayOrder: Number(quizForm.displayOrder),
      isActive: !!quizForm.isActive,
      options: cleanedOptions.map((text, index) => ({
        text,
        displayOrder: index + 1,
        domainWeights: {
          [quizForm.careerDomainId]: 1,
        },
      })),
    };

    try {
      if (quizForm.id) {
        await adminApi.updateQuizQuestion(quizForm.id, payload);
        toast.success('Quiz question updated');
      } else {
        await adminApi.createQuizQuestion(payload);
        toast.success('Quiz question created');
      }
      resetQuizForm();
      await loadAdminData();
    } catch (error) {
      toast.error(error.message || 'Failed to save quiz question');
    }
  };

  const editQuizQuestion = (question) => {
    setQuizForm({
      id: question.id,
      careerDomainId: question.careerDomainId,
      text: question.text,
      category: question.category,
      displayOrder: question.displayOrder,
      isActive: question.isActive,
      options: question.options?.map((option) => option.text) || ['', ''],
    });
  };

  const deleteQuizQuestion = async (questionId) => {
    try {
      await adminApi.deleteQuizQuestion(questionId);
      toast.success('Quiz question deleted');
      await loadAdminData();
    } catch (error) {
      toast.error(error.message || 'Failed to delete quiz question');
    }
  };

  const submitRoadmapModule = async () => {
    if (!roadmapForm.careerDomainId || !roadmapForm.skillId || !roadmapForm.title.trim()) {
      toast.error('Domain, skill, and title are required');
      return;
    }

    const topics = roadmapForm.topics
      .map((topic) => topic.trim())
      .filter(Boolean)
      .map((topic, index) => ({ title: topic, displayOrder: index + 1, resourceUrl: null }));

    const payload = {
      careerDomainId: roadmapForm.careerDomainId,
      skillId: roadmapForm.skillId,
      title: roadmapForm.title,
      description: roadmapForm.description,
      stepNumber: Number(roadmapForm.stepNumber),
      estimatedHours: Number(roadmapForm.estimatedHours),
      isActive: !!roadmapForm.isActive,
      topics,
    };

    try {
      if (roadmapForm.id) {
        await adminApi.updateRoadmapModule(roadmapForm.id, payload);
        toast.success('Roadmap module updated');
      } else {
        await adminApi.createRoadmapModule(payload);
        toast.success('Roadmap module created');
      }
      resetRoadmapForm();
      await loadAdminData();
    } catch (error) {
      toast.error(error.message || 'Failed to save roadmap module');
    }
  };

  const editRoadmapModule = (module) => {
    // Handle both camelCase and PascalCase field names from backend
    setRoadmapForm({
      id: module.id || module.Id,
      careerDomainId: module.careerDomainId || module.CareerDomainId,
      skillId: module.skillId || module.SkillId,
      title: module.title || module.Title || '',
      description: module.description || module.Description || '',
      stepNumber: module.stepNumber || module.StepNumber || 1,
      estimatedHours: module.estimatedHours || module.EstimatedHours || 8,
      isActive: module.isActive !== undefined ? module.isActive : (module.IsActive !== undefined ? module.IsActive : true),
      topics: (module.topics || module.Topics || []).map((topic) => topic.title || topic.Title || ''),
    });
  };

  const deleteRoadmapModule = async (moduleId) => {
    try {
      await adminApi.deleteRoadmapModule(moduleId);
      toast.success('Roadmap module deleted');
      await loadAdminData();
    } catch (error) {
      toast.error(error.message || 'Failed to delete roadmap module');
    }
  };

  const sendAnnouncement = async () => {
    if (!announcement.trim()) {
      toast.error('Announcement cannot be empty');
      return;
    }

    try {
      await adminApi.broadcastAnnouncement(announcement.trim());
      toast.success('Announcement sent');
      setAnnouncement('');
    } catch (error) {
      toast.error(error.message || 'Failed to send announcement');
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-[50vh]">
        <Loader2 className="h-8 w-8 animate-spin" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">Admin Panel Module</h1>
          <p className="text-sm text-muted-foreground">Manage users, mentors, quiz questions, roadmaps, and platform governance</p>
        </div>
        <Button onClick={loadAdminData} variant="outline">Refresh</Button>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <StatCard label="Total Users" value={stats?.totalUsers} icon={Users} />
        <StatCard label="Pending Mentor Approvals" value={stats?.pendingMentorApprovals} icon={UserCheck} />
        <StatCard label="Total Sessions" value={stats?.totalSessions} icon={CalendarClock} />
        <StatCard label="Avg Mentor Rating" value={stats?.averageMentorRating} icon={BarChart3} />
      </div>

      <Tabs value={activeSection} className="space-y-4">

        <TabsContent value="dashboard">
          <div className="space-y-6">
            {/* Quick Overview Cards */}
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
              <Card className="bg-gradient-to-br from-blue-50 to-blue-100 dark:from-blue-950 dark:to-blue-900 border-blue-200 dark:border-blue-800">
                <CardContent className="p-5">
                  <p className="text-sm text-blue-700 dark:text-blue-300">Total Revenue</p>
                  <p className="text-2xl font-bold mt-2">₹{stats?.totalRevenue || 0}</p>
                  <p className="text-xs text-blue-600 dark:text-blue-400 mt-1">From {stats?.completedSessions || 0} completed sessions</p>
                </CardContent>
              </Card>
              <Card className="bg-gradient-to-br from-green-50 to-green-100 dark:from-green-950 dark:to-green-900 border-green-200 dark:border-green-800">
                <CardContent className="p-5">
                  <p className="text-sm text-green-700 dark:text-green-300">Active Students</p>
                  <p className="text-2xl font-bold mt-2">{stats?.activeStudents || 0}</p>
                  <p className="text-xs text-green-600 dark:text-green-400 mt-1">Currently enrolled</p>
                </CardContent>
              </Card>
              <Card className="bg-gradient-to-br from-purple-50 to-purple-100 dark:from-purple-950 dark:to-purple-900 border-purple-200 dark:border-purple-800">
                <CardContent className="p-5">
                  <p className="text-sm text-purple-700 dark:text-purple-300">Total Mentors</p>
                  <p className="text-2xl font-bold mt-2">{stats?.totalMentors || 0}</p>
                  <p className="text-xs text-purple-600 dark:text-purple-400 mt-1">{stats?.pendingMentorApprovals || 0} pending approval</p>
                </CardContent>
              </Card>
              <Card className="bg-gradient-to-br from-orange-50 to-orange-100 dark:from-orange-950 dark:to-orange-900 border-orange-200 dark:border-orange-800">
                <CardContent className="p-5">
                  <p className="text-sm text-orange-700 dark:text-orange-300">Platform Rating</p>
                  <p className="text-2xl font-bold mt-2">{stats?.averageMentorRating?.toFixed(1) || '0.0'} ⭐</p>
                  <p className="text-xs text-orange-600 dark:text-orange-400 mt-1">Average mentor rating</p>
                </CardContent>
              </Card>
            </div>

            {/* Sessions Overview */}
            <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
              <Card className="lg:col-span-1">
                <CardHeader>
                  <CardTitle className="flex items-center gap-2">
                    <CalendarClock className="h-5 w-5" />
                    Session Status Overview
                  </CardTitle>
                </CardHeader>
                <CardContent className="space-y-4">
                  {['Approved', 'Completed', 'Cancelled'].map((status) => {
                    const count = sessions.filter((session) => session.status === status).length;
                    const total = sessions.length || 1;
                    const widthPercent = Math.round((count / total) * 100);
                    const colors = {
                      Approved: 'bg-blue-500',
                      Completed: 'bg-green-500',
                      Cancelled: 'bg-red-500'
                    };
                    return (
                      <div key={status}>
                        <div className="flex justify-between text-sm mb-1">
                          <span className="font-medium">{status}</span>
                          <span className="text-muted-foreground">{count} ({widthPercent}%)</span>
                        </div>
                        <div className="h-3 w-full rounded-full bg-secondary">
                          <div className={`h-3 rounded-full ${colors[status]}`} style={{ width: `${widthPercent}%` }} />
                        </div>
                      </div>
                    );
                  })}
                  <div className="pt-3 border-t">
                    <p className="text-sm text-muted-foreground">Total Sessions</p>
                    <p className="text-3xl font-bold">{sessions.length}</p>
                  </div>
                </CardContent>
              </Card>

              <Card className="lg:col-span-2">
                <CardHeader>
                  <CardTitle className="flex items-center gap-2">
                    <ListChecks className="h-5 w-5" />
                    Recent Sessions
                  </CardTitle>
                </CardHeader>
                <CardContent>
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead>Student</TableHead>
                        <TableHead>Mentor</TableHead>
                        <TableHead>Date</TableHead>
                        <TableHead>Status</TableHead>
                        <TableHead>Price</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {sessions.slice(0, 3).map((session) => (
                        <TableRow key={session.id}>
                          <TableCell className="font-medium">{session.studentName}</TableCell>
                          <TableCell>{session.mentorName}</TableCell>
                          <TableCell className="text-sm">
                            {new Date(session.scheduledDateTime).toLocaleDateString('en-US', { 
                              month: 'short', 
                              day: 'numeric',
                              hour: '2-digit',
                              minute: '2-digit'
                            })}
                          </TableCell>
                          <TableCell>
                            <Badge variant={
                              session.status === 'Completed' ? 'secondary' : 
                              session.status === 'Approved' ? 'default' : 
                              session.status === 'Pending' ? 'outline' : 
                              'destructive'
                            }>
                              {session.status}
                            </Badge>
                          </TableCell>
                          <TableCell className="font-semibold">₹{session.sessionPrice}</TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                  {sessions.length === 0 && (
                    <p className="text-sm text-muted-foreground text-center py-6">No sessions found</p>
                  )}
                  {sessions.length > 3 && (
                    <p className="text-xs text-muted-foreground text-center mt-3">
                      Showing 3 of {sessions.length} sessions. View all in Sessions tab.
                    </p>
                  )}
                </CardContent>
              </Card>
            </div>

            {/* Analytics Overview */}
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
              <Card>
                <CardHeader>
                  <CardTitle className="flex items-center gap-2">
                    <BarChart3 className="h-5 w-5" />
                    Platform Analytics
                  </CardTitle>
                </CardHeader>
                <CardContent className="space-y-4">
                  <div className="grid grid-cols-2 gap-4">
                    <div className="p-4 rounded-lg bg-secondary/50">
                      <p className="text-xs text-muted-foreground">Most Popular Path</p>
                      <p className="text-lg font-semibold mt-1">{analytics?.mostSelectedCareerPath || 'N/A'}</p>
                    </div>
                    <div className="p-4 rounded-lg bg-secondary/50">
                      <p className="text-xs text-muted-foreground">Avg Quiz Score</p>
                      <p className="text-lg font-semibold mt-1">{analytics?.averageQuizScore?.toFixed(1) || 0}%</p>
                    </div>
                    <div className="p-4 rounded-lg bg-secondary/50">
                      <p className="text-xs text-muted-foreground">Daily Signups</p>
                      <p className="text-lg font-semibold mt-1">{analytics?.dailySignups || 0}</p>
                    </div>
                    <div className="p-4 rounded-lg bg-secondary/50">
                      <p className="text-xs text-muted-foreground">Active Users (30d)</p>
                      <p className="text-lg font-semibold mt-1">{analytics?.activeUsersLast30Days || 0}</p>
                    </div>
                  </div>
                  <div className="pt-3 border-t">
                    <p className="text-sm text-muted-foreground mb-2">Weakest Quiz Category</p>
                    <Badge variant="outline" className="text-sm">{analytics?.weakestQuizCategory || 'N/A'}</Badge>
                    <p className="text-xs text-muted-foreground mt-2">Focus on improving content in this area</p>
                  </div>
                </CardContent>
              </Card>

              <Card>
                <CardHeader>
                  <CardTitle className="flex items-center gap-2">
                    <Star className="h-5 w-5" />
                    Top Performing Mentors
                  </CardTitle>
                </CardHeader>
                <CardContent className="space-y-3">
                  {(analytics?.topMentors || []).slice(0, 5).map((mentor, index) => (
                    <div key={mentor.mentorId} className="border rounded-lg p-4 space-y-2">
                      <div className="flex items-center justify-between">
                        <div className="flex items-center gap-2">
                          <div className="flex h-8 w-8 items-center justify-center rounded-full bg-accent text-accent-foreground font-bold text-sm">
                            #{index + 1}
                          </div>
                          <div>
                            <p className="font-semibold">{mentor.mentorName}</p>
                            <p className="text-xs text-muted-foreground">{mentor.completedSessions} sessions handled</p>
                          </div>
                        </div>
                        <div className="text-right">
                          <p className="text-sm font-semibold">⭐ {mentor.averageRating.toFixed(1)}</p>
                          <p className="text-xs text-muted-foreground">₹{mentor.revenue}</p>
                        </div>
                      </div>
                      <div className="h-2 w-full rounded-full bg-secondary">
                        <div 
                          className="h-2 rounded-full bg-gradient-to-r from-yellow-400 to-orange-500" 
                          style={{ width: `${Math.min(100, Math.round((mentor.averageRating / 5) * 100))}%` }} 
                        />
                      </div>
                    </div>
                  ))}
                  {(!analytics?.topMentors || analytics.topMentors.length === 0) && (
                    <p className="text-sm text-muted-foreground text-center py-6">No mentor performance data available</p>
                  )}
                </CardContent>
              </Card>
            </div>
          </div>
        </TabsContent>

        <TabsContent value="users">
          <Card>
            <CardHeader>
              <CardTitle>User Management</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              <div className="flex gap-2 text-sm">
                <Badge variant="outline">Students: {roleCounts.Student || 0}</Badge>
                <Badge variant="outline">Mentors: {roleCounts.Mentor || 0}</Badge>
                <Badge variant="outline">Admins: {roleCounts.Admin || 0}</Badge>
              </div>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Name</TableHead>
                    <TableHead>Email</TableHead>
                    <TableHead>Role</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead className="text-right">Action</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {users.map((user) => (
                    <TableRow key={user.id}>
                      <TableCell>{user.fullName}</TableCell>
                      <TableCell>{user.email}</TableCell>
                      <TableCell>{user.role}</TableCell>
                      <TableCell>
                        <Badge variant={user.isBlocked ? 'destructive' : 'secondary'}>{user.isBlocked ? 'Blocked' : 'Active'}</Badge>
                      </TableCell>
                      <TableCell className="text-right">
                        <Button size="sm" variant="outline" onClick={() => toggleBlockUser(user)}>
                          {user.isBlocked ? 'Unblock' : 'Block'}
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="mentors">
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
            <Card className="lg:col-span-2">
              <CardHeader><CardTitle>Mentor CRUD & Blocking</CardTitle></CardHeader>
              <CardContent>
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Name</TableHead>
                      <TableHead>Email</TableHead>
                      <TableHead>Expertise</TableHead>
                      <TableHead>Status</TableHead>
                      <TableHead>Approval</TableHead>
                      <TableHead className="text-right">Actions</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {allMentors.map((mentor) => (
                      <TableRow key={mentor.id}>
                        <TableCell>{mentor.userName}</TableCell>
                        <TableCell>{mentor.email}</TableCell>
                        <TableCell>{mentor.expertiseArea}</TableCell>
                        <TableCell><Badge variant={mentor.isBlocked ? 'destructive' : 'secondary'}>{mentor.isBlocked ? 'Blocked' : 'Active'}</Badge></TableCell>
                        <TableCell><Badge variant={mentor.isApproved ? 'secondary' : 'outline'}>{mentor.isApproved ? 'Approved' : 'Pending'}</Badge></TableCell>
                        <TableCell className="text-right">
                          <Button size="sm" variant="outline" onClick={() => toggleBlockMentor(mentor)}>
                            {mentor.isBlocked ? 'Unblock' : 'Block'}
                          </Button>
                          <Button size="sm" variant="destructive" className="ml-2" onClick={() => deleteMentor(mentor.id)}>
                            Remove
                          </Button>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </CardContent>
            </Card>
            <Card>
              <CardHeader><CardTitle>Add Mentor</CardTitle></CardHeader>
              <CardContent className="space-y-2">
                <Input placeholder="Email" value={mentorForm.email} onChange={(e) => setMentorForm((p) => ({ ...p, email: e.target.value }))} />
                <Input placeholder="Full Name" value={mentorForm.fullName} onChange={(e) => setMentorForm((p) => ({ ...p, fullName: e.target.value }))} />
                <Input placeholder="Expertise Area" value={mentorForm.expertiseArea} onChange={(e) => setMentorForm((p) => ({ ...p, expertiseArea: e.target.value }))} />
                <Input type="number" placeholder="Years of Experience" value={mentorForm.yearsOfExperience} onChange={(e) => setMentorForm((p) => ({ ...p, yearsOfExperience: e.target.value }))} />
                <Input type="number" placeholder="Session Price" value={mentorForm.sessionPrice} onChange={(e) => setMentorForm((p) => ({ ...p, sessionPrice: e.target.value }))} />
                <Input placeholder="Skills (comma separated)" value={mentorForm.skills} onChange={(e) => setMentorForm((p) => ({ ...p, skills: e.target.value }))} />
                <Textarea placeholder="Bio" value={mentorForm.bio} onChange={(e) => setMentorForm((p) => ({ ...p, bio: e.target.value }))} />
                <Button className="w-full" onClick={createMentor}>Create Mentor</Button>
              </CardContent>
            </Card>
          </div>

          <Card className="mt-4">
            <CardHeader><CardTitle>Pending Mentor Approvals</CardTitle></CardHeader>
            <CardContent className="space-y-4">
              {pendingMentors.length === 0 && <p className="text-sm text-muted-foreground">No pending mentors.</p>}
              {pendingMentors.map((mentor) => (
                <div key={mentor.id} className="border rounded-lg p-4 space-y-3">
                  <div className="flex items-start justify-between gap-4">
                    <div>
                      <p className="font-semibold">{mentor.userName}</p>
                      <p className="text-sm text-muted-foreground">{mentor.expertiseArea || mentor.expertise}</p>
                    </div>
                    <Badge variant="outline">Pending</Badge>
                  </div>
                  <Input
                    placeholder="Rejection reason"
                    value={rejectReasons[mentor.id] || ''}
                    onChange={(e) => setRejectReasons((prev) => ({ ...prev, [mentor.id]: e.target.value }))}
                  />
                  <div className="flex gap-2">
                    <Button size="sm" onClick={() => approveMentor(mentor.id)}>Approve</Button>
                    <Button size="sm" variant="destructive" onClick={() => rejectMentor(mentor.id)}>Reject</Button>
                  </div>
                </div>
              ))}
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="questions">
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
            <Card>
              <CardHeader><CardTitle className="flex items-center gap-2"><ListChecks className="h-5 w-5" /> Quiz Question CRUD</CardTitle></CardHeader>
              <CardContent className="space-y-2">
                <select className="w-full rounded-md border px-3 py-2 text-sm bg-background" value={quizForm.careerDomainId} onChange={(e) => setQuizForm((p) => ({ ...p, careerDomainId: e.target.value }))}>
                  <option value="">Select Domain</option>
                  {domains.map((domain) => <option key={domain.id} value={domain.id}>{domain.name}</option>)}
                </select>
                <Input placeholder="Category" value={quizForm.category} onChange={(e) => setQuizForm((p) => ({ ...p, category: e.target.value }))} />
                <Input placeholder="Question text" value={quizForm.text} onChange={(e) => setQuizForm((p) => ({ ...p, text: e.target.value }))} />
                <Input type="number" placeholder="Display order" value={quizForm.displayOrder} onChange={(e) => setQuizForm((p) => ({ ...p, displayOrder: e.target.value }))} />
                {quizForm.options.map((option, index) => (
                  <Input key={index} placeholder={`Option ${index + 1}`} value={option} onChange={(e) => setQuizForm((p) => ({ ...p, options: p.options.map((item, idx) => idx === index ? e.target.value : item) }))} />
                ))}
                <div className="flex gap-2">
                  <Button variant="outline" onClick={() => setQuizForm((p) => ({ ...p, options: [...p.options, ''] }))}>+ Option</Button>
                  <Button className="flex-1" onClick={submitQuizQuestion}>{quizForm.id ? 'Update' : 'Create'}</Button>
                  {quizForm.id && <Button variant="ghost" onClick={resetQuizForm}>Clear</Button>}
                </div>
              </CardContent>
            </Card>

            <Card className="lg:col-span-2">
              <CardHeader><CardTitle>All Quiz Questions</CardTitle></CardHeader>
              <CardContent>
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Question</TableHead>
                      <TableHead>Domain</TableHead>
                      <TableHead>Category</TableHead>
                      <TableHead>Options</TableHead>
                      <TableHead className="text-right">Actions</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {quizQuestions.map((question) => (
                      <TableRow key={question.id}>
                        <TableCell className="max-w-[280px] truncate">{question.text}</TableCell>
                        <TableCell>{question.careerDomainName}</TableCell>
                        <TableCell>{question.category}</TableCell>
                        <TableCell>{question.options?.length || 0}</TableCell>
                        <TableCell className="text-right space-x-2">
                          <Button size="sm" variant="outline" onClick={() => editQuizQuestion(question)}>Edit</Button>
                          <Button size="sm" variant="destructive" onClick={() => deleteQuizQuestion(question.id)}>Delete</Button>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </CardContent>
            </Card>
          </div>
        </TabsContent>

        <TabsContent value="roadmap">
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
            <Card>
              <CardHeader><CardTitle className="flex items-center gap-2"><Map className="h-5 w-5" /> Roadmap Module CRUD</CardTitle></CardHeader>
              <CardContent className="space-y-2">
                <select className="w-full rounded-md border px-3 py-2 text-sm bg-background" value={roadmapForm.careerDomainId} onChange={(e) => setRoadmapForm((p) => ({ ...p, careerDomainId: e.target.value, skillId: '' }))}>
                  <option value="">Select Domain</option>
                  {domains.map((domain) => <option key={domain.id} value={domain.id}>{domain.name}</option>)}
                </select>
                <select className="w-full rounded-md border px-3 py-2 text-sm bg-background" value={roadmapForm.skillId} onChange={(e) => setRoadmapForm((p) => ({ ...p, skillId: e.target.value }))}>
                  <option value="">Select Skill</option>
                  {roadmapSkillOptions.map((skill) => <option key={skill.id} value={skill.id}>{skill.name}</option>)}
                </select>
                <Input placeholder="Module title" value={roadmapForm.title} onChange={(e) => setRoadmapForm((p) => ({ ...p, title: e.target.value }))} />
                <Textarea placeholder="Description" value={roadmapForm.description} onChange={(e) => setRoadmapForm((p) => ({ ...p, description: e.target.value }))} />
                <Input type="number" placeholder="Step number" value={roadmapForm.stepNumber} onChange={(e) => setRoadmapForm((p) => ({ ...p, stepNumber: e.target.value }))} />
                <Input type="number" placeholder="Estimated hours" value={roadmapForm.estimatedHours} onChange={(e) => setRoadmapForm((p) => ({ ...p, estimatedHours: e.target.value }))} />
                <div className="space-y-2">
                  <p className="text-sm font-medium text-muted-foreground">Topics (edit individually)</p>
                  {roadmapForm.topics.map((topic, index) => (
                    <div key={index} className="flex gap-2">
                      <Input
                        placeholder={`Topic ${index + 1}`}
                        value={topic}
                        onChange={(e) => updateRoadmapTopic(index, e.target.value)}
                      />
                      <Button
                        type="button"
                        variant="outline"
                        onClick={() => removeRoadmapTopic(index)}
                        disabled={roadmapForm.topics.length === 1}
                      >
                        Remove
                      </Button>
                    </div>
                  ))}
                  <Button type="button" variant="secondary" onClick={addRoadmapTopic}>+ Add Topic</Button>
                </div>
                <div className="flex gap-2">
                  <Button className="flex-1" onClick={submitRoadmapModule}>{roadmapForm.id ? 'Update' : 'Create'}</Button>
                  {roadmapForm.id && <Button variant="ghost" onClick={resetRoadmapForm}>Clear</Button>}
                </div>
                <p className="text-xs text-muted-foreground">Click Edit on any module card to load each field here for clear editing.</p>
              </CardContent>
            </Card>

            <Card className="lg:col-span-2">
              <CardHeader>
                <CardTitle>All Domain Roadmap Modules</CardTitle>
                <div className="flex gap-2 mt-3">
                  <select 
                    className="w-1/2 rounded-md border px-3 py-2 text-sm bg-background" 
                    value={roadmapFilterDomain} 
                    onChange={(e) => setRoadmapFilterDomain(e.target.value)}
                  >
                    <option value="">All Domains</option>
                    {uniqueDomainsFromRoadmaps.map((domainName) => <option key={domainName} value={domainName}>{domainName}</option>)}
                  </select>
                  <Input 
                    placeholder="Search by title, skill, or domain..." 
                    value={roadmapSearchText}
                    onChange={(e) => setRoadmapSearchText(e.target.value)}
                    className="w-1/2"
                  />
                </div>
                {(roadmapFilterDomain || roadmapSearchText) && (
                  <div className="flex items-center gap-2 mt-2">
                    <Badge variant="secondary">{filteredRoadmapModules.length} modules found</Badge>
                    {(roadmapFilterDomain || roadmapSearchText) && (
                      <Button 
                        variant="ghost" 
                        size="sm" 
                        onClick={() => { setRoadmapFilterDomain(''); setRoadmapSearchText(''); }}
                      >
                        Clear Filters
                      </Button>
                    )}
                  </div>
                )}
              </CardHeader>
              <CardContent className="space-y-3">
                {filteredRoadmapModules.map((module) => {
                  const domainName = module.careerDomainName || module.CareerDomainName || 'N/A';
                  const skillName = module.skillName || module.SkillName || 'N/A';
                  return (
                  <div key={module.id} className="border rounded-lg p-4 space-y-3">
                    <div className="flex items-center justify-between gap-2">
                      <div>
                        <p className="font-semibold text-lg">{module.title || module.Title}</p>
                        <p className="text-sm text-muted-foreground">Domain: {domainName}</p>
                        <p className="text-sm text-muted-foreground">Skill: {skillName}</p>
                        <p className="text-sm text-muted-foreground">Step: {module.stepNumber || module.StepNumber} • Estimated Hours: {module.estimatedHours || module.EstimatedHours}</p>
                      </div>
                      <div className="space-x-2">
                        <Button size="sm" variant="outline" onClick={() => editRoadmapModule(module)}>Edit</Button>
                        <Button size="sm" variant="destructive" onClick={() => deleteRoadmapModule(module.id)}>Delete</Button>
                      </div>
                    </div>
                    <div>
                      <p className="text-sm font-medium">Description</p>
                      <p className="text-sm text-muted-foreground mt-1">{module.description || module.Description}</p>
                    </div>
                    <div>
                      <p className="text-sm font-medium">Topics</p>
                      <ul className="mt-1 space-y-1 list-disc list-inside text-sm text-muted-foreground">
                        {(module.topics || module.Topics || []).map((topic, index) => (
                          <li key={topic.id || topic.Id || `${module.id}-topic-${index}`}>{topic.title || topic.Title}</li>
                        ))}
                      </ul>
                    </div>
                  </div>
                  );
                })}
                {roadmapModules.length === 0 && <p className="text-sm text-muted-foreground">No roadmap modules found.</p>}
                {roadmapModules.length > 0 && filteredRoadmapModules.length === 0 && (
                  <p className="text-sm text-muted-foreground">No modules match the current filters.</p>
                )}
              </CardContent>
            </Card>
          </div>
        </TabsContent>

        <TabsContent value="progress">
          <Card>
            <CardHeader><CardTitle>All Users Progress Snapshot</CardTitle></CardHeader>
            <CardContent>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>User</TableHead>
                    <TableHead>Email</TableHead>
                    <TableHead>Role</TableHead>
                    <TableHead>Level</TableHead>
                    <TableHead>XP</TableHead>
                    <TableHead>Completed Skills</TableHead>
                    <TableHead>Avg Domain Progress</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {progressSnapshots.map((item) => (
                    <TableRow key={item.userId}>
                      <TableCell>{item.fullName}</TableCell>
                      <TableCell>{item.email}</TableCell>
                      <TableCell>{item.role}</TableCell>
                      <TableCell>{item.level}</TableCell>
                      <TableCell>{item.totalXP}</TableCell>
                      <TableCell>{item.completedSkills}</TableCell>
                      <TableCell>{item.averageDomainProgress}%</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
              {progressSnapshots.length === 0 && <p className="text-sm text-muted-foreground mt-3">No progress data available yet.</p>}
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="sessions">
          <div className="grid grid-cols-1 lg:grid-cols-4 gap-4 mb-4">
            <Card className="lg:col-span-1">
              <CardHeader><CardTitle>Session Snapshot</CardTitle></CardHeader>
              <CardContent className="space-y-3">
                {['Approved', 'Completed', 'Cancelled'].map((status) => {
                  const count = sessions.filter((session) => session.status === status).length;
                  const total = sessions.length || 1;
                  const widthPercent = Math.round((count / total) * 100);
                  return (
                    <div key={status}>
                      <div className="flex justify-between text-sm">
                        <span>{status}</span>
                        <span>{count}</span>
                      </div>
                      <div className="h-2 w-full rounded bg-secondary mt-1">
                        <div className="h-2 rounded bg-primary" style={{ width: `${widthPercent}%` }} />
                      </div>
                    </div>
                  );
                })}
              </CardContent>
            </Card>

            <Card className="lg:col-span-3">
              <CardHeader><CardTitle>Filters</CardTitle></CardHeader>
              <CardContent>
                <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
                  <div>
                    <label className="text-sm font-medium mb-2 block">Status</label>
                    <select 
                      className="w-full px-3 py-2 border rounded-md text-sm bg-background"
                      value={sessionStatusFilter}
                      onChange={(e) => setSessionStatusFilter(e.target.value)}
                    >
                      <option value="">All Status</option>
                      <option value="Pending">Pending</option>
                      <option value="Approved">Approved</option>
                      <option value="Completed">Completed</option>
                      <option value="Cancelled">Cancelled</option>
                    </select>
                  </div>
                  <div>
                    <label className="text-sm font-medium mb-2 block">Student Name</label>
                    <Input 
                      placeholder="Search student..."
                      value={sessionStudentFilter}
                      onChange={(e) => setSessionStudentFilter(e.target.value)}
                      className="text-sm"
                    />
                  </div>
                  <div>
                    <label className="text-sm font-medium mb-2 block">Mentor Name</label>
                    <Input 
                      placeholder="Search mentor..."
                      value={sessionMentorFilter}
                      onChange={(e) => setSessionMentorFilter(e.target.value)}
                      className="text-sm"
                    />
                  </div>
                </div>
              </CardContent>
            </Card>
          </div>

          <Card>
            <CardHeader>
              <CardTitle className="flex items-center justify-between">
                <span>Session Details</span>
                <span className="text-sm font-normal text-muted-foreground">
                  ({sessions.filter((s) => {
                    const statusMatch = !sessionStatusFilter || s.status === sessionStatusFilter;
                    const studentMatch = !sessionStudentFilter || s.studentName.toLowerCase().includes(sessionStudentFilter.toLowerCase());
                    const mentorMatch = !sessionMentorFilter || s.mentorName.toLowerCase().includes(sessionMentorFilter.toLowerCase());
                    return statusMatch && studentMatch && mentorMatch;
                  }).length} sessions)
                </span>
              </CardTitle>
            </CardHeader>
            <CardContent>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Student</TableHead>
                    <TableHead>Mentor</TableHead>
                    <TableHead>Date & Time</TableHead>
                    <TableHead>Duration (min)</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead>Price</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {sessions.filter((s) => {
                    const statusMatch = !sessionStatusFilter || s.status === sessionStatusFilter;
                    const studentMatch = !sessionStudentFilter || s.studentName.toLowerCase().includes(sessionStudentFilter.toLowerCase());
                    const mentorMatch = !sessionMentorFilter || s.mentorName.toLowerCase().includes(sessionMentorFilter.toLowerCase());
                    return statusMatch && studentMatch && mentorMatch;
                  }).map((session) => (
                    <TableRow key={session.id}>
                      <TableCell className="font-medium">{session.studentName}</TableCell>
                      <TableCell>{session.mentorName}</TableCell>
                      <TableCell className="text-sm">{new Date(session.scheduledDateTime).toLocaleString()}</TableCell>
                      <TableCell className="text-sm">{session.durationMinutes || 60}</TableCell>
                      <TableCell>
                        <Badge variant={
                          session.status === 'Completed' ? 'secondary' : 
                          session.status === 'Approved' ? 'default' : 
                          session.status === 'Pending' ? 'outline' : 
                          'destructive'
                        }>
                          {session.status}
                        </Badge>
                      </TableCell>
                      <TableCell className="font-semibold">₹{session.sessionPrice}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
              {sessions.filter((s) => {
                const statusMatch = !sessionStatusFilter || s.status === sessionStatusFilter;
                const studentMatch = !sessionStudentFilter || s.studentName.toLowerCase().includes(sessionStudentFilter.toLowerCase());
                const mentorMatch = !sessionMentorFilter || s.mentorName.toLowerCase().includes(sessionMentorFilter.toLowerCase());
                return statusMatch && studentMatch && mentorMatch;
              }).length === 0 && (
                <p className="text-sm text-muted-foreground text-center py-6">No sessions found matching the filters.</p>
              )}
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="analytics">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <Card>
              <CardHeader><CardTitle>Platform Analytics</CardTitle></CardHeader>
              <CardContent className="space-y-2 text-sm">
                <p><span className="text-muted-foreground">Most Selected Path:</span> {analytics?.mostSelectedCareerPath || 'N/A'}</p>
                <p><span className="text-muted-foreground">Weakest Quiz Category:</span> {analytics?.weakestQuizCategory || 'N/A'}</p>
                <p><span className="text-muted-foreground">Average Quiz Score:</span> {analytics?.averageQuizScore || 0}</p>
                <p><span className="text-muted-foreground">Daily Signups:</span> {analytics?.dailySignups || 0}</p>
                <p><span className="text-muted-foreground">Active Users (30d):</span> {analytics?.activeUsersLast30Days || 0}</p>
                <p><span className="text-muted-foreground">Monthly Sessions:</span> {analytics?.monthlySessions || 0}</p>
              </CardContent>
            </Card>
            <Card>
              <CardHeader><CardTitle>Top Mentors (Quick Chart)</CardTitle></CardHeader>
              <CardContent className="space-y-2">
                {(analytics?.topMentors || []).map((mentor) => (
                  <div key={mentor.mentorId} className="border rounded-md p-3 text-sm">
                    <div className="flex justify-between mb-1">
                      <p className="font-medium">{mentor.mentorName}</p>
                      <p className="text-muted-foreground">Rating: {mentor.averageRating}</p>
                    </div>
                    <div className="h-2 w-full rounded bg-secondary mb-2">
                      <div className="h-2 rounded bg-primary" style={{ width: `${Math.min(100, Math.round((mentor.averageRating / 5) * 100))}%` }} />
                    </div>
                    <p className="text-muted-foreground">Sessions: {mentor.completedSessions} | Revenue: ₹{mentor.revenue}</p>
                  </div>
                ))}
              </CardContent>
            </Card>
          </div>
        </TabsContent>

        <TabsContent value="announcement">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2"><BellRing className="h-5 w-5" /> Broadcast Announcement</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              <Input placeholder="Enter announcement message" value={announcement} onChange={(e) => setAnnouncement(e.target.value)} />
              <Button onClick={sendAnnouncement}>Send Announcement</Button>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
};

export default AdminPage;
