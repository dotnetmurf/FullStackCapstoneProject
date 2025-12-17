using Blazored.LocalStorage;
using System.Net.Http.Headers;

namespace SkillSnap.Client.Services;

public class HttpInterceptorService
{
    private readonly HttpClient _http;
    private readonly ILocalStorageService _localStorage;

    public HttpInterceptorService(HttpClient http, ILocalStorageService localStorage)
    {
        _http = http;
        _localStorage = localStorage;
    }

    public async Task EnsureAuthHeaderAsync()
    {
        var token = await _localStorage.GetItemAsync<string>("authToken");
        
        if (!string.IsNullOrEmpty(token))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        else
        {
            _http.DefaultRequestHeaders.Authorization = null;
        }
    }
}
