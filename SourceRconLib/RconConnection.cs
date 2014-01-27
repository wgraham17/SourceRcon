using System.Net;

namespace SourceRconLib
{
    public struct RconConnection
    {
        public IPAddress Host { get; set; }
        public int Port { get; set; }
        public string Password { get; set; }

        public RconConnection(IPAddress host, int port, string password) 
            : this()
        {
            Host = host;
            Port = port;
            Password = password;
        }
    }
}
