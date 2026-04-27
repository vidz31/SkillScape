import { Toaster as Sonner } from "@/components/ui/sonner";
import { TooltipProvider } from "@/components/ui/tooltip";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import { AuthProvider } from "@/context/AuthContext";
import { ProtectedRoute, PublicRoute } from "@/routes/ProtectedRoute";
import { DashboardLayout, AuthLayout } from "@/components/layout";

// Pages
import LoginPage from "@/pages/Login";
import RegisterPage from "@/pages/Register";
import ForgotPasswordPage from "@/pages/ForgotPassword";
import ResetPasswordPage from "@/pages/ResetPassword";
import DashboardPage from "@/pages/Dashboard";
import MentorDashboard from "@/pages/MentorDashboard";
import QuizPage from "@/pages/Quiz";
import RoadmapPage from "@/pages/Roadmap";
import ProgressPage from "@/pages/Progress";
import ResumePage from "@/pages/Resume";
import MentorsPage from "@/pages/Mentors";
import MentorProfile from "@/pages/MentorProfile";
import MyMentors from "@/pages/MyMentors";
import MessagesPage from "@/pages/Messages";
import ProfilePage from "@/pages/Profile";
import AdminPage from "@/pages/Admin";
import SessionsCalendar from "@/pages/SessionsCalendar";
import UnauthorizedPage from "@/pages/Unauthorized";
import NotFound from "@/pages/NotFound";

const queryClient = new QueryClient();

const App = () => (
  <QueryClientProvider client={queryClient}>
    <AuthProvider>
      <TooltipProvider>
        <Sonner />
        <BrowserRouter>
          <Routes>
            {/* Public Routes */}
            <Route element={<AuthLayout />}>
              <Route
                path="/login"
                element={
                  <PublicRoute>
                    <LoginPage />
                  </PublicRoute>
                }
              />
              <Route
                path="/register"
                element={
                  <PublicRoute>
                    <RegisterPage />
                  </PublicRoute>
                }
              />
              <Route
                path="/forgot-password"
                element={
                  <PublicRoute>
                    <ForgotPasswordPage />
                  </PublicRoute>
                }
              />
              <Route
                path="/reset-password"
                element={
                  <PublicRoute>
                    <ResetPasswordPage />
                  </PublicRoute>
                }
              />
            </Route>

            {/* Protected Dashboard Routes */}
            <Route
              element={
                <ProtectedRoute>
                  <DashboardLayout />
                </ProtectedRoute>
              }
            >
              <Route path="/dashboard" element={<DashboardPage />} />
              <Route path="/mentor-dashboard" element={<MentorDashboard />} />
              <Route path="/mentor-dashboard/requests" element={<MentorDashboard />} />
              <Route path="/mentor-dashboard/students" element={<MentorDashboard />} />
              <Route path="/mentor-dashboard/payments" element={<MentorDashboard />} />
              <Route path="/mentor-dashboard/sessions" element={<MentorDashboard />} />
              <Route path="/mentor-dashboard/feedback" element={<MentorDashboard />} />
              <Route path="/mentor-dashboard/schedule" element={<MentorDashboard />} />
              <Route path="/sessions" element={<SessionsCalendar />} />
              <Route path="/quiz" element={<QuizPage />} />
              <Route path="/roadmap" element={<RoadmapPage />} />
              <Route path="/progress" element={<ProgressPage />} />
              <Route path="/resume" element={<ResumePage />} />
              <Route path="/mentors" element={<MentorsPage />} />
              <Route path="/mentor/:mentorId" element={<MentorProfile />} />
              <Route path="/my-mentors" element={<MyMentors />} />
              <Route path="/messages" element={<MessagesPage />} />
              <Route path="/profile" element={<ProfilePage />} />
              <Route
                path="/admin"
                element={
                  <ProtectedRoute roles="Admin">
                    <AdminPage />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/admin/:section"
                element={
                  <ProtectedRoute roles="Admin">
                    <AdminPage />
                  </ProtectedRoute>
                }
              />
              <Route path="/unauthorized" element={<UnauthorizedPage />} />
            </Route>

            {/* Redirects */}
            <Route path="/" element={<Navigate to="/login" replace />} />

            {/* 404 */}
            <Route path="*" element={<NotFound />} />
          </Routes>
        </BrowserRouter>
      </TooltipProvider>
    </AuthProvider>
  </QueryClientProvider>
);

export default App;
