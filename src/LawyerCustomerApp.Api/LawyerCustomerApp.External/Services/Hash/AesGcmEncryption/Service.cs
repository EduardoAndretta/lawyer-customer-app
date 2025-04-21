using LawyerCustomerApp.External.Exceptions;
using LawyerCustomerApp.External.Hash.Responses.Error;
using LawyerCustomerApp.External.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace LawyerCustomerApp.External.Hash.AesGcmEncryption.Services;

internal class Service : IHashService
{
    private readonly byte[] _key;
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
        var plainBytes = Encoding.UTF8.GetBytes(plaintext);
    
        var nonce = new byte[12];
        var tag   = new byte[16];

        RandomNumberGenerator.Fill(nonce);
    
        var cipherBytes = new byte[plainBytes.Length];
    
        using (var aes = new AesGcm(_key, 16))
        {
            aes.Encrypt(nonce, plainBytes, cipherBytes, tag);
        }
    
        var combined = new byte[nonce.Length + tag.Length + cipherBytes.Length];
        Buffer.BlockCopy(nonce, 0, combined, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, combined, nonce.Length, tag.Length);
        Buffer.BlockCopy(cipherBytes, 0, combined, nonce.Length + tag.Length, cipherBytes.Length);
    
        return Convert.ToBase64String(combined);
    }
    
    public string Decrypt(string encryptedData)
    {
        var combined = Convert.FromBase64String(encryptedData);
    
        var nonce = new byte[12];
        var tag   = new byte[16];

        var cipherBytes = new byte[combined.Length - nonce.Length - tag.Length];
    
        Buffer.BlockCopy(combined, 0, nonce, 0, nonce.Length);
        Buffer.BlockCopy(combined, nonce.Length, tag, 0, tag.Length);
        Buffer.BlockCopy(combined, nonce.Length + tag.Length, cipherBytes, 0, cipherBytes.Length);
    
        var plainBytes = new byte[cipherBytes.Length];
        using (var aes = new AesGcm(_key, 16))
        {
            aes.Decrypt(nonce, cipherBytes, tag, plainBytes);
        }
    
        return Encoding.UTF8.GetString(plainBytes);
    }
}
