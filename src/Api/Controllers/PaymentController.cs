using Api.Data;
using Api.DTOs.External;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PaymentController> _logger;
    private readonly string _walletId;

    public PaymentController(ApplicationDbContext context, IConfiguration configuration, ILogger<PaymentController> logger)
    {
        _context = context;
        _logger = logger;
        _walletId = configuration["LNbits:WalletId"];
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> HandlePaymentWebhook([FromBody] LnbitsWebhookRequest request)
    {
        if (request.wallet_id != _walletId)
        {
            return Unauthorized();
        }

        if (!request.pending && request.status == "success")
        {
            var songRequest = await _context.SongRequests
                .FirstOrDefaultAsync(x => x.PaymentHash == request.payment_hash);

            if (songRequest != null)
            {
                songRequest.PaidAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Payment received for song request {songRequest.Id}", songRequest.Id);
            }
            else
            {
                _logger.LogInformation("Song Request not found for payment {request.payment_hash}", request.payment_hash);
            }
        }

        return Ok();
    }
}