import { apiClient } from './axios';

// Helper function to unwrap response Data
const unwrapData = (response, endpoint = 'careerGuidance') => {
  const data = response.data?.Data ?? response.data?.data ?? response.data;
  console.log(`[${endpoint}] Response unwrapped:`, data);
  return data;
};

export const careerGuidanceApi = {
  /**
   * Get career paths hierarchy tree by academic stage/stream
   */
  getPathsTree: async (streamType) => {
    const response = await apiClient.get('/career-guidance/paths', {
      params: { streamType }
    });
    return unwrapData(response, 'getPathsTree');
  },

  /**
   * Get details of a single career path
   */
  getPathDetails: async (id) => {
    const response = await apiClient.get(`/career-guidance/paths/${id}`);
    return unwrapData(response, 'getPathDetails');
  },

  /**
   * Get adaptive quiz questions filtered by stream
   */
  getQuizQuestions: async (streamType) => {
    const response = await apiClient.get('/career-guidance/quiz/questions', {
      params: { streamType }
    });
    return unwrapData(response, 'getQuizQuestions');
  },

  /**
   * Submit quiz answers and retrieve recommendation profile
   */
  submitQuizAnswers: async (payload) => {
    const response = await apiClient.post('/career-guidance/quiz/submit', payload);
    return unwrapData(response, 'submitQuizAnswers');
  },

  /**
   * Get user career recommendation profile
   */
  getUserProfile: async () => {
    const response = await apiClient.get('/career-guidance/profile');
    return unwrapData(response, 'getUserProfile');
  },

  /**
   * Toggle bookmarking a career path
   */
  toggleBookmark: async (pathId) => {
    const response = await apiClient.post(`/career-guidance/bookmarks/${pathId}`);
    return unwrapData(response, 'toggleBookmark');
  },

  /**
   * Get user bookmarked career paths
   */
  getBookmarkedPaths: async () => {
    const response = await apiClient.get('/career-guidance/bookmarks');
    return unwrapData(response, 'getBookmarkedPaths');
  },

  /**
   * Forecast salary based on target year, location and skill level sliders
   */
  forecastSalary: async (payload) => {
    const response = await apiClient.post('/career-guidance/forecast', payload);
    return unwrapData(response, 'forecastSalary');
  },

  /**
   * Retrieve aggregated metrics and trends charts data
   */
  getAnalytics: async () => {
    const response = await apiClient.get('/career-guidance/analytics');
    return unwrapData(response, 'getAnalytics');
  },

  /**
   * Compare two career paths side-by-side
   */
  compareCareers: async (id1, id2) => {
    const response = await apiClient.get('/career-guidance/compare', {
      params: { id1, id2 }
    });
    return unwrapData(response, 'compareCareers');
  }
};

export default careerGuidanceApi;
