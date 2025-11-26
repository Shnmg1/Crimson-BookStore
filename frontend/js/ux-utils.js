// UX Utilities - Toast notifications, loading states, form validation, etc.

// Toast notification system
let toastContainer = null;

function initToastContainer() {
    if (!toastContainer) {
        toastContainer = document.createElement('div');
        toastContainer.className = 'toast-container position-fixed top-0 end-0 p-3';
        toastContainer.style.zIndex = '9999';
        document.body.appendChild(toastContainer);
    }
    return toastContainer;
}

function showToast(message, type = 'info', duration = 5000) {
    const container = initToastContainer();
    
    const toastId = `toast-${Date.now()}`;
    const bgClass = {
        'success': 'bg-success',
        'danger': 'bg-danger',
        'warning': 'bg-warning',
        'info': 'bg-info'
    }[type] || 'bg-info';
    
    const toastHtml = `
        <div id="${toastId}" class="toast" role="alert" aria-live="assertive" aria-atomic="true">
            <div class="toast-header ${bgClass} text-white">
                <strong class="me-auto">${type.charAt(0).toUpperCase() + type.slice(1)}</strong>
                <button type="button" class="btn-close btn-close-white" data-bs-dismiss="toast"></button>
            </div>
            <div class="toast-body">
                ${escapeHtml(message)}
            </div>
        </div>
    `;
    
    container.insertAdjacentHTML('beforeend', toastHtml);
    const toastElement = document.getElementById(toastId);
    const toast = new bootstrap.Toast(toastElement, { delay: duration });
    toast.show();
    
    // Remove element after it's hidden
    toastElement.addEventListener('hidden.bs.toast', () => {
        toastElement.remove();
    });
}

// Loading state management
function setLoadingState(element, isLoading, loadingText = 'Loading...') {
    if (!element) return;
    
    if (isLoading) {
        element.dataset.originalContent = element.innerHTML;
        element.disabled = true;
        
        if (element.tagName === 'BUTTON') {
            element.innerHTML = `
                <span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>
                ${loadingText}
            `;
        }
    } else {
        element.disabled = false;
        if (element.dataset.originalContent) {
            element.innerHTML = element.dataset.originalContent;
            delete element.dataset.originalContent;
        }
    }
}

// Form validation with real-time feedback
function validateField(field, rules) {
    const value = field.value.trim();
    const errors = [];
    
    if (rules.required && !value) {
        errors.push(`${rules.label || field.name} is required`);
    }
    
    if (rules.minLength && value.length < rules.minLength) {
        errors.push(`${rules.label || field.name} must be at least ${rules.minLength} characters`);
    }
    
    if (rules.maxLength && value.length > rules.maxLength) {
        errors.push(`${rules.label || field.name} must be no more than ${rules.maxLength} characters`);
    }
    
    if (rules.pattern && value && !rules.pattern.test(value)) {
        errors.push(rules.patternMessage || `${rules.label || field.name} format is invalid`);
    }
    
    if (rules.min && value && parseFloat(value) < rules.min) {
        errors.push(`${rules.label || field.name} must be at least ${rules.min}`);
    }
    
    if (rules.max && value && parseFloat(value) > rules.max) {
        errors.push(`${rules.label || field.name} must be no more than ${rules.max}`);
    }
    
    if (rules.email && value && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value)) {
        errors.push('Please enter a valid email address');
    }
    
    return errors;
}

function setupFormValidation(formId, validationRules) {
    const form = document.getElementById(formId);
    if (!form) return;
    
    const fields = form.querySelectorAll('input, select, textarea');
    const fieldErrors = {};
    
    fields.forEach(field => {
        const fieldName = field.id || field.name;
        const rules = validationRules[fieldName];
        
        if (!rules) return;
        
        // Real-time validation on blur
        field.addEventListener('blur', () => {
            const errors = validateField(field, rules);
            displayFieldError(field, errors);
        });
        
        // Clear errors on input
        field.addEventListener('input', () => {
            if (fieldErrors[fieldName]) {
                clearFieldError(field);
                delete fieldErrors[fieldName];
            }
        });
    });
    
    // Validate on submit
    form.addEventListener('submit', (e) => {
        let hasErrors = false;
        
        fields.forEach(field => {
            const fieldName = field.id || field.name;
            const rules = validationRules[fieldName];
            
            if (!rules) return;
            
            const errors = validateField(field, rules);
            if (errors.length > 0) {
                displayFieldError(field, errors);
                fieldErrors[fieldName] = errors;
                hasErrors = true;
            } else {
                clearFieldError(field);
                delete fieldErrors[fieldName];
            }
        });
        
        if (hasErrors) {
            e.preventDefault();
            showToast('Please fix the errors in the form', 'danger');
        }
    });
}

function displayFieldError(field, errors) {
    if (errors.length === 0) {
        clearFieldError(field);
        return;
    }
    
    // Remove existing error message
    clearFieldError(field);
    
    // Add error class
    field.classList.add('is-invalid');
    
    // Create error message element
    const errorDiv = document.createElement('div');
    errorDiv.className = 'invalid-feedback';
    errorDiv.textContent = errors[0]; // Show first error
    field.parentNode.appendChild(errorDiv);
}

function clearFieldError(field) {
    field.classList.remove('is-invalid');
    const errorDiv = field.parentNode.querySelector('.invalid-feedback');
    if (errorDiv) {
        errorDiv.remove();
    }
}

