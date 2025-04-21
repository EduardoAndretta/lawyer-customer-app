namespace LawyerCustomerApp.External.Interfaces;

public interface IDataService
{
    TClass GetData<TClass>(string key) where TClass : class, new();
    void SetData<TClass>(TClass data, string key) where TClass : class;
}
