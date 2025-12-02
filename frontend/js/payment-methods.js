// Payment Methods API functions and UI

// Get user's payment methods
async function getPaymentMethods() {
    try {
        if (!isAuthenticated()) {
            throw new Error('You must be logged in to view payment methods');
        }
        
        const response = await apiCall('/payment-methods');
        return response;
    } catch (error) {
        console.error('Failed to fetch payment methods:', error);
        throw error;
    }
}

// Create a new payment method
async function createPaymentMethod(paymentMethodData) {
    try {
        if (!isAuthenticated()) {
            throw new Error('You must be logged in to add payment methods');
        }
        
        const response = await apiCall('/payment-methods', {
            method: 'POST',
            body: JSON.stringify(paymentMethodData)
        });
        
        return response;
    } catch (error) {
        console.error('Failed to create payment method:', error);
        throw error;
    }
}

// Delete a payment method
async function deletePaymentMethod(paymentMethodId) {
    try {
        if (!isAuthenticated()) {
            throw new Error('You must be logged in to delete payment methods');
        }
        
        const response = await apiCall(`/payment-methods/${paymentMethodId}`, {
            method: 'DELETE'
        });
        
        return response;
    } catch (error) {
        console.error('Failed to delete payment method:', error);
        throw error;
    }
}

// Show payment methods page
async function showPaymentMethodsPage() {
    if (!isAuthenticated()) {
        showAlert('Please log in to manage your payment methods', 'warning');
        showLoginPage();
        return;
    }

    const app = document.getElementById('app');
    app.innerHTML = `
        <div class="ua-card">
            <div class="ua-card-header" style="display: flex; justify-content: space-between; align-items: center;">
                <h2 class="ua-card-title" style="margin: 0;">Payment Methods</h2>
                <button class="btn-ua-primary" onclick="showAddPaymentMethodModal()">
                    <i class="bi bi-plus-circle"></i> Add Payment Method
                </button>
            </div>
        </div>
        <div id="paymentMethodsList">
            <div class="text-center">
                <div class="ua-spinner"></div>
            </div>
        </div>
    `;
    
    // Update active sidebar link
    document.querySelectorAll('.ua-sidebar-link').forEach(link => link.classList.remove('active'));
    const paymentLink = document.getElementById('sidebarPayment');
    if (paymentLink) paymentLink.classList.add('active');
    
    await loadPaymentMethods();
}

// Load and display payment methods
async function loadPaymentMethods() {
    const paymentMethodsList = document.getElementById('paymentMethodsList');
    if (!paymentMethodsList) return;

    try {
        const response = await getPaymentMethods();
        
        if (!response.success || !response.data || response.data.length === 0) {
            paymentMethodsList.innerHTML = `
                <div class="ua-card">
                    <div class="ua-empty-state">
                        <div class="ua-empty-state-icon">
                            <i class="bi bi-credit-card-2-front" style="font-size: 4rem;"></i>
                        </div>
                        <h5 style="color: var(--text-light); margin-bottom: 1rem;">No payment methods</h5>
                        <p style="color: var(--text-muted); margin-bottom: 1.5rem;">You haven't saved any payment methods yet. Add one to get started!</p>
                        <button class="btn-ua-primary" onclick="showAddPaymentMethodModal()">
                            <i class="bi bi-plus-circle"></i> Add Payment Method
                        </button>
                    </div>
                </div>
            `;
            return;
        }

        // Display payment methods
        let html = '<div style="display: grid; grid-template-columns: repeat(auto-fill, minmax(300px, 1fr)); gap: 1.5rem;">';
        
        response.data.forEach(pm => {
            html += `
                <div class="ua-card" style="${pm.isDefault ? 'border: 2px solid var(--ua-crimson);' : ''}">
                    <div style="display: flex; justify-content: space-between; align-items: start; margin-bottom: 1rem;">
                        <div style="flex: 1;">
                            <h5 style="color: var(--text-light); margin-bottom: 0.5rem; display: flex; align-items: center; gap: 0.5rem;">
                                <i class="bi bi-credit-card"></i>
                                ${escapeHtml(pm.displayName)}
                                ${pm.isDefault ? '<span class="ua-badge ua-badge-primary">Default</span>' : ''}
                            </h5>
                            <p style="color: var(--text-muted); font-size: 0.875rem; margin-bottom: 0.25rem;">
                                <strong>Expires:</strong> ${escapeHtml(pm.expirationDate)}
                            </p>
                            <p style="color: var(--text-muted); font-size: 0.875rem; margin: 0;">
                                <strong>Added:</strong> ${new Date(pm.createdDate).toLocaleDateString()}
                            </p>
                        </div>
                    </div>
                    <div style="display: flex; gap: 0.5rem; flex-wrap: wrap;">
                        ${!pm.isDefault ? `
                            <button class="btn-ua-secondary" style="flex: 1; min-width: 120px;" onclick="handleSetDefault(${pm.paymentMethodId})">
                                <i class="bi bi-star"></i> Set as Default
                            </button>
                        ` : ''}
                        <button class="btn-ua-danger" style="flex: 1; min-width: 120px;" onclick="handleDeletePaymentMethod(${pm.paymentMethodId})">
                            <i class="bi bi-trash"></i> Delete
                        </button>
                    </div>
                </div>
            `;
        });

        html += '</div>';
        paymentMethodsList.innerHTML = html;
    } catch (error) {
        paymentMethodsList.innerHTML = `
            <div class="ua-card">
                <div class="ua-alert ua-alert-danger">
                    <h5 style="margin-top: 0;">Error loading payment methods</h5>
                    <p>${escapeHtml(error.message)}</p>
                    <button class="btn-ua-secondary" onclick="loadPaymentMethods()">Retry</button>
                </div>
            </div>
        `;
    }
}

