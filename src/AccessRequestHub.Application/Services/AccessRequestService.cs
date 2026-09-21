using AccessRequestHub.Application.DTOs;
using AccessRequestHub.Application.Interfaces;
using AccessRequestHub.Domain.Entities;
using AccessRequestHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using AppEntity = AccessRequestHub.Domain.Entities.Application;

namespace AccessRequestHub.Application.Services;

public class AccessRequestService : IAccessRequestService
{
    private readonly DbContext _context;

    public AccessRequestService(DbContext context)
    {
        _context = context;
    }

    public async Task<AccessRequestDetailDto> CreateAsync(Guid requesterId, CreateAccessRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.BusinessJustification))
            throw new ValidationException("Business justification is required.");

        var existing = await _context.Set<AccessRequest>()
            .Include(r => r.Requester)
            .Include(r => r.Application)
            .Include(r => r.AuditEvents)
                .ThenInclude(e => e.Actor)
            .FirstOrDefaultAsync(r => r.ClientRequestId == dto.ClientRequestId);

        if (existing != null)
            return MapToDetail(existing);

        var requester = await _context.Set<User>().FindAsync(requesterId)
            ?? throw new NotFoundException("Requester not found.");

        var application = await _context.Set<AppEntity>()
            .Include(a => a.SystemOwner)
            .FirstOrDefaultAsync(a => a.Id == dto.ApplicationId)
            ?? throw new NotFoundException("Application not found.");

        var request = new AccessRequest
        {
            Id = Guid.NewGuid(),
            ClientRequestId = dto.ClientRequestId,
            RequesterId = requesterId,
            ApplicationId = dto.ApplicationId,
            Environment = dto.Environment,
            AccessLevel = dto.AccessLevel,
            Status = RequestStatus.PendingManager,
            BusinessJustification = dto.BusinessJustification,
            PolicyVersion = "v1",
            CreatedAt = DateTime.UtcNow
        };

        var auditEvent = new AuditEvent
        {
            Id = Guid.NewGuid(),
            AccessRequestId = request.Id,
            ActorId = requesterId,
            Action = "RequestCreated",
            Timestamp = DateTime.UtcNow
        };

        _context.Set<AccessRequest>().Add(request);
        _context.Set<AuditEvent>().Add(auditEvent);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            var duplicate = await _context.Set<AccessRequest>()
                .Include(r => r.Requester)
                .Include(r => r.Application)
                .Include(r => r.AuditEvents)
                    .ThenInclude(e => e.Actor)
                .FirstAsync(r => r.ClientRequestId == dto.ClientRequestId);
            return MapToDetail(duplicate);
        }

        request.Requester = requester;
        request.Application = application;
        auditEvent.Actor = requester;
        request.AuditEvents = new List<AuditEvent> { auditEvent };

        return MapToDetail(request);
    }

    public async Task<List<AccessRequestListDto>> GetMyRequestsAsync(Guid requesterId)
    {
        return await _context.Set<AccessRequest>()
            .Where(r => r.RequesterId == requesterId)
            .Include(r => r.Application)
            .Include(r => r.Requester)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => MapToList(r))
            .ToListAsync();
    }

    public async Task<List<AccessRequestListDto>> GetApprovalInboxAsync(Guid approverId)
    {
        var user = await _context.Set<User>()
            .Include(u => u.DirectReports)
            .FirstOrDefaultAsync(u => u.Id == approverId)
            ?? throw new NotFoundException("User not found.");

        var ownedAppIds = await _context.Set<AppEntity>()
            .Where(a => a.SystemOwnerId == approverId)
            .Select(a => a.Id)
            .ToListAsync();

        var directReportIds = user.DirectReports.Select(d => d.Id).ToList();

        var requests = await _context.Set<AccessRequest>()
            .Include(r => r.Application)
            .Include(r => r.Requester)
            .Where(r =>
                (r.Status == RequestStatus.PendingManager && directReportIds.Contains(r.RequesterId) && r.RequesterId != approverId) ||
                (r.Status == RequestStatus.PendingSystemOwner && ownedAppIds.Contains(r.ApplicationId) && r.RequesterId != approverId))
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return requests.Select(MapToList).ToList();
    }

    public async Task<AccessRequestDetailDto?> GetDetailAsync(Guid requestId)
    {
        var request = await _context.Set<AccessRequest>()
            .Include(r => r.Requester)
            .Include(r => r.Application)
            .Include(r => r.AuditEvents.OrderBy(e => e.Timestamp))
                .ThenInclude(e => e.Actor)
            .FirstOrDefaultAsync(r => r.Id == requestId);

        return request == null ? null : MapToDetail(request);
    }

    public async Task<AccessRequestDetailDto> ApproveAsync(Guid requestId, Guid approverId, ApprovalActionDto dto)
    {
        var request = await _context.Set<AccessRequest>()
            .Include(r => r.Requester)
            .Include(r => r.Application)
            .Include(r => r.AuditEvents)
                .ThenInclude(e => e.Actor)
            .FirstOrDefaultAsync(r => r.Id == requestId)
            ?? throw new NotFoundException("Request not found.");

        if (request.RequesterId == approverId)
            throw new ForbiddenException("Cannot approve your own request.");

        _context.Entry(request).Property(r => r.RowVersion).OriginalValue = Convert.FromBase64String(dto.RowVersion);

        if (request.Status == RequestStatus.PendingManager)
        {
            await ValidateManagerApprover(approverId, request.RequesterId);

            if (request.IsHighRisk())
            {
                request.Status = RequestStatus.PendingSystemOwner;
                AddAuditEvent(request, approverId, "ManagerApproved", dto.Reason);
            }
            else
            {
                request.Status = RequestStatus.Approved;
                AddAuditEvent(request, approverId, "ManagerApproved", dto.Reason);
                AddAuditEvent(request, approverId, "RequestApproved", null);
            }
        }
        else if (request.Status == RequestStatus.PendingSystemOwner)
        {
            await ValidateSystemOwnerApprover(approverId, request.ApplicationId);
            request.Status = RequestStatus.Approved;
            AddAuditEvent(request, approverId, "SystemOwnerApproved", dto.Reason);
            AddAuditEvent(request, approverId, "RequestApproved", null);
        }
        else
        {
            throw new InvalidOperationException($"Request cannot be approved in status {request.Status}.");
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyException("This request has been modified by another user. Please refresh and try again.");
        }

        return MapToDetail(request);
    }

    public async Task<AccessRequestDetailDto> RejectAsync(Guid requestId, Guid approverId, ApprovalActionDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new ValidationException("Rejection reason is required.");

        var request = await _context.Set<AccessRequest>()
            .Include(r => r.Requester)
            .Include(r => r.Application)
            .Include(r => r.AuditEvents)
                .ThenInclude(e => e.Actor)
            .FirstOrDefaultAsync(r => r.Id == requestId)
            ?? throw new NotFoundException("Request not found.");

        if (request.RequesterId == approverId)
            throw new ForbiddenException("Cannot reject your own request.");

        _context.Entry(request).Property(r => r.RowVersion).OriginalValue = Convert.FromBase64String(dto.RowVersion);

        if (request.Status == RequestStatus.PendingManager)
        {
            await ValidateManagerApprover(approverId, request.RequesterId);
            request.Status = RequestStatus.Rejected;
            AddAuditEvent(request, approverId, "ManagerRejected", dto.Reason);
        }
        else if (request.Status == RequestStatus.PendingSystemOwner)
        {
            await ValidateSystemOwnerApprover(approverId, request.ApplicationId);
            request.Status = RequestStatus.Rejected;
            AddAuditEvent(request, approverId, "SystemOwnerRejected", dto.Reason);
        }
        else
        {
            throw new InvalidOperationException($"Request cannot be rejected in status {request.Status}.");
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyException("This request has been modified by another user. Please refresh and try again.");
        }

        return MapToDetail(request);
    }

    private async Task ValidateManagerApprover(Guid approverId, Guid requesterId)
    {
        var requester = await _context.Set<User>().FindAsync(requesterId)
            ?? throw new NotFoundException("Requester not found.");

        if (requester.ManagerId != approverId)
            throw new ForbiddenException("You are not the manager of this requester.");
    }

    private async Task ValidateSystemOwnerApprover(Guid approverId, Guid applicationId)
    {
        var application = await _context.Set<AppEntity>().FindAsync(applicationId)
            ?? throw new NotFoundException("Application not found.");

        if (application.SystemOwnerId != approverId)
            throw new ForbiddenException("You are not the system owner of this application.");
    }

    private void AddAuditEvent(AccessRequest request, Guid actorId, string action, string? reason)
    {
        var auditEvent = new AuditEvent
        {
            Id = Guid.NewGuid(),
            AccessRequestId = request.Id,
            ActorId = actorId,
            Action = action,
            Reason = reason,
            Timestamp = DateTime.UtcNow
        };
        _context.Set<AuditEvent>().Add(auditEvent);
        request.AuditEvents.Add(auditEvent);
    }

    private static AccessRequestDetailDto MapToDetail(AccessRequest r)
    {
        return new AccessRequestDetailDto
        {
            Id = r.Id,
            ClientRequestId = r.ClientRequestId,
            RequesterName = r.Requester?.Name ?? string.Empty,
            RequesterEmail = r.Requester?.Email ?? string.Empty,
            ApplicationName = r.Application?.Name ?? string.Empty,
            ApplicationId = r.ApplicationId,
            Environment = r.Environment,
            AccessLevel = r.AccessLevel,
            Status = r.Status,
            BusinessJustification = r.BusinessJustification,
            PolicyVersion = r.PolicyVersion,
            CreatedAt = r.CreatedAt,
            RowVersion = Convert.ToBase64String(r.RowVersion),
            AuditEvents = r.AuditEvents?.OrderBy(e => e.Timestamp).Select(e => new AuditEventDto
            {
                ActorName = e.Actor?.Name ?? string.Empty,
                Action = e.Action,
                Reason = e.Reason,
                Timestamp = e.Timestamp
            }).ToList() ?? new()
        };
    }

    private static AccessRequestListDto MapToList(AccessRequest r)
    {
        return new AccessRequestListDto
        {
            Id = r.Id,
            ApplicationName = r.Application?.Name ?? string.Empty,
            Environment = r.Environment,
            AccessLevel = r.AccessLevel,
            Status = r.Status,
            CreatedAt = r.CreatedAt,
            RequesterName = r.Requester?.Name ?? string.Empty
        };
    }
}
