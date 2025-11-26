// Orders API functions and UI

// Create order from cart (checkout)
async function createOrder(paymentMethodId = null) {
    try {
        if (!isAuthenticated()) {
            throw new Error('You must be logged in to create an order');
        }
        
        const requestBody = {
            paymentMethodId: paymentMethodId
        };
        
        const response = await apiCall('/orders', {
            method: 'POST',
            body: JSON.stringify(requestBody)
        });
        
        return response;
    } catch (error) {
        console.error('Failed to create order:', error);
        throw error;
    }
}

// Get user's order history
async function getOrders(status = null) {
    try {
        if (!isAuthenticated()) {
            throw new Error('You must be logged in to view orders');
        }
        
        let endpoint = '/orders';
        if (status) {
            endpoint += `?status=${encodeURIComponent(status)}`;
        }
        
        const response = await apiCall(endpoint);
        return response;
    } catch (error) {
        console.error('Failed to fetch orders:', error);
        throw error;
    }
}

// Get order details by ID
async function getOrderDetails(orderId) {
    try {
        if (!isAuthenticated()) {
            throw new Error('You must be logged in to view order details');
        }
        
        const response = await apiCall(`/orders/${orderId}`);
        return response;
    } catch (error) {
        console.error('Failed to fetch order details:', error);
        throw error;
    }
}

// Show checkout page
async function showCheckoutPage() {
    if (!isAuthenticated()) {
        showAlert('Please log in to checkout', 'warning');
        showLoginPage();
        return;
    }

    const app = document.getElementById('app');
    
    try {
        // Load cart to show summary
        const cartResponse = await getCart();
        
        if (!cartResponse.success || !cartResponse.data || cartResponse.data.length === 0) {
            showAlert('Your cart is empty', 'warning');
            showCartPage();
            return;
        }

        const items = cartResponse.data;
        const total = cartResponse.total;

        // Create checkout page HTML
        app.innerHTML = `
            <div class="row">
                <div class="col-md-8">
                    <h2>Checkout</h2>
                    
                    <div class="card mb-4">
                        <div class="card-header">
                            <h5 class="mb-0">Order Summary</h5>
                        </div>
                        <div class="card-body">
                            <table class="table table-sm">
                                <thead>
                                    <tr>
                                        <th>Item</th>
                                        <th class="text-end">Price</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    ${items.map(item => `
                                        <tr>
                                            <td>
                                                <strong>${escapeHtml(item.title)}</strong><br>
                                                <small class="text-muted">${escapeHtml(item.author)}</small>
                                            </td>
                                            <td class="text-end">$${item.sellingPrice.toFixed(2)}</td>
                                        </tr>
                                    `).join('')}
                                </tbody>
                                <tfoot>
                                    <tr>
                                        <th>Total</th>
                                        <th class="text-end">$${total.toFixed(2)}</th>
                                    </tr>
                                </tfoot>
                            </table>
                        </div>
                    </div>

                    <div class="card">
                        <div class="card-header">
                            <h5 class="mb-0">Payment Method</h5>
                        </div>
                        <div class="card-body">
                            <div class="mb-3">
                                <div class="form-check">
                                    <input class="form-check-input" type="radio" name="paymentMethod" id="paymentOneTime" value="onetime" checked>
                                    <label class="form-check-label" for="paymentOneTime">
                                        One-time Payment
                                    </label>
                                </div>
                                <small class="text-muted">Payment will be processed without saving your payment method</small>
                            </div>
                            <div class="mb-3">
                                <div class="form-check">
                                    <input class="form-check-input" type="radio" name="paymentMethod" id="paymentSaved" value="saved">
                                    <label class="form-check-label" for="paymentSaved">
                                        Use Saved Payment Method
                                    </label>
                                </div>
                                <div id="savedPaymentMethods" class="mt-2" style="display: none;">
                                    <select class="form-select" id="savedPaymentMethodSelect">
                                        <option value="">Loading...</option>
                                    </select>
                                    <small class="text-muted">No saved payment methods available. Payment methods feature will be implemented in Phase 7.</small>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
                
                <div class="col-md-4">
                    <div class="card">
                        <div class="card-body">
                            <h5>Order Total</h5>
                            <h3 class="text-primary">$${total.toFixed(2)}</h3>
                            <hr>
                            <button class="btn btn-primary btn-lg w-100" onclick="handlePlaceOrder()" id="placeOrderBtn">
                                Place Order
                            </button>
                            <button class="btn btn-secondary w-100 mt-2" onclick="showCartPage()">
                                Back to Cart
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        `;

        // Setup payment method radio button handlers
        document.querySelectorAll('input[name="paymentMethod"]').forEach(radio => {
            radio.addEventListener('change', function() {
                const savedMethodsDiv = document.getElementById('savedPaymentMethods');
                if (this.value === 'saved') {
                    savedMethodsDiv.style.display = 'block';
                    // TODO: Load saved payment methods (Phase 7)
                } else {
                    savedMethodsDiv.style.display = 'none';
                }
            });
        });
    } catch (error) {
        app.innerHTML = `
            <div class="alert alert-danger">
                <h5>Error loading checkout</h5>
                <p>${escapeHtml(error.message)}</p>
                <button class="btn btn-secondary" onclick="showCartPage()">Back to Cart</button>
            </div>
        `;
    }
}

