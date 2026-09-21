using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AccessRequestHub.Application.DTOs;
using AccessRequestHub.Domain.Enums;
using AccessRequestHub.Infrastructure.Data;

namespace AccessRequestHub.Tests;

public class AccessRequestIntegrationTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public AccessRequestIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    private void SetUser(string email)
    {
        _client.DefaultRequestHeaders.Remove("X-User-Email");
        _client.DefaultRequestHeaders.Add("X-User-Email", email);
    }

    private async Task<AccessRequestDetailDto> CreateRequest(
        string userEmail,
        Guid? clientRequestId = null,
        Guid? applicationId = null,
        RequestEnvironment env = RequestEnvironment.NonProduction,
        AccessLevel level = AccessLevel.Read,
        string justification = "Test justification")
    {
        SetUser(userEmail);
        var dto = new CreateAccessRequestDto
        {
            ClientRequestId = clientRequestId ?? Guid.NewGuid(),
            ApplicationId = applicationId ?? DbInitializer.CrmAppId,
            Environment = env,
            AccessLevel = level,
            BusinessJustification = justification
        };

        var response = await _client.PostAsJsonAsync("/api/accessrequests", dto, JsonOptions);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AccessRequestDetailDto>(JsonOptions);
        return result!;
    }

    [Fact]
    public async Task StandardRequest_NonHighRisk_ApprovedByManager()
    {
        var request = await CreateRequest("alice@example.local");
        Assert.Equal(RequestStatus.PendingManager, request.Status);

        SetUser("bob@example.local");
        var approveDto = new ApprovalActionDto { RowVersion = request.RowVersion };
        var response = await _client.PostAsJsonAsync($"/api/accessrequests/{request.Id}/approve", approveDto, JsonOptions);
        response.EnsureSuccessStatusCode();
        var approved = await response.Content.ReadFromJsonAsync<AccessRequestDetailDto>(JsonOptions);

        Assert.Equal(RequestStatus.Approved, approved!.Status);
    }

    [Fact]
    public async Task HighRiskRequest_Production_RequiresSystemOwner()
    {
        var request = await CreateRequest("alice@example.local", env: RequestEnvironment.Production);
        Assert.Equal(RequestStatus.PendingManager, request.Status);

        SetUser("bob@example.local");
        var approveDto = new ApprovalActionDto { RowVersion = request.RowVersion };
        var response = await _client.PostAsJsonAsync($"/api/accessrequests/{request.Id}/approve", approveDto, JsonOptions);
        var afterManager = await response.Content.ReadFromJsonAsync<AccessRequestDetailDto>(JsonOptions);
        Assert.Equal(RequestStatus.PendingSystemOwner, afterManager!.Status);

        SetUser("carol@example.local");
        var soApproveDto = new ApprovalActionDto { RowVersion = afterManager.RowVersion };
        var soResponse = await _client.PostAsJsonAsync($"/api/accessrequests/{request.Id}/approve", soApproveDto, JsonOptions);
        var finalResult = await soResponse.Content.ReadFromJsonAsync<AccessRequestDetailDto>(JsonOptions);
        Assert.Equal(RequestStatus.Approved, finalResult!.Status);
    }

    [Fact]
    public async Task HighRiskRequest_AdminAccess_RequiresSystemOwner()
    {
        var request = await CreateRequest("alice@example.local",
            applicationId: DbInitializer.FinanceAppId,
            level: AccessLevel.Admin);
        Assert.Equal(RequestStatus.PendingManager, request.Status);

        SetUser("bob@example.local");
        var approveDto = new ApprovalActionDto { RowVersion = request.RowVersion };
        var response = await _client.PostAsJsonAsync($"/api/accessrequests/{request.Id}/approve", approveDto, JsonOptions);
        var afterManager = await response.Content.ReadFromJsonAsync<AccessRequestDetailDto>(JsonOptions);
        Assert.Equal(RequestStatus.PendingSystemOwner, afterManager!.Status);
    }

    [Fact]
    public async Task UnauthorizedApproval_NonManagerCannotApprove()
    {
        var request = await CreateRequest("alice@example.local");

        SetUser("carol@example.local");
        var approveDto = new ApprovalActionDto { RowVersion = request.RowVersion };
        var response = await _client.PostAsJsonAsync($"/api/accessrequests/{request.Id}/approve", approveDto, JsonOptions);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SelfApproval_ShouldBeForbidden()
    {
        SetUser("bob@example.local");
        var dto = new CreateAccessRequestDto
        {
            ClientRequestId = Guid.NewGuid(),
            ApplicationId = DbInitializer.CrmAppId,
            Environment = RequestEnvironment.NonProduction,
            AccessLevel = AccessLevel.Read,
            BusinessJustification = "Self approval test"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/accessrequests", dto, JsonOptions);
        var request = await createResponse.Content.ReadFromJsonAsync<AccessRequestDetailDto>(JsonOptions);

        SetUser("bob@example.local");
        var approveDto = new ApprovalActionDto { RowVersion = request!.RowVersion };
        var response = await _client.PostAsJsonAsync($"/api/accessrequests/{request.Id}/approve", approveDto, JsonOptions);

        Assert.True(response.StatusCode == HttpStatusCode.Forbidden ||
                    response.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task IdempotentCreate_DuplicateClientRequestId_ReturnsSameRequest()
    {
        var clientRequestId = Guid.NewGuid();

        var first = await CreateRequest("alice@example.local", clientRequestId: clientRequestId);
        var second = await CreateRequest("alice@example.local", clientRequestId: clientRequestId);

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(first.ClientRequestId, second.ClientRequestId);
    }

    [Fact]
    public async Task ConcurrentApproval_SecondApproverGetsConflict()
    {
        var request = await CreateRequest("alice@example.local", env: RequestEnvironment.Production);

        SetUser("bob@example.local");
        var staleRowVersion = request.RowVersion;

        var approve1Dto = new ApprovalActionDto { RowVersion = staleRowVersion };
        var response1 = await _client.PostAsJsonAsync($"/api/accessrequests/{request.Id}/approve", approve1Dto, JsonOptions);
        response1.EnsureSuccessStatusCode();

        SetUser("carol@example.local");
        var approve2Dto = new ApprovalActionDto { RowVersion = staleRowVersion };
        var response2 = await _client.PostAsJsonAsync($"/api/accessrequests/{request.Id}/approve", approve2Dto, JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, response2.StatusCode);
    }

    [Fact]
    public async Task RejectedRequest_ReasonSaved_TerminalState()
    {
        var request = await CreateRequest("alice@example.local");

        SetUser("bob@example.local");
        var rejectDto = new ApprovalActionDto { RowVersion = request.RowVersion, Reason = "Not justified" };
        var response = await _client.PostAsJsonAsync($"/api/accessrequests/{request.Id}/reject", rejectDto, JsonOptions);
        response.EnsureSuccessStatusCode();
        var rejected = await response.Content.ReadFromJsonAsync<AccessRequestDetailDto>(JsonOptions);

        Assert.Equal(RequestStatus.Rejected, rejected!.Status);

        var approveDto = new ApprovalActionDto { RowVersion = rejected.RowVersion };
        var approveResponse = await _client.PostAsJsonAsync($"/api/accessrequests/{request.Id}/approve", approveDto, JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, approveResponse.StatusCode);
    }

    [Fact]
    public async Task RejectWithoutReason_ShouldFail()
    {
        var request = await CreateRequest("alice@example.local");

        SetUser("bob@example.local");
        var rejectDto = new ApprovalActionDto { RowVersion = request.RowVersion };
        var response = await _client.PostAsJsonAsync($"/api/accessrequests/{request.Id}/reject", rejectDto, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UnauthenticatedRequest_ShouldReturnUnauthorized()
    {
        _client.DefaultRequestHeaders.Remove("X-User-Email");
        var dto = new CreateAccessRequestDto
        {
            ClientRequestId = Guid.NewGuid(),
            ApplicationId = DbInitializer.CrmAppId,
            Environment = RequestEnvironment.NonProduction,
            AccessLevel = AccessLevel.Read,
            BusinessJustification = "Test"
        };

        var response = await _client.PostAsJsonAsync("/api/accessrequests", dto, JsonOptions);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
