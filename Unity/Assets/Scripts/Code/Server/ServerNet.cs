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
        private ServerPlayerManager _playerManager;
        public ushort Tick => _serverTick;
        private ushort _serverTick;


        #region Inner Method
        public async Task StartProgram()
        {
            _playerManager = new ServerPlayerManager(this);
            _netManager = new NetManager(this)
            {
                AutoRecycle = true
            };
            _netManager.Start(Port);

            dic_recv = new Dictionary<uint, Dictionary<int, uint>>();

            while (true)
            {
                Update();
                await Task.Delay(15);
            }
        }

        public void Update()
        {
            _netManager.PollEvents();
        }

        public void Dispose()
        {
            _netManager.Stop();
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
            //UnityEngine.Debug.Log($"[新消息] {pt}");
            switch (pt)
            {
                case PacketType.C2S_TestX1Req:
                    OnTestPVE(reader, peer);
                    break;
                case PacketType.C2S_TestX2Req:
                    OnTestPVP(reader, peer);
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
        void OnTestPVE(NetPacketReader reader, NetPeer peer)
        {
            var cmd = new C2S_JoinPacket();
            cmd.Deserialize(reader);
            UnityEngine.Debug.Log($"[C2S.TestPVE] {peer.Id}: {cmd.UserName}");

            var serverPlayer = new ServerPlayer(_playerManager, cmd.UserName, peer);
            _playerManager.AddPlayer(serverPlayer);

            var host = _playerManager.GetPlayer(0);
            var packet = new S2C_JoinResultPacket { Code = 0, HostId = host.Id, HostName = host.UserName, GuestId = 0, GuestName = "" };
            peer.Send(WriteSerializable(PacketType.S2C_TestX1Result, packet), DeliveryMethod.ReliableOrdered);
        }

        void OnTestPVP(NetPacketReader reader, NetPeer peer)
        {
            var cmd = new C2S_JoinPacket();
            cmd.Deserialize(reader);
            UnityEngine.Debug.Log($"[C2S.TestPVP] {peer.Id}: {cmd.UserName}");

            var serverPlayer = new ServerPlayer(_playerManager, cmd.UserName, peer);
            _playerManager.AddPlayer(serverPlayer);


            if (_playerManager.Count == 2)
            {
                var host = _playerManager.GetPlayer(0);
                var guest = _playerManager.GetPlayer(1);

                for (int i = 0; i < _playerManager.Count; i++)
                {
                    var packet = new S2C_JoinResultPacket { Code = 0, HostId = host.Id, HostName = host.UserName, GuestId = guest.Id, GuestName = guest.Name };

                    var sp = _playerManager.GetPlayer(i);
                    //UnityEngine.Debug.Log($"send to: {sp.Id}---{sp.Name}");
                    sp.AssociatedPeer.Send(WriteSerializable(PacketType.S2C_TestX2Result, packet), DeliveryMethod.ReliableOrdered);
                }
            }
        }

        private Dictionary<uint, Dictionary<int, uint>> dic_recv;

        void OnInputReceived(NetPacketReader reader, NetPeer peer)
        {
            var req = new C2S_InputPacket();
            req.Deserialize(reader);


            int pid = peer.Id;
            if (dic_recv.ContainsKey(req.frameNumber) == false)
            {
                UnityEngine.Debug.Log($"[C2S.Input.111] {pid}: {req.frameNumber}---{req.input}");
                dic_recv[req.frameNumber] = new Dictionary<int, uint>();
                dic_recv[req.frameNumber][pid] = req.input;
            }
            else
            {
                UnityEngine.Debug.Log($"[C2S.Input.222] {pid}: {req.frameNumber}---{req.input}");
                // 同一个帧号，集齐两人份就下发
                dic_recv[req.frameNumber][pid] = req.input;

                // 发回给客户端
                var packet = new S2C_InputPacket
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