// Handle place order
async function handlePlaceOrder() {
    const placeOrderBtn = document.getElementById('placeOrderBtn');
    if (!placeOrderBtn) return;

    // Disable button to prevent double submission
    placeOrderBtn.disabled = true;
    placeOrderBtn.textContent = 'Processing...';

    try {
        // Get selected payment method
        const paymentMethodRadio = document.querySelector('input[name="paymentMethod"]:checked');
        let paymentMethodId = null;

        if (paymentMethodRadio && paymentMethodRadio.value === 'saved') {
            const savedMethodSelect = document.getElementById('savedPaymentMethodSelect');
            if (savedMethodSelect && savedMethodSelect.value) {
                paymentMethodId = parseInt(savedMethodSelect.value);
            }
            // For now, if saved payment is selected but none available, use one-time
            if (!paymentMethodId) {
                paymentMethodId = null;
            }
        }

        const response = await createOrder(paymentMethodId);

        if (response.success) {
            // Show success message
            showAlert(`Order placed successfully! Order ID: ${response.data.orderId}`, 'success');
            
            // Refresh cart badge (should be 0 now)
            await refreshCartBadge();
            
            // Redirect to order history after a short delay
            setTimeout(() => {
                showOrderHistoryPage();
            }, 2000);
        } else {
            showAlert(response.error || 'Failed to place order', 'danger');
            placeOrderBtn.disabled = false;
            placeOrderBtn.textContent = 'Place Order';
        }
    } catch (error) {
        showAlert(error.message || 'Failed to place order. Please try again.', 'danger');
        placeOrderBtn.disabled = false;
        placeOrderBtn.textContent = 'Place Order';
    }
}

// Show order history page
async function showOrderHistoryPage() {
    if (!isAuthenticated()) {
        showAlert('Please log in to view your orders', 'warning');
        showLoginPage();
        return;
    }

    const app = document.getElementById('app');
    app.innerHTML = `
        <div class="row mb-3">
            <div class="col">
                <h2>Order History</h2>
            </div>
            <div class="col-auto">
                <select class="form-select" id="orderStatusFilter" onchange="loadOrderHistory()">
                    <option value="">All Orders</option>
                    <option value="New">New</option>
                    <option value="Processing">Processing</option>
                    <option value="Fulfilled">Fulfilled</option>
                    <option value="Cancelled">Cancelled</option>
                </select>
            </div>
        </div>
        <div id="orderHistoryList">
            <div class="text-center">
                <div class="spinner-border" role="status">
                    <span class="visually-hidden">Loading...</span>
                </div>
            </div>
        </div>
    `;

    await loadOrderHistory();
}

// Load and display order history
async function loadOrderHistory() {
    const orderHistoryList = document.getElementById('orderHistoryList');
    if (!orderHistoryList) return;

    try {
        const statusFilter = document.getElementById('orderStatusFilter')?.value || null;
        const response = await getOrders(statusFilter);

        if (!response.success || !response.data || response.data.length === 0) {
            orderHistoryList.innerHTML = `
                <div class="alert alert-info">
                    <h5>No orders found</h5>
                    <p>You haven't placed any orders yet.</p>
                    <button class="btn btn-primary" onclick="showBooksPage()">Browse Books</button>
                </div>
            `;
            return;
        }

        // Display orders
        let html = '<div class="list-group">';
        
        response.data.forEach(order => {
            const orderDate = new Date(order.orderDate);
            const statusBadgeClass = {
                'New': 'bg-primary',
                'Processing': 'bg-warning',
                'Fulfilled': 'bg-success',
                'Cancelled': 'bg-danger'
            }[order.status] || 'bg-secondary';

            html += `
                <div class="list-group-item list-group-item-action">
                    <div class="d-flex w-100 justify-content-between align-items-start">
                        <div class="flex-grow-1">
                            <div class="d-flex justify-content-between align-items-start mb-2">
                                <h5 class="mb-0">Order #${order.orderId}</h5>
                                <span class="badge ${statusBadgeClass} ms-2">${escapeHtml(order.status)}</span>
                            </div>
                            <div class="row">
                                <div class="col-md-6">
                                    <p class="mb-1">
                                        <strong>Order Date:</strong><br>
                                        <small class="text-muted">${orderDate.toLocaleDateString('en-US', { 
                                            year: 'numeric', 
                                            month: 'long', 
                                            day: 'numeric',
                                            hour: '2-digit',
                                            minute: '2-digit'
                                        })}</small>
                                    </p>
                                </div>
                                <div class="col-md-3">
                                    <p class="mb-1">
                                        <strong>Items:</strong><br>
                                        <small class="text-muted">${order.itemCount} ${order.itemCount === 1 ? 'item' : 'items'}</small>
                                    </p>
                                </div>
                                <div class="col-md-3">
                                    <p class="mb-1">
                                        <strong>Total Amount:</strong><br>
                                        <span class="text-primary fw-bold">$${order.totalAmount.toFixed(2)}</span>
                                    </p>
                                </div>
                            </div>
                        </div>
                        <div class="ms-3">
                            <button class="btn btn-sm btn-outline-primary" onclick="showOrderDetails(${order.orderId})">
                                View Details
                            </button>
                        </div>
                    </div>
                </div>
            `;
        });

        html += '</div>';
        orderHistoryList.innerHTML = html;
    } catch (error) {
        orderHistoryList.innerHTML = `
            <div class="alert alert-danger">
                <h5>Error loading order history</h5>
                <p>${escapeHtml(error.message)}</p>
                <button class="btn btn-secondary" onclick="loadOrderHistory()">Retry</button>
            </div>
        `;
    }
}

