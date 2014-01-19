using System;

namespace SourceRconLib
{
    public class ErrorEventArgs 
        : EventArgs
    {
        private string _message;

        public string Message
        {
            get { return _message; }
            set { _message = value; }
        }

        public ErrorEventArgs(string message)
         : base()
        {
            Message = message;
        }
    }
}
