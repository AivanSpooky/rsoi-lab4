using System.Net;
using System.Net.Http.Json;
using Gateway.Models;

namespace Gateway.Clients;

public sealed class CarsClient : ICarsClient
{
    private readonly HttpClient _http;

    public CarsClient(HttpClient http) => _http = http;

    public async Task<PaginationResponse<CarResponse>?> GetCarsAsync(int? page, int? size, bool showAll,
        CancellationToken cancellationToken = default)
    {
        var url = $"api/v1/cars?showAll={showAll}";
        if (page is not null) url += $"&page={page}";
        if (size is not null) url += $"&size={size}";
        return await _http.GetFromJsonAsync<PaginationResponse<CarResponse>>(url, cancellationToken);
    }

    public async Task<CarResponse?> GetCarAsync(Guid carUid, CancellationToken cancellationToken = default)
    {
        var response = await _http.GetAsync($"api/v1/cars/{carUid}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CarResponse>(cancellationToken);
    }

    public async Task SetAvailabilityAsync(Guid carUid, bool available, CancellationToken cancellationToken = default)
    {
        var response = await _http.PatchAsJsonAsync($"api/v1/cars/{carUid}",
            new UpdateAvailabilityRequest(available), cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
