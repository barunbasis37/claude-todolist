using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace TodoList.Mcp;

public static class ApiKeyAuthenticationDefaults
{
    public const string AuthenticationScheme = "ApiKey";
}

public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
}

/// <summary>
/// Gates the MCP endpoint behind a single fixed API key, sent as a normal
/// bearer token: <c>Authorization: Bearer &lt;key&gt;</c>. The key itself
/// lives in configuration (MCP_API_KEY — see .env), never in source.
/// Registered once in Program.cs and applied via
/// <c>app.MapMcp(...).RequireAuthorization()</c>, the same hook the official
/// MCP C# SDK samples use for OAuth — this is just a much simpler scheme for
/// a single private client.
/// </summary>
public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private const string SchemePrefix = "Bearer ";
    private readonly IConfiguration _configuration;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration configuration)
        : base(options, logger, encoder)
    {
        _configuration = configuration;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var expectedKey = _configuration["MCP_API_KEY"];
        if (string.IsNullOrEmpty(expectedKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("Server MCP_API_KEY is not configured."));
        }

        if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing Authorization header."));
        }

        var headerValue = authHeader.ToString();
        if (!headerValue.StartsWith(SchemePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.Fail("Authorization header must use the Bearer scheme."));
        }

        var providedKey = headerValue[SchemePrefix.Length..].Trim();
        if (!KeysMatch(providedKey, expectedKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));
        }

        var identity = new ClaimsIdentity(Array.Empty<Claim>(), Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    // Constant-time comparison so a mismatched key can't be brute-forced via
    // response-timing differences. Lengths are compared first since
    // CryptographicOperations.FixedTimeEquals requires equal-length inputs —
    // the length check itself isn't constant-time, but leaking only the
    // provided key's length is an acceptable, standard trade-off here.
    private static bool KeysMatch(string provided, string expected)
    {
        var providedBytes = Encoding.UTF8.GetBytes(provided);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        return providedBytes.Length == expectedBytes.Length
            && CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
    }
}
