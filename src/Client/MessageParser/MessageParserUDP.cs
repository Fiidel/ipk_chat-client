using System.Text;
using IPK24chat_client.Client.Message;

namespace IPK24chat_client.Client.MessageParser;

public class MessageParserUDP : MessageParserBase
{
    // =============================================================
    // incoming messages
    // =============================================================

    /// <summary>
    /// Parses the incoming byte array to a Message of the appropriate type.
    /// </summary>
    /// <param name="buffer">the UDP byte array</param>
    /// <returns>a Message of the appropriate type</returns>
    public MessageBase? ParseIncomingMessage(byte[] buffer)
    {
        MessageBase? msg = null;
        string[] messageComponents;
        byte messageCode = buffer[0];
        ushort messageID = BitConverter.ToUInt16(buffer[1..3]);

        // TODO: presumes the server-sent message always has correct arguments, but could cause out of bounds exception on error
        
        switch (messageCode)
        {
            // CONFIRM
            case 0x00:
                // TODO: msg is set to null to that the program doesn't throw an "unrecognized message" error
                // the program doesn't actually wait for confirm or anything
                msg = null;
                break;
            // REPLY
            case 0x01:
                bool replyConfirmation = buffer[3] == 0x01;
                var replyMessage = new MessageReply(MessageDirection.Incoming, replyConfirmation, 
                    Encoding.ASCII.GetString(buffer[6..]));
                msg = replyMessage;
                break;
            // AUTH
            case 0x02:
                messageComponents = GetMessageComponents(buffer[3..]);
                var authMessage = new MessageAuth(MessageDirection.Incoming, messageComponents[0], 
                    messageComponents[2], messageComponents[1]);
                // CAREFUL!!! UDP USES DIFFERENT ARGUMENT ORDER THAN TCP
                msg = authMessage;
                break;
            // JOIN
            case 0x03:
                messageComponents = GetMessageComponents(buffer[3..]);
                var joinMessage = new MessageJoin(MessageDirection.Incoming, 
                    messageComponents[0], messageComponents[1]);
                msg = joinMessage;
                break;
            // MSG
            case 0x04:
                messageComponents = GetMessageComponents(buffer[3..]);
                var msgMessage = new MessageMessage(MessageDirection.Incoming, messageComponents[0], 
                    messageComponents[1]);
                msg = msgMessage;
                break;
            // ERR
            case 0xFE:
                messageComponents = GetMessageComponents(buffer[3..]);
                var errMessage = new MessageMessage(MessageDirection.Incoming, 
                    messageComponents[0], messageComponents[1]);
                msg = errMessage;
                break;
            // BYE
            case 0xFF:
                var byeMessage = new MessageBye(MessageDirection.Incoming);
                msg = byeMessage;
                break;
            // Default - unknown message -> error
            default:
                var fsm = FSM.FSM.Instance;
                fsm.SetErrorFlagsAndState();
                break;
        }

        return msg;
    }

    // returns an array of values delimited in the original byte array by null byte
    string[] GetMessageComponents(byte[] buffer)
    {
        var message = Encoding.ASCII.GetString(buffer);
        return message.Split('\0');
    }
}
