using Api.DTOs.External;

namespace Api.Services;

public class LnHookService
{
    private readonly string _baseUrl;
    private readonly string _webhook;
    private readonly HttpClient _httpClient;
    private readonly ILogger<LnHookService> _logger;
    
    public LnHookService(IConfiguration configuration, HttpClient httpClient, ILogger<LnHookService> logger)
    {
        _baseUrl = configuration["LnHook:BaseUrl"];
        _webhook = configuration["LnHook:Webhook"];
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<(string invoice, Guid id, long amount)> WrapInvoice(string invoice)
    {
        var wrapRequest = new
        {
            invoice,
            webhook = _webhook
        };
        var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/wrap", wrapRequest);
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync<LnHookWrapResponseDto>();
        return (result.invoice, result.id, result.finalAmount);
    } 
}