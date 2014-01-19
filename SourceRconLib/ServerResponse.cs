using System;

namespace SourceRconLib
{
    public class ServerResponse
    {
        private string _message;

        public string Message
        {
            get { return _message; }
            set { _message = value; }
        }

        public ServerResponse()
        {
        }

        public ServerResponse(string message)
        {
            Message = message;
        }
    }
}
