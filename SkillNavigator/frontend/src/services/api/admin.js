import { apiClient } from './axios';

const unwrap = (response) => {
  return response.data?.Data ?? response.data?.data ?? response.data;
};

export const adminApi = {
  getDashboard: async () => {
    const response = await apiClient.get('/admin/dashboard');
    return unwrap(response);
  },

  getUsers: async (params = {}) => {
    const response = await apiClient.get('/admin/users', { params });
    return unwrap(response) || [];
  },

  blockUser: async (payload) => {
    const response = await apiClient.put('/admin/block-user', payload);
    return unwrap(response);
  },

  updateUserRole: async (payload) => {
    const response = await apiClient.put('/admin/users/role', payload);
    return unwrap(response);
  },

  getPendingMentors: async () => {
    const response = await apiClient.get('/admin/mentors/pending');
    return unwrap(response) || [];
  },

  approveMentor: async (mentorId) => {
    const response = await apiClient.put(`/admin/mentor/approve/${mentorId}`);
    return unwrap(response);
  },

  rejectMentor: async (mentorId, reason) => {
    const response = await apiClient.put(`/admin/mentor/reject/${mentorId}`, { reason });
    return unwrap(response);
  },

  getSessions: async (status) => {
    const response = await apiClient.get('/admin/sessions', {
      params: status ? { status } : {},
    });
    return unwrap(response) || [];
  },

  getAnalytics: async () => {
    const response = await apiClient.get('/admin/analytics');
    return unwrap(response);
  },

  broadcastAnnouncement: async (message) => {
    const response = await apiClient.post('/admin/announcement', { message });
    return unwrap(response);
  },

  getComplaints: async (status) => {
    const response = await apiClient.get('/admin/sessions/complaints', {
      params: status ? { status } : {},
    });
    return unwrap(response) || [];
  },

  resolveComplaint: async (complaintId, resolutionNote) => {
    const response = await apiClient.put(`/admin/sessions/complaints/${complaintId}/resolve`, {
      resolutionNote,
    });
    return unwrap(response);
  },

  getQuizQuestions: async () => {
    const response = await apiClient.get('/admin/quiz/questions');
    return unwrap(response) || [];
  },

  createQuizQuestion: async (payload) => {
    const response = await apiClient.post('/admin/quiz/questions', payload);
    return unwrap(response);
  },

  updateQuizQuestion: async (questionId, payload) => {
    const response = await apiClient.put(`/admin/quiz/questions/${questionId}`, payload);
    return unwrap(response);
  },

  deleteQuizQuestion: async (questionId) => {
    const response = await apiClient.delete(`/admin/quiz/questions/${questionId}`);
    return unwrap(response);
  },

  getRoadmapModules: async (domainId) => {
    const response = await apiClient.get('/admin/roadmap/modules', {
      params: domainId ? { domainId } : {},
    });
    return unwrap(response) || [];
  },

  createRoadmapModule: async (payload) => {
    const response = await apiClient.post('/admin/roadmap/modules', payload);
    return unwrap(response);
  },

  updateRoadmapModule: async (moduleId, payload) => {
    const response = await apiClient.put(`/admin/roadmap/modules/${moduleId}`, payload);
    return unwrap(response);
  },

  deleteRoadmapModule: async (moduleId) => {
    const response = await apiClient.delete(`/admin/roadmap/modules/${moduleId}`);
    return unwrap(response);
  },

  getMentors: async () => {
    const response = await apiClient.get('/admin/mentors');
    return unwrap(response) || [];
  },

  createMentor: async (payload) => {
    const response = await apiClient.post('/admin/mentors', payload);
    return unwrap(response);
  },

  blockMentor: async (mentorId, payload) => {
    const response = await apiClient.put(`/admin/mentors/${mentorId}/block`, payload);
    return unwrap(response);
  },

  deleteMentor: async (mentorId) => {
    const response = await apiClient.delete(`/admin/mentors/${mentorId}`);
    return unwrap(response);
  },

  getResumes: async () => {
    const response = await apiClient.get('/admin/resumes');
    return unwrap(response) || [];
  },

  getResumeByUserId: async (userId) => {
    const response = await apiClient.get(`/admin/resumes/${userId}`);
    return unwrap(response);
  },

  getProgressSnapshot: async () => {
    const response = await apiClient.get('/admin/progress/snapshot');
    return unwrap(response) || [];
  },
};

export default adminApi;
