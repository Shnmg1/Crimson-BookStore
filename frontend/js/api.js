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
            // Handle authentication errors (401) - clear session and redirect to login
            if (response.status === 401) {
                // Clear authentication data
                localStorage.removeItem('authToken');
                localStorage.removeItem('userData');
                
                // Redirect to login page if we have the function available
                if (typeof showLoginPage === 'function') {
                    showAlert('Your session has expired. Please log in again.', 'warning');
                    showLoginPage();
                    // Return a rejected promise instead of throwing to prevent further error handling
                    return Promise.reject(new Error('Session expired'));
                } else {
                    // Fallback: reload the page which should redirect to login
                    window.location.reload();
                    return Promise.reject(new Error('Session expired'));
                }
            }
            
            throw new Error(data.error || `HTTP error! status: ${response.status}`);
        }
        
        return data;
    } catch (error) {
        console.error('API call failed:', error);
        throw error;
    }
}

// Health Check
async function checkHealth() {
    return apiCall('/health');
}

// Export functions for use in other files
window.apiCall = apiCall;
window.checkHealth = checkHealth;

