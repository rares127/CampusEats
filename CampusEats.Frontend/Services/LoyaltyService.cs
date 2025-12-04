using System.Net.Http.Json;

namespace CampusEats.Frontend.Services;

public class LoyaltyService
{
    private readonly HttpClient http;

    public LoyaltyService(HttpClient http)
    {
        this.http = http;
    }

    public async Task<int> GetPoints()
        => await http.GetFromJsonAsync<int>("/api/loyalty/points");

    public async Task<bool> Redeem(int amount)
    {
        var res = await http.PostAsync($"/api/loyalty/redeem/{amount}", null);
        return res.IsSuccessStatusCode;
    }

    public async Task<List<LoyaltyTransactionDto>> GetTransactions()
        => await http.GetFromJsonAsync<List<LoyaltyTransactionDto>>("/api/loyalty/transactions")
           ?? new();
}

public class LoyaltyTransactionDto
{
    public int Amount { get; set; }
    public string Description { get; set; } = "";
    public DateTime Timestamp { get; set; }
}