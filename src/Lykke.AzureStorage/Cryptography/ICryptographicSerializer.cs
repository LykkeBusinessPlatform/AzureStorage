namespace Lykke.AzureStorage.Cryptography
{
    public interface ICryptographicSerializer
    {
        string Serialize(string value);
        string Deserialize(string value);
        bool IsEncrypted(string value);
    }
}