// Show order details
async function showOrderDetails(orderId) {
    try {
        const response = await getOrderDetails(orderId);

        if (!response.success || !response.data) {
            showAlert('Order not found', 'danger');
            return;
        }

        const order = response.data;
        const orderDate = new Date(order.orderDate);
        const statusBadgeClass = {
            'New': 'bg-primary',
            'Processing': 'bg-warning',
            'Fulfilled': 'bg-success',
            'Cancelled': 'bg-danger'
        }[order.status] || 'bg-secondary';

        const app = document.getElementById('app');
        app.innerHTML = `
            <div class="mb-3">
                <button class="btn btn-secondary" onclick="showOrderHistoryPage()">
                    ← Back to Order History
                </button>
            </div>
            
            <div class="card mb-4">
                <div class="card-header">
                    <h4>Order #${order.orderId}</h4>
                </div>
                <div class="card-body">
                    <div class="row mb-3">
                        <div class="col-md-6">
                            <p><strong>Order Date:</strong> ${orderDate.toLocaleDateString()} ${orderDate.toLocaleTimeString()}</p>
                            <p><strong>Status:</strong> <span class="badge ${statusBadgeClass}">${escapeHtml(order.status)}</span></p>
                        </div>
                        <div class="col-md-6">
                            <p><strong>Total Amount:</strong> $${order.totalAmount.toFixed(2)}</p>
                            ${order.payment ? `
                                <p><strong>Payment:</strong> ${escapeHtml(order.payment.paymentStatus)}</p>
                                ${order.payment.paymentMethod ? `<p><strong>Payment Method:</strong> ${escapeHtml(order.payment.paymentMethod)}</p>` : ''}
                            ` : ''}
                        </div>
                    </div>
                </div>
            </div>

            <div class="card">
                <div class="card-header">
                    <h5 class="mb-0">Order Items (${order.items.length} ${order.items.length === 1 ? 'item' : 'items'})</h5>
                </div>
                <div class="card-body">
                    <div class="table-responsive">
                        <table class="table table-hover">
                            <thead>
                                <tr>
                                    <th>Book Details</th>
                                    <th class="text-end">Price</th>
                                </tr>
                            </thead>
                            <tbody>
                                ${order.items.map(item => `
                                    <tr>
                                        <td>
                                            <strong>${escapeHtml(item.title)}</strong><br>
                                            <small class="text-muted">
                                                by ${escapeHtml(item.author)}<br>
                                                ISBN: ${escapeHtml(item.isbn)} | Edition: ${escapeHtml(item.edition)}
                                            </small>
                                        </td>
                                        <td class="text-end">
                                            <strong>$${item.priceAtSale.toFixed(2)}</strong>
                                        </td>
                                    </tr>
                                `).join('')}
                            </tbody>
                            <tfoot class="table-light">
                                <tr>
                                    <th>Total</th>
                                    <th class="text-end text-primary">$${order.totalAmount.toFixed(2)}</th>
                                </tr>
                            </tfoot>
                        </table>
                    </div>
                </div>
            </div>
        `;
    } catch (error) {
        showAlert(error.message || 'Failed to load order details', 'danger');
    }
}

// Escape HTML to prevent XSS
function escapeHtml(text) {
    if (text == null) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// Make functions available globally
window.createOrder = createOrder;
window.getOrders = getOrders;
window.getOrderDetails = getOrderDetails;
window.showCheckoutPage = showCheckoutPage;
window.handlePlaceOrder = handlePlaceOrder;
window.showOrderHistoryPage = showOrderHistoryPage;
window.loadOrderHistory = loadOrderHistory;
window.showOrderDetails = showOrderDetails;

