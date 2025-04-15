using System.Text;

namespace IPK24chat_client.Client.Message;

public class MessageJoin(MessageDirection direction, string channelId, string displayName)
    : MessageBase(direction)
{
    public override MessageType Type => MessageType.Join;
    private string ChannelID { get; } = channelId;

    private string DisplayName { get; } = displayName;
    
    public override string ToOutgoingFormatTCP()
    {
        return $"JOIN {this.ChannelID} AS {this.DisplayName}\r\n";
    }
    
    public override byte[] ToOutgoingFormatUDP()
    {
        byte[] nullByte = { 0x00 };
        byte[] code = { 0x03 };
        byte[] ID = BitConverter.GetBytes((ushort)this.MessageID!);
        byte[] channelID = Encoding.ASCII.GetBytes(this.ChannelID);
        byte[] displayName = Encoding.ASCII.GetBytes(this.DisplayName);

        IEnumerable<byte> result = code.Concat(ID)
            .Concat(channelID).Concat(nullByte)
            .Concat(displayName).Concat(nullByte);
        return result.ToArray();
    }
    
    public override string ToIncomingFormat()
    {
        throw new Exception("Join has no client output format.");
    }
}
