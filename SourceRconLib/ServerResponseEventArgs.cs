using System;

namespace SourceRconLib
{
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
