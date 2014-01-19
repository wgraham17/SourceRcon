using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SourceRconLib
{
    internal class PacketState
    {
        internal PacketState()
        {
            PacketLength = -1;
            BytesSoFar = 0;
            IsPacketLength = false;
        }

        public int PacketCount;
        public int PacketLength;
        public int BytesSoFar;
        public bool IsPacketLength;
        public byte[] Data;
    }
}
