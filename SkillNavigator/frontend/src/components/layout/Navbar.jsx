import React from 'react';
import { useAuth } from '@/context/AuthContext';
import { useTheme } from '@/hooks/useTheme';
import { useState, useEffect } from 'react';
import { mentorsApi } from '@/services/api/mentors';
import { Button } from '@/components/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import {
  Bell,
  Moon,
  Sun,
  User,
  Settings,
  LogOut,
  ChevronDown,
  Trophy,
  Calendar,
  CheckCircle,
  MessageCircle,
  UserCheck,
} from 'lucide-react';
import { Badge } from '@/components/ui/badge';

export const Navbar = () => {
  const { user, logout } = useAuth();
  const { theme, toggleTheme } = useTheme();
  const [notifications, setNotifications] = useState([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    fetchNotifications();
  }, []);

  useEffect(() => {
    const handleIncomingNotification = () => {
      fetchNotifications();
    };

    window.addEventListener('skillscape:notification-received', handleIncomingNotification);

    return () => {
      window.removeEventListener('skillscape:notification-received', handleIncomingNotification);
    };
  }, []);

  const fetchNotifications = async () => {
    setLoading(true);
    try {
      const response = await mentorsApi.getNotifications();
      const allNotifications = Array.isArray(response?.data) ? response.data : [];
      setNotifications(allNotifications.filter(notification => !notification.isRead));
    } catch (error) {
      console.error('Error fetching notifications:', error);
      setNotifications([]);
    } finally {
      setLoading(false);
    }
  };

  const handleMarkAsRead = async (notificationId) => {
    try {
      await mentorsApi.markNotificationAsRead(notificationId);
      setNotifications(prev => prev.filter(notif => notif.id !== notificationId));
    } catch (error) {
      console.error('Error marking notification as read:', error);
    }
  };

  const handleMarkAllAsRead = async () => {
    try {
      await mentorsApi.markAllNotificationsAsRead();
      setNotifications([]);
    } catch (error) {
      console.error('Error marking all notifications as read:', error);
    }
  };

  const getNotificationIcon = (type) => {
    switch (type) {
      case 'Badge':
        return <Trophy className="h-4 w-4 text-yellow-500" />;
      case 'LevelUp':
        return <Trophy className="h-4 w-4 text-purple-500" />;
      case 'Session':
        return <Calendar className="h-4 w-4 text-blue-500" />;
      case 'Message':
        return <MessageCircle className="h-4 w-4 text-green-500" />;
      case 'Enrollment':
        return <UserCheck className="h-4 w-4 text-indigo-500" />;
      case 'MentorApproval':
        return <CheckCircle className="h-4 w-4 text-emerald-500" />;
      default:
        return <Bell className="h-4 w-4 text-muted-foreground" />;
    }
  };

  const getNotificationColor = (type) => {
    switch (type) {
      case 'Badge':
        return 'bg-yellow-500/10 text-yellow-500';
      case 'LevelUp':
        return 'bg-purple-500/10 text-purple-500';
      case 'Session':
        return 'bg-blue-500/10 text-blue-500';
      case 'Message':
        return 'bg-green-500/10 text-green-500';
      case 'Enrollment':
        return 'bg-indigo-500/10 text-indigo-500';
      case 'MentorApproval':
        return 'bg-emerald-500/10 text-emerald-500';
      default:
        return 'bg-secondary/10 text-secondary';
    }
  };

  const getNotificationTypeLabel = (type) => {
    return type && type.trim() ? type : 'General';
  };

  const formatTimeAgo = (dateString) => {
    const date = new Date(dateString);
    const now = new Date();
    const seconds = Math.floor((now - date) / 1000);
    
    if (seconds < 60) return 'Just now';
    if (seconds < 3600) return `${Math.floor(seconds / 60)} mins ago`;
    if (seconds < 86400) return `${Math.floor(seconds / 3600)} hours ago`;
    if (seconds < 604800) return `${Math.floor(seconds / 86400)} days ago`;
    return date.toLocaleDateString();
  };

  const unreadCount = notifications.filter(n => !n.isRead).length;

  return (
    <header className="sticky top-0 z-30 flex h-16 items-center justify-between border-b border-border bg-background/80 backdrop-blur-md px-6">
      {/* Empty Left Section */}
      <div className="flex-1"></div>

      {/* Right Section */}
      <div className="flex items-center gap-3">
        {/* Theme Toggle */}
        <Button
          variant="ghost"
          size="icon"
          onClick={toggleTheme}
          className="h-9 w-9 rounded-full"
        >
          {theme === 'dark' ? (
            <Sun className="h-5 w-5 text-muted-foreground" />
          ) : (
            <Moon className="h-5 w-5 text-muted-foreground" />
          )}
        </Button>

        {/* Notifications */}
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="ghost" size="icon" className="h-9 w-9 rounded-full relative">
              <Bell className="h-5 w-5 text-muted-foreground" />
              {unreadCount > 0 && (
                <span className="absolute -top-0.5 -right-0.5 flex h-4 w-4 items-center justify-center rounded-full bg-accent text-[10px] font-bold text-accent-foreground">
                  {unreadCount}
                </span>
              )}
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end" className="w-80">
            <DropdownMenuLabel className="flex items-center justify-between">
              <span>Notifications</span>
              <div className="flex items-center gap-2">
                {unreadCount > 0 && (
                  <span className="text-xs text-muted-foreground font-normal">
                    {unreadCount} unread
                  </span>
                )}
                {unreadCount > 0 && (
                  <Button
                    variant="ghost"
                    size="sm"
                    className="h-6 px-2 text-xs"
                    onClick={handleMarkAllAsRead}
                  >
                    Mark all
                  </Button>
                )}
              </div>
            </DropdownMenuLabel>
            <DropdownMenuSeparator />
            <div className="max-h-64 overflow-y-auto">
              {loading ? (
                <div className="p-4 text-center text-sm text-muted-foreground">
                  Loading notifications...
                </div>
              ) : notifications.length === 0 ? (
                <div className="p-4 text-center text-sm text-muted-foreground">
                  No notifications yet
                </div>
              ) : (
                notifications.map((notification) => (
                  <DropdownMenuItem
                    key={notification.id}
                    className={`flex flex-col items-start gap-2 p-3 cursor-pointer ${
                      !notification.isRead ? 'bg-accent/5' : ''
                    } hover:bg-cyan-50 dark:hover:bg-cyan-950/40 data-[highlighted]:bg-cyan-50 dark:data-[highlighted]:bg-cyan-950/40 data-[highlighted]:text-foreground focus:bg-cyan-50 dark:focus:bg-cyan-950/40 focus:text-foreground`}
                    onClick={() => handleMarkAsRead(notification.id)}
                  >
                    <div className="flex items-center gap-2 w-full">
                      {getNotificationIcon(notification.type)}
                      <Badge
                        variant="secondary"
                        className={`${getNotificationColor(notification.type)} text-xs border-transparent`}
                      >
                        {getNotificationTypeLabel(notification.type)}
                      </Badge>
                      {!notification.isRead && (
                        <div className="ml-auto h-2 w-2 rounded-full bg-accent" />
                      )}
                    </div>
                    <p className="text-sm font-medium text-foreground">{notification.title}</p>
                    <p className="text-xs text-muted-foreground">{notification.message}</p>
                    <span className="text-xs text-muted-foreground">
                      {formatTimeAgo(notification.createdAt)}
                    </span>
                  </DropdownMenuItem>
                ))
              )}
            </div>
          </DropdownMenuContent>
        </DropdownMenu>

        {/* User Menu */}
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="ghost" className="flex items-center gap-2 h-9 px-2 rounded-full">
              <div className="flex h-8 w-8 items-center justify-center rounded-full bg-gradient-accent">
                <span className="text-sm font-semibold text-accent-foreground">
                  {user?.name?.charAt(0).toUpperCase() || 'U'}
                </span>
              </div>
              <span className="hidden md:block text-sm font-medium">
                {user?.name || 'User'}
              </span>
              <ChevronDown className="h-4 w-4 text-muted-foreground" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end" className="w-56">
            <DropdownMenuLabel>
              <div className="flex flex-col">
                <span>{user?.name || 'User'}</span>
                <span className="text-xs font-normal text-muted-foreground">
                  {user?.email || 'user@example.com'}
                </span>
              </div>
            </DropdownMenuLabel>
            <DropdownMenuSeparator />
            <DropdownMenuItem className="cursor-pointer">
              <User className="mr-2 h-4 w-4" />
              Profile
            </DropdownMenuItem>
            <DropdownMenuItem className="cursor-pointer">
              <Settings className="mr-2 h-4 w-4" />
              Settings
            </DropdownMenuItem>
            <DropdownMenuSeparator />
            <DropdownMenuItem onClick={logout} className="cursor-pointer text-destructive">
              <LogOut className="mr-2 h-4 w-4" />
              Logout
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>
    </header>
  );
};

export default Navbar;
