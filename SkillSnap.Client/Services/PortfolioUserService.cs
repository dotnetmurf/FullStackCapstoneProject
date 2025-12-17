using System.Net.Http.Json;
using SkillSnap.Shared.Models;

namespace SkillSnap.Client.Services;

public class PortfolioUserService
{
    private readonly HttpClient _http;
    private readonly HttpInterceptorService _interceptor;
    private readonly AppStateService _appState;
    private readonly string _baseUrl = "api/portfoliousers";

    public PortfolioUserService(HttpClient http, HttpInterceptorService interceptor, AppStateService appState)
    {
        _http = http;
        _interceptor = interceptor;
        _appState = appState;
    }

    public async Task<List<PortfolioUser>> GetPortfolioUsersAsync()
    {
        try
        {
            // Try to get from cache first
            var cachedUsers = _appState.GetCachedPortfolioUsers();
            if (cachedUsers != null)
            {
                Console.WriteLine("PortfolioUsers loaded from cache");
                return cachedUsers;
            }

            // If not in cache, fetch from API
            Console.WriteLine($"Making request to: {_http.BaseAddress}{_baseUrl}");
            var response = await _http.GetAsync(_baseUrl);
            Console.WriteLine($"Response status: {response.StatusCode}");
            
            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error response: {content}");
                throw new HttpRequestException($"API returned {response.StatusCode}: {content}");
            }
            
            var users = await response.Content.ReadFromJsonAsync<List<PortfolioUser>>();
            var userList = users ?? new List<PortfolioUser>();
            Console.WriteLine($"Successfully parsed {userList.Count} users");
            
            // Store in cache
            _appState.SetCachedPortfolioUsers(userList);
            
            return userList;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching portfolio users: {ex.Message}");
            Console.WriteLine($"Exception type: {ex.GetType().Name}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw; // Re-throw to let Portfolio.razor handle it
        }
    }

    public async Task<PagedResult<PortfolioUser>> GetPortfolioUsersPagedAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            var response = await _http.GetFromJsonAsync<PagedResult<PortfolioUser>>(
                $"{_baseUrl}/paged?page={page}&pageSize={pageSize}");
            return response ?? new PagedResult<PortfolioUser>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching paginated portfolio users: {ex.Message}");
            throw;
        }
    }

    public async Task<PortfolioUser?> GetPortfolioUserAsync(int id)
    {
        try
        {
            return await _http.GetFromJsonAsync<PortfolioUser>($"{_baseUrl}/{id}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching portfolio user {id}: {ex.Message}");
            return null;
        }
    }

    public async Task<PortfolioUser?> AddPortfolioUserAsync(PortfolioUser user)
    {
        try
        {
            await _interceptor.EnsureAuthHeaderAsync();
            var response = await _http.PostAsJsonAsync(_baseUrl, user);
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<PortfolioUser>();
            
            // Invalidate cache and notify listeners
            _appState.NotifyPortfolioUsersChanged();
            
            return result;
        }
        catch (HttpRequestException)
        {
            throw; // Re-throw HttpRequestException to preserve status code
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error adding portfolio user: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> UpdatePortfolioUserAsync(int id, PortfolioUser user)
    {
        try
        {
            await _interceptor.EnsureAuthHeaderAsync();
            var response = await _http.PutAsJsonAsync($"{_baseUrl}/{id}", user);
            
            if (response.IsSuccessStatusCode)
            {
                // Invalidate cache and notify listeners
                _appState.NotifyPortfolioUsersChanged();
                return true;
            }
            
            // Throw exception with status code for non-success responses
            throw new HttpRequestException($"Request failed with status code {(int)response.StatusCode}", null, response.StatusCode);
        }
        catch (HttpRequestException)
        {
            throw; // Re-throw HttpRequestException to preserve status code
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating portfolio user: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> DeletePortfolioUserAsync(int id)
    {
        try
        {
            await _interceptor.EnsureAuthHeaderAsync();
            var response = await _http.DeleteAsync($"{_baseUrl}/{id}");
            
            if (response.IsSuccessStatusCode)
            {
                // Invalidate cache and notify listeners
                _appState.NotifyPortfolioUsersChanged();
                return true;
            }
            
            // Throw exception with status code for non-success responses
            throw new HttpRequestException($"Request failed with status code {(int)response.StatusCode}", null, response.StatusCode);
        }
        catch (HttpRequestException)
        {
            throw; // Re-throw HttpRequestException to preserve status code
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting portfolio user: {ex.Message}");
            throw;
        }
    }
}
