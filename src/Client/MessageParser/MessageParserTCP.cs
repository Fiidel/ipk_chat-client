using IPK24chat_client.Client.Message;

namespace IPK24chat_client.Client.MessageParser;

public class MessageParserTCP : MessageParserBase
{
    // =============================================================
    // incoming messages
    // =============================================================

    /// <summary>
    /// Parses an incoming message string to a Message of the appropriate type.
    /// </summary>
    /// <param name="message">a message string</param>
    /// <returns>a Message of the appropriate type</returns>
    public MessageBase? ParseIncomingMessage(string message)
    {
        var messageComponents = message.Split();
        MessageBase? msg = null;
        string messageContent;
        
        // TODO: Incoming messages with these prefixes but wrong format can cause out of bounds exceptions
        switch (messageComponents[0].ToUpper())
        {
            case "REPLY":
                if (messageComponents[1].Equals("OK", StringComparison.CurrentCultureIgnoreCase))
                {
                    messageContent = message.Substring("REPLY IS OK ".Length);
                    msg = new MessageReply(MessageDirection.Incoming, true, messageContent);
                }
                else if (messageComponents[1].Equals("NOK", StringComparison.CurrentCultureIgnoreCase))
                {
                    messageContent = message.Substring("REPLY IS NOK ".Length);
                    msg = new MessageReply(MessageDirection.Incoming, false, messageContent);
                }
                break;
            case "MSG":
                messageContent = message.Substring("MSG FROM  IS ".Length + messageComponents[2].Length);
                msg = new MessageMessage(MessageDirection.Incoming, messageComponents[2], messageContent);
                break;
            case "ERR":
                messageContent = message.Substring("ERR FROM  IS ".Length + messageComponents[2].Length);
                msg = new MessageError(MessageDirection.Incoming, messageComponents[2], messageContent);
                break;
            case "BYE":
                msg = new MessageBye(MessageDirection.Incoming);
                break;
            default:
                var fsm = FSM.FSM.Instance;
                fsm.SetErrorFlagsAndState();
                break;
        }
        
        return msg;
    }
}
