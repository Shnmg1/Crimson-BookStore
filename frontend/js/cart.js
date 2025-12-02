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
                <div class="ua-card">
                    <div class="ua-empty-state">
                        <div class="ua-empty-state-icon">
                            <i class="bi bi-cart-x" style="font-size: 4rem;"></i>
                        </div>
                        <h5 style="color: var(--text-light); margin-bottom: 1rem;">Your cart is empty</h5>
                        <p style="color: var(--text-muted); margin-bottom: 1.5rem;">Start shopping to add items to your cart!</p>
                        <button class="btn-ua-primary" onclick="showBooksPage()">Browse Books</button>
                    </div>
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
        <div class="ua-card">
            <div class="ua-card-header">
                <h3 class="ua-card-title">Shopping Cart (${items.length} ${items.length === 1 ? 'item' : 'items'})</h3>
            </div>
            <div class="table-responsive">
                <table class="ua-table">
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
                    <strong style="color: var(--text-light);">${escapeHtml(item.title)}</strong><br>
                    <small style="color: var(--text-muted);">
                        ${escapeHtml(item.author)}<br>
                        ISBN: ${escapeHtml(item.isbn)} | Edition: ${escapeHtml(item.edition)}<br>
                        ${item.courseMajor ? `Course: ${escapeHtml(item.courseMajor)}` : ''}
                    </small>
                </td>
                <td>
                    <strong style="color: var(--text-light); font-size: 1.125rem;">$${item.sellingPrice.toFixed(2)}</strong>
                </td>
                <td>
                    <span class="ua-badge ua-badge-info">${escapeHtml(item.bookCondition)}</span>
                </td>
                <td>
                    <button class="btn-ua-danger" style="padding: 0.375rem 0.75rem; font-size: 0.875rem;" onclick="handleRemoveFromCart(${item.cartItemId})">
                        <i class="bi bi-trash"></i> Remove
                    </button>
                </td>
            </tr>
        `;
    });

    html += `
                    </tbody>
                    <tfoot>
                        <tr style="border-top: 2px solid var(--border-dark);">
                            <th colspan="2" style="text-align: right; color: var(--text-light);">Total:</th>
                            <th style="color: var(--ua-crimson); font-size: 1.25rem;">$${total.toFixed(2)}</th>
                            <td></td>
                        </tr>
                    </tfoot>
                </table>
            </div>
            <div style="display: flex; justify-content: space-between; margin-top: 1.5rem; gap: 1rem;">
                <button class="btn-ua-secondary" onclick="handleClearCart()">
                    <i class="bi bi-x-circle"></i> Clear Cart
                </button>
                <button class="btn-ua-primary" style="font-size: 1.125rem; padding: 0.75rem 2rem;" onclick="handleCheckout()">
                    <i class="bi bi-arrow-right-circle"></i> Proceed to Checkout
                </button>
            </div>
        </div>
    `;

    cartItems.innerHTML = html;
}

// Handle remove from cart
function handleRemoveFromCart(cartItemId) {
    // Create confirmation modal
    const modalHTML = `
        <div class="modal fade" id="removeCartItemModal" tabindex="-1" aria-labelledby="removeCartItemModalLabel" aria-hidden="true">
            <div class="modal-dialog">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title text-white" id="removeCartItemModalLabel">Remove Item</h5>
                        <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Close"></button>
                    </div>
                    <div class="modal-body">
                        <p class="text-white">Are you sure you want to remove this item from your cart?</p>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                        <button type="button" class="btn btn-danger" onclick="confirmRemoveFromCart(${cartItemId})">
                            Remove
                        </button>
                    </div>
                </div>
            </div>
        </div>
    `;

    // Remove existing modal if any
    const existingModal = document.getElementById('removeCartItemModal');
    if (existingModal) {
        existingModal.remove();
    }

    // Add modal to body
    document.body.insertAdjacentHTML('beforeend', modalHTML);

    // Show the modal
    const modal = new bootstrap.Modal(document.getElementById('removeCartItemModal'));
    modal.show();

    // Clean up modal when hidden
    document.getElementById('removeCartItemModal').addEventListener('hidden.bs.modal', function() {
        this.remove();
    });
}

// Confirm remove from cart
async function confirmRemoveFromCart(cartItemId) {
    // Close modal first
    const modal = bootstrap.Modal.getInstance(document.getElementById('removeCartItemModal'));
    if (modal) modal.hide();

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
function handleClearCart() {
    // Create confirmation modal
    const modalHTML = `
        <div class="modal fade" id="clearCartModal" tabindex="-1" aria-labelledby="clearCartModalLabel" aria-hidden="true">
            <div class="modal-dialog">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title text-white" id="clearCartModalLabel">Clear Cart</h5>
                        <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Close"></button>
                    </div>
                    <div class="modal-body">
                        <p class="text-white">Are you sure you want to clear your entire cart? This action cannot be undone.</p>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                        <button type="button" class="btn btn-danger" onclick="confirmClearCart()">
                            Clear Cart
                        </button>
                    </div>
                </div>
            </div>
        </div>
    `;

    // Remove existing modal if any
    const existingModal = document.getElementById('clearCartModal');
    if (existingModal) {
        existingModal.remove();
    }

    // Add modal to body
    document.body.insertAdjacentHTML('beforeend', modalHTML);

    // Show the modal
    const modal = new bootstrap.Modal(document.getElementById('clearCartModal'));
    modal.show();

    // Clean up modal when hidden
    document.getElementById('clearCartModal').addEventListener('hidden.bs.modal', function() {
        this.remove();
    });
}

// Confirm clear cart
async function confirmClearCart() {
    // Close modal first
    const modal = bootstrap.Modal.getInstance(document.getElementById('clearCartModal'));
    if (modal) modal.hide();

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
    const sidebarCartBadge = document.getElementById('sidebarCartBadge');
    
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
    
    if (sidebarCartBadge) {
        if (count > 0) {
            sidebarCartBadge.textContent = count;
            sidebarCartBadge.style.display = 'inline-block';
        } else {
            sidebarCartBadge.style.display = 'none';
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
window.confirmRemoveFromCart = confirmRemoveFromCart;
window.confirmClearCart = confirmClearCart;
window.handleCheckout = handleCheckout;
window.updateCartBadge = updateCartBadge;
window.refreshCartBadge = refreshCartBadge;

