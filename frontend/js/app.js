// Store API wrapper reference from admin.js before it gets overwritten
// admin.js loads before app.js, so window.updateOrderStatus should be available
// We capture it here at the top level before defining our handler function
const updateOrderStatusAPI = window.updateOrderStatus;

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
    
    document.getElementById('navSell')?.addEventListener('click', (e) => {
        e.preventDefault();
        if (typeof showMySubmissionsPage === 'function') {
            showMySubmissionsPage();
        }
    });
    
    document.getElementById('navPaymentMethods')?.addEventListener('click', (e) => {
        e.preventDefault();
        if (typeof showPaymentMethodsPage === 'function') {
            showPaymentMethodsPage();
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
    
    // Admin navigation
    document.getElementById('navAdminDashboard')?.addEventListener('click', (e) => {
        e.preventDefault();
        showAdminDashboard();
    });
    
    document.getElementById('navAdminBooks')?.addEventListener('click', (e) => {
        e.preventDefault();
        showAdminBooksPage();
    });
    
    document.getElementById('navAdminSubmissions')?.addEventListener('click', (e) => {
        e.preventDefault();
        showAdminSubmissionsPage();
    });
    
    document.getElementById('navAdminOrders')?.addEventListener('click', (e) => {
        e.preventDefault();
        showAdminOrdersPage();
    });
    
    document.getElementById('navAdminUsers')?.addEventListener('click', (e) => {
        e.preventDefault();
        showAdminUsersPage();
    });
}

// Update navigation based on authentication status
function updateNavigation() {
    const isAuth = isAuthenticated();
    const user = getCurrentUser();
    
    const customerSidebar = document.getElementById('customerSidebar');
    const adminSidebar = document.getElementById('adminSidebar');
    const sidebarLoginSection = document.getElementById('sidebarLoginSection');
    const sidebarUserSection = document.getElementById('sidebarUserSection');
    
    // Sidebar links
    const sidebarCart = document.getElementById('sidebarCart');
    const sidebarOrders = document.getElementById('sidebarOrders');
    const sidebarSell = document.getElementById('sidebarSell');
    const sidebarPayment = document.getElementById('sidebarPayment');
    const sidebarUserInfo = document.getElementById('sidebarUserInfo');
    const adminSidebarUserInfo = document.getElementById('adminSidebarUserInfo');
    
    if (isAuth && user) {
        // User is logged in
        sidebarLoginSection.style.display = 'none';
        sidebarUserSection.style.display = 'block';
        
        // Show admin sidebar if user is admin
        if (user.userType === 'Admin') {
            customerSidebar.style.display = 'none';
            adminSidebar.style.display = 'block';
            if (adminSidebarUserInfo) {
                adminSidebarUserInfo.textContent = user.username;
            }
        } else {
            // Regular customer
            customerSidebar.style.display = 'block';
            adminSidebar.style.display = 'none';
            if (sidebarUserInfo) {
                sidebarUserInfo.textContent = user.username;
            }
            
            // Show customer links
            if (sidebarCart) sidebarCart.style.display = 'flex';
            if (sidebarOrders) sidebarOrders.style.display = 'flex';
            if (sidebarSell) sidebarSell.style.display = 'flex';
            if (sidebarPayment) sidebarPayment.style.display = 'flex';
        }
    } else {
        // User is not logged in
        customerSidebar.style.display = 'block';
        adminSidebar.style.display = 'none';
        sidebarLoginSection.style.display = 'block';
        sidebarUserSection.style.display = 'none';
        
        // Hide customer links
        if (sidebarCart) sidebarCart.style.display = 'none';
        if (sidebarOrders) sidebarOrders.style.display = 'none';
        if (sidebarSell) sidebarSell.style.display = 'none';
        if (sidebarPayment) sidebarPayment.style.display = 'none';
    }
    
    // Update cart badge in sidebar
    if (typeof refreshCartBadge === 'function') {
        refreshCartBadge();
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
        <div class="text-center" style="padding: 4rem 2rem;">
            <h1 style="color: var(--text-light); margin-bottom: 1rem;">Welcome to Crimson BookStore</h1>
            <p style="color: var(--text-muted); font-size: 1.125rem; margin-bottom: 2rem;">Your source for used textbooks and reading materials</p>
            <button class="btn-ua-primary" onclick="showBooksPage()" style="font-size: 1.125rem; padding: 0.75rem 2rem;">Browse Books</button>
        </div>
    `;
    
    // Update active sidebar link
    document.querySelectorAll('.ua-sidebar-link').forEach(link => link.classList.remove('active'));
    const homeLink = document.getElementById('sidebarHome');
    if (homeLink) homeLink.classList.add('active');
}

function showBooksPage() {
    const app = document.getElementById('app');
    app.innerHTML = `
        <div class="ua-card" style="margin-bottom: 1.5rem;">
            <div style="display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 1rem;">
                <h2 class="ua-card-title" style="margin: 0;">Browse Books</h2>
                <div style="display: flex; gap: 0.5rem; flex: 1; max-width: 500px;">
                    <input type="text" class="ua-search-bar" id="searchInput" placeholder="Search for what you are looking for..." onkeyup="handleSearchInput(event)" style="margin: 0;">
                    <button class="btn-ua-primary" type="button" onclick="handleSearch()" style="padding: 0.75rem 1rem;">
                        <i class="bi bi-search"></i>
                    </button>
                    <button class="btn-ua-secondary" type="button" onclick="clearSearch()" style="padding: 0.75rem 1rem;">
                        <i class="bi bi-x"></i>
                    </button>
                </div>
            </div>
        </div>
        <div id="booksList" style="display: grid; grid-template-columns: repeat(auto-fill, minmax(300px, 1fr)); gap: 1.5rem;">
            <div class="text-center" style="grid-column: 1 / -1;">
                <div class="ua-spinner"></div>
            </div>
        </div>
    `;
    
    // Update active sidebar link
    document.querySelectorAll('.ua-sidebar-link').forEach(link => link.classList.remove('active'));
    const booksLink = document.getElementById('sidebarBooks');
    if (booksLink) booksLink.classList.add('active');
    
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
        <div id="cartItems">
            <div class="text-center">
                <div class="ua-spinner"></div>
            </div>
        </div>
    `;
    
    // Update active sidebar link
    document.querySelectorAll('.ua-sidebar-link').forEach(link => link.classList.remove('active'));
    const cartLink = document.getElementById('sidebarCart');
    if (cartLink) cartLink.classList.add('active');
    
    // Load cart from API
    loadCart();
}

function showLoginPage() {
    const app = document.getElementById('app');
    app.innerHTML = `
        <div style="max-width: 500px; margin: 0 auto;">
            <div class="ua-card">
                <div class="ua-card-header">
                    <h2 class="ua-card-title">Login</h2>
                </div>
                <form id="loginForm">
                    <div style="margin-bottom: 1.5rem;">
                        <label for="loginUsername" class="ua-form-label">Username</label>
                        <input type="text" class="ua-form-control" id="loginUsername" required>
                    </div>
                    <div style="margin-bottom: 1.5rem;">
                        <label for="loginPassword" class="ua-form-label">Password</label>
                        <input type="password" class="ua-form-control" id="loginPassword" required>
                    </div>
                    <button type="submit" class="btn-ua-primary" style="width: 100%; padding: 0.75rem;">Login</button>
                    <div style="margin-top: 1.5rem; text-align: center;">
                        <p style="color: var(--text-muted);">Don't have an account? <a href="#" onclick="showRegisterPage(); return false;" style="color: var(--ua-crimson);">Register here</a></p>
                    </div>
                </form>
            </div>
        </div>
    `;
    
    // Handle login form submission
    document.getElementById('loginForm').addEventListener('submit', handleLogin);
}

function showRegisterPage() {
    const app = document.getElementById('app');
    app.innerHTML = `
        <div style="max-width: 800px; margin: 0 auto;">
            <div class="ua-card">
                <div class="ua-card-header">
                    <h2 class="ua-card-title">Register</h2>
                </div>
                <form id="registerForm">
                    <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(250px, 1fr)); gap: 1rem; margin-bottom: 1rem;">
                        <div>
                            <label for="regUsername" class="ua-form-label">Username</label>
                            <input type="text" class="ua-form-control" id="regUsername" required>
                        </div>
                        <div>
                            <label for="regEmail" class="ua-form-label">Email</label>
                            <input type="email" class="ua-form-control" id="regEmail" required>
                        </div>
                    </div>
                    <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(250px, 1fr)); gap: 1rem; margin-bottom: 1rem;">
                        <div>
                            <label for="regPassword" class="ua-form-label">Password</label>
                            <input type="password" class="ua-form-control" id="regPassword" required>
                        </div>
                        <div>
                            <label for="regUserType" class="ua-form-label">Account Type</label>
                            <select class="ua-form-control" id="regUserType">
                                <option value="Customer">Customer</option>
                                <option value="Admin">Admin</option>
                            </select>
                        </div>
                    </div>
                    <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(250px, 1fr)); gap: 1rem; margin-bottom: 1rem;">
                        <div>
                            <label for="regFirstName" class="ua-form-label">First Name</label>
                            <input type="text" class="ua-form-control" id="regFirstName" required>
                        </div>
                        <div>
                            <label for="regLastName" class="ua-form-label">Last Name</label>
                            <input type="text" class="ua-form-control" id="regLastName" required>
                        </div>
                    </div>
                    <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(250px, 1fr)); gap: 1rem; margin-bottom: 1.5rem;">
                        <div>
                            <label for="regPhone" class="ua-form-label">Phone (Optional)</label>
                            <input type="tel" class="ua-form-control" id="regPhone">
                        </div>
                        <div>
                            <label for="regAddress" class="ua-form-label">Address (Optional)</label>
                            <input type="text" class="ua-form-control" id="regAddress">
                        </div>
                    </div>
                    <button type="submit" class="btn-ua-primary" style="width: 100%; padding: 0.75rem;">Register</button>
                    <div style="margin-top: 1.5rem; text-align: center;">
                        <p style="color: var(--text-muted);">Already have an account? <a href="#" onclick="showLoginPage(); return false;" style="color: var(--ua-crimson);">Login here</a></p>
                    </div>
                </form>
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
    const alertClass = {
        'success': 'ua-alert-success',
        'danger': 'ua-alert-danger',
        'warning': 'ua-alert-warning',
        'info': 'ua-alert-info'
    }[type] || 'ua-alert-info';
    
    alertDiv.className = `ua-alert ${alertClass}`;
    alertDiv.style.cssText = 'position: fixed; top: 80px; right: 2rem; z-index: 9999; min-width: 300px; max-width: 500px; box-shadow: 0 4px 6px rgba(0,0,0,0.3);';
    alertDiv.innerHTML = `
        ${message}
        <button type="button" class="btn-close btn-close-white" data-bs-dismiss="alert" style="float: right; margin-left: 1rem;"></button>
    `;
    
    document.body.appendChild(alertDiv);
    
    // Auto-dismiss after 5 seconds
    setTimeout(() => {
        if (alertDiv.parentNode) {
            alertDiv.remove();
        }
    }, 5000);
}

// Admin Pages
function showAdminDashboard() {
    if (!isAuthenticated() || getCurrentUser()?.userType !== 'Admin') {
        showAlert('Admin access required', 'danger');
        showHomePage();
        return;
    }
    
    const app = document.getElementById('app');
    app.innerHTML = `
        <div class="row mb-4">
            <div class="col">
                <h2>Admin Dashboard</h2>
            </div>
        </div>
        <div class="row">
            <div class="col-md-3 mb-3">
                <div class="card text-center">
                    <div class="card-body">
                        <h5 class="card-title">Books</h5>
                        <p class="card-text"><a href="#" onclick="showAdminBooksPage(); return false;">Manage Books</a></p>
                    </div>
                </div>
            </div>
            <div class="col-md-3 mb-3">
                <div class="card text-center">
                    <div class="card-body">
                        <h5 class="card-title">Submissions</h5>
                        <p class="card-text"><a href="#" onclick="showAdminSubmissionsPage(); return false;">Review Submissions</a></p>
                    </div>
                </div>
            </div>
            <div class="col-md-3 mb-3">
                <div class="card text-center">
                    <div class="card-body">
                        <h5 class="card-title">Orders</h5>
                        <p class="card-text"><a href="#" onclick="showAdminOrdersPage(); return false;">Manage Orders</a></p>
                    </div>
                </div>
            </div>
            <div class="col-md-3 mb-3">
                <div class="card text-center">
                    <div class="card-body">
                        <h5 class="card-title">Users</h5>
                        <p class="card-text"><a href="#" onclick="showAdminUsersPage(); return false;">View Users</a></p>
                    </div>
                </div>
            </div>
        </div>
    `;
}

function showAdminBooksPage() {
    if (!isAuthenticated() || getCurrentUser()?.userType !== 'Admin') {
        showAlert('Admin access required', 'danger');
        showHomePage();
        return;
    }
    
    const app = document.getElementById('app');
    app.innerHTML = `
        <div class="row mb-3">
            <div class="col">
                <h2>Book Management</h2>
            </div>
            <div class="col-auto">
                <button class="btn btn-primary" onclick="showAddBookModal()">Add New Book</button>
            </div>
        </div>
        <div id="adminBooksList">
            <div class="text-center">
                <div class="spinner-border" role="status">
                    <span class="visually-hidden">Loading...</span>
                </div>
            </div>
        </div>
    `;
    
    loadAdminBooks();
}

function showAdminSubmissionsPage() {
    if (!isAuthenticated() || getCurrentUser()?.userType !== 'Admin') {
        showAlert('Admin access required', 'danger');
        showHomePage();
        return;
    }
    
    const app = document.getElementById('app');
    app.innerHTML = `
        <div class="row mb-3">
            <div class="col">
                <h2>Sell Submission Management</h2>
            </div>
            <div class="col-auto">
                <select class="form-select d-inline-block" id="submissionStatusFilter" style="width: auto;" onchange="loadAdminSubmissions()">
                    <option value="">All Status</option>
                    <option value="Pending Review">Pending Review</option>
                    <option value="Approved">Approved</option>
                    <option value="Rejected">Rejected</option>
                </select>
            </div>
        </div>
        <div id="adminSubmissionsList">
            <div class="text-center">
                <div class="spinner-border" role="status">
                    <span class="visually-hidden">Loading...</span>
                </div>
            </div>
        </div>
    `;
    
    loadAdminSubmissions();
}

function showAdminOrdersPage() {
    if (!isAuthenticated() || getCurrentUser()?.userType !== 'Admin') {
        showAlert('Admin access required', 'danger');
        showHomePage();
        return;
    }
    
    const app = document.getElementById('app');
    app.innerHTML = `
        <div class="row mb-3">
            <div class="col">
                <h2>Order Management</h2>
            </div>
            <div class="col-auto">
                <select class="form-select d-inline-block" id="orderStatusFilter" style="width: auto;" onchange="loadAdminOrders()">
                    <option value="">All Status</option>
                    <option value="New">New</option>
                    <option value="Processing">Processing</option>
                    <option value="Fulfilled">Fulfilled</option>
                    <option value="Cancelled">Cancelled</option>
                </select>
            </div>
        </div>
        <div id="adminOrdersList">
            <div class="text-center">
                <div class="spinner-border" role="status">
                    <span class="visually-hidden">Loading...</span>
                </div>
            </div>
        </div>
    `;
    
    loadAdminOrders();
}

function showAdminUsersPage() {
    if (!isAuthenticated() || getCurrentUser()?.userType !== 'Admin') {
        showAlert('Admin access required', 'danger');
        showHomePage();
        return;
    }
    
    const app = document.getElementById('app');
    app.innerHTML = `
        <div class="row mb-3">
            <div class="col">
                <h2>User Management</h2>
            </div>
            <div class="col-auto">
                <select class="form-select d-inline-block" id="userTypeFilter" style="width: auto;" onchange="loadAdminUsers()">
                    <option value="">All Users</option>
                    <option value="Customer">Customers</option>
                    <option value="Admin">Admins</option>
                </select>
            </div>
        </div>
        <div id="adminUsersList">
            <div class="text-center">
                <div class="spinner-border" role="status">
                    <span class="visually-hidden">Loading...</span>
                </div>
            </div>
        </div>
    `;
    
    loadAdminUsers();
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
window.showAdminDashboard = showAdminDashboard;
window.showAdminBooksPage = showAdminBooksPage;
window.showAdminSubmissionsPage = showAdminSubmissionsPage;
window.showAdminOrdersPage = showAdminOrdersPage;
window.showAdminUsersPage = showAdminUsersPage;

// Admin Data Loading Functions
async function loadAdminBooks() {
    try {
        const response = await getAdminBooks();
        const booksList = document.getElementById('adminBooksList');
        
        if (response.success && response.data) {
            if (response.data.length === 0) {
                booksList.innerHTML = '<p class="text-center">No books found.</p>';
                return;
            }
            
            let html = '<div class="table-responsive"><table class="table table-striped"><thead><tr>';
            html += '<th>ISBN</th><th>Title</th><th>Author</th><th>Edition</th><th>Price Range</th><th>Stock</th><th>Actions</th>';
            html += '</tr></thead><tbody>';
            
            response.data.forEach(book => {
                html += `<tr>`;
                html += `<td>${book.isbn}</td>`;
                html += `<td>${book.title}</td>`;
                html += `<td>${book.author}</td>`;
                html += `<td>${book.edition}</td>`;
                html += `<td>$${book.minPrice.toFixed(2)} - $${book.maxPrice.toFixed(2)}</td>`;
                html += `<td>${book.availableCount} copies</td>`;
                html += `<td><button class="btn btn-sm btn-danger" onclick="handleDeleteBook('${book.isbn}', '${book.edition}')">Delete</button></td>`;
                html += `</tr>`;
            });
            
            html += '</tbody></table></div>';
            booksList.innerHTML = html;
        } else {
            booksList.innerHTML = '<p class="text-center text-danger">Error loading books.</p>';
        }
    } catch (error) {
        document.getElementById('adminBooksList').innerHTML = `<p class="text-center text-danger">Error: ${error.message}</p>`;
    }
}

async function loadAdminSubmissions() {
    try {
        const statusFilter = document.getElementById('submissionStatusFilter')?.value || '';
        const response = await getAdminSubmissions(statusFilter);
        const submissionsList = document.getElementById('adminSubmissionsList');
        
        if (response.success && response.data) {
            if (response.data.length === 0) {
                submissionsList.innerHTML = '<p class="text-center">No submissions found.</p>';
                return;
            }
            
            let html = '<div class="row">';
            
            // Fetch details for each submission to get negotiations
            const submissionsWithDetails = await Promise.all(
                response.data.map(async (submission) => {
                    try {
                        const detailsResponse = await window.getAdminSubmissionDetails(submission.submissionId);
                        if (detailsResponse.success && detailsResponse.data) {
                            return {
                                ...submission,
                                negotiations: detailsResponse.data.negotiations || []
                            };
                        }
                    } catch (error) {
                        console.error(`Error fetching details for submission ${submission.submissionId}:`, error);
                    }
                    return { ...submission, negotiations: [] };
                })
            );
            
            submissionsWithDetails.forEach(submission => {
                // Get latest negotiation price if available
                const latestNegotiation = submission.negotiations && submission.negotiations.length > 0
                    ? submission.negotiations[submission.negotiations.length - 1]
                    : null;
                
                // Check if there's an accepted negotiation
                const hasAcceptedNegotiation = submission.negotiations && submission.negotiations.some(n => n.offerStatus === 'Accepted');
                
                const currentPrice = latestNegotiation 
                    ? latestNegotiation.offeredPrice 
                    : submission.askingPrice;
                
                const priceLabel = latestNegotiation 
                    ? (latestNegotiation.offeredBy === 'Admin' ? 'Your Latest Offer' : 'Customer Counter-Offer')
                    : 'Asking Price';
                
                // Show approve button if: Pending Review (initial approval) OR Approved with accepted negotiation (customer accepted offer)
                // BUT NOT if status is Completed (already processed)
                const canApprove = (submission.status === 'Pending Review' || 
                    (submission.status === 'Approved' && hasAcceptedNegotiation)) &&
                    submission.status !== 'Completed';
                
                html += `
                    <div class="col-md-6 mb-3">
                        <div class="card">
                            <div class="card-body">
                                <h5 class="card-title">${escapeHtml(submission.title)}</h5>
                                <p class="card-text">
                                    <strong>User:</strong> ${escapeHtml(submission.username)}<br>
                                    <strong>ISBN:</strong> ${escapeHtml(submission.isbn)}<br>
                                    <strong>${priceLabel}:</strong> <span class="text-primary fw-bold">$${currentPrice.toFixed(2)}</span><br>
                                    ${latestNegotiation ? `<small class="text-muted">Original asking: $${submission.askingPrice.toFixed(2)}</small><br>` : ''}
                                    <strong>Status:</strong> <span class="badge bg-${getStatusBadgeColor(submission.status)}">${escapeHtml(submission.status)}</span><br>
                                    <strong>Submitted:</strong> ${new Date(submission.submissionDate).toLocaleDateString()}
                                </p>
                                <div class="d-flex gap-2 flex-wrap">
                                    <button class="btn btn-sm btn-info" onclick="showAdminSubmissionDetails(${submission.submissionId})">
                                        View Details
                                    </button>
                                    ${submission.status === 'Pending Review' ? `
                                        <button class="btn btn-sm btn-primary" onclick="showNegotiateModal(${submission.submissionId})">Negotiate</button>
                                        <button class="btn btn-sm btn-success" onclick="showApproveModal(${submission.submissionId})">Approve</button>
                                        <button class="btn btn-sm btn-danger" onclick="handleRejectSubmission(${submission.submissionId})">Reject</button>
                                    ` : canApprove ? `
                                        <button class="btn btn-sm btn-success" onclick="showApproveModal(${submission.submissionId})">Approve</button>
                                    ` : ''}
                                </div>
                            </div>
                        </div>
                    </div>
                `;
            });
            html += '</div>';
            submissionsList.innerHTML = html;
        } else {
            submissionsList.innerHTML = '<p class="text-center text-danger">Error loading submissions.</p>';
        }
    } catch (error) {
        document.getElementById('adminSubmissionsList').innerHTML = `<p class="text-center text-danger">Error: ${error.message}</p>`;
    }
}

async function loadAdminOrders() {
    try {
        const statusFilter = document.getElementById('orderStatusFilter')?.value || '';
        const response = await getAdminOrders(statusFilter);
        const ordersList = document.getElementById('adminOrdersList');
        
        if (response.success && response.data) {
            if (response.data.length === 0) {
                ordersList.innerHTML = '<p class="text-center">No orders found.</p>';
                return;
            }
            
            let html = '<div class="table-responsive"><table class="table table-striped"><thead><tr>';
            html += '<th>Order ID</th><th>User</th><th>Date</th><th>Status</th><th>Total</th><th>Items</th><th>Actions</th>';
            html += '</tr></thead><tbody>';
            
            response.data.forEach(order => {
                html += `<tr>`;
                html += `<td>#${order.orderId}</td>`;
                html += `<td>${order.username}</td>`;
                html += `<td>${new Date(order.orderDate).toLocaleDateString()}</td>`;
                html += `<td><span class="badge bg-${getStatusBadgeColor(order.status)}">${order.status}</span></td>`;
                html += `<td>$${order.totalAmount.toFixed(2)}</td>`;
                html += `<td>${order.itemCount}</td>`;
                html += `<td>`;
                if (order.status === 'New') {
                    html += `<button class="btn btn-sm btn-primary" onclick="updateOrderStatus(${order.orderId}, 'Processing')">Process</button> `;
                    html += `<button class="btn btn-sm btn-danger" onclick="updateOrderStatus(${order.orderId}, 'Cancelled')">Cancel</button>`;
                } else if (order.status === 'Processing') {
                    html += `<button class="btn btn-sm btn-success" onclick="updateOrderStatus(${order.orderId}, 'Fulfilled')">Fulfill</button> `;
                    html += `<button class="btn btn-sm btn-danger" onclick="updateOrderStatus(${order.orderId}, 'Cancelled')">Cancel</button>`;
                }
                html += `</td>`;
                html += `</tr>`;
            });
            
            html += '</tbody></table></div>';
            ordersList.innerHTML = html;
        } else {
            ordersList.innerHTML = '<p class="text-center text-danger">Error loading orders.</p>';
        }
    } catch (error) {
        document.getElementById('adminOrdersList').innerHTML = `<p class="text-center text-danger">Error: ${error.message}</p>`;
    }
}

