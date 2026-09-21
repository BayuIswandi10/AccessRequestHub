using AccessRequestHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AccessRequestHub.Web.Middleware;

public class SimulatedAuthMiddleware
{
    private readonly RequestDelegate _next;

    public SimulatedAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, AccessRequestDbContext dbContext)
    {
        if (context.Request.Headers.TryGetValue("X-User-Email", out var emailHeader))
        {
            var email = emailHeader.ToString();
            var user = await dbContext.Users.SingleOrDefaultAsync(u => u.Email == email);

            if (user != null)
            {
                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Name),
                    new Claim(ClaimTypes.Email, user.Email)
                };

                var identity = new ClaimsIdentity(claims, "SimulatedAuth");
                context.User = new ClaimsPrincipal(identity);
            }
        }

        await _next(context);
    }
}
