using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Text;

class Program
{
    static async Task Main()
    {
        var client = new HttpClient { BaseAddress = new Uri(""http://localhost:5000"") };
        
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } };
        
        // Setup user
        client.DefaultRequestHeaders.Add(""X-User-Email"", ""alice@example.local"");
        
        var dto = new {
            ClientRequestId = Guid.NewGuid(),
            ApplicationId = Guid.Parse(""bb222222-2222-2222-2222-222222222222""),
            Environment = ""NonProduction"",
            AccessLevel = ""Admin"",
            BusinessJustification = ""Test""
        };
        
        var response = await client.PostAsJsonAsync(""/api/accessrequests"", dto, options);
        var responseStr = await response.Content.ReadAsStringAsync();
        Console.WriteLine($""Create Response: {responseStr}"");
        
        var created = JsonSerializer.Deserialize<JsonElement>(responseStr);
        var id = created.GetProperty(""id"").GetString();
        var rowVersion = created.GetProperty(""rowVersion"").GetString();
        
        client.DefaultRequestHeaders.Remove(""X-User-Email"");
        client.DefaultRequestHeaders.Add(""X-User-Email"", ""bob@example.local"");
        var approveDto = new { RowVersion = rowVersion, Reason = ""OK"" };
        var approveResponse = await client.PostAsJsonAsync($""/api/accessrequests/{id}/approve"", approveDto, options);
        var approveResponseStr = await approveResponse.Content.ReadAsStringAsync();
        Console.WriteLine($""Approve Status: {approveResponse.StatusCode}"");
        Console.WriteLine($""Approve Response: {approveResponseStr}"");
    }
}