// Show add payment method modal
function showAddPaymentMethodModal() {
    const modalHtml = `
        <div class="modal fade" id="addPaymentMethodModal" tabindex="-1">
            <div class="modal-dialog">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title" style="color: var(--text-light);">Add Payment Method</h5>
                        <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
                    </div>
                    <div class="modal-body">
                        <form id="addPaymentMethodForm">
                            <div class="mb-3">
                                <label for="pmCardType" class="ua-form-label">Card Type</label>
                                <select class="ua-form-control" id="pmCardType" required>
                                    <option value="">Select card type...</option>
                                    <option value="Visa">Visa</option>
                                    <option value="MasterCard">MasterCard</option>
                                    <option value="American Express">American Express</option>
                                    <option value="Discover">Discover</option>
                                </select>
                            </div>
                            <div class="mb-3">
                                <label for="pmLastFour" class="ua-form-label">Last 4 Digits</label>
                                <input type="text" class="ua-form-control" id="pmLastFour" 
                                       maxlength="4" pattern="[0-9]{4}" 
                                       placeholder="1234" required>
                                <small class="ua-form-text">Enter the last 4 digits of your card</small>
                            </div>
                            <div class="mb-3">
                                <label for="pmExpiration" class="ua-form-label">Expiration Date</label>
                                <input type="text" class="ua-form-control" id="pmExpiration" 
                                       placeholder="MM/YYYY" pattern="(0[1-9]|1[0-2])/[0-9]{4}" 
                                       required>
                                <small class="ua-form-text">Format: MM/YYYY (e.g., 12/2025)</small>
                            </div>
                            <div class="mb-3">
                                <div class="form-check">
                                    <input class="form-check-input" type="checkbox" id="pmIsDefault" style="background-color: var(--bg-darker); border-color: var(--border-dark);">
                                    <label class="form-check-label" for="pmIsDefault" style="color: var(--text-light);">
                                        Set as default payment method
                                    </label>
                                </div>
                            </div>
                        </form>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn-ua-secondary" data-bs-dismiss="modal">Cancel</button>
                        <button type="button" class="btn-ua-primary" onclick="handleAddPaymentMethod()">Add Payment Method</button>
                    </div>
                </div>
            </div>
        </div>
    `;
    
    // Remove existing modal if any
    const existingModal = document.getElementById('addPaymentMethodModal');
    if (existingModal) existingModal.remove();
    
    // Add modal to body
    document.body.insertAdjacentHTML('beforeend', modalHtml);
    
    // Show modal
    const modal = new bootstrap.Modal(document.getElementById('addPaymentMethodModal'));
    modal.show();
    
    // Format expiration date input
    const expirationInput = document.getElementById('pmExpiration');
    if (expirationInput) {
        expirationInput.addEventListener('input', function(e) {
            let value = e.target.value.replace(/\D/g, '');
            if (value.length >= 2) {
                value = value.substring(0, 2) + '/' + value.substring(2, 6);
            }
            e.target.value = value;
        });
    }
    
    // Format last four digits input (numbers only)
    const lastFourInput = document.getElementById('pmLastFour');
    if (lastFourInput) {
        lastFourInput.addEventListener('input', function(e) {
            e.target.value = e.target.value.replace(/\D/g, '').substring(0, 4);
        });
    }
}

