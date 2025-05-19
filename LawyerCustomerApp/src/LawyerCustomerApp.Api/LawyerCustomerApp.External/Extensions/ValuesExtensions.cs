namespace LawyerCustomerApp.External.Extensions;

public static class ValuesExtensions
{
    public static T GetValue<T>(Func<T> func)
    {
        return func();
    }
}