async function loadAdminUsers() {
    try {
        const userTypeFilter = document.getElementById('userTypeFilter')?.value || '';
        const response = await getAdminUsers(userTypeFilter);
        const usersList = document.getElementById('adminUsersList');
        
        if (response.success && response.data) {
            if (response.data.length === 0) {
                usersList.innerHTML = '<p class="text-center">No users found.</p>';
                return;
            }
            
            let html = '<div class="table-responsive"><table class="table table-striped"><thead><tr>';
            html += '<th>ID</th><th>Username</th><th>Email</th><th>Name</th><th>Type</th><th>Created</th>';
            html += '</tr></thead><tbody>';
            
            response.data.forEach(user => {
                html += `<tr>`;
                html += `<td>${user.userId}</td>`;
                html += `<td>${user.username}</td>`;
                html += `<td>${user.email}</td>`;
                html += `<td>${user.firstName} ${user.lastName}</td>`;
                html += `<td><span class="badge bg-${user.userType === 'Admin' ? 'danger' : 'primary'}">${user.userType}</span></td>`;
                html += `<td>${new Date(user.createdDate).toLocaleDateString()}</td>`;
                html += `</tr>`;
            });
            
            html += '</tbody></table></div>';
            usersList.innerHTML = html;
        } else {
            usersList.innerHTML = '<p class="text-center text-danger">Error loading users.</p>';
        }
    } catch (error) {
        document.getElementById('adminUsersList').innerHTML = `<p class="text-center text-danger">Error: ${error.message}</p>`;
    }
}

