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
        showLoadingSpinner(cartItems, 'Loading your cart...');

        const response = await getCart();
        
        if (!response.success || !response.data || response.data.length === 0) {
            showEmptyState(cartItems, 'Your cart is empty. Start shopping to add items!', 'Browse Books', 'showBooksPage()');
            updateCartBadge(0);
            return;
        }

        // Display cart items
        displayCartItems(response.data, response.total);
        updateCartBadge(response.data.length);
    } catch (error) {
        showError(cartItems, error, 'Error Loading Cart');
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
                    <button class="btn btn-sm btn-danger" onclick="handleRemoveFromCart(${item.cartItemId})" data-cart-item-id="${item.cartItemId}">
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
            <button class="btn btn-secondary" id="clearCartBtn" onclick="handleClearCart()">Clear Cart</button>
            <button class="btn btn-primary btn-lg" id="checkoutBtn" onclick="handleCheckout()">Proceed to Checkout</button>
        </div>
    `;

    cartItems.innerHTML = html;
}

// Handle remove from cart
async function handleRemoveFromCart(cartItemId) {
    const confirmed = await confirmAction(
        'Are you sure you want to remove this item from your cart?',
        'Remove Item'
    );
    
    if (!confirmed) {
        return;
    }

    const button = document.querySelector(`button[data-cart-item-id="${cartItemId}"]`);
    
    try {
        if (button) setLoadingState(button, true, 'Removing...');
        const response = await removeFromCart(cartItemId);
        
        if (response.success) {
            showToast('Item removed from cart', 'success');
            // Reload cart to update display
            await loadCart();
        } else {
            showToast(response.error || 'Failed to remove item', 'danger');
        }
    } catch (error) {
        showToast(error.message || 'Failed to remove item from cart', 'danger');
    } finally {
        if (button) setLoadingState(button, false);
    }
}

// Handle clear cart
async function handleClearCart() {
    const confirmed = await confirmAction(
        'Are you sure you want to clear your entire cart? This action cannot be undone.',
        'Clear Cart'
    );
    
    if (!confirmed) {
        return;
    }

    const button = document.getElementById('clearCartBtn');
    
    try {
        if (button) setLoadingState(button, true, 'Clearing...');
        const response = await clearCart();
        
        if (response.success) {
            showToast('Cart cleared', 'success');
            // Reload cart to update display
            await loadCart();
        } else {
            showToast(response.error || 'Failed to clear cart', 'danger');
        }
    } catch (error) {
        showToast(error.message || 'Failed to clear cart', 'danger');
    } finally {
        if (button) setLoadingState(button, false);
    }
}

// Handle checkout - show checkout page
function handleCheckout() {
    if (typeof showCheckoutPage === 'function') {
        showCheckoutPage();
    } else {
        showToast('Checkout page is loading...', 'info');
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

