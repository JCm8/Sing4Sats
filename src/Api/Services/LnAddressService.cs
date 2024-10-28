using System.Text.Json;
using System.Text.RegularExpressions;
using Api.Data;
using Api.DTOs.External;
using Api.Models;

namespace Api.Services;

public partial class LnAddressService
{
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex LnAddressRegex();

    private readonly Dictionary<string, LnAddressWellKnownResponseDto> _endpointCache = new();
    private readonly TimeSpan _cacheDuration = TimeSpan.FromHours(2);
    private DateTime _lastCacheClean = DateTime.UtcNow;

    private readonly ApplicationDbContext _context;
    private readonly HttpClient _httpClient;
    private readonly LnHookService _lnHookService;
    private readonly ILogger<LnAddressService> _logger;

    public LnAddressService(ApplicationDbContext context, HttpClient httpClient, LnHookService lnHookService, ILogger<LnAddressService> logger)
    {
        _context = context;
        _httpClient = httpClient;
        _lnHookService = lnHookService;
        _logger = logger;
    }

    public async Task<string> GetInvoice(string lightningAddress, long amountInSats, string comment = "")
    {
        if (!IsValidLightningAddress(lightningAddress))
            throw new ArgumentException("Invalid lightning address format");

        // Clean cache if needed
        if ((DateTime.UtcNow - _lastCacheClean) > _cacheDuration)
        {
            _endpointCache.Clear();
            _lastCacheClean = DateTime.UtcNow;
        }

        var endpointInfo = await GetLnurlPayEndpointInfo(lightningAddress);

        if (amountInSats < endpointInfo.minSendable || amountInSats > endpointInfo.maxSendable)
            throw new ArgumentException(
                $"Amount must be between {endpointInfo.minSendable} and {endpointInfo.maxSendable} sats");

        var callbackUrl =
            $"{endpointInfo.callback}?amount={amountInSats * 1000}&comment={Uri.EscapeDataString(comment)}";
        var response = await _httpClient.GetFromJsonAsync<JsonElement>(callbackUrl);
        var invoice = response.GetProperty("pr").GetString();

        var id = await SaveInvoice(lightningAddress, amountInSats, invoice, comment);

        (var ourInvoice, var ourId, var ourAmount) = await _lnHookService.WrapInvoice(invoice);
        await UpdateInvoice(id, ourId, ourInvoice, ourAmount);

        return invoice;
    }

    private static bool IsValidLightningAddress(string address)
    {
        return LnAddressRegex().IsMatch(address);
    }

    private static (string username, string domain) ParseLightningAddress(string address)
    {
        var parts = address.Split('@');
        return (parts[0], parts[1]);
    }

    private async Task<LnAddressWellKnownResponseDto> GetLnurlPayEndpointInfo(string lightningAddress)
    {
        if (_endpointCache.TryGetValue(lightningAddress, out var cachedInfo))
            return cachedInfo;

        var (username, domain) = ParseLightningAddress(lightningAddress);
        var wellKnownUrl = $"https://{domain}/.well-known/lnurlp/{username}";

        var endpointInfo = await _httpClient.GetFromJsonAsync<LnAddressWellKnownResponseDto>(wellKnownUrl);
        if (endpointInfo == null || endpointInfo.maxSendable == 0 || string.IsNullOrWhiteSpace(endpointInfo.callback))
        {
            throw new Exception("Error getting lnaddress information.");
        }

        _endpointCache[lightningAddress] = endpointInfo;

        return endpointInfo;
    }

    private async Task SaveInvoice(string lightningAddress, long amount, string invoice, string comment)
    {
        var originalInvoice = new LightningInvoice
        {
            Id = Guid.NewGuid(),
            Amount = amount,
            CreatedAt = DateTime.UtcNow,
            LightningAddress = lightningAddress,
            OriginalInvoice = invoice,
            Comment = comment
        };
        _context.LightningInvoices.Add(originalInvoice);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Invoice retrieved for {lightningAddress}, {amount}", lightningAddress, amount);
    }

    private async Task UpdateInvoice(Guid id, Guid ourId, long ourInvoice, long ourAmount)
    {
        var originalInvoice = _context.LightningInvoices.FirstOrDefault(x => x.Id.Equals(id));
        originalInvoice.OurId = ourId;
        originalInvoice.OurInvoice = ourInvoice;
        originalInvoice.OurAmount = ourAmount;
        await _context.SaveChangesAsync();
        _logger.LogInformation("Invoice update for {id}, {ourId}", id, ourId);
    }
}