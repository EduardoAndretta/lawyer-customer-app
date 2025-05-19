namespace LawyerCustomerApp.External.Database.Common.Models;

public class Key
{
    public virtual string GetIdentifier()
    {
        return string.Empty;
    }
}

public class Keys
{
    public class Key : Models.Key
    {
        public enum ProviderType
        {
            Sqlite
        }
    
        public ProviderType Provider { get; private set; }
    
        public static Key CreateKey(ProviderType providerType)
        {
            return new Key()
            {
                Provider = providerType
            };
        }
    
        public override string GetIdentifier()
        {
            return $"{typeof(Key).FullName}-{Enum.GetName(Provider)}";
        }
    }
}
