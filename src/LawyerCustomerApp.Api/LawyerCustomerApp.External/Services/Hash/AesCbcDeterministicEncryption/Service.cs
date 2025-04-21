using LawyerCustomerApp.External.Exceptions;
using LawyerCustomerApp.External.Hash.Responses.Error;
using LawyerCustomerApp.External.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace LawyerCustomerApp.External.Hash.AesCbcDeterministicEncryption.Services;

internal class Service : IHashService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;
    public Service(IConfiguration configuration)
    {
        var base64Key = configuration["Encryption:Key"];

        if (string.IsNullOrWhiteSpace(base64Key))
            throw new BaseException<NotFoundEncrptyKeyError>()
            {
                Constructor = new()
                {
                    Status = 500
                }
            };

        try
        {
            _key = Convert.FromBase64String(base64Key);

            _iv = new byte[16];
        }
        catch (Exception ex)
        {
            throw new BaseException<InvalidEncrptyKeyError>()
            {
                Constructor = new()
                {
                    Status = 500,
                    Reason = ex.Message
                }
            };
        }

        if (_key.Length != 32)
            throw new BaseException<InvalidEncrptyKeyError>()
            {
                Constructor = new()
                {
                    Status = 500,
                    Reason = "Encryption key must be 32 bytes (256‑bit)."
                }
            };
    }


    public string Encrypt(string plaintext)
    {
        using var aes = Aes.Create();

        aes.Key     = _key;
        aes.IV      = _iv;
        aes.Mode    = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();

        var plainBytes = Encoding.UTF8.GetBytes(plaintext);

        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        return Convert.ToBase64String(cipherBytes);
    }

    public string Decrypt(string encrypted)
    {
        using var aes = Aes.Create();

        aes.Key     = _key;
        aes.IV      = _iv;
        aes.Mode    = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();

        var cipherBytes = Convert.FromBase64String(encrypted);

        var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

        return Encoding.UTF8.GetString(plainBytes);
    }
}
