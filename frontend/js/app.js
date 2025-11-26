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
    
    // Load cart badge count if authenticated
    if (isAuthenticated() && typeof refreshCartBadge === 'function') {
        refreshCartBadge();
    }
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
    
    document.getElementById('navOrders')?.addEventListener('click', (e) => {
        e.preventDefault();
        if (typeof showOrderHistoryPage === 'function') {
            showOrderHistoryPage();
        }
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
    const navOrdersItem = document.getElementById('navOrdersItem');
    
    if (isAuth && user) {
        // User is logged in
        navLoginItem.style.display = 'none';
        navUserItem.style.display = 'block';
        navCartItem.style.display = 'block';
        if (navOrdersItem) navOrdersItem.style.display = 'block';
        document.getElementById('navUserInfo').textContent = `Welcome, ${user.username}`;
    } else {
        // User is not logged in
        navLoginItem.style.display = 'block';
        navUserItem.style.display = 'none';
        navCartItem.style.display = 'none';
        if (navOrdersItem) navOrdersItem.style.display = 'none';
    }
}

// Handle logout
async function handleLogout() {
    try {
        await logout();
        updateNavigation();
        // Reset cart badge
        if (typeof updateCartBadge === 'function') {
            updateCartBadge(0);
        }
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
                <div class="input-group">
                    <input type="text" class="form-control" id="searchInput" placeholder="Search books..." onkeyup="handleSearchInput(event)">
                    <button class="btn btn-outline-secondary" type="button" onclick="handleSearch()">
                        <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" class="bi bi-search" viewBox="0 0 16 16">
                            <path d="M11.742 10.344a6.5 6.5 0 1 0-1.397 1.398h-.001c.03.04.062.078.098.115l3.85 3.85a1 1 0 0 0 1.415-1.414l-3.85-3.85a1.007 1.007 0 0 0-.115-.1zM12 6.5a5.5 5.5 0 1 1-11 0 5.5 5.5 0 0 1 11 0z"/>
                        </svg>
                    </button>
                    <button class="btn btn-outline-secondary" type="button" onclick="clearSearch()">Clear</button>
                </div>
            </div>
        </div>
        <div id="booksList" class="row">
            <div class="col-12 text-center">
                <p>Loading books...</p>
            </div>
        </div>
    `;
    
    // Load books from API
    loadBooks();
    
    // Setup search input enter key handler
    setTimeout(() => {
        const searchInput = document.getElementById('searchInput');
        if (searchInput) {
            searchInput.addEventListener('keypress', (e) => {
                if (e.key === 'Enter') {
                    handleSearch();
                }
            });
        }
    }, 100);
}

function showCartPage() {
    if (!isAuthenticated()) {
        showAlert('Please log in to view your cart', 'warning');
        showLoginPage();
        return;
    }

    const app = document.getElementById('app');
    app.innerHTML = `
        <div class="row mb-3">
            <div class="col">
                <h2>Shopping Cart</h2>
            </div>
        </div>
        <div id="cartItems">
            <div class="text-center">
                <div class="spinner-border" role="status">
                    <span class="visually-hidden">Loading...</span>
                </div>
            </div>
        </div>
    `;
    
    // Load cart from API
    loadCart();
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
            // Refresh cart badge after login
            if (typeof refreshCartBadge === 'function') {
                refreshCartBadge();
            }
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
            // Refresh cart badge after registration
            if (typeof refreshCartBadge === 'function') {
                refreshCartBadge();
            }
            showHomePage();
        } else {
            showAlert(response.error || 'Registration failed', 'danger');
        }
    } catch (error) {
        showAlert(error.message || 'Registration failed. Please try again.', 'danger');
    }
}

// Handle search input (real-time filtering with debounce)
let searchTimeout;
function handleSearchInput(event) {
    clearTimeout(searchTimeout);
    const searchTerm = event.target.value.trim();
    
    // Debounce: wait 500ms after user stops typing
    searchTimeout = setTimeout(() => {
        if (searchTerm.length === 0 || searchTerm.length >= 2) {
            loadBooks(searchTerm || null);
        }
    }, 500);
}

// Handle search button click
function handleSearch() {
    const searchInput = document.getElementById('searchInput');
    if (searchInput) {
        const searchTerm = searchInput.value.trim();
        loadBooks(searchTerm || null);
    }
}

// Clear search
function clearSearch() {
    const searchInput = document.getElementById('searchInput');
    if (searchInput) {
        searchInput.value = '';
        loadBooks();
    }
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
window.handleSearchInput = handleSearchInput;
window.handleSearch = handleSearch;
window.clearSearch = clearSearch;

