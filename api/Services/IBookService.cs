using CrimsonBookStore.Models;

namespace CrimsonBookStore.Services;

public interface IBookService
{
    Task<List<BookListResponse>> GetAvailableBooksAsync(string? search = null, string? isbn = null, string? courseMajor = null);
    Task<BookDetailResponse?> GetBookByIdAsync(int bookId);
    Task<List<BookListResponse>> SearchBooksAsync(string searchTerm);
    
    // Admin methods
    Task<int> CreateBookAsync(CreateBookRequest request);
    Task<bool> UpdateBookAsync(int bookId, UpdateBookRequest request);
    Task<bool> DeleteBookAsync(int bookId);
}

