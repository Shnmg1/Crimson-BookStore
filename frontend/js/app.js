// Main application logic
document.addEventListener('DOMContentLoaded', async () => {
    console.log('Crimson BookStore app loaded');
    
    // Test API connection
    try {
        const health = await checkHealth();
        console.log('API Health:', health);
        
        if (health.database === 'connected') {
            console.log('✅ Database connection successful');
        } else {
            console.warn('⚠️ Database connection failed');
        }
    } catch (error) {
        console.error('❌ API connection failed:', error);
        showAlert('Unable to connect to API. Make sure the backend is running.', 'danger');
    }
    
    // Setup navigation
    setupNavigation();
    
    // Update navigation based on auth status
    updateNavigation();
});

function setupNavigation() {
    // Navigation click handlers
    document.getElementById('navHome')?.addEventListener('click', (e) => {
        e.preventDefault();
        showHomePage();
    });
    
    document.getElementById('navBooks')?.addEventListener('click', (e) => {
        e.preventDefault();
        showBooksPage();
    });
    
    document.getElementById('navCart')?.addEventListener('click', (e) => {
        e.preventDefault();
        showCartPage();
    });
    
    document.getElementById('navLogin')?.addEventListener('click', (e) => {
        e.preventDefault();
        showLoginPage();
    });
    
    document.getElementById('navLogout')?.addEventListener('click', async (e) => {
        e.preventDefault();
        await handleLogout();
    });
}

// Update navigation based on authentication status
function updateNavigation() {
    const isAuth = isAuthenticated();
    const user = getCurrentUser();
    
    // Show/hide login/logout
    const navLoginItem = document.getElementById('navLoginItem');
    const navUserItem = document.getElementById('navUserItem');
    const navCartItem = document.getElementById('navCartItem');
    
    if (isAuth && user) {
        // User is logged in
        navLoginItem.style.display = 'none';
        navUserItem.style.display = 'block';
        navCartItem.style.display = 'block';
        document.getElementById('navUserInfo').textContent = `Welcome, ${user.username}`;
    } else {
        // User is not logged in
        navLoginItem.style.display = 'block';
        navUserItem.style.display = 'none';
        navCartItem.style.display = 'none';
    }
}

// Handle logout
async function handleLogout() {
    try {
        await logout();
        updateNavigation();
        showHomePage();
        showAlert('Logged out successfully', 'success');
    } catch (error) {
        showAlert('Error logging out', 'danger');
    }
}

function showHomePage() {
    const app = document.getElementById('app');
    app.innerHTML = `
        <div class="text-center">
            <h1>Welcome to Crimson BookStore</h1>
            <p class="lead">Your source for used textbooks and reading materials</p>
            <button class="btn btn-primary btn-lg" onclick="showBooksPage()">Browse Books</button>
        </div>
    `;
}

function showBooksPage() {
    const app = document.getElementById('app');
    app.innerHTML = `
        <div class="row mb-3">
            <div class="col-md-8">
                <h2>Browse Books</h2>
            </div>
            <div class="col-md-4">
                <input type="text" class="form-control" id="searchInput" placeholder="Search books...">
            </div>
        </div>
        <div id="booksList" class="row">
            <div class="col-12 text-center">
                <p>Loading books...</p>
            </div>
        </div>
    `;
    
    // TODO: Load books from API
    loadBooks();
}

function showCartPage() {
    const app = document.getElementById('app');
    app.innerHTML = `
        <h2>Shopping Cart</h2>
        <div id="cartItems">
            <p>Your cart is empty.</p>
        </div>
    `;
    
    // TODO: Load cart from API
}

function showLoginPage() {
    const app = document.getElementById('app');
    app.innerHTML = `
        <div class="row justify-content-center">
            <div class="col-md-6">
                <div class="card">
                    <div class="card-body">
                        <h2 class="card-title">Login</h2>
                        <form id="loginForm">
                            <div class="mb-3">
                                <label for="loginUsername" class="form-label">Username</label>
                                <input type="text" class="form-control" id="loginUsername" required>
                            </div>
                            <div class="mb-3">
                                <label for="loginPassword" class="form-label">Password</label>
                                <input type="password" class="form-control" id="loginPassword" required>
                            </div>
                            <button type="submit" class="btn btn-primary w-100">Login</button>
                            <div class="mt-3 text-center">
                                <p>Don't have an account? <a href="#" onclick="showRegisterPage(); return false;">Register here</a></p>
                            </div>
                        </form>
                    </div>
                </div>
            </div>
        </div>
    `;
    
    // Handle login form submission
    document.getElementById('loginForm').addEventListener('submit', handleLogin);
}