// Handle add payment method
async function handleAddPaymentMethod() {
    const cardType = document.getElementById('pmCardType')?.value;
    const lastFour = document.getElementById('pmLastFour')?.value;
    const expiration = document.getElementById('pmExpiration')?.value;
    const isDefault = document.getElementById('pmIsDefault')?.checked || false;

    // Validation
    if (!cardType) {
        showAlert('Please select a card type', 'danger');
        return;
    }

    if (!lastFour || lastFour.length !== 4) {
        showAlert('Please enter exactly 4 digits', 'danger');
        return;
    }

    if (!expiration || !/^(0[1-9]|1[0-2])\/[0-9]{4}$/.test(expiration)) {
        showAlert('Please enter a valid expiration date (MM/YYYY)', 'danger');
        return;
    }

    try {
        const response = await createPaymentMethod({
            cardType: cardType,
            lastFourDigits: lastFour,
            expirationDate: expiration,
            isDefault: isDefault
        });

        if (response.success) {
            showAlert('Payment method added successfully', 'success');
            bootstrap.Modal.getInstance(document.getElementById('addPaymentMethodModal')).hide();
            await loadPaymentMethods();
        } else {
            showAlert(response.error || 'Failed to add payment method', 'danger');
        }
    } catch (error) {
        showAlert(error.message || 'Failed to add payment method. Please try again.', 'danger');
    }
}

// Handle delete payment method
async function handleDeletePaymentMethod(paymentMethodId) {
    if (!confirm('Are you sure you want to delete this payment method? This action cannot be undone.')) {
        return;
    }

    try {
        const response = await deletePaymentMethod(paymentMethodId);
        
        if (response.success) {
            showAlert('Payment method deleted successfully', 'success');
            await loadPaymentMethods();
        } else {
            showAlert(response.error || 'Failed to delete payment method', 'danger');
        }
    } catch (error) {
        if (error.message && error.message.includes('used in orders')) {
            showAlert('Cannot delete payment method that has been used in orders', 'warning');
        } else {
            showAlert(error.message || 'Failed to delete payment method', 'danger');
        }
    }
}

// Set default payment method
async function setDefaultPaymentMethod(paymentMethodId) {
    try {
        if (!isAuthenticated()) {
            throw new Error('You must be logged in to set default payment method');
        }
        
        const response = await apiCall(`/payment-methods/${paymentMethodId}/default`, {
            method: 'PUT'
        });
        
        return response;
    } catch (error) {
        console.error('Failed to set default payment method:', error);
        throw error;
    }
}

// Handle set default payment method
async function handleSetDefault(paymentMethodId) {
    try {
        const response = await setDefaultPaymentMethod(paymentMethodId);
        
        if (response.success) {
            showAlert('Default payment method updated', 'success');
            await loadPaymentMethods();
        } else {
            showAlert(response.error || 'Failed to set default payment method', 'danger');
        }
    } catch (error) {
        showAlert(error.message || 'Failed to set default payment method', 'danger');
    }
}

// Load payment methods for checkout dropdown
async function loadPaymentMethodsForCheckout() {
    try {
        const response = await getPaymentMethods();
        const select = document.getElementById('savedPaymentMethodSelect');
        
        if (!select) return;
        
        if (!response.success || !response.data || response.data.length === 0) {
            select.innerHTML = '<option value="">No saved payment methods</option>';
            return;
        }
        
        let html = '<option value="">Select a payment method...</option>';
        response.data.forEach(pm => {
            html += `<option value="${pm.paymentMethodId}" ${pm.isDefault ? 'selected' : ''}>
                ${escapeHtml(pm.displayName)} ${pm.isDefault ? '(Default)' : ''}
            </option>`;
        });
        
        select.innerHTML = html;
    } catch (error) {
        const select = document.getElementById('savedPaymentMethodSelect');
        if (select) {
            select.innerHTML = '<option value="">Error loading payment methods</option>';
        }
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
window.getPaymentMethods = getPaymentMethods;
window.createPaymentMethod = createPaymentMethod;
window.deletePaymentMethod = deletePaymentMethod;
window.setDefaultPaymentMethod = setDefaultPaymentMethod;
window.showPaymentMethodsPage = showPaymentMethodsPage;
window.loadPaymentMethods = loadPaymentMethods;
window.showAddPaymentMethodModal = showAddPaymentMethodModal;
window.handleAddPaymentMethod = handleAddPaymentMethod;
window.handleDeletePaymentMethod = handleDeletePaymentMethod;
window.handleSetDefault = handleSetDefault;
window.loadPaymentMethodsForCheckout = loadPaymentMethodsForCheckout;

