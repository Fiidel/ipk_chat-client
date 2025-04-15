namespace IPK24chat_client.Client.Message;

public class MessageHelp : MessageBase
{
    public MessageHelp(MessageDirection direction) : base(direction)
    {
    }

    public override MessageType Type => MessageType.Help;

    private const string _helpScreen = """
                                       --- IPK24chat-client help ---
                                       -----------------------------
                                       /auth {Username} {Password} {DisplayName}
                                           - sign in to the server
                                       /join {ChannelID}
                                           - join a channel based on its ID
                                       /rename {DisplayName}
                                           - change your display name
                                       /help
                                           - show this help screen
                                       """;

    public override string ToOutgoingFormatTCP()
    {
        throw new Exception("Help has no outgoing output format.");
    }
    
    public override byte[] ToOutgoingFormatUDP()
    {
        throw new Exception("Help has no outgoing output format.");
    }
    
    public override string ToIncomingFormat()
    {
        return _helpScreen;
    }
}
