using System.Security.Cryptography;
using System.Text;

namespace NCcsds.Sle.Common;

/// <summary>
/// SLE authentication credentials.
/// </summary>
public class SleCredentials
{
    /// <summary>
    /// Username/authority identifier.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Password/secret key.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Hash algorithm to use for credential encoding.
    /// </summary>
    public SleHashAlgorithm HashAlgorithm { get; set; } = SleHashAlgorithm.Sha256;

    /// <summary>
    /// Generates encoded credentials for authentication.
    /// </summary>
    /// <param name="randomNumber">Random number from the protocol.</param>
    /// <param name="time">Current time for encoding.</param>
    /// <returns>The encoded credentials.</returns>
    public byte[] GenerateHashedPassword(byte[] randomNumber, byte[] time)
    {
        // Concatenate: password + random number + time
        var data = new byte[Password.Length + randomNumber.Length + time.Length];
        Encoding.ASCII.GetBytes(Password, data.AsSpan(0, Password.Length));
        randomNumber.CopyTo(data, Password.Length);
        time.CopyTo(data, Password.Length + randomNumber.Length);

        // Hash the concatenation
        return HashAlgorithm switch
        {
            SleHashAlgorithm.Sha1 => SHA1.HashData(data),
            SleHashAlgorithm.Sha256 => SHA256.HashData(data),
            _ => throw new NotSupportedException($"Hash algorithm {HashAlgorithm} not supported.")
        };
    }

    /// <summary>
    /// Validates received credentials.
    /// </summary>
    public bool ValidateCredentials(byte[] receivedHash, byte[] randomNumber, byte[] time, string expectedPassword)
    {
        var tempPassword = Password;
        Password = expectedPassword;
        var expectedHash = GenerateHashedPassword(randomNumber, time);
        Password = tempPassword;

        return receivedHash.SequenceEqual(expectedHash);
    }
}

/// <summary>
/// Hash algorithm for SLE authentication.
/// </summary>
public enum SleHashAlgorithm
{
    /// <summary>
    /// SHA-1 (legacy, for older SLE versions).
    /// </summary>
    Sha1,

    /// <summary>
    /// SHA-256 (recommended).
    /// </summary>
    Sha256
}
