using System.Net.Sockets;
using IPK24chat_client.Client;

namespace IPK24chat_client;

class SocketClient
{
    static void Main(string[] args)
    {
        // parses the program arguments to configuration attributes
        var configuration = new ClientConfiguration(args);
        
        // start communication based on the chosen protocol type
        switch (configuration.Protocol)
        {
            case ProtocolType.Tcp:
                var tcpClient = new TCPClient(configuration);
                tcpClient.Connect();
                break;
            case ProtocolType.Udp:
                var udpClient = new UDPClient(configuration);
                udpClient.Connect();
                break;
        }
    }
}
