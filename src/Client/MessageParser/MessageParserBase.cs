using IPK24chat_client.Client.Message;

namespace IPK24chat_client.Client.MessageParser;

public abstract class MessageParserBase
{
    // =============================================================
    // outgoing messages
    // =============================================================
    
    /// <summary>
    /// Parses user input string to a <c>Message</c> of the appropriate type.
    /// </summary>
    /// <param name="message">a string containing the outgoing message from user</param>
    /// <returns>a <c>Message</c> of the appropriate type</returns>
    public MessageBase? ParseOutgoingMessage(string message)
    {
        var messageComponents = message.Split();
        MessageBase? msg = null;
        
        if (messageComponents[0].StartsWith('/'))
        {
            messageComponents[0] = messageComponents[0].Substring(1); // removes the '/'
            switch (messageComponents[0].ToLower())
            {
                case "auth":
                    msg = CreateAuthMessage(messageComponents, MessageDirection.Outgoing);
                    break;
                case "join":
                    msg = CreateJoinMessage(messageComponents, MessageDirection.Outgoing);
                    break;
                case "rename":
                    msg = CreateRenameMessage(messageComponents);
                    break;
                case "help":
                    msg = CreateHelpMessage(messageComponents);
                    break;
                default:
                    Console.Error.Write("ERR: Unrecognized command. Type \"/help\" to display help.\n");
                    msg = null;
                    break;
            }
        }
        else
        {
            msg = CreateMsgMessage(message, MessageDirection.Outgoing);
        }

        return msg;
    }

    
    private MessageAuth? CreateAuthMessage(string[] messageComponents, MessageDirection direction)
    {
        if (ValidateAuthArguments(messageComponents))
        {
            var authMessage = new MessageAuth(direction, messageComponents[1], messageComponents[2], messageComponents[3]);
            return authMessage;
        }
        else
        {
            return null;
        }
    }

    
    private MessageJoin? CreateJoinMessage(string[] messageComponents, MessageDirection direction)
    {
        if (ValidateJoinArguments(messageComponents))
        {
            var globalConfig = GlobalConfiguration.Instance;
            var joinMessage = new MessageJoin(direction, messageComponents[1], globalConfig.DisplayName ?? throw new Exception("ERR: DisplayName in Join message is null.\n"));
            return joinMessage;
        }
        else
        {
            return null;
        }
    }

    
    private MessageRename? CreateRenameMessage(string[] messageComponents)
    {
        if (ValidateRenameArguments(messageComponents))
        {
            var renameMessage = new MessageRename(MessageDirection.Local, messageComponents[1]);
            return renameMessage;
        }
        else
        {
            return null;
        }
    }

    
    private MessageHelp CreateHelpMessage(string[] messageComponents)
    {
        var helpMessage = new MessageHelp(MessageDirection.Local);
        return helpMessage;
    }

    
    private MessageMessage? CreateMsgMessage(string message, MessageDirection direction)
    {
        if (ValidateMsg(message))
        {
            var globalConfig = GlobalConfiguration.Instance;
            var msgMessage = new MessageMessage(direction, globalConfig.DisplayName ?? throw new Exception("ERR: DisplayName in Message message is null.\n"), message);
            return msgMessage;
        }
        else
        {
            return null;
        }
    }
    
    
    // =============================================================
    // value validation
    // =============================================================
    
    private bool ValidateAuthArguments(string[] messageComponents)
    {
        if (messageComponents.Length != 4)
        {
            Console.Error.Write("ERR: /auth requires 3 arguments.\n");
            return false;
        }

        if (ValidateUsername(messageComponents[1])
            && ValidateSecret(messageComponents[2])
            && ValidateDisplayName(messageComponents[3]))
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    
    private bool ValidateJoinArguments(string[] messageComponents)
    {
        if (messageComponents.Length != 2)
        {
            Console.Error.Write("ERR: /join requires 1 argument.\n");
            return false;
        }

        if (ValidateChannelID(messageComponents[1]))
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    
    private bool ValidateRenameArguments(string[] messageComponents)
    {
        if (messageComponents.Length != 2)
        {
            Console.Error.Write("ERR: /rename requires 1 argument.\n");
            return false;
        }

        if (ValidateDisplayName(messageComponents[1]))
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    
    private bool ValidateMsg(string message)
    {
        return (ValidateMessageContent(message));
    }
    
    protected bool ValidateUsername(string parameter)
    {
        int maxLength = 20;
        if (!CheckLength(parameter, maxLength))
        {
            Console.Error.Write($"ERR: Username must be max {maxLength} characters long.\n");
            return false;
        }

        if (!CheckAlphaNumDash(parameter))
        {
            Console.Error.Write($"ERR: Username must contain only alphanumericals or dash.\n");
            return false;
        }

        return true;
    }

    protected bool ValidateChannelID(string parameter)
    {
        int maxLength = 20;
        if (!CheckLength(parameter, maxLength))
        {
            Console.Error.Write($"ERR: ChannelID must be max {maxLength} characters long.\n");
            return false;
        }
        
        if (!CheckAlphaNumDash(parameter))
        {
            Console.Error.Write($"ERR: ChannelID must contain only alphanumericals or dash.\n");
            return false;
        }
        
        return true;
    }
    
    protected bool ValidateSecret(string parameter)
    {
        int maxLength = 128;
        if (!CheckLength(parameter, maxLength))
        {
            Console.Error.Write($"ERR: Secret must be max {maxLength} characters long.\n");
            return false;
        }
        
        if (!CheckAlphaNumDash(parameter))
        {
            Console.Error.Write($"ERR: Secret must contain only alphanumericals or dash.\n");
            return false;
        }
        
        return true;
    }

    protected bool ValidateDisplayName(string parameter)
    {
        int maxLength = 20;
        if (!CheckLength(parameter, maxLength))
        {
            Console.Error.Write($"ERR: Display name must be max {maxLength} characters long.\n");
            return false;
        }
        
        if (!CheckPrintable(parameter))
        {
            Console.Error.Write("ERR: Display name must only contain printable characters (without space).\n");
            return false;
        }

        return true;
    }
    
    protected bool ValidateMessageContent(string parameter)
    {
        int maxLength = 1400;
        if (!CheckLength(parameter, maxLength))
        {
            Console.Error.Write($"ERR: Message must be max {maxLength} characters long.\n");
            return false;
        }
        
        if (!CheckPrintableWithSpace(parameter))
        {
            Console.Error.Write($"ERR: Username must contain only printable characters or space.\n");
            return false;
        }
        
        return true;
    }

    protected bool CheckLength(string message, int maxLength)
    {
        if (message.Length > maxLength)
        {
            return false;
        }

        return true;
    }
    
    protected bool CheckAlphaNumDash(string message)
    {
        foreach (char c in message)
        {
            if (!char.IsLetterOrDigit(c) && c != '-')
            {
                return false;
            }
        }

        return true;
    }

    protected bool CheckPrintable(string message)
    {
        foreach (char c in message)
        {
            if (!(c >= 0x21 && c <= 0x7E))
            {
                return false;
            }
        }

        return true;
    }
    
    protected bool CheckPrintableWithSpace(string message)
    {
        foreach (char c in message)
        {
            if (!(c >= 0x20 && c <= 0x7E))
            {
                
                return false;
            }
        }

        return true;
    }
}
