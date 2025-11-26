// Cart API functions and UI

// Get user's cart
async function getCart() {
    try {
        if (!isAuthenticated()) {
            throw new Error('You must be logged in to view your cart');
        }
        
        const response = await apiCall('/cart');
        return response;
    } catch (error) {
        console.error('Failed to fetch cart:', error);
        throw error;
    }
}

// Add book to cart
async function addToCart(bookId) {
    try {
        if (!isAuthenticated()) {
            throw new Error('You must be logged in to add items to your cart');
        }
        
        const response = await apiCall('/cart', {
            method: 'POST',
            body: JSON.stringify({ bookId: bookId })
        });
        
        return response;
    } catch (error) {
        console.error('Failed to add to cart:', error);
        throw error;
    }
}

// Remove item from cart
async function removeFromCart(cartItemId) {
    try {
        if (!isAuthenticated()) {
            throw new Error('You must be logged in to remove items from your cart');
        }
        
        const response = await apiCall(`/cart/${cartItemId}`, {
            method: 'DELETE'
        });
        
        return response;
    } catch (error) {
        console.error('Failed to remove from cart:', error);
        throw error;
    }
}

// Clear entire cart
async function clearCart() {
    try {
        if (!isAuthenticated()) {
            throw new Error('You must be logged in to clear your cart');
        }
        
        const response = await apiCall('/cart', {
            method: 'DELETE'
        });
        
        return response;
    } catch (error) {
        console.error('Failed to clear cart:', error);
        throw error;
    }
}

// Load and display cart
async function loadCart() {
    const cartItems = document.getElementById('cartItems');
    if (!cartItems) return;

    try {
        // Show loading state
        cartItems.innerHTML = '<div class="text-center"><div class="spinner-border" role="status"><span class="visually-hidden">Loading...</span></div></div>';

        const response = await getCart();
        
        if (!response.success || !response.data || response.data.length === 0) {
            cartItems.innerHTML = `
                <div class="alert alert-info">
                    <h5>Your cart is empty</h5>
                    <p>Start shopping to add items to your cart!</p>
                    <button class="btn btn-primary" onclick="showBooksPage()">Browse Books</button>
                </div>
            `;
            updateCartBadge(0);
            return;
        }

        // Display cart items
        displayCartItems(response.data, response.total);
        updateCartBadge(response.data.length);
    } catch (error) {
        cartItems.innerHTML = `
            <div class="alert alert-danger">
                <h5>Error loading cart</h5>
                <p>${escapeHtml(error.message)}</p>
                <button class="btn btn-secondary" onclick="loadCart()">Retry</button>
            </div>
        `;
        updateCartBadge(0);
    }
}

// Display cart items
function displayCartItems(items, total) {
    const cartItems = document.getElementById('cartItems');
    if (!cartItems) return;

    let html = `
        <div class="table-responsive">
            <table class="table table-hover">
                <thead>
                    <tr>
                        <th>Book</th>
                        <th>Price</th>
                        <th>Condition</th>
                        <th>Actions</th>
                    </tr>
                </thead>
                <tbody>
    `;

    items.forEach(item => {
        html += `
            <tr id="cart-item-${item.cartItemId}">
                <td>
                    <strong>${escapeHtml(item.title)}</strong><br>
                    <small class="text-muted">
                        ${escapeHtml(item.author)}<br>
                        ISBN: ${escapeHtml(item.isbn)} | Edition: ${escapeHtml(item.edition)}<br>
                        ${item.courseMajor ? `Course: ${escapeHtml(item.courseMajor)}` : ''}
                    </small>
                </td>
                <td>
                    <strong>$${item.sellingPrice.toFixed(2)}</strong>
                </td>
                <td>
                    <span class="badge bg-info">${escapeHtml(item.bookCondition)}</span>
                </td>
                <td>
                    <button class="btn btn-sm btn-danger" onclick="handleRemoveFromCart(${item.cartItemId})">
                        Remove
                    </button>
                </td>
            </tr>
        `;
    });

    html += `
                </tbody>
                <tfoot>
                    <tr>
                        <th colspan="2" class="text-end">Total:</th>
                        <th>$${total.toFixed(2)}</th>
                        <td></td>
                    </tr>
                </tfoot>
            </table>
        </div>
        <div class="mt-3 d-flex justify-content-between">
            <button class="btn btn-secondary" onclick="handleClearCart()">Clear Cart</button>
            <button class="btn btn-primary btn-lg" onclick="handleCheckout()">Proceed to Checkout</button>
        </div>
    `;

    cartItems.innerHTML = html;
}

// Handle remove from cart
async function handleRemoveFromCart(cartItemId) {
    if (!confirm('Are you sure you want to remove this item from your cart?')) {
        return;
    }

    try {
        const response = await removeFromCart(cartItemId);
        
        if (response.success) {
            showAlert('Item removed from cart', 'success');
            // Reload cart to update display
            await loadCart();
        } else {
            showAlert(response.error || 'Failed to remove item', 'danger');
        }
    } catch (error) {
        showAlert(error.message || 'Failed to remove item from cart', 'danger');
    }
}

// Handle clear cart
async function handleClearCart() {
    if (!confirm('Are you sure you want to clear your entire cart? This action cannot be undone.')) {
        return;
    }

    try {
        const response = await clearCart();
        
        if (response.success) {
            showAlert('Cart cleared', 'success');
            // Reload cart to update display
            await loadCart();
        } else {
            showAlert(response.error || 'Failed to clear cart', 'danger');
        }
    } catch (error) {
        showAlert(error.message || 'Failed to clear cart', 'danger');
    }
}

// Handle checkout - show checkout page
function handleCheckout() {
    if (typeof showCheckoutPage === 'function') {
        showCheckoutPage();
    } else {
        showAlert('Checkout page is loading...', 'info');
    }
}

// Update cart badge count in navigation
function updateCartBadge(count) {
    const cartBadge = document.getElementById('cartBadge');
    if (cartBadge) {
        cartBadge.textContent = count;
        if (count === 0) {
            cartBadge.classList.remove('bg-danger');
            cartBadge.classList.add('bg-secondary');
        } else {
            cartBadge.classList.remove('bg-secondary');
            cartBadge.classList.add('bg-danger');
        }
    }
}

// Refresh cart badge count (call this after adding/removing items)
async function refreshCartBadge() {
    try {
        if (!isAuthenticated()) {
            updateCartBadge(0);
            return;
        }
        
        const response = await getCart();
        if (response.success && response.data) {
            updateCartBadge(response.data.length);
        } else {
            updateCartBadge(0);
        }
    } catch (error) {
        updateCartBadge(0);
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
window.getCart = getCart;
window.addToCart = addToCart;
window.removeFromCart = removeFromCart;
window.clearCart = clearCart;
window.loadCart = loadCart;
window.handleRemoveFromCart = handleRemoveFromCart;
window.handleClearCart = handleClearCart;
window.handleCheckout = handleCheckout;
window.updateCartBadge = updateCartBadge;
window.refreshCartBadge = refreshCartBadge;

