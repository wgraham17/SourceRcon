using System;
using System.Collections;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SourceRconLib
{
    /// <summary>
    /// 
    /// </summary>
    public class SourceRcon 
        : IDisposable
    {
        public event ServerResponseEventHandler ServerResponse;
        public delegate void ServerResponseEventHandler(object sender, ServerResponseEventArgs e);
        public event ErrorEventHandler Error;
        public delegate void ErrorEventHandler(object sender, ErrorEventArgs e);
        public event ConnectionStateChangedEventHandler ConnectionStateChanged;
        public delegate void ConnectionStateChangedEventHandler(object sender, ConnectionStateEventArgs e);

        public static string connectionClosedString = "Connection closed by remote host.";
        public static string connectionSuccessString = "Connection Succeeded!";
        public static string connectionFailedString = "Connection Failed!";
        public static string notConnectedString = "Not connected.";
        public static string unknownResponseTypeString = "Unknown response.";
        public static string gotJunkPacketString = "Had junk packet. This is normal.";
        
        #if DEBUG
        public ArrayList tempPackets;
        #endif

        bool alreadyClosed;
        bool isConnected;
        bool hadJunkPacket;

        Socket socket;

        int packetCount;
        int requestIdCounter;

        public bool Connected
        {
            get { return isConnected; }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public SourceRcon()
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Reset();

            #if DEBUG
            tempPackets = new ArrayList();
            #endif
        }

        #region IDisposable Members

        bool Disposed;
        public void Dispose()
        {
            if (!Disposed)
            {
                socket.Close();
            }

            Disposed = true;
            isConnected = false;
        }

        #endregion

        public static IPAddress GetIpAddressFromHostName(string hostname)
        {
            IPAddress[] ipAddresses = Dns.GetHostAddresses(hostname);
            return ipAddresses[0];
        }

        /// <summary>
        /// Attempts to connect to a server.
        /// </summary>
        /// <param name="connection">The <see cref="RconConnection"/> object.</param>
        public void Connect(RconConnection connection)
        {
            if (connection.IP == null)
                connection.IP = SourceRcon.GetIpAddressFromHostName(connection.Hostname);

            Connect(new IPEndPoint(connection.IP, connection.Port), connection.Password);
        }

        /// <summary>
        /// Attempts to connect to a server.
        /// </summary>
        /// <param name="server">The <see cref="IPEndPoint"/> of the server.</param>
        /// <param name="password">The password.</param>
        public void Connect(IPEndPoint server, string password)
        {
            if (Disposed)
            {
                OnError(new ErrorEventArgs(connectionFailedString + "Already disposed."));
                return;
            }

            try
            {
                socket.Connect(server);
            }
            catch (SocketException)
            {
                OnError(new ErrorEventArgs(connectionFailedString));
                OnConnectionStateChanged(new ConnectionStateEventArgs(ConnectionState.Failed));
                return;
            }

            Reset();

            RconPacket serverAuthPacket = new RconPacket();
            ++requestIdCounter;
            serverAuthPacket.RequestId = 1;
            serverAuthPacket.String1 = password;
            serverAuthPacket.ServerDataSent = RconPacket.SERVERDATA_sent.SERVERDATA_AUTH;

            SendRconPacket(serverAuthPacket);

            // Start the listening loop, now that we've sent auth packet, we should be expecting a reply.
            StartGetNewPacket();
        }

        public bool ConnectBlocking(RconConnection connection)
        {
            if (connection.IP == null)
                connection.IP = SourceRcon.GetIpAddressFromHostName(connection.Hostname);

            return ConnectBlocking(new IPEndPoint(connection.IP, connection.Port), connection.Password);
        }

        public bool ConnectBlocking(IPEndPoint server, string password)
        {
            bool connected = false;
            AutoResetEvent resetEvent = new AutoResetEvent(false);

            ConnectionStateChangedEventHandler stateChanged = (object sender, ConnectionStateEventArgs e) => {
                connected = (e.ConnectionState == ConnectionState.Connected);
                resetEvent.Set();
            };

            ConnectionStateChanged += stateChanged;
            Connect(server, password);
            resetEvent.WaitOne();
            ConnectionStateChanged -= stateChanged;

            return connected;
        }

        public void Disconnect()
        {
            if (isConnected)
            {
                OnConnectionStateChanged(new ConnectionStateEventArgs(ConnectionState.Closed));
                isConnected = false;
                socket.Disconnect(true);
            }
            else
            {
                OnError(new ErrorEventArgs(notConnectedString));
            }
        }

        public void Close()
        {
            if (!alreadyClosed)
            {
                OnConnectionStateChanged(new ConnectionStateEventArgs(ConnectionState.Closed));
                isConnected = false;
                alreadyClosed = true;
            }
            socket.Close();
        }

        /// <summary>
        /// Sends a command to the server. Result is returned asynchronously via callbacks
        /// so wire those up before using this.
        /// </summary>
        /// <param name="command">Command to send.</param>
        public void ServerCommand(string command)
        {
            if (isConnected)
            {
                RconPacket packetToSend = new RconPacket();
                ++requestIdCounter;
                packetToSend.RequestId = requestIdCounter;
                packetToSend.ServerDataSent = RconPacket.SERVERDATA_sent.SERVERDATA_EXECCOMMAND;
                packetToSend.String1 = command;
                SendRconPacket(packetToSend);
            }
            else
            {
                OnError(new ErrorEventArgs(notConnectedString));
            }
        }

        public string ServerCommandBlocking(string command)
        {
            string response = null;
            AutoResetEvent resetEvent = new AutoResetEvent(false);

            ServerResponseEventHandler serverResponse = (object sender, ServerResponseEventArgs e) => {
                response = e.ServerResponse.Message;
                resetEvent.Set();
            };

            ServerResponse += serverResponse;
            ServerCommand(command);
            resetEvent.WaitOne();
            ServerResponse -= serverResponse;

            return response;
        }

        void Reset()
        {
            packetCount = 0;
            requestIdCounter = 0;
        }

        void SendRconPacket(RconPacket p)
        {
            byte[] packet = p.OutputAsBytes();
            try
            {
                socket.BeginSend(packet, 0, packet.Length, SocketFlags.None, new AsyncCallback(SendCallback), this);
            }
            catch (SocketException se)
            {
                OnError(new ErrorEventArgs(connectionClosedString + se.Message));
                Disconnect();
            }
        }

        void SendCallback(IAsyncResult ar)
        {
            try
            {
                socket.EndSend(ar);
            }
            catch (SocketException se)
            {
                OnError(new ErrorEventArgs(connectionClosedString + se.Message));
                Disconnect();
            }
        }

        void StartGetNewPacket()
        {
            PacketState state = new PacketState();
            state.IsPacketLength = true;
            state.Data = new byte[4];
            state.PacketCount = packetCount;
            packetCount++;
            
            #if DEBUG
            tempPackets.Add(state);
            #endif

            try
            {
                socket.BeginReceive(state.Data, 0, 4, SocketFlags.None, new AsyncCallback(ReceiveCallback), state);
            }
            catch (SocketException se)
            {
                OnError(new ErrorEventArgs(connectionClosedString + se.Message));
                Disconnect();
            }
        }

        void ReceiveCallback(IAsyncResult ar)
        {
            PacketState state = null;

            try
            {
                int bytesRead = socket.EndReceive(ar);
                state = (PacketState)ar.AsyncState;
                state.BytesSoFar += bytesRead;

                Debug.WriteLine(String.Format("Receive Callback. Packet: {0} First packet: {1}, Bytes so far: {2}",
                    state.PacketCount, state.IsPacketLength, state.BytesSoFar));

                ThreadPool.QueueUserWorkItem((object pool_state) => 
                {
                    ProcessIncomingData(state);
                });
                
            }
            catch (SocketException)
            {
                OnError(new ErrorEventArgs(connectionClosedString));
                OnConnectionStateChanged(new ConnectionStateEventArgs(ConnectionState.Disconnected));
                Close();
            }
            catch (ObjectDisposedException ex)
            {
                Debug.WriteLine("ObjectDisposedException catched: " + ex.Message);
                OnConnectionStateChanged(new ConnectionStateEventArgs(ConnectionState.Disconnected));
                Close();
            }
        }

        void ProcessIncomingData(PacketState state)
        {
            if (state.IsPacketLength)
            {
                // First 4 bytes of a new packet.
                state.PacketLength = BitConverter.ToInt32(state.Data, 0);

                state.IsPacketLength = false;
                state.BytesSoFar = 0;
                state.Data = new byte[state.PacketLength];

                if (state.PacketLength > 0)
                {
                    StartToReceive(state);
                }
                else
                {
                    OnError(new ErrorEventArgs(connectionClosedString + "Empty packet."));
                    Disconnect();
                }
            }
            else
            {
                // This is a fragment of a complete packet.
                if (state.BytesSoFar < state.PacketLength)
                {
                    // We don't have all the data, ask the network for the rest.
                    StartToReceive(state);
                }
                else
                {
                    // This is the whole packet, so we can go ahead and pack it up 
                    // into a structure and then punt it upstairs.
                    Debug.WriteLine("Complete packet.");

                    RconPacket returnedPacket = new RconPacket();
                    returnedPacket.ParseFromBytes(state.Data, this);

                    ThreadPool.QueueUserWorkItem((object pool_state) => 
                    {
                        ProcessResponse(returnedPacket);
                    });

                    // Wait for new packet.
                    StartGetNewPacket();
                }
            }
        }

        void StartToReceive(PacketState state)
        {
            try
            {
                socket.BeginReceive(state.Data, state.BytesSoFar, state.PacketLength - state.BytesSoFar, SocketFlags.None, new AsyncCallback(ReceiveCallback), state);
            }
            catch (SocketException se)
            {
                OnError(new ErrorEventArgs(connectionClosedString + se.Message));
                Disconnect();
            }
        }

        void ProcessResponse(RconPacket P)
        {
            switch (P.ServerDataReceived)
            {
                case RconPacket.SERVERDATA_rec.SERVERDATA_AUTH_RESPONSE:
                    if (P.RequestId != -1)
                    {
                        // Connected.
                        isConnected = true;
                        OnServerResponse(new ServerResponseEventArgs(new ServerResponse(connectionSuccessString)));
                        OnConnectionStateChanged(new ConnectionStateEventArgs(ConnectionState.Success));
                    }
                    else
                    {
                        // Failed!
                        OnError(new ErrorEventArgs(connectionFailedString));
                        OnConnectionStateChanged(new ConnectionStateEventArgs(ConnectionState.Failed));
                    }
                    break;
                case RconPacket.SERVERDATA_rec.SERVERDATA_RESPONSE_VALUE:
                    if (hadJunkPacket)
                    {
                        // Real packet!
                        OnServerResponse(new ServerResponseEventArgs(new ServerResponse(P.String1)));
                    }
                    else
                    {
                        hadJunkPacket = true;
                        OnError(new ErrorEventArgs(gotJunkPacketString));
                    }
                    break;
                default:
                    OnError(new ErrorEventArgs(unknownResponseTypeString));
                    break;
            }
        }

        protected virtual void OnServerResponse(ServerResponseEventArgs e)
        {
            if (ServerResponse != null)
                ServerResponse(this, e);
        }

        protected virtual void OnError(ErrorEventArgs e)
        {
            if (Error != null)
                Error(this, e);
        }

        protected virtual void OnConnectionStateChanged(ConnectionStateEventArgs e)
        {
            if (ConnectionStateChanged != null)
                ConnectionStateChanged(this, e);
        }
    }
}
