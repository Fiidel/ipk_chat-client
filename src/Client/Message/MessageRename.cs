namespace IPK24chat_client.Client.Message;

public class MessageRename(MessageDirection direction, string displayName) : MessageBase(direction)
{
    public override MessageType Type => MessageType.Rename;
    public string DisplayName { get; } = displayName;
    
    public override string ToOutgoingFormatTCP()
    {
        throw new Exception("Rename has no outgoing format.");
    }
    
    public override byte[] ToOutgoingFormatUDP()
    {
        throw new Exception("Rename has no outgoing format.");
    }
    
    public override string ToIncomingFormat()
    {
        throw new Exception("Rename has no client output format.");
    }
}
