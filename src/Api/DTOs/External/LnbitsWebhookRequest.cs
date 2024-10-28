namespace Api.DTOs.External;

public class LnbitsWebhookRequest
{
    public string status { get; set; }
    public bool pending { get; set; }
    public string payment_hash { get; set; }
    public string wallet_id { get; set; }
}