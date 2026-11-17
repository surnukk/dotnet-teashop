using Microsoft.AspNetCore.Identity;

namespace Auth.Api.Common;

public class AuthException(int statusCode, string title, IDictionary<string, string[]>? errors = null)
    : Exception(title)
{
    public int StatusCode { get; } = statusCode;
    public IDictionary<string, string[]>? Errors { get; } = errors;

    public static AuthException FromIdentity(string title, IdentityResult result) =>
        new(StatusCodes.Status400BadRequest, title,
            result.Errors
                .GroupBy(e => e.Code)
                .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray()));
}