import { apiClient } from './axios';

export const roadmapApi = {
    // Get dynamically recommended roadmap based on ML quiz results
    getMyInterestRoadmap: () => apiClient.get('/roadmap/my-interest'),

    // Get all available roadmap domains the user can switch to
    getRoadmapOptions: () => apiClient.get('/roadmap/options'),

    // Get roadmap for a selected domain
    getRoadmapByDomain: (domainId) => apiClient.get(`/roadmap/domain/${domainId}`),

    // Mark a specific roadmap step as completed
    markStepComplete: (stepId) => apiClient.post(`/roadmap/mark-done/${stepId}`)
};
