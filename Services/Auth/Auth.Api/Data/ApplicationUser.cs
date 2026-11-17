using Microsoft.AspNetCore.Identity;

namespace Auth.Api.Data;

public class ApplicationUser : IdentityUser
{
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}