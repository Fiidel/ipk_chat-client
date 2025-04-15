using System.Text;

namespace IPK24chat_client.Client.Message;

public class MessageConfirm(ushort refId)
{
    public MessageType Type => MessageType.Confirm;
    public ushort refID { get;  } = refId;

    public byte[] ToOutgoingFormatUDP()
    {
        byte[] code = { 0x00 };
        byte[] ID = BitConverter.GetBytes((ushort)this.refID);

        IEnumerable<byte> result = code.Concat(ID);
        return result.ToArray();
    }
}
