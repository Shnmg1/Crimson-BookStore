// Books API functions and UI

// Get all available books
async function getBooks(search = null, isbn = null, courseMajor = null, page = 1, pageSize = 20) {
    try {
        const params = new URLSearchParams();
        if (search) params.append('search', search);
        if (isbn) params.append('isbn', isbn);
        if (courseMajor) params.append('courseMajor', courseMajor);
        params.append('page', page.toString());
        params.append('pageSize', pageSize.toString());

        const response = await apiCall(`/books?${params.toString()}`);
        return response;
    } catch (error) {
        console.error('Failed to fetch books:', error);
        throw error;
    }
}

// Get book by ID
async function getBookById(bookId) {
    try {
        const response = await apiCall(`/books/${bookId}`);
        return response;
    } catch (error) {
        console.error('Failed to fetch book:', error);
        throw error;
    }
}

// Search books
async function searchBooks(searchTerm) {
    try {
        const response = await apiCall(`/books/search?q=${encodeURIComponent(searchTerm)}`);
        return response;
    } catch (error) {
        console.error('Failed to search books:', error);
        throw error;
    }
}

// Get stock count for ISBN and edition
async function getStockCount(isbn, edition) {
    try {
        const response = await apiCall(`/books/stock/${encodeURIComponent(isbn)}/${encodeURIComponent(edition)}`);
        return response;
    } catch (error) {
        console.error('Failed to fetch stock count:', error);
        throw error;
    }
}

// Load and display books
async function loadBooks(searchTerm = null, page = 1) {
    const booksList = document.getElementById('booksList');
    if (!booksList) return;

    try {
        // Show loading state
        booksList.innerHTML = '<div class="col-12 text-center"><div class="spinner-border" role="status"><span class="visually-hidden">Loading...</span></div></div>';

        const response = await getBooks(searchTerm, null, null, page);
        
        if (!response.success || !response.data || response.data.length === 0) {
            booksList.innerHTML = '<div class="col-12 text-center"><p class="text-muted">No books found.</p></div>';
            return;
        }

        // Clear and display books
        booksList.innerHTML = '';
        
        response.data.forEach(book => {
            const bookCard = createBookCard(book);
            booksList.appendChild(bookCard);
        });

        // Attach event listeners to view details buttons
        document.querySelectorAll('.view-details-btn').forEach(btn => {
            btn.addEventListener('click', function() {
                const isbn = this.getAttribute('data-isbn');
                const edition = this.getAttribute('data-edition');
                showBookDetail(isbn, edition);
            });
        });

        // Display pagination info if available
        if (response.pagination && response.pagination.totalPages > 1) {
            displayPagination(response.pagination);
        }
    } catch (error) {
        booksList.innerHTML = `<div class="col-12"><div class="alert alert-danger">Error loading books: ${error.message}</div></div>`;
    }
}

// Create Bootstrap card for a book
function createBookCard(book) {
    const col = document.createElement('div');
    col.className = 'col-md-6 col-lg-4 mb-4';

    // Format price range
    const priceText = book.minPrice === book.maxPrice 
        ? `$${book.minPrice.toFixed(2)}`
        : `$${book.minPrice.toFixed(2)} - $${book.maxPrice.toFixed(2)}`;

    // Format stock count
    const stockText = book.availableCount > 0 
        ? `${book.availableCount} ${book.availableCount === 1 ? 'copy' : 'copies'} available`
        : 'Out of stock';

    // Format conditions
    const conditionsText = book.availableConditions && book.availableConditions.length > 0
        ? book.availableConditions.join(', ')
        : 'N/A';

    col.innerHTML = `
        <div class="card h-100">
            <div class="card-body">
                <h5 class="card-title">${escapeHtml(book.title)}</h5>
                <p class="card-text">
                    <strong>Author:</strong> ${escapeHtml(book.author)}<br>
                    <strong>ISBN:</strong> ${escapeHtml(book.isbn)}<br>
                    <strong>Edition:</strong> ${escapeHtml(book.edition)}<br>
                    ${book.courseMajor ? `<strong>Course:</strong> ${escapeHtml(book.courseMajor)}<br>` : ''}
                    <strong>Price:</strong> ${priceText}<br>
                    <strong>Condition:</strong> ${conditionsText}<br>
                    <strong>Stock:</strong> <span class="badge ${book.availableCount > 0 ? 'bg-success' : 'bg-danger'}">${stockText}</span>
                </p>
            </div>
            <div class="card-footer">
                <button class="btn btn-primary w-100 view-details-btn" 
                        data-isbn="${escapeHtml(book.isbn)}"
                        data-edition="${escapeHtml(book.edition)}"
                        ${book.availableCount === 0 ? 'disabled' : ''}>
                    ${book.availableCount > 0 ? 'View Details' : 'Out of Stock'}
                </button>
            </div>
        </div>
    `;

    return col;
}

