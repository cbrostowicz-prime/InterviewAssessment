using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderHistory.Api.Data;
using OrderHistory.Api.Dtos;
using OrderHistory.Api.Services;

namespace OrderHistory.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController(OrderDbContext db, UserStatsService stats) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> List(CancellationToken ct)
    {
        var users = await db.Users
            .AsNoTracking()
            .OrderBy(u => u.Id)
            .Select(u => new UserDto(u.Id, u.Name, u.Email))
            .ToListAsync(ct);
        return Ok(users);
    }

    [HttpGet("{userId:int}/stats")]
    public async Task<ActionResult<UserStatsDto>> Stats(int userId, CancellationToken ct)
    {
        var result = await stats.GetAsync(userId, ct);
        return result is null ? NotFound() : Ok(result);
    }
}