// Helper function to escape HTML
function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// Helper function for status badge colors
function getStatusBadgeColor(status) {
    const colors = {
        'New': 'primary',
        'Processing': 'warning',
        'Fulfilled': 'success',
        'Cancelled': 'danger',
        'Pending Review': 'warning',
        'Approved': 'success',
        'Rejected': 'danger',
        'Completed': 'success'
    };
    return colors[status] || 'secondary';
}

// Admin action handlers
async function updateOrderStatus(orderId, newStatus) {
    if (!confirm(`Are you sure you want to change order #${orderId} status to ${newStatus}?`)) {
        return;
    }
    
    try {
        // Use the stored API wrapper reference from admin.js
        if (!updateOrderStatusAPI || typeof updateOrderStatusAPI !== 'function') {
            throw new Error('Order status API function not available. Make sure admin.js is loaded.');
        }
        
        const response = await updateOrderStatusAPI(orderId, newStatus);
        if (response.success) {
            showAlert(`Order status updated to ${newStatus}`, 'success');
            loadAdminOrders();
        } else {
            showAlert(response.error || 'Failed to update order status', 'danger');
        }
    } catch (error) {
        showAlert(error.message || 'Error updating order status', 'danger');
    }
}

async function handleRejectSubmission(submissionId) {
    const reason = prompt('Enter rejection reason (optional):');
    if (reason === null) return; // User cancelled
    
    try {
        const response = await rejectSubmission(submissionId, reason || null);
        if (response.success) {
            showAlert('Submission rejected', 'success');
            loadAdminSubmissions();
        } else {
            showAlert(response.error || 'Failed to reject submission', 'danger');
        }
    } catch (error) {
        showAlert(error.message || 'Error rejecting submission', 'danger');
    }
}

