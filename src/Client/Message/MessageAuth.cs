using System.Text;

namespace IPK24chat_client.Client.Message;

public class MessageAuth(MessageDirection direction, string username, string secret, string displayName)
    : MessageBase(direction)
{
    public override MessageType Type => MessageType.Auth;
    private string Username { get; } = username;
    private string Secret { get; } = secret;
    public string DisplayName { get; } = displayName;

    public override string ToIncomingFormat()
    {
        throw new Exception("Auth has no client output format.");
    }
    
    public override string ToOutgoingFormatTCP()
    {
        return $"AUTH {this.Username} AS {this.DisplayName} USING {this.Secret}\r\n";
    }

    public override byte[] ToOutgoingFormatUDP()
    {
        byte[] nullByte = { 0x00 };
        byte[] code = { 0x02 };
        byte[] ID = BitConverter.GetBytes((ushort)this.MessageID!);
        byte[] username = Encoding.ASCII.GetBytes(this.Username);
        byte[] displayName = Encoding.ASCII.GetBytes(this.DisplayName);
        byte[] secret = Encoding.ASCII.GetBytes(this.Secret);

        IEnumerable<byte> result = code.Concat(ID)
            .Concat(username).Concat(nullByte)
            .Concat(displayName).Concat(nullByte)
            .Concat(secret).Concat(nullByte);
        return result.ToArray();
    }
}
