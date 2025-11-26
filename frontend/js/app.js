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
                <h2>Login</h2>
                <form id="loginForm">
                    <div class="mb-3">
                        <label for="username" class="form-label">Username</label>
                        <input type="text" class="form-control" id="username" required>
                    </div>
                    <div class="mb-3">
                        <label for="password" class="form-label">Password</label>
                        <input type="password" class="form-control" id="password" required>
                    </div>
                    <button type="submit" class="btn btn-primary">Login</button>
                </form>
            </div>
        </div>
    `;
    
    // TODO: Handle login form submission
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

