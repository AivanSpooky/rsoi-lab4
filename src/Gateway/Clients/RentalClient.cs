using System.Net;
using System.Net.Http.Json;
using Gateway.Models;

namespace Gateway.Clients;

public sealed class RentalClient : IRentalClient
{
    private readonly HttpClient _http;

    public RentalClient(HttpClient http) => _http = http;

    public async Task<List<RentalRecord>> GetRentalsAsync(string username,
        CancellationToken cancellationToken = default)
    {
        var rentals = await _http.GetFromJsonAsync<List<RentalRecord>>(
            $"api/v1/rental?username={Uri.EscapeDataString(username)}", cancellationToken);
        return rentals ?? [];
    }

    public async Task<RentalRecord?> GetRentalAsync(Guid rentalUid, string username,
        CancellationToken cancellationToken = default)
    {
        var response = await _http.GetAsync(
            $"api/v1/rental/{rentalUid}?username={Uri.EscapeDataString(username)}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RentalRecord>(cancellationToken);
    }

    public async Task<RentalRecord?> CreateRentalAsync(CreateRentalRecordRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync("api/v1/rental", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RentalRecord>(cancellationToken);
    }

    public async Task FinishRentalAsync(Guid rentalUid, string username,
        CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsync(
            $"api/v1/rental/{rentalUid}/finish?username={Uri.EscapeDataString(username)}",
            null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task CancelRentalAsync(Guid rentalUid, string username,
        CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsync(
            $"api/v1/rental/{rentalUid}/cancel?username={Uri.EscapeDataString(username)}",
            null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
