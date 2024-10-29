using System.Text;
using NBitcoin.DataEncoders;

namespace Api.Services;

public class LnUrlService(IConfiguration configuration): Bech32Encoder("lnurl"u8.ToArray())
{
    private readonly string _url = configuration["LnUrl:BaseUrl"];
    
    public string Encode(Guid songId)
    {
        var url = $"{_url}/api/LnUrl/data/{songId}";
        var urlBytes = Encoding.UTF8.GetBytes(url);
        var convertedUrl = ConvertBits(urlBytes.AsReadOnly(), 8, 5);
        return EncodeData(convertedUrl, Bech32EncodingType.BECH32).ToUpper();
    }
}