// Sell Submissions API functions and UI

// Create a sell submission
async function createSellSubmission(submissionData) {
    try {
        if (!isAuthenticated()) {
            throw new Error('You must be logged in to submit a book for sale');
        }

        const response = await apiCall('/sell-submissions', {
            method: 'POST',
            body: JSON.stringify(submissionData)
        });

        return response;
    } catch (error) {
        console.error('Failed to create sell submission:', error);
        throw error;
    }
}

// Get user's sell submissions
async function getSellSubmissions(status = null) {
    try {
        if (!isAuthenticated()) {
            throw new Error('You must be logged in to view submissions');
        }

        let endpoint = '/sell-submissions';
        if (status) {
            endpoint += `?status=${encodeURIComponent(status)}`;
        }

        const response = await apiCall(endpoint);
        return response;
    } catch (error) {
        console.error('Failed to fetch sell submissions:', error);
        throw error;
    }
}

// Get submission details with negotiation history
async function getSubmissionDetails(submissionId) {
    try {
        if (!isAuthenticated()) {
            throw new Error('You must be logged in to view submission details');
        }

        const response = await apiCall(`/sell-submissions/${submissionId}`);
        return response;
    } catch (error) {
        console.error('Failed to fetch submission details:', error);
        throw error;
    }
}

// Respond to negotiation (accept, reject, or counter)
async function negotiateSubmission(submissionId, action, negotiationId = null, offeredPrice = null, offerMessage = null) {
    try {
        if (!isAuthenticated()) {
            throw new Error('You must be logged in to respond to negotiations');
        }

        const requestBody = {
            action: action
        };

        if (action === 'accept' || action === 'reject') {
            if (!negotiationId) {
                throw new Error('NegotiationId is required for accept/reject actions');
            }
            requestBody.negotiationId = negotiationId;
        } else if (action === 'counter') {
            if (!offeredPrice || offeredPrice <= 0) {
                throw new Error('OfferedPrice is required and must be greater than 0 for counter action');
            }
            requestBody.offeredPrice = offeredPrice;
            if (offerMessage) {
                requestBody.offerMessage = offerMessage;
            }
        }

        const response = await apiCall(`/sell-submissions/${submissionId}/negotiate`, {
            method: 'POST',
            body: JSON.stringify(requestBody)
        });

        return response;
    } catch (error) {
        console.error('Failed to negotiate:', error);
        throw error;
    }
}

// Show sell submission form page
function showSellSubmissionPage() {
    if (!isAuthenticated()) {
        showAlert('Please log in to submit a book for sale', 'warning');
        showLoginPage();
        return;
    }

    const app = document.getElementById('app');
    app.innerHTML = `
        <div class="row justify-content-center">
            <div class="col-md-8">
                <div class="card">
                    <div class="card-header">
                        <h2 class="mb-0">Sell Your Book</h2>
                    </div>
                    <div class="card-body">
                        <p class="text-muted">Submit your book for review. Our team will review your submission and make an offer.</p>
                        
                        <form id="sellSubmissionForm">
                            <div class="row">
                                <div class="col-md-6 mb-3">
                                    <label for="submissionISBN" class="form-label">ISBN <span class="text-danger">*</span></label>
                                    <input type="text" class="form-control" id="submissionISBN" required>
                                </div>
                                <div class="col-md-6 mb-3">
                                    <label for="submissionEdition" class="form-label">Edition <span class="text-danger">*</span></label>
                                    <input type="text" class="form-control" id="submissionEdition" placeholder="e.g., 5th, 3rd Edition" required>
                                </div>
                            </div>
                            
                            <div class="mb-3">
                                <label for="submissionTitle" class="form-label">Title <span class="text-danger">*</span></label>
                                <input type="text" class="form-control" id="submissionTitle" required>
                            </div>
                            
                            <div class="mb-3">
                                <label for="submissionAuthor" class="form-label">Author <span class="text-danger">*</span></label>
                                <input type="text" class="form-control" id="submissionAuthor" required>
                            </div>
                            
                            <div class="row">
                                <div class="col-md-6 mb-3">
                                    <label for="submissionCondition" class="form-label">Physical Condition <span class="text-danger">*</span></label>
                                    <select class="form-select" id="submissionCondition" required>
                                        <option value="">Select condition...</option>
                                        <option value="New">New</option>
                                        <option value="Good">Good</option>
                                        <option value="Fair">Fair</option>
                                    </select>
                                </div>
                                <div class="col-md-6 mb-3">
                                    <label for="submissionCourseMajor" class="form-label">Course/Major (Optional)</label>
                                    <input type="text" class="form-control" id="submissionCourseMajor" placeholder="e.g., MIS 301">
                                </div>
                            </div>
                            
                            <div class="mb-3">
                                <label for="submissionAskingPrice" class="form-label">Asking Price ($) <span class="text-danger">*</span></label>
                                <div class="input-group">
                                    <span class="input-group-text">$</span>
                                    <input type="number" class="form-control" id="submissionAskingPrice" 
                                           step="0.01" min="0.01" required>
                                </div>
                                <small class="form-text text-muted">Enter the price you would like to receive for this book.</small>
                            </div>
                            
                            <div class="d-grid gap-2">
                                <button type="submit" class="btn btn-primary btn-lg">Submit for Review</button>
                                <button type="button" class="btn btn-secondary" onclick="showMySubmissionsPage()">Cancel</button>
                            </div>
                        </form>
                    </div>
                </div>
            </div>
        </div>
    `;

    // Handle form submission
    document.getElementById('sellSubmissionForm').addEventListener('submit', handleSellSubmissionSubmit);
}

