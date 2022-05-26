using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Code.Shared;
using LiteNetLib;
using LiteNetLib.Utils;

namespace Code.Server
{
    public class ServerNet : INetEventListener, IDisposable
    {
        public const int Port = 5000;
        public const string Key = "ExampleGame";

        private NetManager _netManager;
        private readonly NetDataWriter _cachedWriter = new NetDataWriter();

        public const int MaxPlayers = 64;
        private LogicTimer _logicTimer;
        private ServerPlayerManager _playerManager;
        private PlayerInputPacket _cachedCommand = new PlayerInputPacket();
        public ushort Tick => _serverTick;
        private ushort _serverTick;


        #region Inner Method
        public async Task StartServer()
        {
            _logicTimer = new LogicTimer(OnLogicUpdate);
            _playerManager = new ServerPlayerManager(this);
            _netManager = new NetManager(this)
            {
                AutoRecycle = true
            };
            _netManager.Start(Port);
            _logicTimer.Start();

            while (true)
            {
                Update();
                await Task.Delay(15);
            }
        }

        public void Dispose()
        {
            _netManager.Stop();
            _logicTimer.Stop();
        }

        void OnLogicUpdate()
        {
            _serverTick = (ushort)((_serverTick + 1) % NetworkGeneral.MaxGameSequence);
            _playerManager.LogicUpdate();
            if (_serverTick % 2 == 0)
            {
                //_serverState.Tick = _serverTick;
                int pCount = _playerManager.Count;

                foreach (ServerPlayer p in _playerManager)
                {
                    //int statesMax = p.AssociatedPeer.GetMaxSinglePacketSize(DeliveryMethod.Unreliable) - ServerState.HeaderSize;
                    //statesMax /= PlayerState.Size;

                    //for (int s = 0; s < (pCount - 1) / statesMax + 1; s++)
                    //{
                    //    //TODO: divide
                    //    //_serverState.LastProcessedCommand = p.LastProcessedCommandId;
                    //    //_serverState.PlayerStatesCount = pCount;
                    //    //_serverState.StartState = s * statesMax;
                    //    //p.AssociatedPeer.Send(WriteSerializable(PacketType.ServerState, _serverState), DeliveryMethod.Unreliable);
                    //}
                }
            }
        }

        void Update()
        {
            _netManager.PollEvents();
            _logicTimer.Update();
        }
        #endregion


        #region Interface
        private NetDataWriter WriteSerializable<T>(PacketType type, T packet) where T : struct, INetSerializable
        {
            _cachedWriter.Reset();
            _cachedWriter.Put((byte)type);
            packet.Serialize(_cachedWriter);
            return _cachedWriter;
        }

        void INetEventListener.OnPeerConnected(NetPeer peer)
        {
            Console.WriteLine("[S] Player connected: " + peer.EndPoint);
        }

        void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Console.WriteLine("[S] Player disconnected: " + disconnectInfo.Reason);

            if (peer.Tag != null)
            {
                byte playerId = (byte)peer.Id;
                if (_playerManager.RemovePlayer(playerId))
                {
                    //var plp = new PlayerLeavedPacket { Id = (byte)peer.Id };
                    //_netManager.SendToAll(WritePacket(plp), DeliveryMethod.ReliableOrdered);
                }
            }
        }

        void INetEventListener.OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            Console.WriteLine("[S] NetworkError: " + socketError);
        }

        void INetEventListener.OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
        {
            byte packetType = reader.GetByte();
            if (packetType >= NetworkGeneral.PacketTypesCount)
                return;

            PacketType pt = (PacketType)packetType;
            switch (pt)
            {
                case PacketType.C2S_LoginReq:
                    OnLoginReceived(reader, peer);
                    break;
                case PacketType.C2S_Lockstep:
                    OnInputReceived(reader, peer);
                    break;
                default:
                    Console.WriteLine("Unhandled packet: " + pt);
                    break;
            }
        }

        void INetEventListener.OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {

        }

        void INetEventListener.OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
            if (peer.Tag != null)
            {
                var p = (ServerPlayer)peer.Tag;
                p.Ping = latency;
            }
        }

        void INetEventListener.OnConnectionRequest(ConnectionRequest request)
        {
            request.AcceptIfKey(Key);
        }
        #endregion


        #region Handler
        void OnLoginReceived(NetPacketReader reader, NetPeer peer)
        {
            LoginRequest req = new LoginRequest();
            req.Deserialize(reader);
            Console.WriteLine($"[C2S] OnLogin: {req.UserName}");

            LoginResponse resp = new LoginResponse { UserName = req.UserName, Token = "123ABC" };
            peer.Send(WriteSerializable(PacketType.S2C_LoginResult, resp), DeliveryMethod.ReliableOrdered);
        }

        void OnInputReceived(NetPacketReader reader, NetPeer peer)
        {
            if (peer.Tag == null)
                return;
            _cachedCommand.Deserialize(reader);
            var player = (ServerPlayer)peer.Tag;
        }
        #endregion
    }
}