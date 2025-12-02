// Admin API Functions

// Admin Books
async function getAdminBooks() {
    return apiCall('/books?admin=true');
}

async function createBook(bookData) {
    return apiCall('/books', {
        method: 'POST',
        body: JSON.stringify(bookData)
    });
}

async function updateBook(bookId, bookData) {
    return apiCall(`/books/${bookId}`, {
        method: 'PUT',
        body: JSON.stringify(bookData)
    });
}

async function deleteBook(bookId) {
    return apiCall(`/books/${bookId}`, {
        method: 'DELETE'
    });
}

// Admin Sell Submissions
async function getAdminSubmissions(status = null, page = 1, pageSize = 20) {
    let url = `/admin/sell-submissions?page=${page}&pageSize=${pageSize}`;
    if (status) {
        url += `&status=${status}`;
    }
    return apiCall(url);
}

async function adminNegotiate(submissionId, offerData) {
    return apiCall(`/admin/sell-submissions/${submissionId}/negotiate`, {
        method: 'POST',
        body: JSON.stringify(offerData)
    });
}

async function approveSubmission(submissionId, sellingPrice) {
    return apiCall(`/admin/sell-submissions/${submissionId}/approve`, {
        method: 'PUT',
        body: JSON.stringify({ sellingPrice })
    });
}

async function rejectSubmission(submissionId, reason = null) {
    return apiCall(`/admin/sell-submissions/${submissionId}/reject`, {
        method: 'PUT',
        body: JSON.stringify({ reason })
    });
}

async function getAdminSubmissionDetails(submissionId) {
    return apiCall(`/admin/sell-submissions/${submissionId}`);
}

// Admin Orders
async function getAdminOrders(status = null, page = 1, pageSize = 20) {
    let url = `/admin/orders?page=${page}&pageSize=${pageSize}`;
    if (status) {
        url += `&status=${status}`;
    }
    return apiCall(url);
}

async function updateOrderStatus(orderId, status) {
    return apiCall(`/orders/${orderId}/status`, {
        method: 'PUT',
        body: JSON.stringify({ status })
    });
}

// Admin Users
async function getAdminUsers(userType = null, page = 1, pageSize = 20) {
    let url = `/admin/users?page=${page}&pageSize=${pageSize}`;
    if (userType) {
        url += `&userType=${userType}`;
    }
    return apiCall(url);
}

// Export functions
window.getAdminBooks = getAdminBooks;
window.createBook = createBook;
window.updateBook = updateBook;
window.deleteBook = deleteBook;
window.getAdminSubmissions = getAdminSubmissions;
window.getAdminSubmissionDetails = getAdminSubmissionDetails;
window.adminNegotiate = adminNegotiate;
window.approveSubmission = approveSubmission;
window.rejectSubmission = rejectSubmission;
window.getAdminOrders = getAdminOrders;
window.updateOrderStatus = updateOrderStatus;
window.getAdminUsers = getAdminUsers;

