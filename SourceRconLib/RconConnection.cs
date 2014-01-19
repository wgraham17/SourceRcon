using System;
using System.Net;

namespace SourceRconLib
{
    public class RconConnection
    {
        private string _hostname;
        private IPAddress _ip;
        private int _port;
        private string _password;

        public string Hostname
        {
            get { return _hostname; }
            set { _hostname = value; }
        }

        public IPAddress IP
        {
            get { return _ip; }
            set { _ip = value; }
        }

        public int Port
        {
            get { return _port; }
            set { _port = value; }
        }

        public string Password
        {
            get { return _password; }
            set { _password = value; }
        }

        public RconConnection(string hostname, IPAddress ip, int port, string password)
        {
            Hostname = hostname;
            IP = ip;
            Port = port;
            Password = password;
        }

        public RconConnection(string hostname, string ip, string port, string password) 
            : this(hostname, IPAddress.Parse(ip), Int32.Parse(port), password)
        {
        }

        public RconConnection()
        {
        }
    }
}
