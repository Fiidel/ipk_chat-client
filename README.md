# IPK24chat-client

## Source Code Structure
- `Program.cs` - the entry point of the program, contains the Main() method
- Client
  - `CLOptions.cs` - command line switches for CommandLineParser package
  - `ClientConfiguration.cs` - a class that stores the program arguments (protocol, address, ...)
  - `GlobalConfiguration.cs` - a static class for globally accessible configuration (so far used for `DisplayName`)
  - `ClientBase.cs`, `TCPClient.cs`, `UDPClient.cs` - the actual client implementation
  - Message
    - contains a Message base class and its derived classes, as well as the necessary enums
  - MessageParser
    - contains the parser classes used for parsing client and server communication into `Messages`
  - FSM
    - contains a static class with an implementation of a finite state machine for the client-server communication and the state enum

## So What's Going On? (Theory)
The most important theory for understanding the application code is how the connection is established 
and the form of the data being sent over the connection.

### TCP
For TCP, the client establishes a TCP socket and connects it to the server address and port endpoint. 
TCP communicates through text messages. We can use TextWriter and TextReader streams from the NetworkStream 
to send and receive messages from the server. TCP ensures the messages are delivered.

### UDP
For UDP, the client establishes a UDP socket (but doesn't connect to it) and endpoint which it will use 
to direct messages to the server. UDP communicates via byte arrays which need to be parsed and decoded to text 
output and the communication is unreliable, meaning the application must take into account things like 
possible data loss and resending lost packets.

## Code Overview
### Command Line Arguments
The command line arguments are parsed using the `CommandLineParser` package (see https://github.com/commandlineparser/commandline 
for more). The options can be found in `CLOptions.cs`. The given arguments are stored in a `ClientConfiguration` 
instance. The protocol variant and IP address/hostname are required arguments, while the others (port, UDP confirmation 
timeout and UDP maximum number of retransmissions) are optional and have default values.

### Client
The client establishes a connection (or just an endpoint for UDP) based on the given configuration. It then starts their 
receive and send loops. Each client, no matter the protocol, must parse the messages to the correct format before sending 
it or printing it to the client's Console as what the server accepts and what the client is supposed to see differs.

### Message
I opted for an implementation that lets me work with messages in a unified way no matter the protocol. The MessageBase 
class sets the basic interface of all messages and there are derived classes for each message type that the IPK24chat 
uses. The types are specified in the `MessageType.cs` enum.

Each message has formatting methods:
- `ToIncomingFormat()` - processes the Message's attributes and returns a string to be printed to the client Console
- `ToOutgoingFormatTCP()` - returns a string containing the message attributes in a format accepted by the server
- `ToOutgoingFormatUDP()` - returns a byte array containing the message attributes in a format accepted by the server

### MessageParser
The message parsers assure that all incoming and outgoing messages are parsed into `Message` objects, even local commands, 
for a unified approach to working with them. Additionally, outgoing messages' contents are validated (namely their length 
and the characters they contain).

Outgoing messages are parsed the same way for both protocols (as the user commands and input remains the same). The 
TCP and UDP parsers work a little differently for outgoing messages. While TCP works on a text level and can parse the 
messages relatively easily, UDP must work on a byte level, separating the contents and converting to other types 
(ushort/UInt16, ASCII).

### FSM
The `FSM` keeps track of the current state and evaluates each message, possibly transitioning between states. It has 
a few flags:
- `IsLegitMessage` - can the given message be accepted in the current state?
- `ErrorFlag` - marks that there has been an error and the connection is to be terminated

## Testing
Only cursory testing had been done on this application. (Ran out of budget.)
