using CrimsonBookStore.Models;

namespace CrimsonBookStore.Services;

public interface ISellSubmissionService
{
    Task<SellSubmissionCreateResponse> CreateSellSubmissionAsync(int userId, CreateSellSubmissionRequest request);
    Task<List<SellSubmissionListResponse>> GetUserSubmissionsAsync(int userId, string? status = null);
    Task<SellSubmissionResponse?> GetSubmissionDetailsAsync(int submissionId, int userId);
    Task<NegotiateResponse> NegotiateAsync(int submissionId, int userId, NegotiateRequest request);
    Task<List<AdminSellSubmissionListResponse>> GetAdminSubmissionsAsync(string? status = null);
    Task<AdminSellSubmissionResponse?> GetAdminSubmissionDetailsAsync(int submissionId);
    Task<NegotiateResponse> AdminNegotiateAsync(int submissionId, int adminUserId, AdminNegotiateRequest request);
    Task<ApproveSubmissionResponse> ApproveSubmissionAsync(int submissionId, int adminUserId, ApproveSubmissionRequest request);
    Task<bool> RejectSubmissionAsync(int submissionId, int adminUserId, RejectSubmissionRequest request);
}

