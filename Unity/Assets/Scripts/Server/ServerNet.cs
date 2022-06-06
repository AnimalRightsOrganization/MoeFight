using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;
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
        public ushort Tick => _serverTick;
        private ushort _serverTick;


        #region Inner Method
        public async Task StartProgram()
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
        public void StartServer()
        {
            _logicTimer = new LogicTimer(OnLogicUpdate);
            _playerManager = new ServerPlayerManager(this);
            _netManager = new NetManager(this)
            {
                AutoRecycle = true
            };
            _netManager.Start(Port);
            _logicTimer.Start();

            //while (true)
            //{
            //    Update();
            //    await Task.Delay(15);
            //}

            dic_recv = new Dictionary<uint, Dictionary<int, uint>>();
        }
        public void Update()
        {
            _netManager.PollEvents();
            _logicTimer.Update();
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
            UnityEngine.Debug.Log("[S] Player connected: " + peer.EndPoint);
        }

        void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            UnityEngine.Debug.Log("[S] Player disconnected: " + disconnectInfo.Reason);

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
            UnityEngine.Debug.Log("[S] NetworkError: " + socketError);
        }

        void INetEventListener.OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
        {
            byte packetType = reader.GetByte();
            if (packetType >= NetworkGeneral.PacketTypesCount)
                return;

            PacketType pt = (PacketType)packetType;
            UnityEngine.Debug.Log($"[新消息] {pt}");
            switch (pt)
            {
                case PacketType.C2S_LoginReq:
                    OnLoginReceived(reader, peer);
                    break;
                case PacketType.C2S_Lockstep:
                    OnInputReceived(reader, peer);
                    break;
                default:
                    UnityEngine.Debug.Log("Unhandled packet: " + pt);
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
            var req = new C2S_LoginPacket();
            req.Deserialize(reader);
            UnityEngine.Debug.Log($"[C2S.Login] {peer.Id}: {req.UserName}");

            var p = (ServerPlayer)peer.Tag;
            _playerManager.AddPlayer(p);

            var resp = new S2C_LoginResultPacket { Code = 0, PeerId = (short)peer.Id, UserName = req.UserName };
            peer.Send(WriteSerializable(PacketType.S2C_LoginResult, resp), DeliveryMethod.ReliableOrdered);
        }

        private Dictionary<uint, Dictionary<int, uint>> dic_recv;

        void OnInputReceived(NetPacketReader reader, NetPeer peer)
        {
            var req = new C2S_InputPacket();
            req.Deserialize(reader);
            UnityEngine.Debug.Log($"[C2S.Input] {peer.Id}: {req.frameNumber}---{req.input}");


            int pid = peer.Id;
            if (dic_recv.ContainsKey(req.frameNumber) == false)
            {
                dic_recv[req.frameNumber] = new Dictionary<int, uint>();
                dic_recv[req.frameNumber][pid] = req.input;
            }
            else
            {
                // 同一个帧号，集齐两人份就下发
                dic_recv[req.frameNumber][pid] = req.input;

                // 发回给客户端
                S2C_InputPacket packet = new S2C_InputPacket
                {
                    frameNumber = req.frameNumber,
                    inputs = new uint[] { dic_recv[req.frameNumber][0], dic_recv[req.frameNumber][1] }
                };
                _netManager.SendToAll(WriteSerializable(PacketType.S2C_Lockstep, packet), DeliveryMethod.ReliableOrdered);
            }
        }
        #endregion
    }
}