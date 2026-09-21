using AccessRequestHub.Application.DTOs;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AccessRequestHub.Web.Services;

public class ApiClient
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public ApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public void SetCurrentUser(string email)
    {
        _httpClient.DefaultRequestHeaders.Remove("X-User-Email");
        _httpClient.DefaultRequestHeaders.Add("X-User-Email", email);
    }

    public async Task<List<UserDto>> GetUsersAsync()
    {
        var response = await _httpClient.GetAsync("/api/users");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<UserDto>>(JsonOptions) ?? new();
    }

    public async Task<List<ApplicationDto>> GetApplicationsAsync()
    {
        var response = await _httpClient.GetAsync("/api/users/applications");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<ApplicationDto>>(JsonOptions) ?? new();
    }

    public async Task<AccessRequestDetailDto?> CreateRequestAsync(CreateAccessRequestDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/accessrequests", dto, JsonOptions);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(error, null, response.StatusCode);
        }
        return await response.Content.ReadFromJsonAsync<AccessRequestDetailDto>(JsonOptions);
    }

    public async Task<List<AccessRequestListDto>> GetMyRequestsAsync()
    {
        var response = await _httpClient.GetAsync("/api/accessrequests/my");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<AccessRequestListDto>>(JsonOptions) ?? new();
    }

    public async Task<List<AccessRequestListDto>> GetApprovalInboxAsync()
    {
        var response = await _httpClient.GetAsync("/api/accessrequests/inbox");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<AccessRequestListDto>>(JsonOptions) ?? new();
    }

    public async Task<AccessRequestDetailDto?> GetRequestDetailAsync(Guid id)
    {
        var response = await _httpClient.GetAsync($"/api/accessrequests/{id}");
        if (!response.IsSuccessStatusCode)
            return null;
        return await response.Content.ReadFromJsonAsync<AccessRequestDetailDto>(JsonOptions);
    }

    public async Task<AccessRequestDetailDto?> ApproveRequestAsync(Guid id, ApprovalActionDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/accessrequests/{id}/approve", dto, JsonOptions);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(error, null, response.StatusCode);
        }
        return await response.Content.ReadFromJsonAsync<AccessRequestDetailDto>(JsonOptions);
    }

    public async Task<AccessRequestDetailDto?> RejectRequestAsync(Guid id, ApprovalActionDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/accessrequests/{id}/reject", dto, JsonOptions);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(error, null, response.StatusCode);
        }
        return await response.Content.ReadFromJsonAsync<AccessRequestDetailDto>(JsonOptions);
    }
}