function showRegisterPage() {
    const app = document.getElementById('app');
    app.innerHTML = `
        <div class="row justify-content-center">
            <div class="col-md-8">
                <div class="card">
                    <div class="card-body">
                        <h2 class="card-title">Register</h2>
                        <form id="registerForm">
                            <div class="row">
                                <div class="col-md-6 mb-3">
                                    <label for="regUsername" class="form-label">Username</label>
                                    <input type="text" class="form-control" id="regUsername" required>
                                </div>
                                <div class="col-md-6 mb-3">
                                    <label for="regEmail" class="form-label">Email</label>
                                    <input type="email" class="form-control" id="regEmail" required>
                                </div>
                            </div>
                            <div class="row">
                                <div class="col-md-6 mb-3">
                                    <label for="regPassword" class="form-label">Password</label>
                                    <input type="password" class="form-control" id="regPassword" required>
                                </div>
                                <div class="col-md-6 mb-3">
                                    <label for="regUserType" class="form-label">Account Type</label>
                                    <select class="form-select" id="regUserType">
                                        <option value="Customer">Customer</option>
                                        <option value="Admin">Admin</option>
                                    </select>
                                </div>
                            </div>
                            <div class="row">
                                <div class="col-md-6 mb-3">
                                    <label for="regFirstName" class="form-label">First Name</label>
                                    <input type="text" class="form-control" id="regFirstName" required>
                                </div>
                                <div class="col-md-6 mb-3">
                                    <label for="regLastName" class="form-label">Last Name</label>
                                    <input type="text" class="form-control" id="regLastName" required>
                                </div>
                            </div>
                            <div class="row">
                                <div class="col-md-6 mb-3">
                                    <label for="regPhone" class="form-label">Phone (Optional)</label>
                                    <input type="tel" class="form-control" id="regPhone">
                                </div>
                                <div class="col-md-6 mb-3">
                                    <label for="regAddress" class="form-label">Address (Optional)</label>
                                    <input type="text" class="form-control" id="regAddress">
                                </div>
                            </div>
                            <button type="submit" class="btn btn-primary w-100">Register</button>
                            <div class="mt-3 text-center">
                                <p>Already have an account? <a href="#" onclick="showLoginPage(); return false;">Login here</a></p>
                            </div>
                        </form>
                    </div>
                </div>
            </div>
        </div>
    `;
    
    // Handle register form submission
    document.getElementById('registerForm').addEventListener('submit', handleRegister);
}

// Handle login form submission
async function handleLogin(e) {
    e.preventDefault();
    
    const username = document.getElementById('loginUsername').value;
    const password = document.getElementById('loginPassword').value;
    
    try {
        const response = await login({ username, password });
        
        if (response.success) {
            showAlert('Login successful!', 'success');
            updateNavigation();
            showHomePage();
        } else {
            showAlert(response.error || 'Login failed', 'danger');
        }
    } catch (error) {
        showAlert(error.message || 'Login failed. Please try again.', 'danger');
    }
}

// Handle register form submission
async function handleRegister(e) {
    e.preventDefault();
    
    const userData = {
        username: document.getElementById('regUsername').value,
        email: document.getElementById('regEmail').value,
        password: document.getElementById('regPassword').value,
        firstName: document.getElementById('regFirstName').value,
        lastName: document.getElementById('regLastName').value,
        phone: document.getElementById('regPhone').value || null,
        address: document.getElementById('regAddress').value || null,
        userType: document.getElementById('regUserType').value
    };
    
    try {
        const response = await register(userData);
        
        if (response.success) {
            showAlert('Registration successful! You are now logged in.', 'success');
            updateNavigation();
            showHomePage();
        } else {
            showAlert(response.error || 'Registration failed', 'danger');
        }
    } catch (error) {
        showAlert(error.message || 'Registration failed. Please try again.', 'danger');
    }
}

function loadBooks() {
    // TODO: Implement book loading
    console.log('Loading books...');
}

function showAlert(message, type = 'info') {
    const alertDiv = document.createElement('div');
    alertDiv.className = `alert alert-${type} alert-dismissible fade show`;
    alertDiv.innerHTML = `
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;
    
    const app = document.getElementById('app');
    app.insertBefore(alertDiv, app.firstChild);
    
    // Auto-dismiss after 5 seconds
    setTimeout(() => {
        alertDiv.remove();
    }, 5000);
}

// Make functions available globally
window.showHomePage = showHomePage;
window.showBooksPage = showBooksPage;
window.showCartPage = showCartPage;
window.showLoginPage = showLoginPage;
window.showRegisterPage = showRegisterPage;

