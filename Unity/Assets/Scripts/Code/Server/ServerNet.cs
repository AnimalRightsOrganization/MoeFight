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
        static ServerNet _get;
        public static ServerNet Get
        {
            get
            {
                if (_get == null)
                    _get = new ServerNet();
                return _get;
            }
        }

        public const int MaxPlayers = 64;
        public const int Port = 5000;
        public const string Key = "ExampleGame";

        public NetManager _netManager;
        private readonly NetDataWriter _cachedWriter = new NetDataWriter();

        public ServerRoomManager m_RoomManager;
        public ServerPlayerManager m_PlayerManager;
        public ushort Tick => _serverTick;
        private ushort _serverTick;

        public List<ServerPlayer> m_WaitingPeers = new List<ServerPlayer>();

        #region Inner Method
        public async Task StartProgram()
        {
            m_PlayerManager = new ServerPlayerManager();
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
                //byte playerId = (byte)peer.Id;
                if (m_PlayerManager.RemovePlayer(peer.Id))
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
                case PacketType.C2S_MatchRequest:
                    OnMatchRequestReceived(reader, peer);
                    break;
                case PacketType.C2S_MatchCancel:
                    OnMatchCancelReceived(reader, peer);
                    break;
                case PacketType.C2S_MatchQuit:
                    OnMatchQuitReceived(reader, peer);
                    break;
                case PacketType.C2S_RoleSelect:
                    OnRoleSelectReceived(reader, peer);
                    break;
                case PacketType.C2S_GameReady:
                    OnGameReadyReceived(reader, peer);
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

            var serverPlayer = new ServerPlayer(cmd.UserName, peer);
            m_PlayerManager.AddPlayer(serverPlayer);

            var host = m_PlayerManager.GetPlayerByPeerId(0);
            var packet = new S2C_JoinResultPacket { Code = 0, HostId = host.PeerId, HostName = host.UserName, GuestId = 0, GuestName = "" };
            peer.Send(WriteSerializable(PacketType.S2C_TestPVE, packet), DeliveryMethod.ReliableOrdered);
        }

        void OnTestPVP(NetPacketReader reader, NetPeer peer)
        {
            var cmd = new C2S_JoinPacket();
            cmd.Deserialize(reader);
            UnityEngine.Debug.Log($"[C2S.TestPVP] {peer.Id}: {cmd.UserName}");

            var serverPlayer = new ServerPlayer(cmd.UserName, peer);
            m_PlayerManager.AddPlayer(serverPlayer);


            if (m_PlayerManager.Count == 2)
            {
                var host = m_PlayerManager.GetPlayerByPeerId(0);
                var guest = m_PlayerManager.GetPlayerByPeerId(1);

                for (int i = 0; i < m_PlayerManager.Count; i++)
                {
                    var packet = new S2C_JoinResultPacket { Code = 0, HostId = host.PeerId, HostName = host.UserName, GuestId = guest.PeerId, GuestName = guest.UserName };

                    var sp = m_PlayerManager.GetPlayerByPeerId((short)i);
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
            string query = $"SELECT Count(*) FROM tb_user WHERE username='{cmd.UserName}' AND password='{cmd.Password}'";
            int check1 = DatabaseEssential.DatabaseManager.Count(query);
            UnityEngine.Debug.Log($"check1: {check1}");
            if (check1 == 0)
            {
                UnityEngine.Debug.LogError("账号或密码错误");
                var packet = new S2C_LoginResultPacket { Code = 1 };
                peer.Send(WriteSerializable(PacketType.S2C_LoginResult, packet), DeliveryMethod.ReliableOrdered);
                return;
            }

            // 校验是否已登录
            //var list = m_PlayerManager.GetPlayersAll();
            //UnityEngine.Debug.Log($"list:{list.Length}"); //64
            #endregion

            #region 登录逻辑
            // 新建玩家对象
            var player = new ServerPlayer(cmd.UserName, peer);
            m_PlayerManager.AddPlayer(player);

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

        private void OnSettingsReceived(NetPacketReader reader, NetPeer peer)
        {
            if (peer.Tag == null) return;
            var player = (ServerPlayer)peer.Tag;

            Settings cmd = new Settings();
            cmd.Deserialize(reader);

            //var user = ConfigManager.m_DBConfig.users.Where(x => x.userName == player.UserName).ToArray().FirstOrDefault();
            //user.musicVolume = cmd.MusicVolume;
            //user.soundVolume = cmd.SoundVolume;
            //ConfigManager.m_DBConfig.Save();

            peer.Send(WriteSerializable(PacketType.S2C_Settings, cmd), DeliveryMethod.ReliableOrdered);
        }

        private void OnMatchRequestReceived(NetPacketReader reader, NetPeer peer)
        {
            if (peer.Tag == null) return;
            var player = (ServerPlayer)peer.Tag;

            // 加入列表。
            lock (m_WaitingPeers)
            {
                m_WaitingPeers.Add(player);
            }
            player.SetStatus(PlayerStatus.Matching);
        }

        private void OnMatchCancelReceived(NetPacketReader reader, NetPeer peer)
        {
            if (peer.Tag == null) return;
            var player = (ServerPlayer)peer.Tag;

            // 移除列表。
            lock (m_WaitingPeers)
            {
                m_WaitingPeers.Remove(player);
                player.ResetToLobby();
                UnityEngine.Debug.Log($"用户取消匹配，移除后排队人数为：{m_WaitingPeers.Count}");
            }

            var packet = new S2C_MatchResultPacket { Code = 1 };
            peer.Send(WriteSerializable(PacketType.S2C_MatchResult, packet), DeliveryMethod.ReliableOrdered);
        }

        private void OnMatchQuitReceived(NetPacketReader reader, NetPeer peer)
        {
            if (peer.Tag == null) return;
            var player = (ServerPlayer)peer.Tag;
            UnityEngine.Debug.Log($"[S] OnMatchQuitReceived: {player.UserName}，房间：{player.RoomId}");

            // 通知房间内的另一个人，并移除列表。
            short serverRoomId = (short)player.RoomId;
            var serverRoom = m_RoomManager.GetServerRoom(serverRoomId);
            var otherPlayer = serverRoom.GetOtherPlayer(player.PeerId);
            var packet = new S2C_MatchResultPacket { Code = 2, RoomId = serverRoomId };
            //BroadcastToRoom(serverRoomId, WriteSerializable(PacketType.S2C_MatchResult, packet), DeliveryMethod.ReliableOrdered);

            lock (m_WaitingPeers)
            {
                m_WaitingPeers.Remove(player);
                m_WaitingPeers.Remove(otherPlayer);
            }
            player.ResetToLobby();
            otherPlayer.ResetToLobby();
            m_RoomManager.RemoveServerRoom(serverRoomId); //一方取消匹配解散房间
        }

        private void OnRoleSelectReceived(NetPacketReader reader, NetPeer peer)
        {
            if (peer.Tag == null) return;
            var player = (ServerPlayer)peer.Tag;

            C2S_RoleSelectPacket cmd = new C2S_RoleSelectPacket();
            cmd.Deserialize(reader);
            UnityEngine.Debug.Log($"[S]{player.UserName}选择了{cmd.Index}，房间{player.RoomId}内广播");
            if (player.Status == PlayerStatus.AtRoomReady || player.Status == PlayerStatus.AtBattle)
            {
                UnityEngine.Debug.LogError("准备好的人不能再选择");
                return;
            }

            short serverRoomID = (short)player.RoomId;
            ServerRoom serverRoom = m_RoomManager.GetServerRoom(serverRoomID);
            bool playerIsHost = player.SeatId == 0;
            if (playerIsHost)
                serverRoom.hostPlayer.RoleIndex = cmd.Index;
            else
                serverRoom.guestPlayer.RoleIndex = cmd.Index;

            var packet = new S2C_RoleSelectPacket { SeatId = (byte)player.SeatId, RoleIndex = cmd.Index };
            //BroadcastToRoom(serverRoomID, WriteSerializable(PacketType.S2C_RoleSelect, packet), DeliveryMethod.ReliableOrdered);
        }

        private void OnGameReadyReceived(NetPacketReader reader, NetPeer peer)
        {
            if (peer.Tag == null) return;
            var player = (ServerPlayer)peer.Tag;
            player.SetStatus(PlayerStatus.AtRoomReady);
            UnityEngine.Debug.Log($"[S] {player.ToString()}，准备好了");

            short serverRoomID = (short)player.RoomId;
            ServerRoom serverRoom = m_RoomManager.GetServerRoom(serverRoomID);
            ServerPlayer otherPlayer = serverRoom.GetOtherPlayer(player.PeerId);

            bool playerIsHost = player.SeatId == 0;
            ServerPlayer host = playerIsHost ? player : otherPlayer;
            ServerPlayer guest = !playerIsHost ? player : otherPlayer;

            if (host.Status == PlayerStatus.AtRoomReady && guest.Status == PlayerStatus.AtRoomWait ||
                host.Status == PlayerStatus.AtRoomWait && guest.Status == PlayerStatus.AtRoomReady)
            {
                UnityEngine.Debug.Log("111---一个人准备好了，房间内广播。");
                // 一个人准备好了，房间内广播。
                var packet = new S2C_GameReadyPacket { HostStatus = (byte)host.Status, GuestStatus = (byte)guest.Status };
                //BroadcastToRoom(serverRoomID, WriteSerializable(PacketType.S2C_GameReady, packet), DeliveryMethod.ReliableOrdered);
            }
            else if (player.Status == PlayerStatus.AtRoomReady && otherPlayer.Status == player.Status)
            {
                UnityEngine.Debug.Log("222---两人都准备好了，直接由服务器开始。");
                // 两人都准备好了，直接由服务器开始。
                var packet1 = new S2C_GameReadyPacket { HostStatus = (byte)host.Status, GuestStatus = (byte)guest.Status };
                //BroadcastToRoom(serverRoomID, WriteSerializable(PacketType.S2C_GameReady, packet1), DeliveryMethod.ReliableOrdered);

                // 服务端立即下发，客户端做延迟表现
                var packet2 = new S2C_LoadScenePacket
                {
                    RoomId = serverRoomID,
                    BattleId = serverRoom.BattleID,
                    Seed = serverRoom.Seed,
                    MapId = serverRoom.MapId,
                    BattleMode = (byte)serverRoom.BattleMode,
                    Host = new PlayerLoadPacket { UserName = host.UserName, PeerId = host.PeerId, RoleIndex = host.RoleIndex },
                    Guest = new PlayerLoadPacket { UserName = guest.UserName, PeerId = guest.PeerId, RoleIndex = guest.RoleIndex },
                };
                //BroadcastToRoom(serverRoomID, WriteSerializable(PacketType.S2C_LoadScene, packet2), DeliveryMethod.ReliableOrdered);

                player.SetStatus(PlayerStatus.AtBattle);
                otherPlayer.SetStatus(PlayerStatus.AtBattle);
            }
            else
            {
                UnityEngine.Debug.LogError($"严重的错误：{host.Status} + {guest.Status}");
            }
        }

        #endregion
    }
}