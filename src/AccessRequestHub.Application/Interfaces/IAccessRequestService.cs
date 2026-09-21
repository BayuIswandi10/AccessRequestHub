using AccessRequestHub.Application.DTOs;

namespace AccessRequestHub.Application.Interfaces;

public interface IAccessRequestService
{
    Task<AccessRequestDetailDto> CreateAsync(Guid requesterId, CreateAccessRequestDto dto);
    Task<List<AccessRequestListDto>> GetMyRequestsAsync(Guid requesterId);
    Task<List<AccessRequestListDto>> GetApprovalInboxAsync(Guid approverId);
    Task<AccessRequestDetailDto?> GetDetailAsync(Guid requestId);
    Task<AccessRequestDetailDto> ApproveAsync(Guid requestId, Guid approverId, ApprovalActionDto dto);
    Task<AccessRequestDetailDto> RejectAsync(Guid requestId, Guid approverId, ApprovalActionDto dto);
}
