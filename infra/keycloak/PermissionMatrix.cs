using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

public static class PermissionMatrix
{
    public const string Admin = "admin";
    public const string Manager = "manager";
    public const string User = "user";
    public const string Guest = "guest";

    public static readonly string[] Roles = { Admin, Manager, User, Guest };

    // Таблиця 6: повний набір scopes на кожному Resource Server
    public static readonly Dictionary<string, string[]> Scopes = new()
    {
        ["orders-api"] = new[]
        {
            "orders:read", "orders:create", "orders:update", "orders:delete", "orders:approve"
        },
        ["products-api"] = new[]
        {
            "products:read", "products:create", "products:update", "products:delete", "products:publish"
        },
        ["payments-api"] = new[]
        {
            "payments:read", "payments:process", "payments:refund"
        },
    };

    // Таблиця 7: Resource Server -> роль -> scopes.
    // Ролі, яких тут немає, доступу до цього Resource Server не мають.
    public static readonly Dictionary<string, Dictionary<string, string[]>> RoleScopes = new()
    {
        ["orders-api"] = new()
        {
            [Admin] = Scopes["orders-api"],
            [Manager] = new[] { "orders:read", "orders:update", "orders:approve" },
            [User] = new[] { "orders:read", "orders:create" },
        },
        ["products-api"] = new()
        {
            [Admin] = Scopes["products-api"],
            [Manager] = new[] { "products:read", "products:update", "products:publish" },
            [User] = new[] { "products:read" },
            [Guest] = new[] { "products:read" },
        },
        // Свідоме рішення: платежі доступні ЛИШЕ admin
        ["payments-api"] = new()
        {
            [Admin] = Scopes["payments-api"],
        },
    };

    public static string BuildAuthorizationAttribute(string role)
    {
        var values = RoleScopes
            .Where(api => api.Value.ContainsKey(role))
            .Select(api => JsonSerializer.Serialize(new { rsname = api.Key, scopes = api.Value[role] }));

        return string.Join("##", values);
    }
}