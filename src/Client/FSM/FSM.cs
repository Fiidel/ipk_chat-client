using System.Runtime.InteropServices.JavaScript;
using IPK24chat_client.Client.Message;

namespace IPK24chat_client.Client.FSM;

public sealed class FSM
{
    private static readonly FSM s_instance = new FSM();
    
    private FSM()
    {
        CurrentState = FSMState.Start;
        WaitingForReply = false;
        ErrorFlag = false;
        IsLegitMessage = false;
    }

    public static FSM Instance
    {
        get
        {
            return s_instance;
        }
    }

    public FSMState CurrentState;
    public bool WaitingForReply;
    public bool ErrorFlag;
    public bool IsLegitMessage;

    /// <summary>
    /// Evaluates whether the received message is of an acceptable type.
    /// Prints an error if it isn't, or transitions into an appropriate state if it is.
    /// </summary>
    /// <param name="message">a <c>Message</c></param>
    public void EvaluateState(MessageBase message)
    {
        IsLegitMessage = true; // set to false in error states (usually in default)
        switch (CurrentState)
        {
            case FSMState.Start:
                switch (message.Direction)
                {
                    case MessageDirection.Incoming:
                        SetErrorFlagsAndState();
                        break;
                        
                    case MessageDirection.Outgoing:
                        if (message.Type == MessageType.Auth)
                        {
                            CurrentState = FSMState.Auth;
                            WaitingForReply = true;
                            SetDisplayName(message);
                        }
                        else
                        {
                            Console.Error.Write("ERR: Unaccepted message type in state Start.\n");
                            IsLegitMessage = false;
                        }
                        break;
                }
                break;
            
             case FSMState.Auth:
                 switch (message.Direction)
                 {
                     case MessageDirection.Incoming:
                         if (WaitingForReply && message.Type == MessageType.Reply)
                         {
                             WaitingForReply = false;
                             var msg = (MessageReply)message;
                             CurrentState = msg.ReplyConfirmation ? FSMState.Open : FSMState.Auth;
                         }
                         else
                         {
                             switch (message.Type)
                             {
                                 case MessageType.Err:
                                     CurrentState = FSMState.End;
                                     break;
                                 default:
                                     SetErrorFlagsAndState();
                                     break;
                             }
                         }
                         break;
                     case MessageDirection.Outgoing:
                         switch (message.Type)
                         {
                             case MessageType.Auth:
                                 CurrentState = FSMState.Auth;
                                 WaitingForReply = true;
                                 SetDisplayName(message);
                                 break;
                             case MessageType.Bye:
                                 CurrentState = FSMState.End;
                                 break;
                             default:
                                 Console.Error.Write("ERR: Unaccepted outgoing message type in state Auth.\n");
                                 IsLegitMessage = false;
                                 break;
                         }
                         break;
                 }
                 break;
            
            case FSMState.Open:
                switch (message.Direction)
                {
                    case MessageDirection.Incoming:
                        if (WaitingForReply && message.Type == MessageType.Reply)
                        {
                            WaitingForReply = false;
                            CurrentState = FSMState.Open;
                        }
                        else
                        {
                            switch (message.Type)
                            {
                                case MessageType.Msg:
                                    CurrentState = FSMState.Open;
                                    break;
                                case MessageType.Err:
                                    CurrentState = FSMState.End;
                                    break;
                                case MessageType.Bye:
                                    CurrentState = FSMState.End;
                                    break;
                                default:
                                    SetErrorFlagsAndState();
                                    break;
                            }
                        }
                        break;
                    case MessageDirection.Outgoing:
                        switch (message.Type)
                        {
                            case MessageType.Join:
                                WaitingForReply = true;
                                CurrentState = FSMState.Open;
                                break;
                            case MessageType.Msg:
                                CurrentState = FSMState.Open;
                                break;
                            default:
                                Console.Error.Write("ERR: Unaccepted outgoing message type in state Open.\n");
                                IsLegitMessage = false;
                                break;
                        }
                        break;
                }
                break;
            
            case FSMState.Error:
                Console.Error.Write("ERR: Unexpected message from server. Terminating connection.\n");
                CurrentState = FSMState.End;
                break;
            
            case FSMState.End:
                break;
        }
    }

    /// <summary>
    /// Sets the globally accessible DisplayName for the client after the <c>/auth</c> command.
    /// </summary>
    /// <param name="message">the <c>MessageAuth</c> message</param>
    private void SetDisplayName(MessageBase message)
    {
        var msg = (MessageAuth)message;
        var globalConfig = GlobalConfiguration.Instance;
        globalConfig.DisplayName = msg.DisplayName;
    }

    /// <summary>
    /// Sets the appropriate FSM variables signaling an error.
    /// </summary>
    public void SetErrorFlagsAndState()
    {
        CurrentState = FSMState.Error;
        ErrorFlag = true;
        IsLegitMessage = false;
    }
}
