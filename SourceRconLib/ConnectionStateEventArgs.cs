using System;

namespace SourceRconLib
{
    public delegate void ConnectionStateChangedEventHandler(object sender, ConnectionStateEventArgs e);

    public class ConnectionStateEventArgs 
        : EventArgs
    {
        private ConnectionState _state;

        public ConnectionState ConnectionState
        {
            get { return _state; }
            set { _state = value; }
        }

        public ConnectionStateEventArgs(ConnectionState state)
         : base()
        {
            ConnectionState = state;
        }
    }
}
