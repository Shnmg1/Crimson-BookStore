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
async function getBookById(bookId, admin = false) {
    try {
        const url = admin ? `/books/${bookId}?admin=true` : `/books/${bookId}`;
        const response = await apiCall(url);
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

// Get all individual copies of a book by ISBN and Edition
async function getBookCopies(isbn, edition) {
    try {
        const response = await apiCall(`/books/copies/${encodeURIComponent(isbn)}/${encodeURIComponent(edition)}`);
        return response;
    } catch (error) {
        console.error('Failed to fetch book copies:', error);
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
            booksList.innerHTML = `
                <div class="ua-card" style="grid-column: 1 / -1;">
                    <div class="ua-empty-state">
                        <div class="ua-empty-state-icon">
                            <i class="bi bi-book" style="font-size: 4rem;"></i>
                        </div>
                        <h5 style="color: var(--text-light);">No books found</h5>
                        <p style="color: var(--text-muted);">Try adjusting your search criteria.</p>
                    </div>
                </div>
            `;
            return;
        }

        // Clear and display books
        booksList.innerHTML = '';
        booksList.style.display = 'grid';
        booksList.style.gridTemplateColumns = 'repeat(auto-fill, minmax(300px, 1fr))';
        booksList.style.gap = '1.5rem';
        
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
        booksList.innerHTML = `
            <div class="ua-card" style="grid-column: 1 / -1;">
                <div class="ua-alert ua-alert-danger">
                    <h5 style="margin-top: 0;">Error loading books</h5>
                    <p>${escapeHtml(error.message)}</p>
                </div>
            </div>
        `;
    }
}

// Create Bootstrap card for a book
function createBookCard(book) {
    const col = document.createElement('div');

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
        ? book.availableConditions.map(c => `<span class="ua-badge ua-badge-info" style="margin-right: 0.25rem;">${escapeHtml(c)}</span>`).join('')
        : 'N/A';

    col.innerHTML = `
        <div class="ua-card" style="height: 100%; display: flex; flex-direction: column;">
            <div style="flex: 1;">
                <h5 style="color: var(--text-light); margin-bottom: 1rem; font-size: 1.125rem;">${escapeHtml(book.title)}</h5>
                <div style="color: var(--text-muted); font-size: 0.875rem; line-height: 1.6;">
                    <p style="margin-bottom: 0.5rem;"><strong style="color: var(--text-light);">Author:</strong> ${escapeHtml(book.author)}</p>
                    <p style="margin-bottom: 0.5rem;"><strong style="color: var(--text-light);">ISBN:</strong> ${escapeHtml(book.isbn)}</p>
                    <p style="margin-bottom: 0.5rem;"><strong style="color: var(--text-light);">Edition:</strong> ${escapeHtml(book.edition)}</p>
                    ${book.courseMajor ? `<p style="margin-bottom: 0.5rem;"><strong style="color: var(--text-light);">Course:</strong> ${escapeHtml(book.courseMajor)}</p>` : ''}
                    <p style="margin-bottom: 0.5rem;"><strong style="color: var(--text-light);">Price:</strong> <span style="color: var(--ua-crimson); font-weight: 600;">${priceText}</span></p>
                    <p style="margin-bottom: 0.5rem;"><strong style="color: var(--text-light);">Condition:</strong> ${conditionsText}</p>
                    <p style="margin-bottom: 0;"><strong style="color: var(--text-light);">Stock:</strong> <span class="ua-badge ${book.availableCount > 0 ? 'ua-badge-success' : 'ua-badge-danger'}">${stockText}</span></p>
                </div>
            </div>
            <div style="margin-top: 1rem; padding-top: 1rem; border-top: 1px solid var(--border-dark);">
                <button class="btn-ua-primary view-details-btn" 
                        data-isbn="${escapeHtml(book.isbn)}"
                        data-edition="${escapeHtml(book.edition)}"
                        style="width: 100%;"
                        ${book.availableCount === 0 ? 'disabled' : ''}>
                    ${book.availableCount > 0 ? '<i class="bi bi-eye"></i> View Details' : 'Out of Stock'}
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

        // Display modal with individual copies
        await displayBookDetailModal(matchingBook, isbn, edition);
    } catch (error) {
        showAlert(`Error loading book details: ${error.message}`, 'danger');
    }
}

// Display book detail in a Bootstrap modal
async function displayBookDetailModal(book, isbn, edition) {
    const app = document.getElementById('app');
    
    // Fetch individual copies
    let copies = [];
    let copiesError = null;
    try {
        const copiesResponse = await getBookCopies(isbn, edition);
        if (copiesResponse.success && copiesResponse.data) {
            copies = copiesResponse.data;
        }
    } catch (error) {
        copiesError = error.message;
    }
    
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
                        <div class="row mb-4">
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
                                <h6>Availability</h6>
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
                        
                        ${copies.length > 0 ? `
                        <div class="mt-4">
                            <h6>Available Copies</h6>
                            <p class="text-muted small">Select a copy to add to your cart:</p>
                            <div class="table-responsive">
                                <table class="table table-hover">
                                    <thead>
                                        <tr>
                                            <th>Price</th>
                                            <th>Condition</th>
                                            <th>Action</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        ${copies.map(copy => `
                                            <tr>
                                                <td><strong>$${copy.price.toFixed(2)}</strong></td>
                                                <td><span class="badge bg-info">${escapeHtml(copy.condition)}</span></td>
                                                <td>
                                                    <button class="btn btn-sm btn-success" 
                                                            onclick="addBookToCartById(${copy.bookId})">
                                                        Add to Cart
                                                    </button>
                                                </td>
                                            </tr>
                                        `).join('')}
                                    </tbody>
                                </table>
                            </div>
                        </div>
                        ` : `
                        ${copiesError ? `
                        <div class="alert alert-warning">
                            <small>Unable to load individual copies. ${escapeHtml(copiesError)}</small>
                        </div>
                        ` : `
                        <div class="alert alert-info">
                            <small>No copies available at this time.</small>
                        </div>
                        `}
                        `}
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>
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
window.getBookCopies = getBookCopies;
window.loadBooks = loadBooks;
window.loadBooksPage = loadBooksPage;
window.showBookDetail = showBookDetail;
window.handleAddToCartById = handleAddToCartById;
window.addBookToCartById = addBookToCartById;