function showApproveModal(submissionId) {
    const sellingPrice = prompt('Enter selling price for the book:');
    if (!sellingPrice || isNaN(parseFloat(sellingPrice)) || parseFloat(sellingPrice) <= 0) {
        showAlert('Invalid selling price', 'danger');
        return;
    }
    
    approveSubmission(submissionId, parseFloat(sellingPrice))
        .then(response => {
            if (response.success) {
                showAlert('Submission approved and book added to inventory', 'success');
                loadAdminSubmissions();
            } else {
                showAlert(response.error || 'Failed to approve submission', 'danger');
            }
        })
        .catch(error => {
            showAlert(error.message || 'Error approving submission', 'danger');
        });
}

function showNegotiateModal(submissionId) {
    const offeredPrice = prompt('Enter your counter-offer price:');
    if (!offeredPrice || isNaN(parseFloat(offeredPrice)) || parseFloat(offeredPrice) <= 0) {
        showAlert('Invalid price', 'danger');
        return;
    }
    
    const message = prompt('Enter optional message:') || '';
    
    adminNegotiate(submissionId, {
        offeredPrice: parseFloat(offeredPrice),
        offerMessage: message
    })
        .then(response => {
            if (response.success) {
                showAlert('Counter-offer submitted', 'success');
                loadAdminSubmissions();
            } else {
                showAlert(response.error || 'Failed to submit counter-offer', 'danger');
            }
        })
        .catch(error => {
            showAlert(error.message || 'Error submitting counter-offer', 'danger');
        });
}