// Display pagination controls
function displayPagination(pagination) {
    const booksList = document.getElementById('booksList');
    if (!booksList || pagination.totalPages <= 1) return;

    const paginationDiv = document.createElement('div');
    paginationDiv.className = 'col-12 mt-4';
    paginationDiv.innerHTML = `
        <nav aria-label="Books pagination">
            <ul class="pagination justify-content-center">
                <li class="page-item ${pagination.page === 1 ? 'disabled' : ''}">
                    <a class="page-link" href="#" onclick="loadBooksPage(${pagination.page - 1}); return false;">Previous</a>
                </li>
                ${Array.from({ length: pagination.totalPages }, (_, i) => i + 1)
                    .map(pageNum => `
                        <li class="page-item ${pageNum === pagination.page ? 'active' : ''}">
                            <a class="page-link" href="#" onclick="loadBooksPage(${pageNum}); return false;">${pageNum}</a>
                        </li>
                    `).join('')}
                <li class="page-item ${pagination.page === pagination.totalPages ? 'disabled' : ''}">
                    <a class="page-link" href="#" onclick="loadBooksPage(${pagination.page + 1}); return false;">Next</a>
                </li>
            </ul>
        </nav>
        <p class="text-center text-muted">Page ${pagination.page} of ${pagination.totalPages} (${pagination.totalItems} total books)</p>
    `;

    booksList.appendChild(paginationDiv);
}

// Load books for a specific page
function loadBooksPage(page) {
    const searchInput = document.getElementById('searchInput');
    const searchTerm = searchInput ? searchInput.value.trim() : null;
    loadBooks(searchTerm || null, page);
    
    // Scroll to top of books list
    const booksList = document.getElementById('booksList');
    if (booksList) {
        booksList.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }
}

// Show book detail modal/page
async function showBookDetail(isbn, edition) {
    try {
        // First, get books with this ISBN to find the bookId
        const booksResponse = await getBooks(null, isbn);
        
        if (!booksResponse.success || !booksResponse.data || booksResponse.data.length === 0) {
            showAlert('Book not found', 'danger');
            return;
        }

        // Find a book with matching edition (get the first available one)
        const matchingBook = booksResponse.data.find(b => 
            b.isbn === isbn && b.edition === edition && b.availableCount > 0
        );

        if (!matchingBook) {
            showAlert('No available copies found for this book', 'warning');
            return;
        }

        // Get all books with this ISBN+Edition to find a specific bookId
        // For now, we'll show the grouped information, but ideally we'd get a specific bookId
        // Since the API groups by ISBN+Edition, we'll create a detail view from the grouped data
        displayBookDetailModal(matchingBook, isbn, edition);
    } catch (error) {
        showAlert(`Error loading book details: ${error.message}`, 'danger');
    }
}

