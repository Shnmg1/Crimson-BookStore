// Authentication functions

// Register a new user
async function register(userData) {
    try {
        const response = await apiCall('/auth/register', {
            method: 'POST',
            body: JSON.stringify(userData)
        });
        
        if (response.success) {
            // Auto-login after registration
            const loginResponse = await login({
                username: userData.username,
                password: userData.password
            });
            
            return loginResponse;
        }
        
        return response;
    } catch (error) {
        console.error('Registration failed:', error);
        throw error;
    }
}

// Login user
async function login(credentials) {
    try {
        const response = await apiCall('/auth/login', {
            method: 'POST',
            body: JSON.stringify(credentials)
        });
        
        if (response.success && response.data.token) {
            // Store token in localStorage
            localStorage.setItem('authToken', response.data.token);
            localStorage.setItem('userData', JSON.stringify({
                userId: response.data.userId,
                username: response.data.username,
                userType: response.data.userType
            }));
            
            return response;
        }
        
        return response;
    } catch (error) {
        console.error('Login failed:', error);
        throw error;
    }
}

// Logout user
async function logout() {
    try {
        await apiCall('/auth/logout', {
            method: 'POST'
        });
    } catch (error) {
        console.error('Logout failed:', error);
    } finally {
        // Clear local storage
        localStorage.removeItem('authToken');
        localStorage.removeItem('userData');
    }
}

// Check if user is logged in
function isAuthenticated() {
    return localStorage.getItem('authToken') !== null;
}

// Get current user data
function getCurrentUser() {
    const userData = localStorage.getItem('userData');
    return userData ? JSON.parse(userData) : null;
}

// Make functions available globally
window.register = register;
window.login = login;
window.logout = logout;
window.isAuthenticated = isAuthenticated;
window.getCurrentUser = getCurrentUser;

