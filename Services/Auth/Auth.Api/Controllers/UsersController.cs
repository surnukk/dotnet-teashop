using Auth.Api.Common;
using Auth.Api.Contracts;
using Auth.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Platform.ServiceDefaults.Security;

namespace Auth.Api.Controllers;

[ApiController]
[Route("users")]
[Authorize]
public class UsersController(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    ApplicationDbContext db) : ControllerBase
{
    private bool IsOwnerOrAdmin(string id) => User.GetUserId() == id || User.IsAdmin();

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<PagedResponse<UserDto>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = userManager.Users.OrderBy(u => u.UserName);
        var total = await query.CountAsync(ct);
        var users = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        var ids = users.Select(u => u.Id).ToList();
        var roleRows = await (from ur in db.UserRoles
                              join r in db.Roles on ur.RoleId equals r.Id
                              where ids.Contains(ur.UserId)
                              select new { ur.UserId, RoleName = r.Name }).ToListAsync(ct);
        var rolesByUser = roleRows.ToLookup(x => x.UserId, x => x.RoleName!);

        var items = users.Select(u => ToDto(u, rolesByUser[u.Id].ToList())).ToList();
        return Ok(new PagedResponse<UserDto>(items, page, pageSize, total));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetById(string id)
    {
        if (!IsOwnerOrAdmin(id)) return Forbid();

        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        return Ok(ToDto(user, (await userManager.GetRolesAsync(user)).ToList()));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<UserDto>> Update(string id, UpdateUserRequest request)
    {
        if (!IsOwnerOrAdmin(id)) return Forbid();

        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        if (request.UserName is not null && request.UserName != user.UserName)
        {
            var r = await userManager.SetUserNameAsync(user, request.UserName);
            if (!r.Succeeded) throw AuthException.FromIdentity("Profile update failed", r);
        }

        if (request.PhoneNumber is not null && request.PhoneNumber != user.PhoneNumber)
        {
            var r = await userManager.SetPhoneNumberAsync(user, request.PhoneNumber);
            if (!r.Succeeded) throw AuthException.FromIdentity("Profile update failed", r);
        }

        return Ok(ToDto(user, (await userManager.GetRolesAsync(user)).ToList()));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(string id)
    {
        if (id == User.GetUserId())
            throw new AuthException(StatusCodes.Status400BadRequest, "You cannot delete your own account");

        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        var result = await userManager.DeleteAsync(user); 
        if (!result.Succeeded) throw AuthException.FromIdentity("Delete failed", result);

        return NoContent();
    }

    [HttpGet("{id}/refresh-tokens")]
    public async Task<ActionResult<IReadOnlyList<RefreshTokenDto>>> GetRefreshTokens(string id, CancellationToken ct)
    {
        if (!IsOwnerOrAdmin(id)) return Forbid();

        var now = DateTime.UtcNow;
        var tokens = await db.RefreshTokens.AsNoTracking()
            .Where(t => t.UserId == id)
            .OrderByDescending(t => t.CreatedAtUtc)
            .Select(t => new RefreshTokenDto(t.Id, t.CreatedAtUtc, t.ExpiresAtUtc, t.CreatedByIp,
                t.RevokedAtUtc, t.RevokeReason, t.RevokedAtUtc == null && t.ExpiresAtUtc > now))
            .ToListAsync(ct);

        return Ok(tokens);   
    }

    [HttpGet("{id}/roles")]
    public async Task<ActionResult<IList<string>>> GetRoles(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        return Ok(await userManager.GetRolesAsync(user));
    }

    [HttpPost("{id}/roles")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<IList<string>>> AssignRole(string id, AssignRoleRequest request)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        if (!await roleManager.RoleExistsAsync(request.Role))
            throw new AuthException(StatusCodes.Status404NotFound, $"Role '{request.Role}' does not exist");

        var result = await userManager.AddToRoleAsync(user, request.Role);
        if (!result.Succeeded) throw AuthException.FromIdentity("Role assignment failed", result);

        return Ok(await userManager.GetRolesAsync(user));
    }

    [HttpDelete("{id}/roles/{role}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<IList<string>>> RemoveRole(string id, string role)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        var result = await userManager.RemoveFromRoleAsync(user, role);
        if (!result.Succeeded) throw AuthException.FromIdentity("Role removal failed", result);

        return Ok(await userManager.GetRolesAsync(user));
    }

    private static UserDto ToDto(ApplicationUser u, IReadOnlyList<string> roles) =>
        new(u.Id, u.UserName!, u.Email, u.EmailConfirmed, u.PhoneNumber, u.CreatedAtUtc, roles);
}