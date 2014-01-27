using System;
using System.Collections;
using System.Diagnostics;
using System.Text;

namespace SourceRconLib
{
    internal class RconPacket
    {
        public enum SERVERDATA_sent : int
        {
            SERVERDATA_AUTH = 3,
            SERVERDATA_EXECCOMMAND = 2,
            None = 255
        }

        public enum SERVERDATA_recv : int
        {
            SERVERDATA_RESPONSE_VALUE = 0,
            SERVERDATA_AUTH_RESPONSE = 2,
            None = 255
        }

        internal int RequestId;
        internal string String1;
        internal string String2;
        internal RconPacket.SERVERDATA_sent ServerDataSent;
        internal RconPacket.SERVERDATA_recv ServerDataReceived;

        internal RconPacket()
        {
            RequestId = 0;
            String1 = "blah";
            String2 = String.Empty;
            ServerDataSent = SERVERDATA_sent.None;
            ServerDataReceived = SERVERDATA_recv.None;
        }

        internal byte[] OutputAsBytes()
        {
            byte[] packetsize;
            byte[] reqid;
            byte[] serverdata;
            byte[] bstring1;
            byte[] bstring2;

            UTF8Encoding utf = new UTF8Encoding();

            bstring1 = utf.GetBytes(String1);
            bstring2 = utf.GetBytes(String2);

            serverdata = BitConverter.GetBytes((int)ServerDataSent);
            reqid = BitConverter.GetBytes(RequestId);

            // Compose into one packet.
            byte[] FinalPacket = new byte[4 + 4 + 4 + bstring1.Length + 1 + bstring2.Length + 1];
            packetsize = BitConverter.GetBytes(FinalPacket.Length - 4);

            int BPtr = 0;
            packetsize.CopyTo(FinalPacket, BPtr);
            BPtr += 4;

            reqid.CopyTo(FinalPacket, BPtr);
            BPtr += 4;

            serverdata.CopyTo(FinalPacket, BPtr);
            BPtr += 4;

            bstring1.CopyTo(FinalPacket, BPtr);
            BPtr += bstring1.Length;

            FinalPacket[BPtr] = (byte)0;
            BPtr++;

            bstring2.CopyTo(FinalPacket, BPtr);
            BPtr += bstring2.Length;

            FinalPacket[BPtr] = (byte)0;
            BPtr++;

            return FinalPacket;
        }

        internal void ParseFromBytes(byte[] bytes)
        {
            int BPtr = 0;
            ArrayList stringcache;
            UTF8Encoding utf = new UTF8Encoding();

            // First 4 bytes are ReqId.
            RequestId = BitConverter.ToInt32(bytes, BPtr);
            BPtr += 4;
            // Next 4 are server data.
            ServerDataReceived = (SERVERDATA_recv)BitConverter.ToInt32(bytes, BPtr);
            BPtr += 4;
            // string1 till /0
            stringcache = new ArrayList();
            while (bytes[BPtr] != 0)
            {
                stringcache.Add(bytes[BPtr]);
                BPtr++;
            }
            String1 = utf.GetString((byte[])stringcache.ToArray(typeof(byte)));
            BPtr++;

            // string2 till /0

            stringcache = new ArrayList();
            while (bytes[BPtr] != 0)
            {
                stringcache.Add(bytes[BPtr]);
                BPtr++;
            }
            String2 = utf.GetString((byte[])stringcache.ToArray(typeof(byte)));
            BPtr++;

            // Repeat if there's more data?

            if (BPtr != bytes.Length)
            {
                //parent.OnError("Urk, extra data!");
                Debug.WriteLine("Urk, extra data!");
            }
        }
    }
}
