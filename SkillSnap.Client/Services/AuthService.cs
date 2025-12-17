using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using SkillSnap.Shared.DTOs;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;

namespace SkillSnap.Client.Services;

/// <summary>
/// Client-side authentication service managing user registration, login, logout, and token persistence.
/// Coordinates with LocalStorage for token storage and AuthenticationStateProvider for state updates.
/// </summary>
public class AuthService
{
    private readonly HttpClient _http;
    private readonly ILocalStorageService _localStorage;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly AppStateService _appState;

    private const string TokenKey = "authToken";
    private const string ExpirationKey = "tokenExpiration";

    public AuthService(
        HttpClient http,
        ILocalStorageService localStorage,
        AuthenticationStateProvider authStateProvider,
        AppStateService appState)
    {
        _http = http;
        _localStorage = localStorage;
        _authStateProvider = authStateProvider;
        _appState = appState;
    }

    /// <summary>
    /// Registers a new user account with the API.
    /// Stores the JWT token in LocalStorage on successful registration.
    /// </summary>
    /// <param name="request">The registration request containing email and password.</param>
    /// <returns>An authentication response indicating success or failure with error message.</returns>
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/auth/register", request);
            var result = await response.Content.ReadFromJsonAsync<AuthResponse>();

            if (result != null && result.Success && !string.IsNullOrEmpty(result.Token))
            {
                await StoreTokenAsync(result.Token, result.Expiration ?? DateTime.UtcNow.AddHours(1));
                NotifyAuthenticationStateChanged();
            }

            return result ?? new AuthResponse { Success = false, Message = "Unknown error occurred" };
        }
        catch (Exception ex)
        {
            return new AuthResponse { Success = false, Message = $"Error: {ex.Message}" };
        }
    }

    /// <summary>
    /// Authenticates a user with the API and stores the JWT token.
    /// Updates the authentication state on successful login.
    /// </summary>
    /// <param name="request">The login request containing email and password.</param>
    /// <returns>An authentication response indicating success or failure with error message.</returns>
    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/auth/login", request);
            var result = await response.Content.ReadFromJsonAsync<AuthResponse>();

            if (result != null && result.Success && !string.IsNullOrEmpty(result.Token))
            {
                await StoreTokenAsync(result.Token, result.Expiration ?? DateTime.UtcNow.AddHours(1));
                NotifyAuthenticationStateChanged();
            }

            return result ?? new AuthResponse { Success = false, Message = "Unknown error occurred" };
        }
        catch (Exception ex)
        {
            return new AuthResponse { Success = false, Message = $"Error: {ex.Message}" };
        }
    }

    /// <summary>
    /// Logs out the current user by removing the JWT token and clearing all cached data.
    /// Updates the authentication state to reflect the logged-out status.
    /// </summary>
    public async Task LogoutAsync()
    {
        await _localStorage.RemoveItemAsync(TokenKey);
        await _localStorage.RemoveItemAsync(ExpirationKey);
        
        // Clear all cached data on logout
        _appState.ClearAllCaches();
        
        NotifyAuthenticationStateChanged();
    }

    /// <summary>
    /// Retrieves the stored JWT token if it exists and has not expired.
    /// Automatically logs out the user if the token has expired.
    /// </summary>
    /// <returns>The JWT token string, or null if no valid token exists.</returns>
    public async Task<string?> GetTokenAsync()
    {
        var token = await _localStorage.GetItemAsync<string>(TokenKey);
        var expiration = await _localStorage.GetItemAsync<DateTime>(ExpirationKey);

        if (string.IsNullOrEmpty(token) || expiration < DateTime.UtcNow)
        {
            await LogoutAsync();
            return null;
        }

        return token;
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var token = await GetTokenAsync();
        return !string.IsNullOrEmpty(token);
    }

    public async Task<string?> GetUserEmailAsync()
    {
        var token = await GetTokenAsync();
        if (string.IsNullOrEmpty(token))
            return null;

        var claims = ParseClaimsFromJwt(token);
        return claims.FirstOrDefault(c => c.Type == "email")?.Value;
    }

    public async Task<bool> IsInRoleAsync(string role)
    {
        var token = await GetTokenAsync();
        if (string.IsNullOrEmpty(token))
            return false;

        var claims = ParseClaimsFromJwt(token);
        return claims.Any(c => c.Type == ClaimTypes.Role && c.Value == role);
    }

    private async Task StoreTokenAsync(string token, DateTime expiration)
    {
        await _localStorage.SetItemAsync(TokenKey, token);
        await _localStorage.SetItemAsync(ExpirationKey, expiration);

        // Set authorization header
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private void NotifyAuthenticationStateChanged()
    {
        if (_authStateProvider is CustomAuthStateProvider provider)
        {
            provider.NotifyAuthenticationStateChanged();
        }
    }

    private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var claims = new List<Claim>();
        var payload = jwt.Split('.')[1];
        var jsonBytes = ParseBase64WithoutPadding(payload);
        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

        if (keyValuePairs != null)
        {
            keyValuePairs.TryGetValue(ClaimTypes.Role, out object? roles);

            if (roles != null)
            {
                if (roles.ToString()!.Trim().StartsWith("["))
                {
                    var parsedRoles = JsonSerializer.Deserialize<string[]>(roles.ToString()!);
                    if (parsedRoles != null)
                    {
                        claims.AddRange(parsedRoles.Select(role => new Claim(ClaimTypes.Role, role)));
                    }
                }
                else
                {
                    claims.Add(new Claim(ClaimTypes.Role, roles.ToString()!));
                }

                keyValuePairs.Remove(ClaimTypes.Role);
            }

            claims.AddRange(keyValuePairs.Select(kvp => new Claim(kvp.Key, kvp.Value.ToString()!)));
        }

        return claims;
    }

    private byte[] ParseBase64WithoutPadding(string base64)
    {
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }
}
