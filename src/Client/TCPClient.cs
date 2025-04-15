using System.Net;
using System.Net.Sockets;
using IPK24chat_client.Client.Message;
using IPK24chat_client.Client.MessageParser;

namespace IPK24chat_client.Client;

/// <summary>
/// The <c>TCPClient</c> class models a chat client using TCP to transmit and receive data.
/// </summary>
/// <param name="configuration">the client configuration (tcp, address/hostname, port)</param>
public class TCPClient(ClientConfiguration configuration) : ClientBase(configuration)
{
    /// <summary>
    /// Establishes a connection to the server and starts the receive and send loops.
    /// </summary>
    public override void Connect()
    {
        // create a tcp socket and connect to it
        Socket? socket = null;
        try
        {
            socket = EstablishSocket();
            IPEndPoint endPoint = new IPEndPoint(Address, Port);
            socket.Connect(endPoint);

            // establish streams to write to/read from (`using` disposes of them after)
            using (NetworkStream stream = new NetworkStream(socket))
            using (TextWriter writer = new StreamWriter(stream))
            using (TextReader reader = new StreamReader(stream))
            {
                // listen to ctrl+c interrupt
                Console.CancelKeyPress += delegate
                {
                    // terminate connection gracefully
                    SendByeAndExit(socket, writer, 0);
                };
                
                // start send and receive loops
                StartConsoleInputThread(socket, writer);
                StartReceiveLoop(socket, reader, writer);
            }
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
        }
        finally
        {
            socket?.Close();
        }
    }

    /// <summary>
    /// Sends a BYE message to server, closes connection and exits.
    /// </summary>
    /// <param name="socket">the tcp socket</param>
    /// <param name="writer">the TextWriter stream to server</param>
    /// <param name="exitCode">the return value of the program</param>
    private void SendByeAndExit(Socket socket, TextWriter writer, int exitCode)
    {
        var byeMessage = new MessageBye(MessageDirection.Outgoing);
        writer.Write(byeMessage.ToOutgoingFormatTCP());
        writer.Flush();
        socket.Close();
        Environment.Exit(exitCode);
    }
    
    /// <summary>
    /// Establishes a TCP socket.
    /// </summary>
    /// <returns>a tcp socket</returns>
    private Socket EstablishSocket()
    {
        // CheckAddressNull() to prevent a null warning
        // (the program will not start without an address/hostname though, see CLOptions.cs - required switches)
        CheckAddressNull();
        
        var socket = new Socket(Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        return socket;
    }
    
    /// <summary>
    /// Starts reading Console input and sending it to server on a new thread.
    /// </summary>
    /// <param name="socket">the tcp socket</param>
    /// <param name="writer">the TextWriter stream to server</param>
    private void StartConsoleInputThread(Socket socket, TextWriter writer)
    {
        Thread consoleInputThread = new Thread(() =>
        {
            string? message;
            while ((message = Console.ReadLine()) != null)
            {
                if (socket.Poll(10000, SelectMode.SelectWrite))
                {
                    SendMessage(writer, message);
                }
            }
            
            // end of user input
            SendByeAndExit(socket, writer, 0);
            
        });
        consoleInputThread.Start();
    }

    /// <summary>
    /// Starts polling input from server. Receives message if there is one available in the TextReader stream.
    /// </summary>
    /// <param name="socket">the tcp socket</param>
    /// <param name="reader">the TextReader stream from server</param>
    /// <param name="writer">the TextWriter stream to server</param>
    private void StartReceiveLoop(Socket socket, TextReader reader, TextWriter writer)
    {
        while (true)
        {
            if (socket.Poll(10000, SelectMode.SelectRead))
            {
                ReceiveMessage(socket, reader, writer);
            }
        }
    }
    
    /// <summary>
    /// Parses a user input string to a <c>Message</c> and sends it to server in the proper format for a given message type.
    /// It the input is a local command (like /help or /rename), it executes it and returns, sending no message to server.
    /// </summary>
    /// <param name="writer">the TextWriter stream to server</param>
    /// <param name="message">the message to be sent to server</param>
    private void SendMessage(TextWriter writer, string message)
    {
        var parser = new MessageParserTCP();
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
                        writer.Write(msg.ToOutgoingFormatTCP());
                        writer.Flush();
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);;
        }
    }

    /// <summary>
    /// Returns true if the message is a local command (like /help or /reply).
    /// </summary>
    /// <param name="message">a <c>Message</c></param>
    /// <returns>whether the message is a local command</returns>
    private bool IsLocalCommand(MessageBase message)
    {
        return message.Direction is MessageDirection.Local;
    }

    /// <summary>
    /// Executes a local command (like /help and /rename)
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
    /// Reads a server-sent message, parses it to a <c>Message</c> and prints it to Console if appropriate.
    /// </summary>
    /// <param name="socket">the tcp socket</param>
    /// <param name="reader">the TextReader stream from server</param>
    /// <param name="writer">the TextWriter stream to server</param>
    private void ReceiveMessage(Socket socket, TextReader reader, TextWriter writer)
    {
        var message = reader.ReadLine();
        
        var parser = new MessageParserTCP();
        var fsm = FSM.FSM.Instance;

        try
        {
            if (message != null)
            {
                var msg = parser.ParseIncomingMessage(message);
                
                // check FSM state for a possible error
                // sends error message and exits if there is an error
                CheckForFSMError(socket, writer);
                
                if (msg != null)
                {
                    // check if the received message was ERR or BYE
                    CheckForReceivedError(msg, writer, socket);
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
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
        }
    }
    
    /// <summary>
    /// Checks the error flag in the FSM.
    /// Sends an error message to server and exits if the error flag is true.
    /// </summary>
    /// <param name="socket">the tcp socket</param>
    /// <param name="writer">the TextWriter stream to server</param>
    private void CheckForFSMError(Socket socket, TextWriter writer)
    {
        var fsm = FSM.FSM.Instance;
        if (fsm.ErrorFlag)
        {
            SendErrorMessage(writer);
            
            // terminate connection
            SendByeAndExit(socket, writer, 1);
        }
    }
    
    /// <summary>
    /// Sends error message to server in the appropriate format.
    /// </summary>
    /// <param name="writer">the TextWriter stream to server</param>
    private void SendErrorMessage(TextWriter writer)
    {
        var fsm = FSM.FSM.Instance;
        var globalConfig = GlobalConfiguration.Instance;
        var errorMessage = new MessageError(MessageDirection.Outgoing, globalConfig.DisplayName, "Unexpected message received from server");
        fsm.EvaluateState(errorMessage);
        writer.Write(errorMessage.ToOutgoingFormatTCP());
        writer.Flush();
    }

    /// <summary>
    /// Checks if the received <c>Message</c> was BYE.
    /// Closes socket and exits if it was.
    /// </summary>
    /// <param name="msg">the received <c>Message</c></param>
    /// <param name="socket">the tcp socket</param>
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
    /// <param name="writer">the TextWriter stream to server</param>
    /// <param name="socket">the tcp socket</param>
    private void CheckForReceivedError(MessageBase msg, TextWriter writer, Socket socket)
    {
        if (msg.Type == MessageType.Err)
        {
            var errorMessage = (MessageError)msg;
            Console.Error.Write(errorMessage.ToIncomingFormat());
            SendByeAndExit(socket, writer, 1);
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
