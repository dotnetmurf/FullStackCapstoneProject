using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.Text.Json;

namespace SkillSnap.Client.Services;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage;
    private readonly HttpClient _http;

    private const string TokenKey = "authToken";

    public CustomAuthStateProvider(ILocalStorageService localStorage, HttpClient http)
    {
        _localStorage = localStorage;
        _http = http;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _localStorage.GetItemAsync<string>(TokenKey);

        if (string.IsNullOrEmpty(token))
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        var claims = ParseClaimsFromJwt(token);
        var identity = new ClaimsIdentity(claims, "jwt");
        var user = new ClaimsPrincipal(identity);

        return new AuthenticationState(user);
    }

    public void NotifyAuthenticationStateChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var claims = new List<Claim>();
        
        // JWT format: header.payload.signature
        // Extract the payload (middle section) which contains the claims
        var payload = jwt.Split('.')[1];
        
        // Decode the Base64-encoded payload into JSON bytes
        var jsonBytes = ParseBase64WithoutPadding(payload);
        
        // Deserialize JSON payload into key-value pairs
        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

        if (keyValuePairs != null)
        {
            // Extract role claims separately as they need special handling
            keyValuePairs.TryGetValue(ClaimTypes.Role, out object? roles);

            if (roles != null)
            {
                // Roles can be a single string or an array of strings
                // Check if it's an array by looking for the opening bracket
                if (roles.ToString()!.Trim().StartsWith("["))
                {
                    // Multiple roles: deserialize as string array
                    var parsedRoles = JsonSerializer.Deserialize<string[]>(roles.ToString()!);
                    if (parsedRoles != null)
                    {
                        claims.AddRange(parsedRoles.Select(role => new Claim(ClaimTypes.Role, role)));
                    }
                }
                else
                {
                    // Single role: add directly
                    claims.Add(new Claim(ClaimTypes.Role, roles.ToString()!));
                }

                // Remove role from dictionary to avoid duplicate claim processing
                keyValuePairs.Remove(ClaimTypes.Role);
            }

            // Convert remaining key-value pairs to claims
            claims.AddRange(keyValuePairs.Select(kvp => new Claim(kvp.Key, kvp.Value.ToString()!)));
            
            // Ensure Name claim is set for display purposes
            // Fall back to email if name claim is missing
            if (!claims.Any(c => c.Type == ClaimTypes.Name))
            {
                var emailClaim = claims.FirstOrDefault(c => c.Type == "email" || c.Type == ClaimTypes.Email);
                if (emailClaim != null)
                {
                    claims.Add(new Claim(ClaimTypes.Name, emailClaim.Value));
                }
            }
        }

        return claims;
    }

    private byte[] ParseBase64WithoutPadding(string base64)
    {
        // Base64 strings must be divisible by 4 characters
        // JWT tokens often omit padding ('=') characters
        // Add the appropriate padding based on the string length
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;  // Missing 2 padding characters
            case 3: base64 += "="; break;    // Missing 1 padding character
            // case 0: No padding needed
            // case 1: Invalid - should never occur in valid Base64
        }
        return Convert.FromBase64String(base64);
    }
}