async function showAdminSubmissionDetails(submissionId) {
    try {
        const response = await window.getAdminSubmissionDetails(submissionId);
        
        if (!response.success || !response.data) {
            showAlert('Failed to load submission details', 'danger');
            return;
        }
        
        const submission = response.data;
        const app = document.getElementById('app');
        
        app.innerHTML = `
            <div class="container-fluid">
                <div class="row mb-3">
                    <div class="col">
                        <button class="btn btn-secondary" onclick="showAdminSubmissionsPage()">
                            ← Back to Submissions
                        </button>
                    </div>
                </div>
                
                <div class="card mb-3">
                    <div class="card-header">
                        <h4 class="mb-0">Submission Details</h4>
                    </div>
                    <div class="card-body">
                        <div class="row">
                            <div class="col-md-6">
                                <table class="table table-borderless">
                                    <tr>
                                        <th>Title:</th>
                                        <td>${escapeHtml(submission.title)}</td>
                                    </tr>
                                    <tr>
                                        <th>Author:</th>
                                        <td>${escapeHtml(submission.author)}</td>
                                    </tr>
                                    <tr>
                                        <th>ISBN:</th>
                                        <td>${escapeHtml(submission.isbn)}</td>
                                    </tr>
                                    <tr>
                                        <th>Edition:</th>
                                        <td>${escapeHtml(submission.edition)}</td>
                                    </tr>
                                    <tr>
                                        <th>Condition:</th>
                                        <td>${escapeHtml(submission.physicalCondition)}</td>
                                    </tr>
                                    ${submission.courseMajor ? `
                                    <tr>
                                        <th>Course/Major:</th>
                                        <td>${escapeHtml(submission.courseMajor)}</td>
                                    </tr>
                                    ` : ''}
                                </table>
                            </div>
                            <div class="col-md-6">
                                <table class="table table-borderless">
                                    <tr>
                                        <th>User:</th>
                                        <td>${escapeHtml(submission.username)}</td>
                                    </tr>
                                    <tr>
                                        <th>Asking Price:</th>
                                        <td>$${submission.askingPrice.toFixed(2)}</td>
                                    </tr>
                                    <tr>
                                        <th>Status:</th>
                                        <td><span class="badge bg-${getStatusBadgeColor(submission.status)}">${escapeHtml(submission.status)}</span></td>
                                    </tr>
                                    <tr>
                                        <th>Submitted:</th>
                                        <td>${new Date(submission.submissionDate).toLocaleString()}</td>
                                    </tr>
                                </table>
                            </div>
                        </div>
                    </div>
                </div>
                
                <div class="card">
                    <div class="card-header">
                        <h5 class="mb-0">Price Negotiation History</h5>
                    </div>
                    <div class="card-body">
                        ${submission.negotiations && submission.negotiations.length > 0 ? `
                            <div class="timeline">
                                ${submission.negotiations.map((negotiation, index) => {
                                    const offerDate = new Date(negotiation.offerDate);
                                    const offerStatusBadgeClass = {
                                        'Pending': 'bg-warning',
                                        'Accepted': 'bg-success',
                                        'Rejected': 'bg-danger'
                                    }[negotiation.offerStatus] || 'bg-secondary';
                                    
                                    return `
                                        <div class="card mb-3 ${negotiation.offerStatus === 'Pending' ? 'border-warning' : ''}">
                                            <div class="card-body">
                                                <div class="d-flex justify-content-between align-items-start mb-2">
                                                    <div>
                                                        <h6 class="mb-0">
                                                            Round ${negotiation.roundNumber} - ${negotiation.offeredBy === 'Admin' ? 'Your Offer' : 'Customer Counter-Offer'}
                                                        </h6>
                                                        <small class="text-muted">${offerDate.toLocaleDateString()} ${offerDate.toLocaleTimeString()}</small>
                                                    </div>
                                                    <span class="badge ${offerStatusBadgeClass}">${escapeHtml(negotiation.offerStatus)}</span>
                                                </div>
                                                <div class="mt-2">
                                                    <p class="mb-1">
                                                        <strong>Offered Price:</strong> 
                                                        <span class="text-primary fw-bold">$${negotiation.offeredPrice.toFixed(2)}</span>
                                                    </p>
                                                    ${negotiation.offerMessage ? `
                                                        <p class="mb-0"><strong>Message:</strong> ${escapeHtml(negotiation.offerMessage)}</p>
                                                    ` : ''}
                                                </div>
                                                ${negotiation.offerStatus === 'Pending' && negotiation.offeredBy === 'User' && submission.status === 'Pending Review' ? `
                                                    <div class="mt-3">
                                                        <button class="btn btn-sm btn-primary" 
                                                                onclick="showNegotiateModal(${submission.submissionId})">
                                                            Counter-Offer
                                                        </button>
                                                    </div>
                                                ` : ''}
                                            </div>
                                        </div>
                                    `;
                                }).join('')}
                            </div>
                        ` : `
                            <div class="alert alert-info">
                                <p class="mb-0">No negotiations yet.</p>
                            </div>
                        `}
                    </div>
                </div>
                
                ${(() => {
                    const hasAcceptedNegotiation = submission.negotiations && submission.negotiations.some(n => n.offerStatus === 'Accepted');
                    const canApprove = (submission.status === 'Pending Review' || 
                        (submission.status === 'Approved' && hasAcceptedNegotiation)) &&
                        submission.status !== 'Completed';
                    
                    if (submission.status === 'Pending Review') {
                        return `
                        <div class="card mt-3">
                            <div class="card-body">
                                <h6>Actions</h6>
                                <button class="btn btn-primary me-2" onclick="showNegotiateModal(${submission.submissionId})">Negotiate</button>
                                <button class="btn btn-success me-2" onclick="showApproveModal(${submission.submissionId})">Approve</button>
                                <button class="btn btn-danger" onclick="handleRejectSubmission(${submission.submissionId})">Reject</button>
                            </div>
                        </div>
                        `;
                    } else if (canApprove) {
                        return `
                        <div class="card mt-3">
                            <div class="card-body">
                                <h6>Actions</h6>
                                <button class="btn btn-success me-2" onclick="showApproveModal(${submission.submissionId})">Approve</button>
                            </div>
                        </div>
                        `;
                    } else if (submission.status === 'Completed') {
                        return `
                        <div class="card mt-3">
                            <div class="card-body">
                                <div class="alert alert-success">
                                    <strong>✓ Processed:</strong> This submission has been approved and the book has been added to inventory.
                                </div>
                            </div>
                        </div>
                        `;
                    }
                    return '';
                })()}
            </div>
        `;
    } catch (error) {
        showAlert(error.message || 'Error loading submission details', 'danger');
    }
}

