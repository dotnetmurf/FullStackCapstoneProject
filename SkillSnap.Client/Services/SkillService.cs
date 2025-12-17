using System.Net.Http.Json;
using SkillSnap.Shared.Models;

namespace SkillSnap.Client.Services;

public class SkillService
{
    private readonly HttpClient _http;
    private readonly HttpInterceptorService _interceptor;
    private readonly AppStateService _appState;
    private readonly string _baseUrl = "api/skills";

    public SkillService(HttpClient http, HttpInterceptorService interceptor, AppStateService appState)
    {
        _http = http;
        _interceptor = interceptor;
        _appState = appState;
    }

    public async Task<List<Skill>> GetSkillsAsync()
    {
        try
        {
            // Try to get from cache first
            var cachedSkills = _appState.GetCachedSkills();
            if (cachedSkills != null)
            {
                Console.WriteLine("Skills loaded from cache");
                return cachedSkills;
            }

            // If not in cache, fetch from API
            Console.WriteLine("Skills fetched from API");
            var skills = await _http.GetFromJsonAsync<List<Skill>>(_baseUrl);
            var skillList = skills ?? new List<Skill>();
            
            // Store in cache
            _appState.SetCachedSkills(skillList);
            
            return skillList;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching skills: {ex.Message}");
            return new List<Skill>();
        }
    }

    public async Task<PagedResult<Skill>> GetSkillsPagedAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            var response = await _http.GetFromJsonAsync<PagedResult<Skill>>(
                $"{_baseUrl}/paged?page={page}&pageSize={pageSize}");
            return response ?? new PagedResult<Skill>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching paginated skills: {ex.Message}");
            return new PagedResult<Skill>();
        }
    }

    public async Task<Skill?> GetSkillAsync(int id)
    {
        try
        {
            return await _http.GetFromJsonAsync<Skill>($"{_baseUrl}/{id}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching skill {id}: {ex.Message}");
            return null;
        }
    }

    public async Task<Skill?> AddSkillAsync(Skill skill)
    {
        try
        {
            await _interceptor.EnsureAuthHeaderAsync();
            var response = await _http.PostAsJsonAsync(_baseUrl, skill);
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<Skill>();
            
            // Invalidate cache and notify listeners
            _appState.NotifySkillsChanged();
            
            return result;
        }
        catch (HttpRequestException)
        {
            throw; // Re-throw HttpRequestException to preserve status code
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error adding skill: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> UpdateSkillAsync(int id, Skill skill)
    {
        try
        {
            await _interceptor.EnsureAuthHeaderAsync();
            var response = await _http.PutAsJsonAsync($"{_baseUrl}/{id}", skill);
            
            if (response.IsSuccessStatusCode)
            {
                // Invalidate cache and notify listeners
                _appState.NotifySkillsChanged();
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
            Console.WriteLine($"Error updating skill: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> DeleteSkillAsync(int id)
    {
        try
        {
            await _interceptor.EnsureAuthHeaderAsync();
            var response = await _http.DeleteAsync($"{_baseUrl}/{id}");
            
            if (response.IsSuccessStatusCode)
            {
                // Invalidate cache and notify listeners
                _appState.NotifySkillsChanged();
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
            Console.WriteLine($"Error deleting skill: {ex.Message}");
            throw;
        }
    }
}
