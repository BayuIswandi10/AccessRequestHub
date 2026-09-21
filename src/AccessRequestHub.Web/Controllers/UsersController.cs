using AccessRequestHub.Application.DTOs;
using AccessRequestHub.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccessRequestHub.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly AccessRequestDbContext _context;

    public UsersController(AccessRequestDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _context.Users
            .OrderBy(u => u.Name)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("applications")]
    public async Task<IActionResult> GetApplications()
    {
        var apps = await _context.Applications
            .OrderBy(a => a.Name)
            .Select(a => new ApplicationDto
            {
                Id = a.Id,
                Name = a.Name
            })
            .ToListAsync();

        return Ok(apps);
    }
}