// Handle sell submission form submit
async function handleSellSubmissionSubmit(e) {
    e.preventDefault();

    const submitBtn = e.target.querySelector('button[type="submit"]');
    const originalText = submitBtn.textContent;
    submitBtn.disabled = true;
    submitBtn.textContent = 'Submitting...';

    try {
        const submissionData = {
            isbn: document.getElementById('submissionISBN').value.trim(),
            title: document.getElementById('submissionTitle').value.trim(),
            author: document.getElementById('submissionAuthor').value.trim(),
            edition: document.getElementById('submissionEdition').value.trim(),
            physicalCondition: document.getElementById('submissionCondition').value,
            courseMajor: document.getElementById('submissionCourseMajor').value.trim() || null,
            askingPrice: parseFloat(document.getElementById('submissionAskingPrice').value)
        };

        // Validate
        if (submissionData.askingPrice <= 0) {
            throw new Error('Asking price must be greater than 0');
        }

        const response = await createSellSubmission(submissionData);

        if (response.success) {
            showAlert('Book submitted successfully! Our team will review your submission.', 'success');
            setTimeout(() => {
                showMySubmissionsPage();
            }, 1500);
        } else {
            showAlert(response.error || 'Failed to submit book', 'danger');
            submitBtn.disabled = false;
            submitBtn.textContent = originalText;
        }
    } catch (error) {
        showAlert(error.message || 'Failed to submit book. Please try again.', 'danger');
        submitBtn.disabled = false;
        submitBtn.textContent = originalText;
    }
}

// Show my submissions page
async function showMySubmissionsPage() {
    if (!isAuthenticated()) {
        showAlert('Please log in to view your submissions', 'warning');
        showLoginPage();
        return;
    }

    const app = document.getElementById('app');
    app.innerHTML = `
        <div class="row mb-3">
            <div class="col">
                <h2>My Sell Submissions</h2>
            </div>
            <div class="col-auto">
                <button class="btn btn-primary" onclick="showSellSubmissionPage()">
                    + Submit New Book
                </button>
            </div>
        </div>
        <div class="row mb-3">
            <div class="col-auto">
                <select class="form-select" id="submissionStatusFilter" onchange="loadMySubmissions()">
                    <option value="">All Statuses</option>
                    <option value="Pending Review">Pending Review</option>
                    <option value="Approved">Approved</option>
                    <option value="Rejected">Rejected</option>
                </select>
            </div>
        </div>
        <div id="submissionsList">
            <div class="text-center">
                <div class="spinner-border" role="status">
                    <span class="visually-hidden">Loading...</span>
                </div>
            </div>
        </div>
    `;

    await loadMySubmissions();
}

