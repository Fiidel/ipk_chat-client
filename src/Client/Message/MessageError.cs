using System.Text;

namespace IPK24chat_client.Client.Message;

public class MessageError(MessageDirection direction, string displayName, string messageContent)
    : MessageBase(direction)
{
    public override MessageType Type => MessageType.Err;
    private string DisplayName { get; } = displayName;

    private string MessageContent { get; } = messageContent;
    
    public override string ToOutgoingFormatTCP()
    {
        return $"ERR FROM {this.DisplayName} IS {this.MessageContent}\r\n";
    }
    
    public override byte[] ToOutgoingFormatUDP()
    {
        byte[] nullByte = { 0x00 };
        byte[] code = { 0xFE };
        byte[] ID = BitConverter.GetBytes((ushort)this.MessageID!);
        byte[] displayName = Encoding.ASCII.GetBytes(this.DisplayName);
        byte[] messageContents = Encoding.ASCII.GetBytes(this.MessageContent);

        IEnumerable<byte> result = code.Concat(ID)
            .Concat(displayName).Concat(nullByte)
            .Concat(messageContents).Concat(nullByte);
        return result.ToArray();
    }
    
    public override string ToIncomingFormat()
    {
        return $"ERR FROM {this.DisplayName}: {this.MessageContent}\n";
    }
}
