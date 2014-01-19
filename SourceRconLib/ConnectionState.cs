using System;

namespace SourceRconLib
{
    public enum ConnectionState
    {
        /// <summary>
        /// Authentication successful.
        /// </summary>
        Success,
        /// <summary>
        /// Authentication failed.
        /// </summary>
        Failed,
        /// <summary>
        /// Client is connected.
        /// </summary>
        Connected,
        /// <summary>
        /// Client connection was unexpectedly closed.
        /// </summary>
        Disconnected,
        /// <summary>
        /// Connection was closed.
        /// </summary>
        Closed
    }
}
