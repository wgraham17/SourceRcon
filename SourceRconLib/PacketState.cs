using System;
using System.Net.Sockets;

namespace SourceRconLib
{
    internal class PacketState
    {
        internal PacketState()
        {
            PacketLength = -1;
            BytesReceived = 0;
            IsPacketLength = false;
        }

        public int PacketCount;
        public int PacketLength;
        public int BytesReceived;
        public bool IsPacketLength;
        public byte[] Data;
        public Socket Socket;
    }
}
