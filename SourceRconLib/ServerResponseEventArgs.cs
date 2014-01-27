using System;

namespace SourceRconLib
{
    public delegate void ServerResponseEventHandler(object sender, ServerResponseEventArgs e);

    public class ServerResponseEventArgs 
        : EventArgs
    {
        private ServerResponse _serverResponse;

        public ServerResponse ServerResponse
        {
            get { return _serverResponse; }
            set { _serverResponse = value; }
        }

        public ServerResponseEventArgs(ServerResponse response)
         : base()
        {
            ServerResponse = response;
        }
    }
}