function showAddBookModal() {
    const form = `
        <div class="modal fade" id="addBookModal" tabindex="-1">
            <div class="modal-dialog">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title">Add New Book</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                    </div>
                    <div class="modal-body">
                        <form id="addBookForm">
                            <div class="mb-3">
                                <label class="form-label">ISBN</label>
                                <input type="text" class="form-control" id="bookISBN" required>
                            </div>
                            <div class="mb-3">
                                <label class="form-label">Title</label>
                                <input type="text" class="form-control" id="bookTitle" required>
                            </div>
                            <div class="mb-3">
                                <label class="form-label">Author</label>
                                <input type="text" class="form-control" id="bookAuthor" required>
                            </div>
                            <div class="mb-3">
                                <label class="form-label">Edition</label>
                                <input type="text" class="form-control" id="bookEdition" required>
                            </div>
                            <div class="row">
                                <div class="col-md-6 mb-3">
                                    <label class="form-label">Selling Price</label>
                                    <input type="number" step="0.01" class="form-control" id="bookSellingPrice" required>
                                </div>
                                <div class="col-md-6 mb-3">
                                    <label class="form-label">Acquisition Cost</label>
                                    <input type="number" step="0.01" class="form-control" id="bookAcquisitionCost" required>
                                </div>
                            </div>
                            <div class="mb-3">
                                <label class="form-label">Condition</label>
                                <select class="form-select" id="bookCondition" required>
                                    <option value="New">New</option>
                                    <option value="Good">Good</option>
                                    <option value="Fair">Fair</option>
                                </select>
                            </div>
                            <div class="mb-3">
                                <label class="form-label">Course/Major (Optional)</label>
                                <input type="text" class="form-control" id="bookCourseMajor">
                            </div>
                        </form>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                        <button type="button" class="btn btn-primary" onclick="handleAddBook()">Add Book</button>
                    </div>
                </div>
            </div>
        </div>
    `;
    
    // Remove existing modal if any
    const existingModal = document.getElementById('addBookModal');
    if (existingModal) existingModal.remove();
    
    // Add modal to body
    document.body.insertAdjacentHTML('beforeend', form);
    
    // Show modal
    const modal = new bootstrap.Modal(document.getElementById('addBookModal'));
    modal.show();
}

