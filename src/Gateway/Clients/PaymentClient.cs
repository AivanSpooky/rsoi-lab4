using System.Net;
using System.Net.Http.Json;
using Gateway.Models;

namespace Gateway.Clients;

public sealed class PaymentClient : IPaymentClient
{
    private readonly HttpClient _http;

    public PaymentClient(HttpClient http) => _http = http;

    public async Task<PaymentInfo?> GetPaymentAsync(Guid paymentUid, CancellationToken cancellationToken = default)
    {
        var response = await _http.GetAsync($"api/v1/payment/{paymentUid}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PaymentInfo>(cancellationToken);
    }

    public async Task<PaymentInfo?> CreatePaymentAsync(int price, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync("api/v1/payment",
            new CreatePaymentRequest(price), cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PaymentInfo>(cancellationToken);
    }

    public async Task CancelPaymentAsync(Guid paymentUid, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsync($"api/v1/payment/{paymentUid}/cancel", null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
