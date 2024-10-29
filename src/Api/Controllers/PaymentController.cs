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
        _walletId = configuration["LNbits:WalletId"] ?? throw new Exception("You need to set the walletid in the config file.");
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> HandlePaymentWebhook([FromBody] LnbitsWebhookRequestDto requestDto)
    {
        if (requestDto.wallet_id != _walletId)
        {
            return Unauthorized();
        }

        if (!requestDto.pending && requestDto.status == "success")
        {
            var songRequest = await _context.SongRequests
                .FirstOrDefaultAsync(x => x.PaymentHash == requestDto.payment_hash);

            if (songRequest != null)
            {
                songRequest.PaidAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Payment received for song requestDto {songRequest.Id}", songRequest.Id);
            }
            else
            {
                _logger.LogInformation("Song Request not found for payment {requestDto.payment_hash}", requestDto.payment_hash);
            }
        }

        return Ok();
    }
}