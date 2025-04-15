namespace IPK24chat_client.Client.Message;

public enum MessageDirection
{
    Incoming,
    Outgoing,
    Local // for "/rename" and "/help" commands
}