// Display book detail in a Bootstrap modal
function displayBookDetailModal(book, isbn, edition) {
    const app = document.getElementById('app');
    
    // Create modal HTML
    const modalHTML = `
        <div class="modal fade" id="bookDetailModal" tabindex="-1" aria-labelledby="bookDetailModalLabel" aria-hidden="true">
            <div class="modal-dialog modal-lg">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title" id="bookDetailModalLabel">${escapeHtml(book.title)}</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                    </div>
                    <div class="modal-body">
                        <div class="row">
                            <div class="col-md-6">
                                <h6>Book Information</h6>
                                <table class="table table-borderless">
                                    <tr>
                                        <th>Title:</th>
                                        <td>${escapeHtml(book.title)}</td>
                                    </tr>
                                    <tr>
                                        <th>Author:</th>
                                        <td>${escapeHtml(book.author)}</td>
                                    </tr>
                                    <tr>
                                        <th>ISBN:</th>
                                        <td>${escapeHtml(book.isbn)}</td>
                                    </tr>
                                    <tr>
                                        <th>Edition:</th>
                                        <td>${escapeHtml(book.edition)}</td>
                                    </tr>
                                    ${book.courseMajor ? `
                                    <tr>
                                        <th>Course/Major:</th>
                                        <td>${escapeHtml(book.courseMajor)}</td>
                                    </tr>
                                    ` : ''}
                                </table>
                            </div>
                            <div class="col-md-6">
                                <h6>Pricing & Availability</h6>
                                <table class="table table-borderless">
                                    <tr>
                                        <th>Price Range:</th>
                                        <td>
                                            ${book.minPrice === book.maxPrice 
                                                ? `<strong>$${book.minPrice.toFixed(2)}</strong>`
                                                : `<strong>$${book.minPrice.toFixed(2)} - $${book.maxPrice.toFixed(2)}</strong>`
                                            }
                                        </td>
                                    </tr>
                                    <tr>
                                        <th>Available Conditions:</th>
                                        <td>${book.availableConditions && book.availableConditions.length > 0 
                                            ? book.availableConditions.map(c => `<span class="badge bg-info me-1">${escapeHtml(c)}</span>`).join('')
                                            : 'N/A'
                                        }</td>
                                    </tr>
                                    <tr>
                                        <th>Stock:</th>
                                        <td>
                                            <span class="badge ${book.availableCount > 0 ? 'bg-success' : 'bg-danger'} fs-6">
                                                ${book.availableCount > 0 
                                                    ? `${book.availableCount} ${book.availableCount === 1 ? 'copy' : 'copies'} available`
                                                    : 'Out of stock'
                                                }
                                            </span>
                                        </td>
                                    </tr>
                                </table>
                            </div>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>
                        ${book.availableCount > 0 ? `
                        <div class="input-group me-2" style="max-width: 200px;">
                            <input type="number" class="form-control" id="bookIdInput" placeholder="Book ID" min="1">
                            <button type="button" class="btn btn-primary" 
                                    onclick="handleAddToCartById()">
                                Add to Cart
                            </button>
                        </div>
                        <small class="text-muted me-2">Enter a Book ID to add</small>
                        ` : `
                        <button type="button" class="btn btn-secondary" disabled>Out of Stock</button>
                        `}
                    </div>
                </div>
            </div>
        </div>
    `;

    // Remove existing modal if any
    const existingModal = document.getElementById('bookDetailModal');
    if (existingModal) {
        existingModal.remove();
    }

    // Add modal to body (not app div, so it overlays everything)
    document.body.insertAdjacentHTML('beforeend', modalHTML);

    // Show the modal
    const modal = new bootstrap.Modal(document.getElementById('bookDetailModal'));
    modal.show();

    // Clean up modal when hidden
    document.getElementById('bookDetailModal').addEventListener('hidden.bs.modal', function() {
        this.remove();
    });
}

// Handle Add to Cart by BookID (from modal input)
async function handleAddToCartById() {
    const bookIdInput = document.getElementById('bookIdInput');
    if (!bookIdInput) {
        showAlert('Book ID input not found', 'danger');
        return;
    }

    const bookId = parseInt(bookIdInput.value);
    if (!bookId || bookId <= 0) {
        showAlert('Please enter a valid Book ID', 'warning');
        return;
    }

    if (!isAuthenticated()) {
        showAlert('Please log in to add items to your cart', 'warning');
        const modal = bootstrap.Modal.getInstance(document.getElementById('bookDetailModal'));
        if (modal) modal.hide();
        setTimeout(() => showLoginPage(), 300);
        return;
    }

    try {
        // First verify the book exists and matches the ISBN+Edition
        const bookDetail = await getBookById(bookId);
        
        if (!bookDetail.success || !bookDetail.data) {
            showAlert('Book not found. Please enter a valid Book ID.', 'danger');
            return;
        }

        // Add to cart
        const response = await addToCart(bookId);
        
        if (response.success) {
            showAlert('Book added to cart successfully!', 'success');
            // Refresh cart badge
            await refreshCartBadge();
            // Close modal
            const modal = bootstrap.Modal.getInstance(document.getElementById('bookDetailModal'));
            if (modal) modal.hide();
        } else {
            showAlert(response.error || 'Failed to add book to cart', 'danger');
        }
    } catch (error) {
        showAlert(error.message || 'Failed to add book to cart', 'danger');
    }
}

// Add book to cart by BookID (new function for direct book ID)
async function addBookToCartById(bookId) {
    if (!isAuthenticated()) {
        showAlert('Please log in to add items to your cart', 'warning');
        return;
    }

    try {
        const response = await addToCart(bookId);
        
        if (response.success) {
            showAlert('Book added to cart successfully!', 'success');
            // Refresh cart badge
            await refreshCartBadge();
        } else {
            showAlert(response.error || 'Failed to add book to cart', 'danger');
        }
    } catch (error) {
        showAlert(error.message || 'Failed to add book to cart', 'danger');
    }
}

// Escape HTML to prevent XSS
function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// Make functions available globally
window.getBooks = getBooks;
window.getBookById = getBookById;
window.searchBooks = searchBooks;
window.getStockCount = getStockCount;
window.loadBooks = loadBooks;
window.loadBooksPage = loadBooksPage;
window.showBookDetail = showBookDetail;
window.handleAddToCartById = handleAddToCartById;
window.addBookToCartById = addBookToCartById;

