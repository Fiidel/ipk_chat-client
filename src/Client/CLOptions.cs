using CommandLine;

namespace IPK24chat_client.Client;

public class CommandLineOptions
{
    [Option('h', Required = false, HelpText = "Shows the help screen.")]
    public bool DisplayHelp { get; set; }
    
    [Option('t', Required = true, HelpText = "Choose transport protocol (tcp or udp).")]
    public required string Protocol { get; set; }
    
    [Option('s', Required = true, HelpText = "Enter IP address or hostname.")]
    public required string IPHost { get; set; }
    
    [Option('p', Required = false, HelpText = "(Optional) Enter server port.")]
    public ushort ServerPort { get; set; }
    
    [Option('d', Required = false, HelpText = "(Optional) Enter UDP confirmation timeout.")]
    public ushort UDPConfirmationTimeout { get; set; }
    
    [Option('r', Required = false, HelpText = "(Optional) Enter maximum number of UDP retransmissions.")]
    public byte UDPMaxRetransmissions { get; set; }
}