// Load and display user's submissions
async function loadMySubmissions() {
    const submissionsList = document.getElementById('submissionsList');
    if (!submissionsList) return;

    try {
        const statusFilter = document.getElementById('submissionStatusFilter')?.value || null;
        const response = await getSellSubmissions(statusFilter);

        if (!response.success || !response.data || response.data.length === 0) {
            submissionsList.innerHTML = `
                <div class="alert alert-info">
                    <h5>No submissions found</h5>
                    <p>You haven't submitted any books for sale yet.</p>
                    <button class="btn btn-primary" onclick="showSellSubmissionPage()">Submit Your First Book</button>
                </div>
            `;
            return;
        }

        // Display submissions
        let html = '<div class="list-group">';

        response.data.forEach(submission => {
            const submissionDate = new Date(submission.submissionDate);
            const statusBadgeClass = {
                'Pending Review': 'bg-warning',
                'Approved': 'bg-success',
                'Rejected': 'bg-danger'
            }[submission.status] || 'bg-secondary';

            html += `
                <div class="list-group-item list-group-item-action">
                    <div class="d-flex w-100 justify-content-between align-items-start">
                        <div class="flex-grow-1">
                            <div class="d-flex justify-content-between align-items-start mb-2">
                                <h5 class="mb-0">${escapeHtml(submission.title)}</h5>
                                <span class="badge ${statusBadgeClass} ms-2">${escapeHtml(submission.status)}</span>
                            </div>
                            <div class="row">
                                <div class="col-md-6">
                                    <p class="mb-1">
                                        <strong>Author:</strong> ${escapeHtml(submission.author)}<br>
                                        <strong>ISBN:</strong> ${escapeHtml(submission.isbn)}<br>
                                        <strong>Edition:</strong> ${escapeHtml(submission.edition)}
                                    </p>
                                </div>
                                <div class="col-md-3">
                                    <p class="mb-1">
                                        <strong>Asking Price:</strong><br>
                                        <span class="text-primary fw-bold">$${submission.askingPrice.toFixed(2)}</span>
                                    </p>
                                </div>
                                <div class="col-md-3">
                                    <p class="mb-1">
                                        <strong>Submitted:</strong><br>
                                        <small class="text-muted">${submissionDate.toLocaleDateString()}</small>
                                    </p>
                                </div>
                            </div>
                        </div>
                        <div class="ms-3">
                            <button class="btn btn-sm btn-outline-primary" onclick="showSubmissionDetails(${submission.submissionId})">
                                View Details
                            </button>
                        </div>
                    </div>
                </div>
            `;
        });

        html += '</div>';
        submissionsList.innerHTML = html;
    } catch (error) {
        submissionsList.innerHTML = `
            <div class="alert alert-danger">
                <h5>Error loading submissions</h5>
                <p>${escapeHtml(error.message)}</p>
                <button class="btn btn-secondary" onclick="loadMySubmissions()">Retry</button>
            </div>
        `;
    }
}

