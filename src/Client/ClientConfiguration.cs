using System.Net;
using System.Net.Sockets;
using CommandLine;
using CommandLine.Text;

namespace IPK24chat_client.Client;

public class ClientConfiguration
{
    public ClientConfiguration(string[] args)
    {
        ParseProgramArgs(args);
    }

    public ProtocolType Protocol { get; set; }
    public IPAddress Address { get; set; } = null!; // null warning suppression - address is a required program argument, handled by CommandLineParser
    public ushort Port { get; set; } = 4567;
    public ushort UDPConfirmationTimeout { get; set; } = 250;
    public byte UDPMaxRetransmissions { get; set; } = 3;
    
    /// <summary>
    /// Parses the program's arguments to configuration attributes (protocol, address, port, UDP confirmation timeout and UDP maximum number of retransmissions).
    /// </summary>
    /// <param name="args">the arguments given to the program during start</param>
    /// <exception cref="ArgumentException">Thrown when it cannot parse the address or hostname.</exception>
    public void ParseProgramArgs(string[] args)
    {
        try
        {
            var parser = new Parser(with => with.HelpWriter = null);
            var result = parser.ParseArguments<CommandLineOptions>(args);
            var helpText = HelpText.AutoBuild(result, err => err, ex => ex);
        
            result.WithParsed<CommandLineOptions>(option =>
            {
                // Help
                if (option.DisplayHelp)
                {
                    Console.WriteLine(helpText);
                    Environment.Exit(0);
                }
                
                // Protocol
                switch (option.Protocol.ToLower())
                {
                    case "tcp":
                        this.Protocol = ProtocolType.Tcp;
                        break;
                    case "udp":
                        this.Protocol = ProtocolType.Udp;
                        break;
                    default:
                        throw new ArgumentException("Unknown protocol. Choose tcp or udp.");
                }

                // IP or hostname
                var hostType = Uri.CheckHostName(option.IPHost);
                if (hostType == UriHostNameType.Dns)
                {
                    var address = Dns.GetHostAddresses(option.IPHost);
                    this.Address = address[0];
                }
                else if (hostType == UriHostNameType.IPv4)
                {
                    var address = IPAddress.Parse(option.IPHost);
                    this.Address = address;
                }
                else
                {
                    throw new ArgumentException("Unknown IP Address or Hostname.");
                }
                
                // Port number
                if (option.ServerPort != 0)
                {
                    this.Port = option.ServerPort;
                }

                // UDP confirmation timeout
                if (option.UDPConfirmationTimeout != 0)
                {
                    this.UDPConfirmationTimeout = option.UDPConfirmationTimeout;
                }

                // UDP max number of retransmissions
                if (option.UDPMaxRetransmissions != 0)
                {
                    this.UDPMaxRetransmissions = option.UDPMaxRetransmissions;
                }
            });
            // shows help screen for switches it couldn't parse
            result.WithNotParsed(errors =>
            {
                Console.WriteLine(helpText);
                Environment.Exit(0);
            });
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
        }
    }
}
