using System;
using System.Linq;
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
            if (packetType >= 1024) return;

            PacketType pt = (PacketType)packetType;
            //UnityEngine.Debug.Log($"[新消息] {pt}");
            switch (pt)
            {
                case PacketType.C2S_TestPVE:
                    OnTestPVE(reader, peer);
                    break;
                case PacketType.C2S_TestPVP:
                    OnTestPVP(reader, peer);
                    break;
                case PacketType.C2S_Lockstep:
                    OnInputReceived(reader, peer);
                    break;
                case PacketType.C2S_LoginReq:
                    OnLoginReceived(reader, peer);
                    break;
                default:
                    UnityEngine.Debug.Log("Unhandled packet: " + pt);
                    break;
            }
        }

        void INetEventListener.OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { }

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
            var packet = new S2C_JoinResultPacket { Code = 0, HostId = host.PeerId, HostName = host.UserName, GuestId = 0, GuestName = "" };
            peer.Send(WriteSerializable(PacketType.S2C_TestPVE, packet), DeliveryMethod.ReliableOrdered);
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
                    var packet = new S2C_JoinResultPacket { Code = 0, HostId = host.PeerId, HostName = host.UserName, GuestId = guest.PeerId, GuestName = guest.UserName };

                    var sp = _playerManager.GetPlayer(i);
                    //UnityEngine.Debug.Log($"send to: {sp.Id}---{sp.Name}");
                    sp.AssociatedPeer.Send(WriteSerializable(PacketType.S2C_TestPVP, packet), DeliveryMethod.ReliableOrdered);
                }
            }
        }

        private Dictionary<uint, Dictionary<int, uint>> dic_recv;

        private void OnInputReceived(NetPacketReader reader, NetPeer peer)
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

        private void OnLoginReceived(NetPacketReader reader, NetPeer peer)
        {
            C2S_LoginPacket cmd = new C2S_LoginPacket();
            cmd.Deserialize(reader);
            UnityEngine.Debug.Log($"<color=green>[S] Login packet received: [{peer.Id}]{cmd.UserName},{cmd.Password}</color>");

            #region 检查逻辑
            // 校验账号密码
            string query = $"SELECT Count(*) FROM tb_user WHERE username='{cmd.UserName}'";
            int check1 = DatabaseEssential.DatabaseManager.instance.Count(query);
            if (check1 == 0)
            {
                UnityEngine.Debug.LogError("账号或密码错误");
                var packet = new S2C_LoginResultPacket { Code = 1 };
                peer.Send(WriteSerializable(PacketType.S2C_LoginResult, packet), DeliveryMethod.ReliableOrdered);
                return;
            }

            // 校验是否已登录
            var list = _playerManager.GetPlayersAll();
            var check2 = list.Where(x => x.UserName == cmd.UserName);
            if (check2.Count() > 0)
            {
                var player0 = check2.FirstOrDefault();

                // 这里也可能是同一个人走了重连
                UnityEngine.Debug.Log($"检测是否重连：{player0.ToString()}");
                //if (player0.Status == PlayerStatus.Reconnect)
                //{
                //    UnityEngine.Debug.Log($"<color=green>是重连的，返还数据</color>");
                //}
                //else
                //{
                //    UnityEngine.Debug.LogError("该账号已经登录，顶号");
                //    var _peer = _netManager.ConnectedPeerList.Where(x => x.Id == check2.First().PeerId).ToList()[0];
                //    _playerManager.RemovePlayer(_peer.Id); //踢人
                //    _peer.Disconnect();
                //}
            }
            #endregion

            #region 登录逻辑
            // 新建玩家对象
            var player = new ServerPlayer(_playerManager, cmd.UserName, peer);
            _playerManager.AddPlayer(player);

            // 第一个包，登录许可
            var packet1 = new S2C_LoginResultPacket
            {
                Code = 0,
                PeerId = player.PeerId,
                UserName = player.UserName,
                NickName = player.NickName,
            };
            peer.Send(WriteSerializable(PacketType.S2C_LoginResult, packet1), DeliveryMethod.ReliableOrdered);
            
            // 第二个包，用户设置
            //var packet2 = new Settings
            //{
            //    ScreenSize = 0,
            //    FullScreen = 0,
            //    MusicVolume = userData.musicVolume,
            //    SoundVolume = userData.soundVolume,
            //};
            //peer.Send(WriteSerializable(PacketType.S2C_Settings, packet2), DeliveryMethod.ReliableOrdered);
            #endregion
        }
        #endregion
    }
}