namespace LawyerCustomerApp.External.Interfaces;

public interface IHashService
{
    public string Encrypt(string plaintext);
    public string Decrypt(string encryptedData);
}
