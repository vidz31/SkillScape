import React, { useState, useEffect } from 'react';
import { motion } from 'framer-motion';
import { useAuth } from '@/context/AuthContext';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Progress } from '@/components/ui/progress';
import { Badge } from '@/components/ui/badge';
import {
  Sparkles,
  TrendingUp,
  Target,
  Calendar,
  ArrowRight,
  Flame,
  BookOpen,
  Clock,
  Star,
  Loader2,
  BellRing,
  X,
} from 'lucide-react';
import { Link, Navigate, useNavigate } from 'react-router-dom';
import { progressApi, mentorsApi, sessionsApi } from '@/services/api';
import { toast } from 'sonner';

const containerVariants = {
  hidden: { opacity: 0 },
  visible: { opacity: 1, transition: { staggerChildren: 0.08 } },
};

const itemVariants = {
  hidden: { opacity: 0, y: 12 },
  visible: { opacity: 1, y: 0, transition: { duration: 0.36 } },
};

const DashboardPage = () => {
  const { user } = useAuth();
  const navigate = useNavigate();

  if (user?.role === 'Admin') {
    return <Navigate to="/admin" replace />;
  }

  if (user?.role === 'Mentor') {
    return <Navigate to="/mentor-dashboard" replace />;
  }

  const [stats, setStats] = useState(null);
  const [badges, setBadges] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [notifications, setNotifications] = useState([]);
  const [upcomingSessions, setUpcomingSessions] = useState([]);

  const [roadmapData, setRoadmapData] = useState(null);

  useEffect(() => {
    const fetchDashboardData = async () => {
      try {
        setLoading(true);
        setError(null);

        // Fetch dashboard data and the custom ML roadmap concurrently
        const [statsRes, badgesRes, roadmapRes, notificationsRes, sessionsRes] = await Promise.all([
          progressApi.getMyProgress(),
          progressApi.getBadges(),
          import('@/services/api').then(m => m.roadmapApi.getMyInterestRoadmap().catch(() => ({ data: { data: null } }))),
          mentorsApi.getNotifications().catch(() => ({ data: [] })),
          sessionsApi.getUpcomingSessions().catch(() => ({ data: [] }))
        ]);

        setStats(statsRes.data);
        setBadges(badgesRes.data || []);
        setRoadmapData(roadmapRes.data?.data);
        setNotifications(notificationsRes.data || []);
        setUpcomingSessions(sessionsRes.data || []);
      } catch (err) {
        console.error('Failed to fetch dashboard data:', err);
        setError(err.message || 'Failed to load dashboard data');
      } finally {
        setLoading(false);
      }
    };

    fetchDashboardData();
  }, []);

  // Helper function to format session date/time
  const formatSessionTime = (scheduledDateTime) => {
    const date = new Date(scheduledDateTime);
    const now = new Date();
    const tomorrow = new Date(now);
    tomorrow.setDate(tomorrow.getDate() + 1);
    
    const isToday = date.toDateString() === now.toDateString();
    const isTomorrow = date.toDateString() === tomorrow.toDateString();
    
    const timeStr = date.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit', hour12: true });
    
    if (isToday) return `Today, ${timeStr}`;
    if (isTomorrow) return `Tomorrow, ${timeStr}`;
    
    const dayName = date.toLocaleDateString('en-US', { weekday: 'short' });
    return `${dayName}, ${timeStr}`;
  };

  // Show loading state
  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-[60vh]">
        <div className="text-center">
          <Loader2 className="h-12 w-12 animate-spin mx-auto text-accent" />
          <p className="mt-4 text-muted-foreground">Loading your dashboard...</p>
        </div>
      </div>
    );
  }

  // Show error state
  if (error) {
    return (
      <div className="flex items-center justify-center min-h-[60vh]">
        <Card className="max-w-md">
          <CardContent className="p-6 text-center">
            <p className="text-destructive mb-4">{error}</p>
            <Button onClick={() => window.location.reload()}>Try Again</Button>
          </CardContent>
        </Card>
      </div>
    );
  }

  // Calculate progress values using the actual Roadmap Data
  const xp = stats?.totalXP || 0;
  const level = stats?.level || 1;
  const xpToNextLevel = stats?.xpToNextLevel || 1000;
  const xpProgress = (xp / xpToNextLevel) * 100;
  const currentStreak = stats?.currentStreak || 0;

  // Mark notification as read handler
  const handleDismissNotification = async (notificationId) => {
    try {
      await mentorsApi.markNotificationAsRead(notificationId);
      setNotifications(notifications.filter(n => n.id !== notificationId));
    } catch (err) {
      console.error('Failed to mark notification as read:', err);
      toast.error('Failed to dismiss notification');
    }
  };

  // Get unread notifications
  const unreadNotifications = notifications.filter(n => !n.isRead);

  // Get recent badges (limit to 3)
  const recentBadges = badges
    .filter(b => b.earned)
    .sort((a, b) => new Date(b.earnedAt) - new Date(a.earnedAt))
    .slice(0, 3);

  // Get domain progressions from backend (already filtered and accurate)
  const domainProgressions = (stats?.domainProgressions || []).filter(d => d.totalSkills > 0 && d.skillsCompleted > 0);
  
  // Use backend domain progression data for active domain
  // If roadmapData exists, find matching domain from progressions, otherwise use highest progress domain
  const activeDomain = roadmapData && domainProgressions.length > 0
    ? domainProgressions.find(d => d.domainName === roadmapData.domainName) || domainProgressions[0]
    : domainProgressions.length > 0
    ? domainProgressions[0]
    : null;


  return (
    <motion.div
      variants={containerVariants}
      initial="hidden"
      animate="visible"
      className="space-y-6"
    >
      {/* Header */}
      <motion.div variants={itemVariants}>
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div>
            <h1 className="text-3xl font-bold">Welcome back, {user?.fullName?.split(' ')[0] || 'Learner'}! 👋</h1>
            <p className="text-sm text-muted-foreground mt-1">Continue your learning journey. You're doing great!</p>
          </div>
          <Link to="/quiz">
            <Button className="bg-gradient-accent hover:opacity-90">
              <Sparkles className="mr-2 h-5 w-5" />
              Take Career Quiz
            </Button>
          </Link>
        </div>
      </motion.div>

      {/* Announcements */}
      {unreadNotifications.length > 0 && (
        <motion.div variants={itemVariants}>
          <Card className="border-accent/50 bg-accent/5">
            <CardHeader className="pb-3">
              <CardTitle className="text-lg font-semibold flex items-center gap-2">
                <BellRing className="h-5 w-5 text-accent" />
                Announcements
                <Badge variant="secondary" className="ml-auto bg-accent/10 text-accent">
                  {unreadNotifications.length} New
                </Badge>
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              {unreadNotifications.slice(0, 3).map((notification) => (
                <div
                  key={notification.id}
                  className="flex items-start gap-3 p-4 rounded-lg bg-background border border-border hover:border-accent/50 transition-colors"
                >
                  <div className="flex h-10 w-10 items-center justify-center rounded-full bg-accent/10 flex-shrink-0">
                    <BellRing className="h-5 w-5 text-accent" />
                  </div>
                  <div className="flex-1 min-w-0">
                    <p className="text-sm font-medium text-foreground">{notification.message}</p>
                    <p className="text-xs text-muted-foreground mt-1">
                      {new Date(notification.createdAt).toLocaleDateString('en-US', {
                        month: 'short',
                        day: 'numeric',
                        year: 'numeric',
                        hour: '2-digit',
                        minute: '2-digit',
                      })}
                    </p>
                  </div>
                  <Button
                    variant="ghost"
                    size="sm"
                    className="h-8 w-8 p-0"
                    onClick={() => handleDismissNotification(notification.id)}
                  >
                    <X className="h-4 w-4" />
                  </Button>
                </div>
              ))}
              {unreadNotifications.length > 3 && (
                <p className="text-xs text-muted-foreground text-center pt-2">
                  +{unreadNotifications.length - 3} more announcements
                </p>
              )}
            </CardContent>
          </Card>
        </motion.div>
      )}

      {/* Stats */}
      <motion.div variants={itemVariants} className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        <Card className="card-hover bg-gradient-to-br from-card to-secondary/30 border-border/50">
          <CardContent className="p-5">
            <div className="flex items-center justify-between">
              <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-accent/10"><Sparkles className="h-6 w-6 text-accent" /></div>
              <Badge variant="secondary" className="bg-accent/10 text-accent">+{stats?.xpEarnedToday || 0} today</Badge>
            </div>
            <div className="mt-4">
              <p className="text-sm text-muted-foreground">Total XP</p>
              <p className="text-3xl font-bold">{xp.toLocaleString()}</p>
            </div>
          </CardContent>
        </Card>

        <Card className="card-hover bg-gradient-to-br from-card to-secondary/30 border-border/50">
          <CardContent className="p-5">
            <div className="flex items-center justify-between">
              <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-success/10"><TrendingUp className="h-6 w-6 text-success" /></div>
              <span className="text-sm text-muted-foreground">Level</span>
            </div>
            <div className="mt-4">
              <p className="text-sm text-muted-foreground">Current Level</p>
              <p className="text-3xl font-bold">{level}</p>
              <Progress value={xpProgress} className="mt-2 h-2" />
              <p className="text-xs text-muted-foreground mt-1">{xp} / {xpToNextLevel} XP to Level {level + 1}</p>
            </div>
          </CardContent>
        </Card>

        <Card className="card-hover bg-gradient-to-br from-card to-secondary/30 border-border/50">
          <CardContent className="p-5">
            <div className="flex items-center justify-between">
              <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-warning/10"><Flame className="h-6 w-6 text-warning" /></div>
              <span className="text-2xl">🔥</span>
            </div>
            <div className="mt-4">
              <p className="text-sm text-muted-foreground">Learning Streak</p>
              <p className="text-3xl font-bold">{currentStreak} days</p>
            </div>
          </CardContent>
        </Card>

        <Card className="card-hover bg-gradient-to-br from-card to-secondary/30 border-border/50">
          <CardContent className="p-5">
            <div className="flex items-center justify-between">
              <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-primary/10"><Target className="h-6 w-6 text-primary" /></div>
              <Badge variant="secondary">This week: {stats?.skillsCompletedThisWeek || 0}</Badge>
            </div>
            <div className="mt-4">
              <p className="text-sm text-muted-foreground">Skills Completed</p>
              <p className="text-3xl font-bold">{stats?.completedSkills || 0}</p>
            </div>
          </CardContent>
        </Card>
      </motion.div>

      {/* Content Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <motion.div variants={itemVariants}>
          <Card className="shadow-soft border-border/50">
            <CardHeader className="pb-4">
              <div className="flex items-center justify-between">
                <CardTitle className="text-lg font-semibold">Current Learning Path</CardTitle>
                <Link to="/roadmap">
                  <Button variant="ghost" size="sm" className="text-accent">View Roadmap <ArrowRight className="ml-1 h-4 w-4" /></Button>
                </Link>
              </div>
            </CardHeader>
            <CardContent>
              {activeDomain ? (
                <>
                  <div className="flex items-center gap-4 p-4 rounded-xl bg-secondary/50">
                    <div className="flex h-16 w-16 items-center justify-center rounded-xl bg-gradient-accent"><BookOpen className="h-8 w-8 text-accent-foreground" /></div>
                    <div className="flex-1">
                      <h3 className="font-semibold">{activeDomain.domainName}</h3>
                      <p className="text-sm text-muted-foreground">{activeDomain.skillsCompleted} of {activeDomain.totalSkills} skills completed</p>
                      <div className="mt-2"><Progress value={activeDomain.progressPercentage} className="h-2" /></div>
                    </div>
                    <div className="text-right">
                      <span className="text-2xl font-bold text-accent">{Math.round(activeDomain.progressPercentage)}%</span>
                      <p className="text-xs text-muted-foreground">Complete</p>
                    </div>
                  </div>

                  {domainProgressions.length > 1 && (
                    <div className="mt-6">
                      <h4 className="text-sm font-medium text-muted-foreground mb-3">Other Domains</h4>
                      <div className="space-y-3">
                        {domainProgressions.slice(1).map((domain) => (
                          <div key={domain.id} className="flex items-center justify-between p-3 rounded-lg bg-background border border-border/50 hover:border-accent/50 transition-colors">
                            <div className="flex items-center gap-3">
                              <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-success/10"><Star className="h-4 w-4 text-success" /></div>
                              <div>
                                <p className="text-sm font-medium">{domain.domainName}</p>
                                <p className="text-xs text-muted-foreground">{domain.skillsCompleted} / {domain.totalSkills} skills</p>
                              </div>
                            </div>
                            <Badge variant="secondary" className="bg-accent/10 text-accent">{Math.round(domain.progressPercentage)}%</Badge>
                          </div>
                        ))}
                      </div>
                    </div>
                  )}
                </>
              ) : (
                <div className="text-center py-12">
                  <BookOpen className="mx-auto h-12 w-12 text-muted-foreground opacity-50" />
                  <p className="mt-4 text-sm text-muted-foreground">Start your learning journey by taking the career quiz!</p>
                  <Link to="/quiz">
                    <Button className="mt-4 bg-gradient-accent hover:opacity-90">
                      <Sparkles className="mr-2 h-5 w-5" />
                      Take Career Quiz
                    </Button>
                  </Link>
                </div>
              )}
            </CardContent>
          </Card>
        </motion.div>

        <motion.div variants={itemVariants}>
          <Card className="shadow-soft border-border/50 h-full">
            <CardHeader className="pb-3">
              <div className="flex items-center justify-between">
                <CardTitle className="text-lg font-semibold">Recent Badges</CardTitle>
                <Link to="/progress"><Button variant="ghost" size="sm" className="text-accent">View All</Button></Link>
              </div>
            </CardHeader>
            <CardContent>
              {recentBadges.length > 0 ? (
                <div className="flex gap-3">
                  {recentBadges.map((badge) => (
                    <motion.div key={badge.id} whileHover={{ scale: 1.05 }} className="flex-1 flex flex-col items-center p-3 rounded-xl bg-secondary/50 cursor-pointer" title={badge.description}>
                      <span className="text-3xl mb-2">{badge.iconUrl || '🏆'}</span>
                      <span className="text-xs text-center font-medium text-foreground">{badge.name}</span>
                      <Badge variant="secondary" className={`mt-1 text-[10px] ${badge.rarity?.toLowerCase() === 'epic' ? 'bg-purple-500/10 text-purple-500' :
                        badge.rarity?.toLowerCase() === 'rare' ? 'bg-accent/10 text-accent' :
                          'bg-muted text-muted-foreground'
                        }`}>
                        {badge.rarity || 'Common'}
                      </Badge>
                    </motion.div>
                  ))}
                </div>
              ) : (
                <div className="text-center py-6">
                  <Star className="mx-auto h-8 w-8 text-muted-foreground opacity-50" />
                  <p className="mt-2 text-sm text-muted-foreground">No badges earned yet</p>
                  <p className="text-xs text-muted-foreground">Complete skills to earn badges!</p>
                </div>
              )}
            </CardContent>
          </Card>
        </motion.div>
      </div>

      {/* Quick Actions & Sessions Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <motion.div variants={itemVariants}>
          <Card className="shadow-soft border-border/50">
            <CardHeader className="pb-3"><CardTitle className="text-lg font-semibold">Quick Actions</CardTitle></CardHeader>
            <CardContent className="grid grid-cols-2 gap-3">
              <Link to="/quiz" className="block">
                <Button variant="outline" className="w-full h-24 flex-col gap-2 hover:border-accent hover:bg-accent/5">
                  <Sparkles className="h-6 w-6 text-accent" />
                  <span className="text-sm font-medium">Discover Career</span>
                </Button>
              </Link>
              <Link to="/roadmap" className="block">
                <Button variant="outline" className="w-full h-24 flex-col gap-2 hover:border-success hover:bg-success/5">
                  <Target className="h-6 w-6 text-success" />
                  <span className="text-sm font-medium">Continue Learning</span>
                </Button>
              </Link>
              <Link to="/mentors" className="block">
                <Button variant="outline" className="w-full h-24 flex-col gap-2 hover:border-primary hover:bg-primary/5">
                  <Calendar className="h-6 w-6 text-primary" />
                  <span className="text-sm font-medium">Book Session</span>
                </Button>
              </Link>
              <Link to="/resume" className="block">
                <Button variant="outline" className="w-full h-24 flex-col gap-2 hover:border-warning hover:bg-warning/5">
                  <BookOpen className="h-6 w-6 text-warning" />
                  <span className="text-sm font-medium">Update Resume</span>
                </Button>
              </Link>
            </CardContent>
          </Card>
        </motion.div>

        <motion.div variants={itemVariants}>
          <Card className="shadow-soft border-border/50">
            <CardHeader className="pb-3">
              <div className="flex items-center justify-between">
                <CardTitle className="text-lg font-semibold">Upcoming Sessions</CardTitle>
                <Link to="/mentors"><Button variant="ghost" size="sm" className="text-accent">View All</Button></Link>
              </div>
            </CardHeader>
            <CardContent className="space-y-3">
              {upcomingSessions.length > 0 ? (
                upcomingSessions.map((session) => (
                  <div key={session.id} className="flex items-center gap-3 p-3 rounded-lg bg-secondary/50">
                    <div className="flex h-10 w-10 items-center justify-center rounded-full bg-primary/10">
                      <Clock className="h-5 w-5 text-primary" />
                    </div>
                    <div className="flex-1 min-w-0">
                      <p className="text-sm font-medium text-foreground truncate">
                        {session.notes || 'Mentorship Session'}
                      </p>
                      <p className="text-xs text-muted-foreground">with {session.mentorName}</p>
                    </div>
                    <div className="flex items-center gap-2">
                      {session.isPaid ? (
                        <Badge variant="outline" className="text-xs whitespace-nowrap bg-emerald-100 text-emerald-800 border-emerald-200">
                          Paid
                        </Badge>
                      ) : session.status === 'Approved' ? (
                        <Button
                          size="sm"
                          className="bg-cyan-500 hover:bg-cyan-600 text-white h-8"
                          onClick={() => navigate('/my-mentors')}
                        >
                          💳 Pay Now
                        </Button>
                      ) : (
                        <Badge variant="outline" className="text-xs whitespace-nowrap bg-yellow-100 text-yellow-800 border-yellow-200">
                          Pending Approval
                        </Badge>
                      )}

                      <Badge variant="outline" className="text-xs whitespace-nowrap">
                        {formatSessionTime(session.scheduledDateTime).split(',')[0]}
                      </Badge>
                    </div>
                  </div>
                ))
              ) : (
                <div className="text-center py-8">
                  <Calendar className="mx-auto h-8 w-8 text-muted-foreground opacity-50" />
                  <p className="mt-2 text-sm text-muted-foreground">No upcoming sessions</p>
                  <p className="text-xs text-muted-foreground">Book a session with a mentor!</p>
                </div>
              )}
            </CardContent>
          </Card>
        </motion.div>
      </div>
    </motion.div>
  );
};

export default DashboardPage;

