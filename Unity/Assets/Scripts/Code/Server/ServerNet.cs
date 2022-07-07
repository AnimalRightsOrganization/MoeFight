using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Code.Shared;
using LiteNetLib;
using LiteNetLib.Utils;

namespace Code.Server
{
    public class ServerNet : INetEventListener, IDisposable
    {
        static ServerNet _instance;
        public static ServerNet Get
        {
            get
            {
                if (_instance == null)
                    _instance = new ServerNet();
                return _instance;
            }
        }

        public const int Port = 5000;
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
                UnityEngine.Debug.LogError("server has been running");
                return false;
            }

            m_RoomManager = new ServerRoomManager();
            m_PlayerManager = new ServerPlayerManager();
            m_WaitingPeers = new List<ServerPlayer>();

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
            UnityEngine.Debug.LogError("server was stopped");
        }
        protected void Update()
        {
            _netManager.PollEvents();
        }
        #endregion


        #region Interface
        public NetDataWriter WriteSerializable<T>(PacketType type, T packet) where T : struct, INetSerializable
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
            ServerPlayer player = (ServerPlayer)peer.Tag;
            UnityEngine.Debug.Log($"[S] {player} disconnected: {disconnectInfo.Reason}");

            if (player != null)
            {
                int serverRoomID = player.RoomId;

                if (player.Status == PlayerStatus.AtBattle)
                {
                    // 1.掉线（保留房间）
                    // 2.杀掉进程（主动发送认输）
                    //switch (disconnectInfo.Reason)
                    //{
                    //    case DisconnectReason.Timeout: //超时
                    //        break;
                    //    case DisconnectReason.RemoteConnectionClose: //主动关闭
                    //        break;
                    //}

                    ServerRoom serverRoom = m_RoomManager.GetServerRoom(serverRoomID);
                    ServerPlayer otherPlayer = serverRoom.GetOtherPlayer(player.PeerId); //BOT is null
                    if (otherPlayer.IsBot == false)
                    {
                        otherPlayer.AssociatedPeer.Send(WriteSerializable(PacketType.S2C_BattlePause, new EmptyPacket()), DeliveryMethod.ReliableOrdered);
                    }
                }
                else if (player.Status == PlayerStatus.AtRoomWait || player.Status == PlayerStatus.AtRoomReady)
                {
                    ServerRoom serverRoom = m_RoomManager.GetServerRoom(serverRoomID);
                    ServerPlayer otherPlayer = serverRoom.GetOtherPlayer(player.PeerId); //BOT is null
                    if (otherPlayer != null)
                    {
                        var packet = new S2C_MatchResultPacket { Code = 2 };
                        otherPlayer.AssociatedPeer.Send(WriteSerializable(PacketType.S2C_MatchResult, packet), DeliveryMethod.ReliableOrdered);
                    }
                    m_RoomManager.RemoveServerRoom(serverRoomID);
                    m_PlayerManager.RemovePlayer(peer.Id);
                    otherPlayer.ResetToLobby();
                }
                else if (player.Status == PlayerStatus.Matching)
                {
                    lock (m_WaitingPeers)
                    {
                        m_WaitingPeers.Remove(player);
                    }
                    m_PlayerManager.RemovePlayer(peer.Id);
                }
                else
                {
                    m_PlayerManager.RemovePlayer(peer.Id);
                }
            }
            UnityEngine.Debug.Log($"Player count:{m_PlayerManager.Count}, Room count:{m_RoomManager.Count}");
        }

        void INetEventListener.OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            UnityEngine.Debug.Log("[S] NetworkError: " + socketError); //本地断网
        }

        void INetEventListener.OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
        {
            byte packetType = reader.GetByte();
            if (packetType >= Enum.GetValues(typeof(PacketType)).Length) return;

            PacketType pt = (PacketType)packetType;
            //UnityEngine.Debug.Log($"[packet] {pt}");
            switch (pt)
            {
                case PacketType.C2S_TestPVE:
                    OnTestPVE(reader, peer);
                    break;
                case PacketType.C2S_TestPVP:
                    OnTestPVP(reader, peer);
                    break;
                case PacketType.C2S_Input:
                    OnInputReceived(reader, peer);
                    break;
                //以上是测试，保留
                case PacketType.C2S_RegisterReq:
                    //OnRegisterReceived(reader, peer);
                    break;
                case PacketType.C2S_LoginReq:
                    OnLoginReceived(reader, peer);
                    break;
                case PacketType.C2S_LogoutReq:
                    OnLogoutReceived(reader, peer);
                    break;
                case PacketType.C2S_UserInfo:
                    //OnGetUserInfoReceived(reader, peer);
                    break;
                case PacketType.C2S_Settings:
                    OnSettingsReceived(reader, peer);
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
                case PacketType.C2S_BattleStart:
                    OnBattleStartReceived(reader, peer);
                    break;
                case PacketType.C2S_BattlePause:
                    OnBattlePauseReceived(reader, peer);
                    break;
                case PacketType.C2S_BattleQuit:
                    OnBattleQuitReceived(reader, peer);
                    break;
                case PacketType.C2S_BattleEnd:
                    OnBattleEndReceived(reader, peer);
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
        private void OnTestPVE(NetPacketReader reader, NetPeer peer)
        {
            var cmd = new C2S_JoinPacket();
            cmd.Deserialize(reader);
            UnityEngine.Debug.Log($"[S] PVE [{peer.Id}]{cmd.UserName}");

            ServerPlayer player = (ServerPlayer)peer.Tag;
            ServerPlayer bot = new ServerPlayer("BOT");

            ServerRoom serverRoom = m_RoomManager.CreateServerRoom(player, bot);
            serverRoom.BattleMode = BattleMode.TestPVE;
            int serverRoomID = serverRoom.RoomID;
            player.SetRoomID(serverRoomID).SetSeatID(0).SetStatus(PlayerStatus.AtBattle);
            bot.SetRoomID(serverRoomID).SetSeatID(1).SetStatus(PlayerStatus.AtBattle);
            serverRoom.DoInit();
            m_RoomManager.SetBattle(serverRoom);
            UnityEngine.Debug.Log($"PVE create room#{serverRoomID}");

            var packet = new S2C_JoinResultPacket { Code = 0, HostId = player.PeerId, HostName = player.UserName, GuestId = bot.PeerId, GuestName = bot.UserName };
            var writer = WriteSerializable(PacketType.S2C_TestPVE, packet);
            peer.Send(writer, DeliveryMethod.ReliableOrdered);

            m_PlayerManager.Print();
            UnityEngine.Debug.Log($"PVE status: \n{player} \n{bot}");
        }

        private void OnTestPVP(NetPacketReader reader, NetPeer peer)
        {
            var cmd = new C2S_JoinPacket();
            cmd.Deserialize(reader);

            ServerPlayer player = (ServerPlayer)peer.Tag;
            UnityEngine.Debug.Log($"[S] PVP [{peer.Id}]{cmd.UserName}---{m_PlayerManager.Count}/2");

            if (m_PlayerManager.Count == 2)
            {
                var host = m_PlayerManager.GetPlayerByPeerId(0);
                var guest = m_PlayerManager.GetPlayerByPeerId(1);

                ServerRoom serverRoom = m_RoomManager.CreateServerRoom(host, guest);
                serverRoom.BattleMode = BattleMode.TestPVP;
                int serverRoomID = serverRoom.RoomID;
                host.SetRoomID(serverRoomID).SetSeatID(0).SetStatus(PlayerStatus.AtBattle);
                guest.SetRoomID(serverRoomID).SetSeatID(1).SetStatus(PlayerStatus.AtBattle);
                serverRoom.DoInit();
                m_RoomManager.SetBattle(serverRoom);

                var packet = new S2C_JoinResultPacket { Code = 0, HostId = host.PeerId, HostName = host.UserName, GuestId = guest.PeerId, GuestName = guest.UserName };
                var writer = WriteSerializable(PacketType.S2C_TestPVP, packet);
                serverRoom.Send(writer);
            }
        }

        private void OnInputReceived(NetPacketReader reader, NetPeer peer)
        {
            if (peer.Tag == null) return;
            ServerPlayer player = (ServerPlayer)peer.Tag;

            var cmd = new C2S_InputPacket();
            cmd.Deserialize(reader);

            // 派发给指定房间
            int serverRoomID = player.RoomId;
            ServerRoom serverRoom = null;
            var serverBattles = m_RoomManager.GetBattles();
            if (serverBattles.TryGetValue(serverRoomID, out serverRoom))
            {
                serverRoom.OnInputReceived(player.SeatId, cmd);
            }
        }
        //以上是测试，保留

        private void OnLoginReceived(NetPacketReader reader, NetPeer peer)
        {
            var cmd = new C2S_LoginPacket();
            cmd.Deserialize(reader);
            UnityEngine.Debug.Log($"[S] Login packet received: [{peer.Id}]{cmd.UserName},{cmd.Password}");

            #region 验证逻辑
#if UNITY_SERVER || UNITY_EDITOR
            string query = $"SELECT Count(*) FROM tb_user WHERE username='{cmd.UserName}' AND password='{cmd.Password}'";
            int check1 = DatabaseEssential.DatabaseManager.Count(query);
            //UnityEngine.Debug.Log($"check username & password: {check1}");
            if (check1 <= 0)
            {
                UnityEngine.Debug.LogError("username or password is incorrect");
                var packet = new S2C_LoginResultPacket { Code = 1 };
                peer.Send(WriteSerializable(PacketType.S2C_LoginResult, packet), DeliveryMethod.ReliableOrdered);
                return;
            }
#endif
            #endregion

            #region 登录逻辑
            bool isReconnect = false;
            ServerPlayer player = null;
            // 校验是否已登录，是否重连
            ServerPlayer lastPlayer = m_PlayerManager.GetPlayerByUsername(cmd.UserName);
            if (lastPlayer != null)
            {
                if (lastPlayer.Status == PlayerStatus.AtBattle || lastPlayer.Status == PlayerStatus.Reconnect)
                {
                    UnityEngine.Debug.Log($"is reconnect: {lastPlayer}");
                    player = lastPlayer;
                    isReconnect = true;
                }
                else
                {
                    UnityEngine.Debug.Log("is multipe login");
                    var packet = new S2C_ErrorPacket { ErrorCode = (byte)ErrorCode.HAS_LOGIN };
                    peer.Send(WriteSerializable(PacketType.S2C_ErrorOperate, packet), DeliveryMethod.ReliableOrdered);
                    return;
                }
            }
            else
            {
                player = new ServerPlayer(cmd.UserName, peer); //新建玩家对象
                m_PlayerManager.AddPlayer(player);
            }

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
            var packet2 = new Settings
            {
                ScreenSize = 0,
                FullScreen = 0,
                MusicVolume = 0,
                SoundVolume = 0,
            };
            peer.Send(WriteSerializable(PacketType.S2C_Settings, packet2), DeliveryMethod.ReliableOrdered);

            // 第三个包，重连战场
            if (isReconnect)
            {
                int serverRoomID = player.RoomId;
                ServerRoom serverRoom = m_RoomManager.GetServerRoom(serverRoomID);
                ServerPlayer p1 = serverRoom.hostPlayer as ServerPlayer;
                ServerPlayer p2 = serverRoom.guestPlayer as ServerPlayer;

                // 服务器下令开始
                var packet3 = new S2C_LoadScenePacket
                {
                    RoomId = (short)serverRoomID,
                    BattleId = serverRoom.BattleID,
                    Seed = serverRoom.Seed,
                    MapId = serverRoom.MapId,
                    BattleMode = (byte)serverRoom.BattleMode,
                    Host = new PlayerLoadPacket { UserName = p1.UserName, PeerId = p1.PeerId, RoleIndex = p1.RoleIndex },
                    Guest = new PlayerLoadPacket { UserName = p2.UserName, PeerId = p2.PeerId, RoleIndex = p2.RoleIndex },
                };
                peer.Send(WriteSerializable(PacketType.S2C_BattleReconnect, packet3), DeliveryMethod.ReliableOrdered);

                // 下发缺失帧
                var packet4 = serverRoom.ConvertInputs();
                UnityEngine.Debug.Log($"{packet4.frameNumber}/{packet4.inputs.Length}");
                peer.Send(WriteSerializable(PacketType.S2C_LackInput, packet4), DeliveryMethod.ReliableOrdered);
            }
            #endregion
        }

        private void OnLogoutReceived(NetPacketReader reader, NetPeer peer)
        {
            if (peer.Tag == null) return;
            UnityEngine.Debug.Log($"[S] OnLogoutReceived");

            // 登出，从用户表中移除
            m_PlayerManager.RemovePlayer(peer.Id);

            peer.Send(WriteSerializable(PacketType.S2C_LogoutResult, new EmptyPacket()), DeliveryMethod.ReliableOrdered);
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
                UnityEngine.Debug.Log($"match cancel, waiting count={m_WaitingPeers.Count}");
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
            int serverRoomID = player.RoomId;
            ServerRoom serverRoom = m_RoomManager.GetServerRoom(serverRoomID);
            ServerPlayer otherPlayer = serverRoom.GetOtherPlayer(player.PeerId);

            var packet = new S2C_MatchResultPacket { Code = 2, RoomId = (short)serverRoomID };
            var writer = WriteSerializable(PacketType.S2C_MatchResult, packet);
            serverRoom.Send(writer);

            lock (m_WaitingPeers)
            {
                m_WaitingPeers.Remove(player);
                m_WaitingPeers.Remove(otherPlayer);
            }
            player.ResetToLobby();
            otherPlayer.ResetToLobby();
            m_RoomManager.RemoveServerRoom(serverRoomID); //一方取消匹配解散房间
        }

        private void OnRoleSelectReceived(NetPacketReader reader, NetPeer peer)
        {
            if (peer.Tag == null) return;
            var player = (ServerPlayer)peer.Tag;

            var cmd = new C2S_RoleSelectPacket();
            cmd.Deserialize(reader);
            UnityEngine.Debug.Log($"[S]{player.UserName} select {cmd.Index}, @Room#{player.RoomId}");
            if (player.Status == PlayerStatus.AtRoomReady || player.Status == PlayerStatus.AtBattle)
            {
                UnityEngine.Debug.LogError("ready one cannot select");
                return;
            }

            int serverRoomID = player.RoomId;
            ServerRoom serverRoom = m_RoomManager.GetServerRoom(serverRoomID);
            bool playerIsHost = player.SeatId == 0;
            if (playerIsHost)
                serverRoom.hostPlayer.RoleIndex = cmd.Index;
            else
                serverRoom.guestPlayer.RoleIndex = cmd.Index;

            var packet = new S2C_RoleSelectPacket { SeatId = (byte)player.SeatId, RoleIndex = cmd.Index };
            var writer = WriteSerializable(PacketType.S2C_RoleSelect, packet);
            serverRoom.Send(writer);
        }

        private void OnGameReadyReceived(NetPacketReader reader, NetPeer peer)
        {
            if (peer.Tag == null) return;
            var player = (ServerPlayer)peer.Tag;
            player.SetStatus(PlayerStatus.AtRoomReady);
            UnityEngine.Debug.Log($"[S] {player} is Ready");

            int serverRoomID = player.RoomId;
            ServerRoom serverRoom = m_RoomManager.GetServerRoom(serverRoomID);
            ServerPlayer otherPlayer = serverRoom.GetOtherPlayer(player.PeerId);

            bool playerIsHost = player.SeatId == 0;
            ServerPlayer host = playerIsHost ? player : otherPlayer;
            ServerPlayer guest = !playerIsHost ? player : otherPlayer;

            if (host.Status == PlayerStatus.AtRoomReady && guest.Status == PlayerStatus.AtRoomWait ||
                host.Status == PlayerStatus.AtRoomWait && guest.Status == PlayerStatus.AtRoomReady)
            {
                UnityEngine.Debug.Log("111---one player is ready");

                // 一个人准备好了
                var packet = new S2C_GameReadyPacket { HostStatus = (byte)host.Status, GuestStatus = (byte)guest.Status };
                var writer = WriteSerializable(PacketType.S2C_GameReady, packet);
                serverRoom.Send(writer);
            }
            else if (player.Status == PlayerStatus.AtRoomReady && otherPlayer.Status == player.Status)
            {
                UnityEngine.Debug.Log("222---all players are ready, wait server start command");
                
                // 双方都准备好了
                var packet1 = new S2C_GameReadyPacket { HostStatus = (byte)host.Status, GuestStatus = (byte)guest.Status };
                var writer1 = WriteSerializable(PacketType.S2C_GameReady, packet1);
                serverRoom.Send(writer1);

                // 服务器下令开始
                var packet2 = new S2C_LoadScenePacket
                {
                    RoomId = (short)serverRoomID,
                    BattleId = serverRoom.BattleID,
                    Seed = serverRoom.Seed,
                    MapId = serverRoom.MapId,
                    BattleMode = (byte)serverRoom.BattleMode,
                    Host = new PlayerLoadPacket { UserName = host.UserName, PeerId = host.PeerId, RoleIndex = host.RoleIndex },
                    Guest = new PlayerLoadPacket { UserName = guest.UserName, PeerId = guest.PeerId, RoleIndex = guest.RoleIndex },
                };
                var writer2 = WriteSerializable(PacketType.S2C_LoadScene, packet2);
                serverRoom.Send(writer2);

                // 状态设置为战斗
                player.SetStatus(PlayerStatus.AtBattle);
                otherPlayer.SetStatus(PlayerStatus.AtBattle);
            }
            else
            {
                UnityEngine.Debug.LogError($"严重的错误：{host.Status} + {guest.Status}");
            }
        }

        // 第一帧同步
        private void OnBattleStartReceived(NetPacketReader reader, NetPeer peer)
        {
            if (peer.Tag == null) return;
            var player = (ServerPlayer)peer.Tag;

            var cmd = new C2S_BattleStartPacket();
            cmd.Deserialize(reader);
            UnityEngine.Debug.Log($"[S] BattleStart: {player}, Stage={cmd.Stage}");

            // 更新统计
            int serverRoomID = player.RoomId;
            ServerRoom serverRoom = m_RoomManager.GetServerRoom(serverRoomID);
            serverRoom.StageCount(cmd.Stage, player);

            // 判断阶段
            if (cmd.Stage == 0)
            {
                // 等待集齐两条，再广播。
                if (serverRoom.Stage_0_Count == 2)
                {
                    // 让客户端开始倒计时。
                    var packet = new S2C_BattleStartPacket { Stage = 0 };
                    var writer = WriteSerializable(PacketType.S2C_BattleStart, packet);
                    serverRoom.Send(writer);

                    serverRoom.DoInit();
                }
            }
            else if (cmd.Stage == 1)
            {
                // 等待集齐两条，再广播。
                if (serverRoom.Stage_1_Count == 2)
                {
                    // 此时客户端倒计时结束。服务器完成第一帧同步，同时下发。
                    var packet = new S2C_BattleStartPacket { Stage = 1 };
                    var writer = WriteSerializable(PacketType.S2C_BattleStart, packet);
                    serverRoom.Send(writer);

                    // 标记为战场，方便主循环Update中取
                    m_RoomManager.SetBattle(serverRoom);
                }
            }
            else if (cmd.Stage == 2)
            {
                var packet = new S2C_BattleStartPacket { Stage = 2 };
                var writer = WriteSerializable(PacketType.S2C_BattleStart, packet);
                serverRoom.Send(writer);
                UnityEngine.Debug.Log($"server resume battle");
            }
            else
            {
                UnityEngine.Debug.LogError("[BUG] OnBattleStart Error");
            }
        }

        private void OnBattlePauseReceived(NetPacketReader reader, NetPeer peer)
        {
            if (peer.Tag == null) return;
            var player = (ServerPlayer)peer.Tag;

            UnityEngine.Debug.Log($"<color=red>[S] {player.ToString()}@Room#{player.RoomId} command pause</color>");

            int serverRoomID = player.RoomId;
            ServerRoom serverRoom = m_RoomManager.GetServerRoom(serverRoomID);

            var writer = WriteSerializable(PacketType.S2C_BattlePause, new EmptyPacket());
            serverRoom.Send(writer);
        }

        private void OnBattleQuitReceived(NetPacketReader reader, NetPeer peer)
        {
            if (peer.Tag == null) return;
            var player = (ServerPlayer)peer.Tag;
            UnityEngine.Debug.Log($"[S] {player} quit battle");

            /*
            int serverRoomID = player.RoomId;
            ServerRoom serverRoom = m_RoomManager.GetServerRoom(serverRoomID);
            ServerPlayer otherPlayer = serverRoom.GetOtherPlayer(player.PeerId);

            // 结算比赛，返回结算结果（主动退出者判负）
            //TODO: 一方掉线后，另一方在超时时间内强退，一样判输。但是要出提示。
            short winnerSeatId = (short)(player.SeatId == 0 ? 1 : 0);
            var packet = new S2C_BattleEndPacket { WinnerSeatId = winnerSeatId };
            var writer = WriteSerializable(PacketType.S2C_BattleEnd, packet);
            serverRoom.Send(writer);

            // 解散房间（因一方认输解散）
            m_RoomManager.RemoveServerRoom(serverRoomID);

            // 用户状态变更
            //TODO: 这里会是null。一方掉线（游戏中，非正常登出离开），在超时时间内不要立即清除用户，而是设置成Offline。
            player?.ResetToLobby();
            otherPlayer?.ResetToLobby();
            UnityEngine.Debug.Log($"重置：{player?.UserName}和{otherPlayer?.UserName}");
            */
        }

        private void OnBattleEndReceived(NetPacketReader reader, NetPeer peer)
        {
            if (peer.Tag == null) return;
            var player = (ServerPlayer)peer.Tag;

            // 解析客户端消息
            var cmd = new C2S_BattleEndPacket();
            cmd.Deserialize(reader);
            UnityEngine.Debug.Log($"[S] {player}battle end, host:{cmd.HostHP} vs guest:{cmd.GuestHP}, time left:{cmd.TimeLeft}s");

            int serverRoomID = player.RoomId;
            ServerRoom serverRoom = m_RoomManager.GetServerRoom(serverRoomID);
            ServerPlayer otherPlayer = serverRoom.GetOtherPlayer(player.PeerId);
            serverRoom.EndCount++;

            // 给上报者回包
            short winnerSeatId = (short)BaseRoom.CheckWinnerSeatId(cmd.HostHP, cmd.GuestHP);
            var packet = new S2C_BattleEndPacket { WinnerSeatId = winnerSeatId };
            peer.Send(WriteSerializable(PacketType.S2C_BattleEnd, packet), DeliveryMethod.ReliableOrdered);

            // 收到双方消息，关闭房间
            if (otherPlayer.Status == PlayerStatus.AtBattle)
            {
                // 要等两条消息
                if (serverRoom.EndCount == 2)
                {
                    UnityEngine.Debug.Log($"Remove Room.1(normal) #{serverRoomID}");
                    serverRoom.Dump();
                    m_RoomManager.RemoveServerRoom(serverRoomID); //双方上报结果解散房间
                    player.ResetToLobby();
                    otherPlayer.ResetToLobby();
                }
            }
            else
            {
                // 一条消息即可。通知另一个人不要重连了。
                otherPlayer.AssociatedPeer.Send(WriteSerializable(PacketType.S2C_BattleEnd, packet), DeliveryMethod.ReliableOrdered);

                UnityEngine.Debug.Log($"Remove Room.2(one drop net) #{serverRoomID}");
                m_RoomManager.RemoveServerRoom(serverRoomID); //一方掉线，一方上报结果解散房间
                player.ResetToLobby();
                otherPlayer.ResetToLobby();
            }
        }
        #endregion


        #region Server Commands
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
                ServerRoom serverRoom = m_RoomManager.CreateServerRoom(p1, p2);
                int serverRoomID = serverRoom.RoomID;
                UnityEngine.Debug.Log($"match success, put {p1.PeerId}, {p2.PeerId} into Room#{serverRoomID}");
                p1.SetRoomID(serverRoomID).SetSeatID(0).SetStatus(PlayerStatus.AtRoomWait);
                p2.SetRoomID(serverRoomID).SetSeatID(1).SetStatus(PlayerStatus.AtRoomWait);
                UserInfo hostPlayer = new UserInfo { PeerId = p1.PeerId, UserName = p1.UserName };
                UserInfo guestPlayer = new UserInfo { PeerId = p2.PeerId, UserName = p2.UserName };
                var packet = new S2C_MatchResultPacket { Code = 0, RoomId = (short)serverRoomID, Host = hostPlayer, Guest = guestPlayer };
                var writer = WriteSerializable(PacketType.S2C_MatchResult, packet);
                serverRoom.Send(writer);

                lock (m_WaitingPeers)
                {
                    m_WaitingPeers.Remove(p1);
                    m_WaitingPeers.Remove(p2);
                    UnityEngine.Debug.Log($"send ok, waiting count={m_WaitingPeers.Count}");
                }

                string timeStr = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
                int randSeed = System.Guid.NewGuid().GetHashCode();
                serverRoom.BattleID = $"{timeStr}_{hostPlayer.PeerId}_{guestPlayer.PeerId}";
                serverRoom.Seed = randSeed;
                serverRoom.MapId = 0; //来自客户端
                serverRoom.BattleMode = BattleMode.Matching;
            }
        }

        // 房间内广播
        public void BroadcastToRoom(int roomId, NetDataWriter writer)
        {
            ServerRoom serverRoom = m_RoomManager.GetServerRoom(roomId);
            var players = serverRoom.m_PlayerList; //直接从内存取
            for (int i = 0; i < players.Length; i++)
            {
                short peedId = players[i].PeerId;
                _netManager.GetPeerById(peedId).Send(writer, DeliveryMethod.ReliableOrdered);
            }
        }
        #endregion
    }
}