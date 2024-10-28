namespace Api.Models;

public class LightningInvoice
{
    public Guid Id { get; set; }
    public string LightningAddress { get; set; }
    public string OriginalInvoice { get; set; }
    public long Amount { get; set; }
    public string OurId { get; set; }
    public string OurInvoice { get; set; }
    public long OurAmount { get; set; }
    public string Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
}