using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;
using Code.Shared;
using LiteNetLib;
using LiteNetLib.Utils;
using System.Threading;

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
        public const ushort TICK_RATE = 10;
        public const string Key = "ExampleGame";

        public NetManager _netManager;
        private readonly NetDataWriter _cachedWriter = new NetDataWriter();

        public ServerRoomManager m_RoomManager;
        public ServerPlayerManager m_PlayerManager;


        #region Inner Method
        public async Task StartProgram()
        {
            bool result = StartServer();
            if (!result) return;

            while (true)
            {
                Update();
                await Task.Delay(15);
            }
        }
        public void Dispose()
        {
            StopServer();
        }

        protected bool StartServer()
        {
            if (_netManager != null && _netManager.IsRunning)
            {
                UnityEngine.Debug.LogError("服务器已经启动");
                return false;
            }

            m_RoomManager = new ServerRoomManager();
            m_PlayerManager = new ServerPlayerManager();
            m_WaitingPeers = new List<ServerPlayer>();
            dic_recv = new Dictionary<uint, Dictionary<int, uint>>();

            _netManager = new NetManager(this);
            _netManager.AutoRecycle = true;

            bool result = _netManager.Start(Port);
            if (result == false)
            {
                UnityEngine.Debug.LogError("服务器启动失败，请检查端口");
                return false;
            }

            StartMatchTask();
            return true;
        }
        protected void StopServer()
        {
            _netManager.Stop();
            CancelMatchTask();
            m_RoomManager.RemoveAll();
            m_PlayerManager.RemoveAll();
            UnityEngine.Debug.LogError("服务器已经停止");
        }
        protected void Update()
        {
            _netManager.PollEvents();
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
            var cmd = new C2S_LoginPacket();
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
            UnityEngine.Debug.Log($"[S] Match Request received: [{peer.Id}]{player.UserName}");

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
            UnityEngine.Debug.Log($"[S] Match Cancel received: [{peer.Id}]{player.UserName}");

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
            UnityEngine.Debug.Log($"[S] OnMatchQuitReceived: [{peer.Id}]{player.UserName}@Room#{player.RoomId}");

            // 通知房间内的另一个人，并移除列表。
            short serverRoomId = (short)player.RoomId;
            var serverRoom = m_RoomManager.GetServerRoom(serverRoomId);
            var otherPlayer = serverRoom.GetOtherPlayer(player.PeerId);
            var packet = new S2C_MatchResultPacket { Code = 2, RoomId = serverRoomId };
            BroadcastToRoom(serverRoomId, WriteSerializable(PacketType.S2C_MatchResult, packet), DeliveryMethod.ReliableOrdered);

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
            BroadcastToRoom(serverRoomID, WriteSerializable(PacketType.S2C_RoleSelect, packet), DeliveryMethod.ReliableOrdered);
        }

        private void OnGameReadyReceived(NetPacketReader reader, NetPeer peer)
        {
            if (peer.Tag == null) return;
            var player = (ServerPlayer)peer.Tag;
            player.SetStatus(PlayerStatus.AtRoomReady);
            UnityEngine.Debug.Log($"[S] {player.ToString()} is Ready");

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
                BroadcastToRoom(serverRoomID, WriteSerializable(PacketType.S2C_GameReady, packet), DeliveryMethod.ReliableOrdered);
            }
            else if (player.Status == PlayerStatus.AtRoomReady && otherPlayer.Status == player.Status)
            {
                UnityEngine.Debug.Log("222---两人都准备好了，直接由服务器开始。");
                // 两人都准备好了，直接由服务器开始。
                var packet1 = new S2C_GameReadyPacket { HostStatus = (byte)host.Status, GuestStatus = (byte)guest.Status };
                BroadcastToRoom(serverRoomID, WriteSerializable(PacketType.S2C_GameReady, packet1), DeliveryMethod.ReliableOrdered);

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
                BroadcastToRoom(serverRoomID, WriteSerializable(PacketType.S2C_LoadScene, packet2), DeliveryMethod.ReliableOrdered);

                player.SetStatus(PlayerStatus.AtBattle);
                otherPlayer.SetStatus(PlayerStatus.AtBattle);
            }
            else
            {
                UnityEngine.Debug.LogError($"严重的错误：{host.Status} + {guest.Status}");
            }
        }

        #endregion


        #region 服务器命令
        private List<ServerPlayer> m_WaitingPeers;
        private CancellationTokenSource tokenSource;
        private void StartMatchTask()
        {
            tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;
            ManualResetEvent resetEvent = new ManualResetEvent(true);
            var matchLoop = new Task(async () => {
                while (true)
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    // 初始化为true时执行WaitOne不阻塞
                    resetEvent.WaitOne();

                    // Doing something.......
                    DoMatch();

                    // 模拟等待3000ms
                    await Task.Delay(3000);
                }
            }, token);
            matchLoop.Start();
        }
        private void CancelMatchTask()
        {
            tokenSource?.Cancel();
        }
        private void DoMatch()
        {
            lock (m_WaitingPeers)
            {
                //UnityEngine.Debug.Log($"match once，waiting count={m_WaitingPeers.Count}");
                if (m_WaitingPeers.Count <= 1)
                {
                    //UnityEngine.Debug.LogError("not enough players");
                    return;
                }
                ServerPlayer p1 = m_WaitingPeers[0];
                ServerPlayer p2 = m_WaitingPeers[1];

                // 通知匹配成功
                var serverRoom = m_RoomManager.CreateServerRoom(p1, p2);
                short serverRoomID = (short)serverRoom.RoomID;
                UnityEngine.Debug.Log($"match success, put {p1.PeerId}, {p1.PeerId} into Room#{serverRoomID}");
                p1.SetRoomID(serverRoomID).SetSeatID(0).SetStatus(PlayerStatus.AtRoomWait);
                p2.SetRoomID(serverRoomID).SetSeatID(1).SetStatus(PlayerStatus.AtRoomWait);
                UserInfo hostPlayer = new UserInfo { PeerId = p1.PeerId, UserName = p1.UserName };
                UserInfo guestPlayer = new UserInfo { PeerId = p2.PeerId, UserName = p2.UserName };
                var packet = new S2C_MatchResultPacket { Code = 0, RoomId = serverRoomID, Host = hostPlayer, Guest = guestPlayer };
                BroadcastToRoom(serverRoomID, WriteSerializable(PacketType.S2C_MatchResult, packet), DeliveryMethod.ReliableOrdered);

                m_WaitingPeers.Remove(p1);
                m_WaitingPeers.Remove(p2);
                UnityEngine.Debug.Log($"send ok, waiting count={m_WaitingPeers.Count}");

                string timeStr = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
                int randSeed = System.Guid.NewGuid().GetHashCode();
                serverRoom.BattleID = $"{timeStr}_{hostPlayer.PeerId}_{guestPlayer.PeerId}";
                serverRoom.Seed = randSeed;
                serverRoom.MapId = 0; //来自客户端
                serverRoom.BattleMode = BattleMode.Matching;
            }
        }

        // 大厅内广播
        public void BroadcastToLobby(NetDataWriter writer, DeliveryMethod method)
        {
            ServerPlayer[] array = m_PlayerManager.GetPlayersByLobby();
            for (int i = 0; i < array.Length; i++)
            {
                NetPeer peer = _netManager.GetPeerById(array[i].PeerId);
                peer?.Send(writer, method);
            }
        }
        // 房间内广播
        public void BroadcastToRoom(int roomId, NetDataWriter writer, DeliveryMethod method)
        {
            ServerRoom serverRoom = m_RoomManager.GetServerRoom(roomId);
            var players = serverRoom.m_PlayerList; //直接从内存取
            for (int i = 0; i < players.Length; i++)
            {
                short peedId = players[i].PeerId;
                _netManager.GetPeerById(peedId).Send(writer, method);
            }
        }
        #endregion
    }
}