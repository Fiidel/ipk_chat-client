namespace IPK24chat_client.Client.Message;

public abstract class MessageBase
{
    public MessageDirection Direction { get; }
    private static ushort s_messageId = 0;
    public ushort? MessageID { get; }

    /// <summary>
    /// A <c>Message</c> base constructor that sets the direction and (for outgoing messages only) creates a unique ID (incrementally).
    /// </summary>
    /// <param name="direction">the message direction (outgoing, incoming, local)</param>
    protected MessageBase(MessageDirection direction)
    {
        Direction = direction;
        if (direction == MessageDirection.Outgoing)
        {
            MessageID = s_messageId++;
        }
    }
    public abstract MessageType Type { get; }
    
    /// <summary>
    /// A method that transforms the attributes of an incoming <c>Message</c> to a client output format.
    /// </summary>
    /// <returns>a string of the message in the client Console output format</returns>
    public abstract string ToIncomingFormat();
    
    /// <summary>
    /// A method that transforms the attributes of an outgoing <c>Message</c> to the TCP server format.
    /// </summary>
    /// <returns>a string of the message in the TCP server format</returns>
    public abstract string ToOutgoingFormatTCP();
    
    /// <summary>
    /// A method that transforms the attributes of an outgoing <c>Message</c> to the UDP server format.
    /// </summary>
    /// <returns>a byte array of the message in the UDP server format</returns>
    public abstract byte[] ToOutgoingFormatUDP();
}