// Show submission details with negotiation history
async function showSubmissionDetails(submissionId) {
    try {
        const response = await getSubmissionDetails(submissionId);

        if (!response.success || !response.data) {
            showAlert('Submission not found', 'danger');
            return;
        }

        const submission = response.data;
        const submissionDate = new Date(submission.submissionDate);
        const statusBadgeClass = {
            'Pending Review': 'bg-warning',
            'Approved': 'bg-success',
            'Rejected': 'bg-danger'
        }[submission.status] || 'bg-secondary';

        const app = document.getElementById('app');
        app.innerHTML = `
            <div class="mb-3">
                <button class="btn btn-secondary" onclick="showMySubmissionsPage()">
                    ← Back to My Submissions
                </button>
            </div>

            <div class="card mb-4">
                <div class="card-header">
                    <h4>Submission #${submission.submissionId}</h4>
                </div>
                <div class="card-body">
                    <div class="row mb-3">
                        <div class="col-md-6">
                            <h6>Book Information</h6>
                            <table class="table table-borderless table-sm">
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
                            </table>
                        </div>
                        <div class="col-md-6">
                            <h6>Submission Details</h6>
                            <table class="table table-borderless table-sm">
                                <tr>
                                    <th>Status:</th>
                                    <td><span class="badge ${statusBadgeClass}">${escapeHtml(submission.status)}</span></td>
                                </tr>
                                <tr>
                                    <th>Asking Price:</th>
                                    <td><strong class="text-primary">$${submission.askingPrice.toFixed(2)}</strong></td>
                                </tr>
                                <tr>
                                    <th>Submitted:</th>
                                    <td>${submissionDate.toLocaleDateString()} ${submissionDate.toLocaleTimeString()}</td>
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
                            ${(() => {
                                // Find the latest pending admin offer
                                const latestPendingAdminOffer = submission.negotiations
                                    .filter(n => n.offeredBy === 'Admin' && n.offerStatus === 'Pending')
                                    .sort((a, b) => b.roundNumber - a.roundNumber)[0];
                                
                                return submission.negotiations.map((negotiation, index) => {
                                    const offerDate = new Date(negotiation.offerDate);
                                    const offerStatusBadgeClass = {
                                        'Pending': 'bg-warning',
                                        'Accepted': 'bg-success',
                                        'Rejected': 'bg-danger'
                                    }[negotiation.offerStatus] || 'bg-secondary';
                                    
                                    const isLatestPending = latestPendingAdminOffer && 
                                        negotiation.negotiationId === latestPendingAdminOffer.negotiationId;

                                    return `
                                        <div class="card mb-3 ${negotiation.offerStatus === 'Pending' ? 'border-warning' : ''}">
                                            <div class="card-body">
                                                <div class="d-flex justify-content-between align-items-start mb-2">
                                                    <div>
                                                        <h6 class="mb-0">
                                                            Round ${negotiation.roundNumber} - ${negotiation.offeredBy === 'Admin' ? 'Admin Offer' : 'Your Counter-Offer'}
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
                                                        <p class="mb-0">
                                                            <strong>Message:</strong> ${escapeHtml(negotiation.offerMessage)}
                                                        </p>
                                                    ` : ''}
                                                </div>
                                                ${negotiation.offerStatus === 'Pending' && negotiation.offeredBy === 'Admin' && submission.status === 'Pending Review' ? `
                                                    ${isLatestPending ? `
                                                        <div class="mt-3">
                                                            <button class="btn btn-sm btn-success me-2" 
                                                                    onclick="handleNegotiationAction(${submission.submissionId}, ${negotiation.negotiationId}, 'accept')">
                                                                Accept Offer
                                                            </button>
                                                            <button class="btn btn-sm btn-danger me-2" 
                                                                    onclick="handleNegotiationAction(${submission.submissionId}, ${negotiation.negotiationId}, 'reject')">
                                                                Reject Offer
                                                            </button>
                                                            <button class="btn btn-sm btn-primary" 
                                                                    onclick="showCounterOfferForm(${submission.submissionId}, ${negotiation.negotiationId})">
                                                                Counter-Offer
                                                            </button>
                                                        </div>
                                                    ` : `
                                                        <div class="mt-3">
                                                            <span class="badge bg-secondary">This offer has been superseded by a newer offer</span>
                                                        </div>
                                                    `}
                                                ` : ''}
                                            </div>
                                        </div>
                                    `;
                                }).join('');
                            })()}
                        </div>
                    ` : `
                        <div class="alert alert-info">
                            <p class="mb-0">No negotiations yet. Waiting for admin review.</p>
                        </div>
                    `}
                </div>
            </div>
        `;
    } catch (error) {
        showAlert(error.message || 'Failed to load submission details', 'danger');
    }
}