// Confirmation dialog
function confirmAction(message, title = 'Confirm Action') {
    return new Promise((resolve) => {
        const modalHtml = `
            <div class="modal fade" id="confirmModal" tabindex="-1">
                <div class="modal-dialog">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h5 class="modal-title">${escapeHtml(title)}</h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                        </div>
                        <div class="modal-body">
                            <p>${escapeHtml(message)}</p>
                        </div>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                            <button type="button" class="btn btn-danger" id="confirmBtn">Confirm</button>
                        </div>
                    </div>
                </div>
            </div>
        `;
        
        // Remove existing modal if any
        const existingModal = document.getElementById('confirmModal');
        if (existingModal) existingModal.remove();
        
        document.body.insertAdjacentHTML('beforeend', modalHtml);
        const modal = new bootstrap.Modal(document.getElementById('confirmModal'));
        
        document.getElementById('confirmBtn').addEventListener('click', () => {
            modal.hide();
            resolve(true);
        });
        
        modal._element.addEventListener('hidden.bs.modal', () => {
            modal._element.remove();
            resolve(false);
        });
        
        modal.show();
    });
}

// Empty state component
function showEmptyState(container, message, actionText = null, actionCallback = null) {
    if (!container) return;
    
    container.innerHTML = `
        <div class="text-center py-5">
            <svg xmlns="http://www.w3.org/2000/svg" width="64" height="64" fill="currentColor" class="bi bi-inbox text-muted mb-3" viewBox="0 0 16 16">
                <path d="M4.98 4a.5.5 0 0 0-.39.188L1.54 8H6a.5.5 0 0 1 .5.5 1.5 1.5 0 1 0 3 0A.5.5 0 0 1 8 8h4.46l-3.05-3.812A.5.5 0 0 0 9.02 4H4.98zm9.954 5H10.45a2.5 2.5 0 0 1-4.9 0H1.066l.32 2.562a.5.5 0 0 0 .497.438h12.234a.5.5 0 0 0 .496-.438L14.933 9zM3.809 3.563A1.5 1.5 0 0 1 4.981 3h6.038a1.5 1.5 0 0 1 1.172.563l3.7 4.625a.5.5 0 0 1 .105.374l-.39 3.124A1.5 1.5 0 0 1 14.117 13H1.883a1.5 1.5 0 0 1-1.489-1.314l-.39-3.124a.5.5 0 0 1 .106-.374l3.7-4.625z"/>
            </svg>
            <h5 class="text-muted">${escapeHtml(message)}</h5>
            ${actionText && actionCallback ? `
                <button class="btn btn-primary mt-3" onclick="${actionCallback}">${escapeHtml(actionText)}</button>
            ` : ''}
        </div>
    `;
}

// Loading spinner component
function showLoadingSpinner(container, message = 'Loading...') {
    if (!container) return;
    
    container.innerHTML = `
        <div class="text-center py-5">
            <div class="spinner-border text-primary" role="status" style="width: 3rem; height: 3rem;">
                <span class="visually-hidden">${escapeHtml(message)}</span>
            </div>
            <p class="mt-3 text-muted">${escapeHtml(message)}</p>
        </div>
    `;
}

// Error display component
function showError(container, error, title = 'Error') {
    if (!container) return;
    
    const errorMessage = error?.message || error?.error || error || 'An unexpected error occurred';
    
    container.innerHTML = `
        <div class="alert alert-danger" role="alert">
            <h5 class="alert-heading">
                <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" fill="currentColor" class="bi bi-exclamation-triangle me-2" viewBox="0 0 16 16">
                    <path d="M7.938 2.016A.13.13 0 0 1 8.002 2a.13.13 0 0 1 .063.016.146.146 0 0 1 .054.057l6.857 11.667c.036.06.035.124.002.183a.163.163 0 0 1-.054.06.116.116 0 0 1-.066.017H1.146a.115.115 0 0 1-.066-.017.163.163 0 0 1-.054-.06.176.176 0 0 1 .002-.183L7.884 2.073a.147.147 0 0 1 .054-.057zm1.044-.45a1.13 1.13 0 0 0-1.96 0L.165 13.233c-.457.778.091 1.767.98 1.767h13.713c.889 0 1.438-.99.98-1.767L8.982 1.566z"/>
                    <path d="M7.002 12a1 1 0 1 1 2 0 1 1 0 0 1-2 0zM7.1 5.995a.905.905 0 1 1 1.8 0l-.35 3.507a.552.552 0 0 1-1.1 0L7.1 5.995z"/>
                </svg>
                ${escapeHtml(title)}
            </h5>
            <p class="mb-0">${escapeHtml(errorMessage)}</p>
        </div>
    `;
}

// Helper function to escape HTML
function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// Enhanced API call wrapper with loading states
async function apiCallWithLoading(endpoint, options = {}, loadingElement = null, loadingText = 'Loading...') {
    if (loadingElement) {
        setLoadingState(loadingElement, true, loadingText);
    }
    
    try {
        const response = await apiCall(endpoint, options);
        if (loadingElement) {
            setLoadingState(loadingElement, false);
        }
        return response;
    } catch (error) {
        if (loadingElement) {
            setLoadingState(loadingElement, false);
        }
        throw error;
    }
}

// Export functions
window.showToast = showToast;
window.setLoadingState = setLoadingState;
window.setupFormValidation = setupFormValidation;
window.confirmAction = confirmAction;
window.showEmptyState = showEmptyState;
window.showLoadingSpinner = showLoadingSpinner;
window.showError = showError;
window.apiCallWithLoading = apiCallWithLoading;

