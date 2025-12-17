using System.Net.Http.Json;
using SkillSnap.Shared.Models;

namespace SkillSnap.Client.Services;

/// <summary>
/// Client-side service for managing project data with caching support.
/// Handles CRUD operations for projects and coordinates with AppStateService for cache management.
/// </summary>
public class ProjectService
{
    private readonly HttpClient _http;
    private readonly HttpInterceptorService _interceptor;
    private readonly AppStateService _appState;
    private readonly string _baseUrl = "api/projects";

    public ProjectService(HttpClient http, HttpInterceptorService interceptor, AppStateService appState)
    {
        _http = http;
        _interceptor = interceptor;
        _appState = appState;
    }

    /// <summary>
    /// Retrieves all projects from cache if available, otherwise fetches from the API.
    /// Caches the result for subsequent requests.
    /// </summary>
    /// <returns>A list of all projects, or an empty list if an error occurs.</returns>
    public async Task<List<Project>> GetProjectsAsync()
    {
        try
        {
            // Try to get from cache first
            var cachedProjects = _appState.GetCachedProjects();
            if (cachedProjects != null)
            {
                Console.WriteLine("Projects loaded from cache");
                return cachedProjects;
            }

            // If not in cache, fetch from API
            Console.WriteLine("Projects fetched from API");
            var projects = await _http.GetFromJsonAsync<List<Project>>(_baseUrl);
            var projectList = projects ?? new List<Project>();
            
            // Store in cache
            _appState.SetCachedProjects(projectList);
            
            return projectList;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching projects: {ex.Message}");
            return new List<Project>();
        }
    }

    /// <summary>
    /// Retrieves a paginated list of projects from the API.
    /// </summary>
    /// <param name="page">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page (default 20).</param>
    /// <returns>A paginated result containing projects and pagination metadata.</returns>
    public async Task<PagedResult<Project>> GetProjectsPagedAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            var response = await _http.GetFromJsonAsync<PagedResult<Project>>(
                $"{_baseUrl}/paged?page={page}&pageSize={pageSize}");
            return response ?? new PagedResult<Project>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching paginated projects: {ex.Message}");
            return new PagedResult<Project>();
        }
    }

    /// <summary>
    /// Retrieves a specific project by ID from the API.
    /// </summary>
    /// <param name="id">The project ID.</param>
    /// <returns>The project if found, or null if not found or an error occurs.</returns>
    public async Task<Project?> GetProjectAsync(int id)
    {
        try
        {
            return await _http.GetFromJsonAsync<Project>($"{_baseUrl}/{id}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching project {id}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Creates a new project via the API (requires authentication).
    /// Invalidates the projects cache after successful creation.
    /// </summary>
    /// <param name="project">The project to create.</param>
    /// <returns>The created project with assigned ID, or null if creation fails.</returns>
    /// <exception cref="HttpRequestException">Thrown when the API request fails.</exception>
    public async Task<Project?> AddProjectAsync(Project project)
    {
        try
        {
            await _interceptor.EnsureAuthHeaderAsync();
            var response = await _http.PostAsJsonAsync(_baseUrl, project);
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<Project>();
            
            // Invalidate cache and notify listeners
            _appState.NotifyProjectsChanged();
            
            return result;
        }
        catch (HttpRequestException)
        {
            throw; // Re-throw HttpRequestException to preserve status code
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error adding project: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> UpdateProjectAsync(int id, Project project)
    {
        try
        {
            await _interceptor.EnsureAuthHeaderAsync();
            var response = await _http.PutAsJsonAsync($"{_baseUrl}/{id}", project);
            
            if (response.IsSuccessStatusCode)
            {
                // Invalidate cache and notify listeners
                _appState.NotifyProjectsChanged();
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
            Console.WriteLine($"Error updating project: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> DeleteProjectAsync(int id)
    {
        try
        {
            await _interceptor.EnsureAuthHeaderAsync();
            var response = await _http.DeleteAsync($"{_baseUrl}/{id}");
            
            if (response.IsSuccessStatusCode)
            {
                // Invalidate cache and notify listeners
                _appState.NotifyProjectsChanged();
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
            Console.WriteLine($"Error deleting project: {ex.Message}");
            throw;
        }
    }
}
