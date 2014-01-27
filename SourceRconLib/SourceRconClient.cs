using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SourceRconLib
{
    /// <summary>
    /// Encapsulates RCON communication with a Source server.
    /// </summary>
    public class SourceRconClient
    {
        public event ConnectionStateChangedEventHandler ConnectionStateChanged;
        public event ServerResponseEventHandler ServerResponse;
        public event ErrorEventHandler Error;

        private bool hadJunkPacket;

        private Socket socket;
        private RconConnection connection;

        private int packetCount;
        private int requestIdCounter;

        private DateTime packetSent;
        private DateTime packetReceived;

        /// <summary>
        /// Gets the connection status.
        /// </summary>
        public bool Connected
        {
            get { return socket != null && socket.Connected; }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="conn">The <see cref="RconConnection"/> object.</param>
        public SourceRconClient(RconConnection conn)
        {
            connection = conn;
        }

        #region Public methods

        /// <summary>
        /// Connect to a Source RCON server.
        /// </summary>
        public void Connect()
        {
            Reset();
            try
            {
                EndPoint remoteEP = new IPEndPoint(connection.Host, connection.Port);
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                socket.Connect(remoteEP);

                RconPacket packet = new RconPacket();
                ++requestIdCounter;
                packet.RequestId = requestIdCounter;
                packet.String1 = connection.Password;
                packet.ServerDataSent = RconPacket.SERVERDATA_sent.SERVERDATA_AUTH;

                SendPacket(packet);
                Receive();
            }
            catch
            {
                OnError("Connection Failed!");
                OnConnectionStateChanged(ConnectionState.Failed);
            }
        }

        /// <summary>
        /// Disconnect from the server.
        /// </summary>
        public void Disconnect()
        {
            if (socket.Connected)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
            OnConnectionStateChanged(ConnectionState.Closed);
        }

        /// <summary>
        /// Send a command to the connected server.
        /// </summary>
        /// <param name="command"></param>
        /// <returns>True if successful, false if otherwise.</returns>
        public bool ServerCommand(string command)
        {
            try
            {
                if (!socket.Connected)
                    return false;

                RconPacket packet = new RconPacket();
                ++requestIdCounter;
                packet.RequestId = requestIdCounter;
                packet.ServerDataSent = RconPacket.SERVERDATA_sent.SERVERDATA_EXECCOMMAND;
                packet.String1 = command;

                SendPacket(packet);
            }
            catch
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Parse a string and split into Host and Port parts.
        /// </summary>
        /// <param name="hostname"></param>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <returns>True if successful, false if otherwise.</returns>
        public static bool TryParseHostString(string hostname, out string host, out int port)
        {
            if (hostname.Contains(":"))
            {
                string[] parts = hostname.Split(':');

                if (parts.Length == 2)
                {
                    host = parts[0];
                    if (int.TryParse(parts[1], out port))
                    {
                        return true;
                    }
                    else
                    {
                        port = 0;
                        return false;
                    }
                }
                else
                {
                    host = string.Empty;
                    port = 0;
                    return false;
                }
            }
            else
            {
                host = string.Empty;
                port = 0;
                return false;
            }
        }

        #endregion

        private void Disconnect(DisconnectionType? disconnectionType)
        {
            if (socket.Connected)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }

            if (disconnectionType != null)
            {
                switch (disconnectionType)
                {
                    case DisconnectionType.Manual:
                        OnConnectionStateChanged(ConnectionState.Closed);
                        break;
                    case DisconnectionType.ConnectionLost:
                        OnConnectionStateChanged(ConnectionState.Disconnected);
                        break;
                }
            }
        }

        private void Reset()
        {
            hadJunkPacket = false;
            packetCount = 0;
            requestIdCounter = 0;
            packetSent = DateTime.Now;
            packetReceived = DateTime.Now;
        }

        private void SendPacket(RconPacket packet)
        {
            byte[] p = packet.OutputAsBytes();
            try
            {
                socket.BeginSend(p, 0, p.Length, SocketFlags.None, new AsyncCallback(SendCallback), socket);
                packetSent = DateTime.Now;
            }
            catch (SocketException ex)
            {
                OnError("Socket Exception: " + ex.Message);
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                Socket client = (Socket)ar.AsyncState;
                client.EndSend(ar);
            }
            catch (SocketException ex)
            {
                OnError("Socket Exception: " + ex.Message);
            }
        }

        private void Receive()
        {
            try
            {
                // Is the socket still connected?
                if (!socket.Connected)
                {
                    return;
                }

                // Create the state object.
                PacketState state = new PacketState();
                state.Socket = socket;
                state.IsPacketLength = true;
                state.Data = new byte[4];
                state.PacketCount = packetCount;
                packetCount++;

                // Begin receiving the data from the remote device.
                socket.BeginReceive(state.Data, 0, 4, SocketFlags.None, new AsyncCallback(ReceiveCallback), state);
            }
            catch (SocketException ex)
            {
                OnError("Socket Exception: " + ex.Message);
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the state object and the client socket
                // from the asynchronous state object.
                PacketState state = (PacketState)ar.AsyncState;
                Socket client = state.Socket;

                // Is the socket still connected?
                if (!client.Connected)
                {
                    return;
                }

                // Read data from the remote device.
                int bytesRead = client.EndReceive(ar);

                if (bytesRead > 0)
                {
                    state.BytesReceived += bytesRead;

                    Debug.WriteLine(String.Format("Receive Callback. Packet: {0} First packet: {1}, Bytes so far: {2}",
                        state.PacketCount, state.IsPacketLength, state.BytesReceived));

                    if (state.IsPacketLength)
                    {
                        // First 4 bytes of a new packet.
                        state.PacketLength = BitConverter.ToInt32(state.Data, 0);
                        state.IsPacketLength = false;
                        state.BytesReceived = 0;
                        state.Data = new byte[state.PacketLength];

                        BeginReceive(client, state);
                    }
                    else
                    {
                        // This is a complete packet.
                        if (state.BytesReceived == state.PacketLength)
                        {
                            RconPacket packet = new RconPacket();
                            packet.ParseFromBytes(state.Data);
                            packetReceived = DateTime.Now;
                            ProcessResponse(packet);

                            if (!client.Connected)
                            {
                                return;
                            }

                            // Wait for new packets...
                            Receive();
                        }
                        else
                        {
                            // We don't have all the data, ask the network for the rest.
                            BeginReceive(client, state);
                        }
                    }
                }
                else
                {
                    // Received zero bytes.
                    Debug.WriteLine("Received zero bytes.");
                    Disconnect(DisconnectionType.ConnectionLost);
                }
            }
            catch (SocketException ex)
            {
                OnError("Socket Exception: " + ex.Message);
            }
        }

        private void BeginReceive(Socket client, PacketState state)
        {
            client.BeginReceive(state.Data, state.BytesReceived, state.PacketLength - state.BytesReceived,
                SocketFlags.None, new AsyncCallback(ReceiveCallback), state);
        }

        private void ProcessResponse(RconPacket packet)
        {
            switch (packet.ServerDataReceived)
            {
                case RconPacket.SERVERDATA_recv.SERVERDATA_AUTH_RESPONSE:
                    if (packet.RequestId != -1)
                    {
                        // Successfully authenticated to server.
                        OnServerResponse("Connection Succeeded!");
                        OnConnectionStateChanged(ConnectionState.Success);
                    }
                    else
                    {
                        // Failed authentication.
                        OnError("Invalid password.");
                        OnConnectionStateChanged(ConnectionState.InvalidLogin);
                        Disconnect(null);
                    }
                    break;
                case RconPacket.SERVERDATA_recv.SERVERDATA_RESPONSE_VALUE:
                    if (hadJunkPacket)
                    {
                        // The real packet.
                        OnServerResponse(packet.String1);
                    }
                    else
                    {
                        // We receiced a "junk" packet, set to skip.
                        hadJunkPacket = true;
                        OnError("Had junk packet. This is normal.");
                    }
                    break;
                default:
                    OnError("Unknown response.");
                    break;
            }
        }

        private void OnServerResponse(string message)
        {
            if (ServerResponse != null)
                ServerResponse(this, new ServerResponseEventArgs(new ServerResponse(message)));
        }

        private void OnError(string message)
        {
            if (Error != null)
                Error(this, new ErrorEventArgs(message));
        }

        private void OnConnectionStateChanged(ConnectionState state)
        {
            if (ConnectionStateChanged != null)
                ConnectionStateChanged(this, new ConnectionStateEventArgs(state));
        }
    }
}
