using System.Net;
using System.Net.Sockets;

namespace IPK24chat_client.Client;

public abstract class ClientBase(ClientConfiguration configuration)
{
    public ProtocolType Protocol { get; set; } = configuration.Protocol;
    public IPAddress Address { get; set; } = configuration.Address;
    public ushort Port { get; set; } = configuration.Port;


    /// <summary>
    /// Connects the client to the server and starts the receive and send loops.
    /// </summary>
    public abstract void Connect();
    
    /// <summary>
    /// Checks if the address/hostname is null.
    /// </summary>
    /// <exception cref="NullReferenceException">Thrown when the address or hostname is null.</exception>
    protected void CheckAddressNull()
    {
        if (Address == null)
        {
            throw new NullReferenceException("Error: Missing IP address or hostname.");
        }
    }
}
