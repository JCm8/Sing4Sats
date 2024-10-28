namespace Api.Services;

public class HashService
{
    // In production, this should be in a secure configuration
    private const string EXPECTED_HASH = "8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918"; // This is SHA256("admin")

    public bool VerifyHash(string preImage)
    {
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(preImage);
            var hash = sha256.ComputeHash(bytes);
            var computedHash = BitConverter.ToString(hash).Replace("-", "").ToLower();
            return computedHash == EXPECTED_HASH;
        }
    }
}