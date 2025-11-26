// API Configuration
const API_BASE_URL = 'http://localhost:5000/api';

// Generic API call function
async function apiCall(endpoint, options = {}) {
    const url = `${API_BASE_URL}${endpoint}`;
    const defaultOptions = {
        headers: {
            'Content-Type': 'application/json',
        },
    };

    const config = { ...defaultOptions, ...options };
    
    // Add auth token if available
    const token = localStorage.getItem('authToken');
    if (token) {
        config.headers['Authorization'] = `Bearer ${token}`;
    }

    try {
        const response = await fetch(url, config);
        const data = await response.json();
        
        if (!response.ok) {
            // Extract error message from response
            const errorMessage = data.error || data.message || `HTTP error! status: ${response.status}`;
            const error = new Error(errorMessage);
            error.statusCode = response.status;
            error.response = data;
            throw error;
        }
        
        return data;
    } catch (error) {
        console.error('API call failed:', error);
        // If it's already our custom error, re-throw it
        if (error.message && error.statusCode) {
            throw error;
        }
        // Otherwise, wrap it
        throw new Error(error.message || 'An unexpected error occurred');
    }
}

// Health Check
async function checkHealth() {
    return apiCall('/health');
}

// Export functions for use in other files
window.apiCall = apiCall;
window.checkHealth = checkHealth;

