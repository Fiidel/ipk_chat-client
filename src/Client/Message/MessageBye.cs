using System.Text;

namespace IPK24chat_client.Client.Message;

public class MessageBye : MessageBase
{
    public MessageBye(MessageDirection direction) : base(direction)
    {
    }

    public override MessageType Type => MessageType.Bye;

    public override string ToOutgoingFormatTCP()
    {
        return "BYE\r\n";
    }
    
    public override byte[] ToOutgoingFormatUDP()
    {
        byte[] code = { 0xFF };
        byte[] ID = BitConverter.GetBytes((ushort)this.MessageID!);

        IEnumerable<byte> result = code.Concat(ID);
        return result.ToArray();
    }
    
    public override string ToIncomingFormat()
    {
        throw new Exception("Bye has no client output format.");
    }
}
