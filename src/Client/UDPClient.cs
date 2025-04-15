using System.Net;
using System.Net.Sockets;
using System.Text;
using IPK24chat_client.Client.Message;
using IPK24chat_client.Client.MessageParser;

namespace IPK24chat_client.Client;

/// <summary>
/// The <c>UDPClient</c> class models a chat client using UDP to transmit and receive data.
/// </summary>
/// <param name="configuration">the client configuration (udp, address/hostname, port,
/// confirmation timeout, maximum number of retransmissions)</param>
public class UDPClient(ClientConfiguration configuration) : ClientBase(configuration)
{
    public ushort UDPConfirmationTimeout { get; set; } = configuration.UDPConfirmationTimeout;
    public byte UDPMaxRetransmissions { get; set; } = configuration.UDPMaxRetransmissions;
    
    /// <summary>
    /// Establishes a connection to the server and starts the receive and send loops.
    /// </summary>
    public override void Connect()
    {
        // create a udp socket
        Socket? socket = null;
        try
        {
            socket = EstablishSocket();
            EndPoint endPoint = new IPEndPoint(Address, Port);

            // listen to ctrl+c interrupt
            Console.CancelKeyPress += delegate
            {
                // terminate connection gracefully
                SendByeAndExit(socket, endPoint, 0);
            };

            // start send and receive loops
            StartConsoleInputThread(socket, endPoint);
            StartReceiveLoop(socket, endPoint);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        finally
        {
            socket?.Close();
        }
    }

    /// <summary>
    /// Sends a BYE message to server, closes connection and exits.
    /// </summary>
    /// <param name="socket">the udp socket</param>
    /// <param name="endPoint">the connection endpoint</param>
    /// <param name="exitCode">the return value of the program</param>
    private void SendByeAndExit(Socket socket, EndPoint endPoint, int exitCode)
    {
        var byeMessage = new MessageBye(MessageDirection.Outgoing);
        var data = byeMessage.ToOutgoingFormatUDP();
        socket.SendTo(data, 0, data.Length, SocketFlags.None, endPoint);
        socket.Close();
        Environment.Exit(exitCode);
    }

    /// <summary>
    /// Establishes a UDP socket.
    /// </summary>
    /// <returns>a udp socket</returns>
    private Socket EstablishSocket()
    {
        // CheckAddressNull() to prevent a null warning
        // (the program will not start without an address/hostname though, see CLOptions.cs - required switches)
        CheckAddressNull();
        var socket = new Socket(Address.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
        return socket;
    }
    
    /// <summary>
    /// Starts reading Console input and sending it to server on a new thread.
    /// </summary>
    /// <param name="socket">the udp socket</param>
    /// <param name="endPoint">the connection endpoint</param>
    private void StartConsoleInputThread(Socket socket, EndPoint endPoint)
    {
        Thread consoleInputThread = new Thread(() =>
        {
            string? message;
            while ((message = Console.ReadLine()) != null)
            {
                if (socket.Poll(10000, SelectMode.SelectWrite))
                {
                    SendMessage(socket, endPoint, message);
                }
            }
            
            // end of user input
            SendByeAndExit(socket, endPoint, 0);
            
        });
        consoleInputThread.Start();
    }

    /// <summary>
    /// Parses a user input string to a <c>Message</c> and sends it to server in the proper format for a given message type.
    /// It the input is a local command (like /help or /rename), it executes it and returns, sending no message to server.
    /// </summary>
    /// <param name="socket">the udp socket</param>
    /// <param name="endPoint">the connection endpoint</param>
    /// <param name="message"></param>
    private void SendMessage(Socket socket, EndPoint endPoint, string message)
    {
        var parser = new MessageParserUDP();
        var fsm = FSM.FSM.Instance;

        try
        {
            var msg = parser.ParseOutgoingMessage(message);
            
            // if msg is null, the parsing failed (and an error message was printed)
            if (msg != null)
            {
                if (IsLocalCommand(msg))
                {
                    ExecuteLocalCommand(msg);
                }
                else
                {
                    while (fsm.WaitingForReply)
                    {
                        // stopper until a reply comes
                        Thread.Sleep(100);
                    }
                    
                    // let the FSM process the message and transition if necessary
                    // (or mark the message as an error)
                    fsm.EvaluateState(msg);

                    // if the message type is legitimate in the current state, send it to server
                    if (fsm.IsLegitMessage)
                    {
                        var data = msg.ToOutgoingFormatUDP();
                        socket.SendTo(data, 0, data.Length, SocketFlags.None, endPoint);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            throw;
        }
    }

    /// <summary>
    /// Returns true if the message is a local command (like /help or /reply).
    /// </summary>
    /// <param name="message"></param>
    /// <returns>whether the message is a local command</returns>
    private bool IsLocalCommand(MessageBase message)
    {
        return message.Direction is MessageDirection.Local;
    }

    /// <summary>
    /// Executes a local command (like /help and /rename).
    /// </summary>
    /// <param name="message">a <c>Message</c></param>
    private void ExecuteLocalCommand(MessageBase message)
    {
        switch (message.Type)
        {
            case MessageType.Help:
                var helpMessage = (MessageHelp)message;
                Console.WriteLine(helpMessage.ToIncomingFormat());
                break;
            case MessageType.Rename:
                var renameMessage = (MessageRename)message;
                var globalConfig = GlobalConfiguration.Instance;
                globalConfig.DisplayName = renameMessage.DisplayName;
                break;
        }
    }

    /// <summary>
    /// Starts polling input from server. Receives message if there is one available.
    /// </summary>
    /// <param name="socket">the udp socket</param>
    /// <param name="endPoint">the connection endpoint</param>
    private void StartReceiveLoop(Socket socket, EndPoint endPoint)
    {
        byte[] recvBuffer = new byte[2048];
        
        while (true)
        {
            if (socket.Poll(10000, SelectMode.SelectRead))
            {
                ReceiveMessage(socket, endPoint, recvBuffer);
            }
        }
    }

    /// <summary>
    /// Reads a server-sent message, parses it to a &lt;c&gt;Message&lt;/c&gt; and prints it to Console if appropriate.
    /// </summary>
    /// <param name="socket">the udp socket</param>
    /// <param name="endPoint">the connection endpoint</param>
    /// <param name="recvBuffer"></param>
    private void ReceiveMessage(Socket socket, EndPoint endPoint, byte[] recvBuffer)
    {
        int numOfRecBytes = socket.ReceiveFrom(recvBuffer, 0, recvBuffer.Length, SocketFlags.None, ref endPoint);

        // immediately send confirm back
        var confirmMessage = new MessageConfirm(BitConverter.ToUInt16(recvBuffer[1..3]));
        var data = confirmMessage.ToOutgoingFormatUDP();
        socket.SendTo(data, 0, data.Length, SocketFlags.None, endPoint);
        
        var parser = new MessageParserUDP();
        var fsm = FSM.FSM.Instance;

        try
        {
            var msg = parser.ParseIncomingMessage(recvBuffer);
            
            // check FSM state for a possible error
            // sends error message and exits if there is an error
            CheckForFSMError(socket, endPoint);
                
            if (msg != null)
            {
                // check if the received message was ERR or BYE
                CheckForReceivedError(msg, socket, endPoint);
                CheckForReceivedBye(msg, socket);
                
                // let the FSM process and possibly transition
                fsm.EvaluateState(msg);

                // if the received message is legitimate, print to Console if appropriate
                if (fsm.IsLegitMessage)
                {
                    // MSG messages are printed to stdout
                    if (msg.Type is MessageType.Msg)
                    {
                        Console.Write(msg.ToIncomingFormat());
                    }

                    // ERR and REPLY have a special print format for client's stderr Console stream
                    if (IsClientErrOutput(msg))
                    {
                        Console.Error.Write(msg.ToIncomingFormat());
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            throw;
        }
    }
    
    /// <summary>
    /// Checks the error flag in the FSM.
    /// Sends an error message to server and exits if the error flag is true.
    /// </summary>
    /// <param name="socket">the udp socket</param>
    /// <param name="endPoint">the connection endpoint</param>
    private void CheckForFSMError(Socket socket, EndPoint endPoint)
    {
        var fsm = FSM.FSM.Instance;
        if (fsm.ErrorFlag)
        {
            SendErrorMessage(socket, endPoint);
            
            // terminate connection
            SendByeAndExit(socket, endPoint, 1);
        }
    }
    
    /// <summary>
    /// Sends error message to server in the appropriate format.
    /// </summary>
    /// <param name="socket">the udp socket</param>
    /// <param name="endPoint">the connection endpoint</param>
    private void SendErrorMessage(Socket socket, EndPoint endPoint)
    {
        var globalConfig = GlobalConfiguration.Instance;
        var errorMessage = new MessageError(MessageDirection.Outgoing, globalConfig.DisplayName, "Unexpected message received from server");
        var data = errorMessage.ToOutgoingFormatUDP();
        socket.SendTo(data, 0, data.Length, SocketFlags.None, endPoint);
    }

    /// <summary>
    /// Checks if the received <c>Message</c> was BYE.
    /// Closes socket and exits if it was.
    /// </summary>
    /// <param name="msg">the received <c>Message</c></param>
    /// <param name="socket">the udp socket</param>
    private void CheckForReceivedBye(MessageBase msg, Socket socket)
    {
        if (msg.Type == MessageType.Bye)
        {
            socket.Close();
            Environment.Exit(0);
        }
    }

    /// <summary>
    /// Checks if the received <c>Message</c> was ERR.
    /// If it was, it prints the ERR <c>messageContent</c> to client Console stderr, sends BYE and terminates connection.
    /// </summary>
    /// <param name="msg">the received <c>Message</c></param>
    /// <param name="socket">the udp socket</param>
    /// <param name="endPoint">the connection endpoint</param>
    private void CheckForReceivedError(MessageBase msg, Socket socket, EndPoint endPoint)
    {
        if (msg.Type == MessageType.Err)
        {
            var errorMessage = (MessageError)msg;
            Console.Error.Write(errorMessage.ToIncomingFormat());
            SendByeAndExit(socket, endPoint, 1);
        }
    }

    /// <summary>
    /// Checks if the received message is of a type that should be printed to client Console stderr.
    /// </summary>
    /// <param name="msg">the received <c>Message</c></param>
    /// <returns>whether the <c>Message</c> should be printed to client's stderr stream</returns>
    private bool IsClientErrOutput(MessageBase msg)
    {
        return msg.Type is MessageType.Err or MessageType.Reply;
    }
}
