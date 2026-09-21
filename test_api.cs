using System;
using System.Net.Http;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        var client = new HttpClient { BaseAddress = new Uri(""http://localhost:5000"") };
        client.DefaultRequestHeaders.Add(""X-User-Email"", ""alice@example.local"");
        try {
            var response = await client.GetAsync(""/api/users/applications"");
            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($""Status: {response.StatusCode}"");
            Console.WriteLine($""Body: {content}"");
        } catch (Exception ex) {
            Console.WriteLine(ex);
        }
    }
}
