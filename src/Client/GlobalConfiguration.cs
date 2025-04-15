namespace IPK24chat_client.Client;

public sealed class GlobalConfiguration
{
    private static readonly GlobalConfiguration s_instance = new GlobalConfiguration();
    
    private GlobalConfiguration()
    {
    }

    public static GlobalConfiguration Instance
    {
        get
        {
            return s_instance;
        }
    }
    
    public string DisplayName { get; set; } = string.Empty;
}
