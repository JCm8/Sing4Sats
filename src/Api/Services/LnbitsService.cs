using Api.DTOs.External;

namespace Api.Services;

public class LnbitsService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _adminKey;
    private readonly string _invoiceReadWriteKey;
    private readonly string _webhookBaseUrl;
    private readonly ILogger<LnbitsService> _logger;

    public LnbitsService(IConfiguration configuration, HttpClient httpClient, ILogger<LnbitsService> logger)
    {
        _httpClient = httpClient;
        _baseUrl = configuration["LNbits:BaseUrl"];
        _adminKey = configuration["LNbits:AdminKey"];
        _webhookBaseUrl = configuration["LNbits:WebhookBaseUrl"];
        _invoiceReadWriteKey = configuration["LNbits:InvoiceReadWriteKey"];
        _logger = logger;

        _httpClient.DefaultRequestHeaders.Add("X-Api-Key", _invoiceReadWriteKey);
    }

    public async Task<(string invoice, string paymentHash)> CreateInvoice(int amount, string description)
    {
        var requestBody = new
        {
            @out = false,
            amount,
            memo = description,
            webhook = $"{_webhookBaseUrl}/api/payment/webhook" // Your webhook endpoint
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"{_baseUrl}/api/v1/payments",
            requestBody);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<LnbitsInvoiceResponse>();
        return (result.payment_request, result.payment_hash);
    }
}