async function handleAddBook() {
    const bookData = {
        isbn: document.getElementById('bookISBN').value,
        title: document.getElementById('bookTitle').value,
        author: document.getElementById('bookAuthor').value,
        edition: document.getElementById('bookEdition').value,
        sellingPrice: parseFloat(document.getElementById('bookSellingPrice').value),
        acquisitionCost: parseFloat(document.getElementById('bookAcquisitionCost').value),
        bookCondition: document.getElementById('bookCondition').value,
        courseMajor: document.getElementById('bookCourseMajor').value || null
    };
    
    try {
        const response = await createBook(bookData);
        if (response.success) {
            showAlert('Book added successfully', 'success');
            bootstrap.Modal.getInstance(document.getElementById('addBookModal')).hide();
            loadAdminBooks();
        } else {
            showAlert(response.error || 'Failed to add book', 'danger');
        }
    } catch (error) {
        showAlert(error.message || 'Error adding book', 'danger');
    }
}

async function handleDeleteBook(isbn, edition) {
    // Note: This is simplified - in a real app, you'd need to get the actual BookID
    // For now, we'll show a message that this needs the BookID
    showAlert('Delete functionality requires BookID. Please use the API directly or enhance this feature.', 'info');
}

// Export admin functions
window.loadAdminBooks = loadAdminBooks;
window.loadAdminSubmissions = loadAdminSubmissions;
window.loadAdminOrders = loadAdminOrders;
window.loadAdminUsers = loadAdminUsers;
window.updateOrderStatus = updateOrderStatus;
window.handleRejectSubmission = handleRejectSubmission;
window.showApproveModal = showApproveModal;
window.showNegotiateModal = showNegotiateModal;
window.showAdminSubmissionDetails = showAdminSubmissionDetails;
window.showAddBookModal = showAddBookModal;
window.handleAddBook = handleAddBook;
window.handleDeleteBook = handleDeleteBook;

