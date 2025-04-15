using System.Text;

namespace IPK24chat_client.Client.Message;

public class MessageReply(MessageDirection direction, bool replyConfirmation, string messageContent)
    : MessageBase(direction)
{
    public override MessageType Type => MessageType.Reply;
    public bool ReplyConfirmation { get; } = replyConfirmation;

    private string MessageContent { get; } = messageContent;
    
    public override string ToOutgoingFormatTCP()
    {
        throw new Exception("Reply message has no outgoing format.");
    }
    
    public override byte[] ToOutgoingFormatUDP()
    {
        throw new Exception("Reply message has no outgoing format.");
    }
    
    public override string ToIncomingFormat()
    {
        if (this.ReplyConfirmation)
        {
            return $"Success: {this.MessageContent}\n";
        }
        else
        {
            return $"Failure: {this.MessageContent}\n";
        }
    }
}
