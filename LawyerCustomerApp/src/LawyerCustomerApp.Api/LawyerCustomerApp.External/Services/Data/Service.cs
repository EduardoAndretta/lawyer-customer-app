using LawyerCustomerApp.External.Interfaces;

namespace LawyerCustomerApp.External.Data.Services;

internal class Service : IDataService
{
    private IDictionary<object, object?> _data;
    public Service()
    {
        _data = new Dictionary<object, object?>();
    }

    public TClass GetData<TClass>(string key) where TClass : class, new()
    {
        if (!_data.TryGetValue(key, out _) || _data[key] is not TClass data)
        {
            data = new TClass();

            SetData(data, key);
        }
        
        return data;
    }

    public void SetData<TClass>(TClass data, string key) where TClass : class
    {
        _data.TryAdd(key, data);
    }
}
