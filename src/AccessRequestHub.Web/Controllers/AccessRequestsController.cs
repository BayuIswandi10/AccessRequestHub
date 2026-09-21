using AccessRequestHub.Application;
using AccessRequestHub.Application.DTOs;
using AccessRequestHub.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AccessRequestHub.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccessRequestsController : ControllerBase
{
    private readonly IAccessRequestService _service;

    public AccessRequestsController(IAccessRequestService service)
    {
        _service = service;
    }

    private Guid GetCurrentUserId()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(idClaim) || !Guid.TryParse(idClaim, out var userId))
            throw new UnauthorizedAccessException();
        return userId;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAccessRequestDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _service.CreateAsync(userId, dto);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "Authentication required." });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyRequests()
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _service.GetMyRequestsAsync(userId);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "Authentication required." });
        }
    }

    [HttpGet("inbox")]
    public async Task<IActionResult> GetApprovalInbox()
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _service.GetApprovalInboxAsync(userId);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "Authentication required." });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDetail(Guid id)
    {
        try
        {
            GetCurrentUserId();
            var result = await _service.GetDetailAsync(id);
            if (result == null)
                return NotFound(new { error = "Request not found." });
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "Authentication required." });
        }
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ApprovalActionDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _service.ApproveAsync(id, userId, dto);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "Authentication required." });
        }
        catch (ForbiddenException ex)
        {
            return StatusCode(403, new { error = ex.Message });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ConcurrencyException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] ApprovalActionDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _service.RejectAsync(id, userId, dto);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "Authentication required." });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (ForbiddenException ex)
        {
            return StatusCode(403, new { error = ex.Message });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ConcurrencyException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
