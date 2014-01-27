using System;

namespace SourceRconLib
{
    public enum DisconnectionType
    {
        /// <summary>
        /// Connection was manually closed.
        /// </summary>
        Manual,
        /// <summary>
        /// Client connection was unexpectedly lost.
        /// </summary>
        ConnectionLost
    }
}
