namespace Lykke.AzureStorage.Cryptography
{
    /// <summary>
    /// String-based serializer which use encryption.
    /// </summary>
    public interface ICryptographicSerializer
    {
        /// <summary>
        /// Serialize string value.
        /// </summary>
        string Serialize(string value);

        /// <summary>
        /// Deserialize string value.
        /// </summary>
        string Deserialize(string value);

        /// <summary>
        /// Check if provided value is encrypted with the same parameters as this serializer.
        /// </summary>
        bool IsEncrypted(string value);
    }
}
