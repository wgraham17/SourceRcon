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
        /// Connection failed, host unreachable.
        /// </summary>
        Failed,
        /// <summary>
        /// Authentication failed.
        /// </summary>
        InvalidLogin,
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
        Closed,
        /// <summary>
        /// Something went terribly wrong.
        /// </summary>
        SocketException
    }
}