// Handle negotiation action (accept or reject)
async function handleNegotiationAction(submissionId, negotiationId, action) {
    if (!confirm(`Are you sure you want to ${action} this offer?`)) {
        return;
    }

    try {
        const response = await negotiateSubmission(submissionId, action, negotiationId);

        if (response.success) {
            showAlert(response.data.message || `Offer ${action}ed successfully`, 'success');
            // Reload submission details
            setTimeout(() => {
                showSubmissionDetails(submissionId);
            }, 1000);
        } else {
            showAlert(response.error || `Failed to ${action} offer`, 'danger');
        }
    } catch (error) {
        showAlert(error.message || `Failed to ${action} offer. Please try again.`, 'danger');
    }
}

// Show counter-offer form
function showCounterOfferForm(submissionId, negotiationId) {
    const app = document.getElementById('app');
    
    // Create modal for counter-offer
    const modalHTML = `
        <div class="modal fade" id="counterOfferModal" tabindex="-1" aria-labelledby="counterOfferModalLabel" aria-hidden="true">
            <div class="modal-dialog">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title" id="counterOfferModalLabel">Make Counter-Offer</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                    </div>
                    <div class="modal-body">
                        <form id="counterOfferForm">
                            <div class="mb-3">
                                <label for="counterOfferPrice" class="form-label">Your Counter-Offer Price ($)</label>
                                <div class="input-group">
                                    <span class="input-group-text">$</span>
                                    <input type="number" class="form-control" id="counterOfferPrice" 
                                           step="0.01" min="0.01" required>
                                </div>
                            </div>
                            <div class="mb-3">
                                <label for="counterOfferMessage" class="form-label">Message (Optional)</label>
                                <textarea class="form-control" id="counterOfferMessage" rows="3" 
                                          placeholder="Add any message to your counter-offer..."></textarea>
                            </div>
                        </form>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                        <button type="button" class="btn btn-primary" onclick="handleCounterOffer(${submissionId}, ${negotiationId})">
                            Submit Counter-Offer
                        </button>
                    </div>
                </div>
            </div>
        </div>
    `;

    // Remove existing modal if any
    const existingModal = document.getElementById('counterOfferModal');
    if (existingModal) {
        existingModal.remove();
    }

    // Add modal to body
    document.body.insertAdjacentHTML('beforeend', modalHTML);

    // Show the modal
    const modal = new bootstrap.Modal(document.getElementById('counterOfferModal'));
    modal.show();

    // Clean up modal when hidden
    document.getElementById('counterOfferModal').addEventListener('hidden.bs.modal', function() {
        this.remove();
    });
}

// Handle counter-offer submission
async function handleCounterOffer(submissionId, negotiationId) {
    const priceInput = document.getElementById('counterOfferPrice');
    const messageInput = document.getElementById('counterOfferMessage');

    if (!priceInput || !priceInput.value || parseFloat(priceInput.value) <= 0) {
        showAlert('Please enter a valid counter-offer price', 'warning');
        return;
    }

    const price = parseFloat(priceInput.value);
    const message = messageInput ? messageInput.value.trim() : null;

    // Close modal first
    const modal = bootstrap.Modal.getInstance(document.getElementById('counterOfferModal'));
    if (modal) modal.hide();

    try {
        const response = await negotiateSubmission(submissionId, 'counter', null, price, message);

        if (response.success) {
            showAlert('Counter-offer submitted successfully!', 'success');
            // Reload submission details
            setTimeout(() => {
                showSubmissionDetails(submissionId);
            }, 1000);
        } else {
            showAlert(response.error || 'Failed to submit counter-offer', 'danger');
        }
    } catch (error) {
        showAlert(error.message || 'Failed to submit counter-offer. Please try again.', 'danger');
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
window.createSellSubmission = createSellSubmission;
window.getSellSubmissions = getSellSubmissions;
window.getSubmissionDetails = getSubmissionDetails;
window.negotiateSubmission = negotiateSubmission;
window.showSellSubmissionPage = showSellSubmissionPage;
window.showMySubmissionsPage = showMySubmissionsPage;
window.loadMySubmissions = loadMySubmissions;
window.showSubmissionDetails = showSubmissionDetails;
window.handleNegotiationAction = handleNegotiationAction;
window.showCounterOfferForm = showCounterOfferForm;
window.handleCounterOffer = handleCounterOffer;

