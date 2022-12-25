using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Code.Shared;
using LiteNetLib;
using LiteNetLib.Utils;
#if UNITY_SERVER || UNITY_EDITOR
using DatabaseEssential;
#endif

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
        //void Start()
        //{
        //    StartServer();
        //}
        public async Task StartProgram()
        {
#if UNITY_SERVER || UNITY_EDITOR
            UnityEngine.Debug.Log("StartProgram-----------------------UNITY_SERVER || UNITY_EDITOR-----------------------");
            UnityEngine.Debug.Log("StartProgram-----------------------UNITY_SERVER || UNITY_EDITOR-----------------------");
            UnityEngine.Debug.Log("StartProgram-----------------------UNITY_SERVER || UNITY_EDITOR-----------------------");
#elif UNITY_PLAYER
            UnityEngine.Debug.Log("StartProgram-----------------------UNITY_PLAYER-----------------------");
            UnityEngine.Debug.Log("StartProgram-----------------------UNITY_PLAYER-----------------------");
            UnityEngine.Debug.Log("StartProgram-----------------------UNITY_PLAYER-----------------------");
#else
            UnityEngine.Debug.Log("StartProgram-----------------------NOTHING-----------------------");
            UnityEngine.Debug.Log("StartProgram-----------------------NOTHING-----------------------");
            UnityEngine.Debug.Log("StartProgram-----------------------NOTHING-----------------------");
#endif
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
            UnityEngine.Debug.Log($"StartServer, listen on {Port}");

            //UnityEngine.Application.targetFrameRate = 64; //0.02, 默认1E-05

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

            UpdateRoom();
        }
        protected void UpdateRoom()
        {
            var rooms = m_RoomManager.GetBattles();
            foreach (var bt in rooms)
            {
                bt.Value.DoUpdate();
            }
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
            //UnityEngine.Debug.Log("[S] Player connected: " + peer.EndPoint);
        }

        void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            ServerPlayer player = (ServerPlayer)peer.Tag;
            if (player == null) return;
            UnityEngine.Debug.Log($"[S] {player} disconnected: {disconnectInfo.Reason}");

            int serverRoomID = player.RoomId;
            ServerRoom serverRoom = m_RoomManager.GetServerRoom(serverRoomID);

            switch (player.Status)
            {
                case PlayerStatus.Matching: //匹配中
                    {
                        lock (m_WaitingPeers)
                        {
                            m_WaitingPeers.Remove(player);
                        }
                        m_PlayerManager.RemovePlayer(peer.Id);
                    }
                    break;
                case PlayerStatus.AtRoomWait: //房间里
                case PlayerStatus.AtRoomReady:
                    {
                        ServerPlayer otherPlayer = serverRoom.GetOtherPlayer(player.SeatId); //BOT is null
                        if (otherPlayer != null)
                        {
                            var packet = new S2C_MatchResultPacket { Code = 2 }; //解散房间，另一人退至大厅
                            otherPlayer.AssociatedPeer.Send(WriteSerializable(PacketType.S2C_MatchResult, packet), DeliveryMethod.ReliableOrdered);
                        }
                        m_RoomManager.RemoveServerRoom(serverRoomID);
                        m_PlayerManager.RemovePlayer(peer.Id);
                        otherPlayer.ResetToLobby();
                    }
                    break;
                case PlayerStatus.AtBattle: //战斗中
                    {
                        ServerPlayer otherPlayer = serverRoom.GetOtherPlayer(player.SeatId); //BOT is null

                        // 有重连规则的比赛中
                        if (serverRoom.BattleMode == BattleMode.Matching)
                        {
                            // 方案①掉线暂停
                            if (otherPlayer.Status == PlayerStatus.AtBattle)
                            {
                                // 一方断线，保留房间，通知另一方等待。
                                switch (disconnectInfo.Reason)
                                {
                                    case DisconnectReason.Timeout: //关闭网络，超时
                                    case DisconnectReason.RemoteConnectionClose: //杀进程，远程主动关闭
                                        var packet = new S2C_BattlePausePacket { SeatID = (byte)player.SeatId, Duration = 60 };
                                        otherPlayer.AssociatedPeer.Send(WriteSerializable(PacketType.S2C_BattleLostNet, packet), DeliveryMethod.ReliableOrdered);
                                        player.SetStatus(PlayerStatus.Reconnect); //把离线者标记未断线重连
                                        //serverRoom.CutDown(); //掉线倒计时
                                        break;
                                    default:
                                        UnityEngine.Debug.Log($"disconnect by: {disconnectInfo.Reason}");
                                        break;
                                }
                            }
                            else
                            {
                                // 双方都断线
                                m_RoomManager.RemoveServerRoom(serverRoomID); //解散房间
                                m_PlayerManager.RemovePlayer(peer.Id); //移除用户
                                m_PlayerManager.RemovePlayer(otherPlayer.AssociatedPeer.Id);
                            }

                            // 方案②掉线直接结算
                            //var packet = new S2C_BattleEndPacket { WinnerSeatId = otherPlayer.SeatId };
                            //otherPlayer.AssociatedPeer.Send(WriteSerializable(PacketType.S2C_BattleEnd, packet), DeliveryMethod.ReliableOrdered);
                            //otherPlayer.ResetToLobby();
                        }
                        else
                        {
                            m_RoomManager.RemoveServerRoom(serverRoomID); //解散房间
                            m_PlayerManager.RemovePlayer(peer.Id);
                            UnityEngine.Debug.Log("没有重连的比赛，移除用户");
                        }
                    }
                    break;
                default: //大厅等
                    m_PlayerManager.RemovePlayer(peer.Id);
                    break;
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
                case PacketType.C2S_Input:
                    OnInputReceived(reader, peer);
                    break;
                //以上是测试，保留
                case PacketType.C2S_RegisterReq:
                    //OnRegisterReceived(reader, peer);
                    break;
                case PacketType.C2S_UserInfo:
                    //OnGetUserInfoReceived(reader, peer);
                    break;
                case PacketType.C2S_LoginReq:
                    OnLoginReceived(reader, peer);
                    break;
                case PacketType.C2S_LoginByToken:
                    OnLoginByTokenReceived(reader, peer);
                    break;
                case PacketType.C2S_LogoutReq:
                    OnLogoutReceived(reader, peer);
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
                case PacketType.C2S_BattleInputs:
                    OnLackInputReceived(reader, peer);
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

        private void OnLoginReceived(NetPacketReader reader, NetPeer peer)
        {
            var cmd = new C2S_LoginPacket();
            cmd.Deserialize(reader);
            UnityEngine.Debug.Log($"[S] Login packet received: [{peer.Id}]{cmd.UserName},{cmd.Password}");

            string userName = string.Empty;
            byte _screensize = 0;
            byte _fullscreen = 0;
            byte _audio = 0;
            byte _sound = 0;
            byte _language = 1;
            bool isReconnect = false;

            #region 验证逻辑
#if UNITY_SERVER || UNITY_EDITOR
            string query = $"SELECT Count(*) FROM tb_user WHERE username='{cmd.UserName}' AND password='{cmd.Password}'";
            int check1 = DatabaseManager.Count(DatabaseManager.db_user, query);
            //UnityEngine.Debug.Log($"check username & password: {check1}");
            if (check1 <= 0)
            {
                UnityEngine.Debug.LogError("username or password is incorrect");
                var packet = new S2C_LoginResultPacket { Code = 1 };
                peer.Send(WriteSerializable(PacketType.S2C_LoginResult, packet), DeliveryMethod.ReliableOrdered);
                return;
            }

            string columnName = "username,screensize,fullscreen,audio,sound,language";
            List<string>[] results = DatabaseManager.SelectAllRecord(DatabaseManager.db_moefight, $"tb_settings WHERE username='{cmd.UserName}'", columnName); //固定长度4
            List<string> _screensizeList = results[1];
            List<string> _fullscreenList = results[2];
            List<string> _audioList = results[3];
            List<string> _soundList = results[4];
            List<string> _languageList = results[5];
            _screensize = (_screensizeList.Count == 0 || string.IsNullOrEmpty(_screensizeList[0])) ? (byte)0 : (byte)int.Parse(_screensizeList[0]);
            _fullscreen = (_fullscreenList.Count == 0 || string.IsNullOrEmpty(_fullscreenList[0])) ? (byte)0 : (byte)int.Parse(_fullscreenList[0]);
            _audio = (_audioList.Count == 0 || string.IsNullOrEmpty(_audioList[0])) ? (byte)0 : (byte)int.Parse(_audioList[0]);
            _sound = (_soundList.Count == 0 || string.IsNullOrEmpty(_soundList[0])) ? (byte)0 : (byte)int.Parse(_soundList[0]);
            _language = (_languageList.Count == 0 || string.IsNullOrEmpty(_languageList[0])) ? (byte)1 : (byte)int.Parse(_languageList[0]);
#endif
            #endregion

            #region 登录逻辑
            ServerPlayer player = null;
            // 校验重复登录或重连，m_PlayerManager中已有该玩家
            ServerPlayer lastPlayer = m_PlayerManager.GetPlayerByUsername(cmd.UserName);
            if (lastPlayer != null)
            {
                if (lastPlayer.Status == PlayerStatus.Reconnect)
                {
                    UnityEngine.Debug.Log($"重连登录: Peer:{lastPlayer.PeerId},UserName:{lastPlayer.UserName}");
                    isReconnect = true;
                    m_PlayerManager.RemovePlayer(lastPlayer.PeerId);
                    //player = lastPlayer;
                    //peer.Tag = lastPlayer;
                    player = new ServerPlayer(cmd.UserName, peer); //新建玩家对象
                    m_PlayerManager.AddPlayer(player);
                    player.CopyFrom(lastPlayer); //拷贝玩家信息
                }
                else
                {
                    UnityEngine.Debug.Log("重复登录");
                    var packet = new S2C_ErrorPacket { ErrorCode = (byte)ErrorCode.HAS_LOGIN };
                    peer.Send(WriteSerializable(PacketType.S2C_ErrorOperate, packet), DeliveryMethod.ReliableOrdered);
                    return;
                }
            }
            else
            {
                player = new ServerPlayer(cmd.UserName, peer);
                m_PlayerManager.AddPlayer(player);
                player.ResetToLobby();
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
                ScreenSize = _screensize,
                FullScreen = _fullscreen,
                MusicVolume = _audio,
                SoundVolume = _sound,
                Language = _language,
            };
            peer.Send(WriteSerializable(PacketType.S2C_Settings, packet2), DeliveryMethod.ReliableOrdered);
            UnityEngine.Debug.Log($"settings.music:{packet2.MusicVolume}, sound:{packet2.SoundVolume}, lang:{packet2.Language}");

            // 第三个包，重连数据
            if (isReconnect)
            {
                int serverRoomID = player.RoomId;
                ServerRoom serverRoom = m_RoomManager.GetServerRoom(serverRoomID);
                if (serverRoom == null)
                {
                    UnityEngine.Debug.LogError($"room not exist: {serverRoomID}");
                    return;
                }

                // 之前的Peer连接失效，更新房间内保存的用户对象
                if (player.SeatId == 0)
                    serverRoom.hostPlayer = player;
                else
                    serverRoom.guestPlayer = player;

                ServerPlayer p1 = serverRoom.hostPlayer;
                ServerPlayer p2 = serverRoom.guestPlayer;

                // 下发房间信息，弹出是否重连
                // 是，下发所有帧
                // 否，结算比赛，关闭弹窗
                var packet3 = new S2C_LoadScenePacket
                {
                    RoomId = (short)serverRoomID,
                    BattleId = serverRoom.BattleID,
                    MapId = serverRoom.MapId,
                    Host = new PlayerLoadPacket { UserName = p1.UserName, PeerId = p1.PeerId, RoleIndex = p1.RoleIndex },
                    Guest = new PlayerLoadPacket { UserName = p2.UserName, PeerId = p2.PeerId, RoleIndex = p2.RoleIndex },
                };
                peer.Send(WriteSerializable(PacketType.S2C_BattleReconnect, packet3), DeliveryMethod.ReliableOrdered);
                UnityEngine.Debug.Log($"<color=yellow>{player.UserName} is lostnet to reconnect</color>");
            }
            /*
            //模拟超大消息包收发（最多60*100=6000个，72KB）
            S2C_InputPacket[] array = new S2C_InputPacket[5001]; //多一个废帧[0]
            for (int i = 0; i < array.Length; i++)
            {
                if (i == 0)
                {
                    array[0] = new S2C_InputPacket();
                }
                else
                {
                    uint tick = (uint)i;
                    var input = new Dictionary<int, uint>();
                    input[0] = 0;
                    input[1] = 1;

                    uint[] _inputs = new uint[2] { input[0], input[1] };
                    array[i] = new S2C_InputPacket { frameNumber = tick, inputs = _inputs };
                }
            }
            var packet4 = new S2C_LackInputPacket
            {
                frameNumber = 5000,
                inputs = array,
            };
            peer.Send(WriteSerializable(PacketType.S2C_BattleInputs, packet4), DeliveryMethod.ReliableOrdered);
            */
            #endregion
        }

        private void OnLoginByTokenReceived(NetPacketReader reader, NetPeer peer)
        {
            var cmd = new C2S_LoginByTokenPacket();
            cmd.Deserialize(reader);
            UnityEngine.Debug.Log($"[S] Login packet received: [{peer.Id}]{cmd.Token}");

            string userName = string.Empty;
            byte _screensize = 0;
            byte _fullscreen = 0;
            byte _audio = 0;
            byte _sound = 0;
            byte _language = 1;
            bool isReconnect = false;

            #region 验证逻辑
#if UNITY_SERVER || UNITY_EDITOR
            string query = $"SELECT Count(*) FROM tb_user WHERE token='{cmd.Token}'";
            int check1 = DatabaseManager.Count(DatabaseManager.db_user, query);
            //UnityEngine.Debug.Log($"check username & password: {check1}");
            if (check1 <= 0)
            {
                UnityEngine.Debug.LogError("token not exist");
                var packet = new S2C_LoginResultPacket { Code = 1 };
                peer.Send(WriteSerializable(PacketType.S2C_LoginResult, packet), DeliveryMethod.ReliableOrdered);
                return;
            }

            string columnName = "username,screensize,fullscreen,audio,sound,language";
            List<string>[] results = DatabaseManager.SelectAllRecord(DatabaseManager.db_moefight, $"tb_settings WHERE token='{cmd.Token}'", columnName); //固定长度4
            List<string> _userList = results[0];
            List<string> _screensizeList = results[1];
            List<string> _fullscreenList = results[2];
            List<string> _audioList = results[3];
            List<string> _soundList = results[4];
            List<string> _languageList = results[5];
            userName = (_userList.Count == 0 || string.IsNullOrEmpty(_userList[0])) ? string.Empty : _userList[0].ToString();
            _screensize = (_screensizeList.Count == 0 || string.IsNullOrEmpty(_screensizeList[0])) ? (byte)0 : (byte)int.Parse(_screensizeList[0]);
            _fullscreen = (_fullscreenList.Count == 0 || string.IsNullOrEmpty(_fullscreenList[0])) ? (byte)0 : (byte)int.Parse(_fullscreenList[0]);
            _audio = (_audioList.Count == 0 || string.IsNullOrEmpty(_audioList[0])) ? (byte)0 : (byte)int.Parse(_audioList[0]);
            _sound = (_soundList.Count == 0 || string.IsNullOrEmpty(_soundList[0])) ? (byte)0 : (byte)int.Parse(_soundList[0]);
            _language = (_languageList.Count == 0 || string.IsNullOrEmpty(_languageList[0])) ? (byte)1 : (byte)int.Parse(_languageList[0]);
#endif
            #endregion

            #region 登录逻辑
            ServerPlayer player = null;
            // 校验重复登录或重连，m_PlayerManager中已有该玩家
            ServerPlayer lastPlayer = m_PlayerManager.GetPlayerByUsername(userName);
            if (lastPlayer != null)
            {
                if (lastPlayer.Status == PlayerStatus.Reconnect)
                {
                    UnityEngine.Debug.Log($"重连登录: Peer:{lastPlayer.PeerId},UserName:{lastPlayer.UserName}");
                    isReconnect = true;
                    m_PlayerManager.RemovePlayer(lastPlayer.PeerId);
                    //player = lastPlayer;
                    //peer.Tag = lastPlayer;
                    player = new ServerPlayer(userName, peer); //新建玩家对象
                    m_PlayerManager.AddPlayer(player);
                    player.CopyFrom(lastPlayer); //拷贝玩家信息
                }
                else
                {
                    UnityEngine.Debug.Log("重复登录");
                    var packet = new S2C_ErrorPacket { ErrorCode = (byte)ErrorCode.HAS_LOGIN };
                    peer.Send(WriteSerializable(PacketType.S2C_ErrorOperate, packet), DeliveryMethod.ReliableOrdered);
                    return;
                }
            }
            else
            {
                player = new ServerPlayer(userName, peer);
                m_PlayerManager.AddPlayer(player);
                player.ResetToLobby();
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
                ScreenSize = _screensize,
                FullScreen = _fullscreen,
                MusicVolume = _audio,
                SoundVolume = _sound,
                Language = _language,
            };
            peer.Send(WriteSerializable(PacketType.S2C_Settings, packet2), DeliveryMethod.ReliableOrdered);
            UnityEngine.Debug.Log($"settings.music:{packet2.MusicVolume}, sound:{packet2.SoundVolume}, lang:{packet2.Language}");

            // 第三个包，重连数据
            if (isReconnect)
            {
                int serverRoomID = player.RoomId;
                ServerRoom serverRoom = m_RoomManager.GetServerRoom(serverRoomID);
                if (serverRoom == null)
                {
                    UnityEngine.Debug.LogError($"room not exist: {serverRoomID}");
                    return;
                }

                // 之前的Peer连接失效，更新房间内保存的用户对象
                if (player.SeatId == 0)
                    serverRoom.hostPlayer = player;
                else
                    serverRoom.guestPlayer = player;

                ServerPlayer p1 = serverRoom.hostPlayer;
                ServerPlayer p2 = serverRoom.guestPlayer;

                // 下发房间信息，弹出是否重连
                // 是，下发所有帧
                // 否，结算比赛，关闭弹窗
                var packet3 = new S2C_LoadScenePacket
                {
                    RoomId = (short)serverRoomID,
                    BattleId = serverRoom.BattleID,
                    MapId = serverRoom.MapId,
                    Host = new PlayerLoadPacket { UserName = p1.UserName, PeerId = p1.PeerId, RoleIndex = p1.RoleIndex },
                    Guest = new PlayerLoadPacket { UserName = p2.UserName, PeerId = p2.PeerId, RoleIndex = p2.RoleIndex },
                };
                peer.Send(WriteSerializable(PacketType.S2C_BattleReconnect, packet3), DeliveryMethod.ReliableOrdered);
                UnityEngine.Debug.Log($"<color=yellow>{player.UserName} is lostnet to reconnect</color>");
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
            UnityEngine.Debug.Log($"[S] OnSettingsReceived: {player.UserName}: {cmd.MusicVolume}, {cmd.SoundVolume}, {cmd.Language}");

#if UNITY_SERVER || UNITY_EDITOR
            string tableName = "tb_settings";
            string query = $"SELECT Count(*) FROM {tableName} WHERE username='{player.UserName}'";
            int check1 = DatabaseManager.Count(DatabaseManager.db_moefight, query);
            //UnityEngine.Debug.Log($"check1={check1}");
            if (check1 == 0)
            {
                //①如果没有，创建
                DatabaseManager.InsertRecord(DatabaseManager.db_moefight, tableName, "username,audio,sound,language", $"'{player.UserName}', '{cmd.MusicVolume}', '{cmd.SoundVolume}', '{cmd.Language}'");
                UnityEngine.Debug.Log($"insert sql:");
            }
            else
            {
                //②如果有，更新
                DatabaseManager.UpdateRecord(DatabaseManager.db_moefight, tableName, $"audio='{cmd.MusicVolume}',sound='{cmd.SoundVolume}',language='{cmd.Language}' WHERE Username='{player.UserName}'");
                UnityEngine.Debug.Log($"update sql:");
            }
#endif
            peer.Send(WriteSerializable(PacketType.S2C_Settings, cmd), DeliveryMethod.ReliableOrdered);
        }

        // 请求匹配
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

        // 取消匹配
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

        // 匹配成功后离开
        private void OnMatchQuitReceived(NetPacketReader reader, NetPeer peer)
        {
            if (peer.Tag == null) return;
            var player = (ServerPlayer)peer.Tag;
            UnityEngine.Debug.Log($"[S] OnMatchQuitReceived: [{peer.Id}]{player.UserName}@Room#{player.RoomId}@Seat#{player.SeatId}");

            // 通知房间内的另一个人，并移除列表。
            int serverRoomID = player.RoomId;
            if (serverRoomID <= 0)
            {
                UnityEngine.Debug.LogError($"err: room #{serverRoomID} not exist");
                return;
            }
            ServerRoom serverRoom = m_RoomManager.GetServerRoom(serverRoomID);
            ServerPlayer otherPlayer = serverRoom.GetOtherPlayer(player.SeatId);

            var packet = new S2C_MatchResultPacket { Code = 2, RoomId = (short)serverRoomID };
            var writer = WriteSerializable(PacketType.S2C_MatchResult, packet);
            serverRoom.Send(writer);

            //lock (m_WaitingPeers)
            //{
            //    m_WaitingPeers.Remove(player);
            //    m_WaitingPeers.Remove(otherPlayer);
            //}
            player.ResetToLobby();
            otherPlayer.ResetToLobby();
            m_RoomManager.RemoveServerRoom(serverRoomID); //一方取消匹配解散房间
            UnityEngine.Debug.Log($"room#{serverRoomID} is dissoluted");
        }

        // 选择角色
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

        // 准备开局
        private void OnGameReadyReceived(NetPacketReader reader, NetPeer peer)
        {
            if (peer.Tag == null) return;
            var player = (ServerPlayer)peer.Tag;
            player.SetStatus(PlayerStatus.AtRoomReady);
            UnityEngine.Debug.Log($"[S] {player} is Ready");

            int serverRoomID = player.RoomId;
            ServerRoom serverRoom = m_RoomManager.GetServerRoom(serverRoomID);
            ServerPlayer otherPlayer = serverRoom.GetOtherPlayer(player.SeatId);

            bool playerIsHost = player.SeatId == 0;
            ServerPlayer host = playerIsHost ? player : otherPlayer;
            ServerPlayer guest = !playerIsHost ? player : otherPlayer;

            if (host.Status == PlayerStatus.AtRoomReady && guest.Status == PlayerStatus.AtRoomWait ||
                host.Status == PlayerStatus.AtRoomWait && guest.Status == PlayerStatus.AtRoomReady)
            {
                UnityEngine.Debug.Log("[1]one player is ready");

                // 一个人准备好了
                var packet = new S2C_GameReadyPacket { HostStatus = (byte)host.Status, GuestStatus = (byte)guest.Status };
                var writer = WriteSerializable(PacketType.S2C_GameReady, packet);
                serverRoom.Send(writer);
            }
            else if (player.Status == PlayerStatus.AtRoomReady && otherPlayer.Status == player.Status)
            {
                UnityEngine.Debug.Log($"[2]all players are ready, {host.RoleIndex} vs {guest.RoleIndex}, wait server start command");
                
                // 双方都准备好了
                var packet1 = new S2C_GameReadyPacket { HostStatus = (byte)host.Status, GuestStatus = (byte)guest.Status };
                var writer1 = WriteSerializable(PacketType.S2C_GameReady, packet1);
                serverRoom.Send(writer1);

                // 服务器下令开始
                var packet2 = new S2C_LoadScenePacket
                {
                    RoomId = (short)serverRoomID,
                    BattleId = serverRoom.BattleID,
                    MapId = serverRoom.MapId,
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

        // 比赛开始（①场景加载完第一帧同步/②暂停后恢复比赛）
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
            if (cmd.Stage == 0) //场景加载完（同步）
            {
                // 让客户端开始倒计时
                if (serverRoom.Stage_0_Count == 2)
                {
                    // 让客户端开始倒计时。
                    var packet = new S2C_BattleStartPacket { Stage = 0 };
                    var writer = WriteSerializable(PacketType.S2C_BattleStart, packet);
                    serverRoom.Send(writer);

                    serverRoom.DoInit();
                }
            }
            else if (cmd.Stage == 1) //3,2,1,倒计时完（同步）
            {
                if (serverRoom.Stage_1_Count == 2)
                {
                    // 此时客户端倒计时结束。服务器完成第一帧同步，同时下发。
                    var packet = new S2C_BattleStartPacket { Stage = 1 };
                    var writer = WriteSerializable(PacketType.S2C_BattleStart, packet);
                    serverRoom.Send(writer);

                    // 标记为战场，方便主循环Update中取
                    m_RoomManager.SetBattle(serverRoom);
                    serverRoom.SetStage(BattleStage.Running);
                }
            }
            else if (cmd.Stage == 2) //比赛恢复（只需收到一方）
            {
                var packet = new S2C_BattleStartPacket { Stage = 2 }; //从暂停恢复
                var writer = WriteSerializable(PacketType.S2C_BattleStart, packet);
                serverRoom.Send(writer);
                UnityEngine.Debug.Log($"server resume battle");
                serverRoom.SetStage(BattleStage.Running);
            }
            else
            {
                UnityEngine.Debug.LogError("[BUG] OnBattleStart Error");
            }
        }

        // 客户端请求暂停
        private void OnBattlePauseReceived(NetPacketReader reader, NetPeer peer)
        {
            if (peer.Tag == null) return;
            var player = (ServerPlayer)peer.Tag;

            UnityEngine.Debug.Log($"<color=red>[S] {player.ToString()}@Room#{player.RoomId} command pause</color>");

            int serverRoomID = player.RoomId;
            ServerRoom serverRoom = m_RoomManager.GetServerRoom(serverRoomID);
            if (serverRoom == null || serverRoom.BattleStage == BattleStage.End)
            {
                return; //服务器自身接口保护
            }
            int chanceLeft = serverRoom.PauseChance[player.SeatId]; //剩余次数
            if (chanceLeft <= 0)
            {
                var packet0 = new S2C_BattlePausePacket { SeatID = (byte)player.SeatId, Duration = 0 }; //暂停次数用尽
                var err = WriteSerializable(PacketType.S2C_BattlePause, packet0);
                peer.Send(err, DeliveryMethod.ReliableOrdered);
                return;
            }
            serverRoom.PauseChance[player.SeatId]--;
            serverRoom.SetStage(BattleStage.Paused);

            var packet1 = new S2C_BattlePausePacket { SeatID = (byte)player.SeatId, Duration = 30 };
            var writer = WriteSerializable(PacketType.S2C_BattlePause, packet1);
            serverRoom.Send(writer);
        }

        // 主动认输（①比赛中/②重连后）
        private void OnBattleQuitReceived(NetPacketReader reader, NetPeer peer)
        {
            if (peer.Tag == null) return;
            var player = (ServerPlayer)peer.Tag;
            UnityEngine.Debug.Log($"[S] {player.UserName},{player.RoomId},{player.SeatId} quit battle");

            int serverRoomID = player.RoomId;
            //UnityEngine.Debug.Log($"AAA: {serverRoomID}"); //1
            ServerRoom serverRoom = m_RoomManager.GetServerRoom(serverRoomID);
            //UnityEngine.Debug.Log($"BBB: {serverRoom != null}"); //true
            ServerPlayer otherPlayer = serverRoom.GetOtherPlayer(player.SeatId);
            //UnityEngine.Debug.Log($"CCC: {otherPlayer != null}"); //false

            // 结算比赛，返回结算结果（主动退出者判负）
            // 一方掉线后，另一方在超时时间内强退，一样判输。
            var packet = new S2C_BattleEndPacket { WinnerSeatId = otherPlayer.SeatId };
            var writer = WriteSerializable(PacketType.S2C_BattleEnd, packet);
            serverRoom.Send(writer);

            UnityEngine.Debug.Log($"Other: {otherPlayer.Status}, {otherPlayer.AssociatedPeer.ConnectionState}"); //false
            //Reconnect, Disconnected
            if (otherPlayer.Status == PlayerStatus.Reconnect)
            {
                if (otherPlayer.AssociatedPeer.ConnectionState != ConnectionState.Connected)
                {
                    //therPlayer.SetStatus(PlayerStatus.Offline);
                    m_PlayerManager.RemovePlayer(otherPlayer.PeerId);
                }
                else
                {
                    otherPlayer.SetStatus(PlayerStatus.AtLobby);
                }
            }

            // 解散房间（因一方认输解散）
            m_RoomManager.RemoveServerRoom(serverRoomID);

            // 用户状态变更
            player?.ResetToLobby();
            otherPlayer?.ResetToLobby();
        }

        // 比赛结束结算（①认输/②战死/③时间到）
        private void OnBattleEndReceived(NetPacketReader reader, NetPeer peer)
        {
            if (peer.Tag == null) return;
            var player = (ServerPlayer)peer.Tag;

            // 解析客户端消息
            var cmd = new C2S_BattleEndPacket();
            cmd.Deserialize(reader);
            UnityEngine.Debug.Log($"[S] {player}battle end, Winner:{cmd.Winner}");

            int serverRoomID = player.RoomId;
            ServerRoom serverRoom = m_RoomManager.GetServerRoom(serverRoomID);
            ServerPlayer otherPlayer = serverRoom.GetOtherPlayer(player.SeatId);
            serverRoom.EndCount++;
            serverRoom.SetStage(BattleStage.End);

            // 给上报者回包
            var packet = new S2C_BattleEndPacket { WinnerSeatId = cmd.Winner };
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

        // 请求缺失帧
        private void OnLackInputReceived(NetPacketReader reader, NetPeer peer)
        {
            if (peer.Tag == null) return;
            var player = (ServerPlayer)peer.Tag;

            var cmd = new C2S_LackInputPacket();
            cmd.Deserialize(reader);
            //UnityEngine.Debug.Log($"[S] LackInput received: {cmd.startTick}~{cmd.endTick}");

            int serverRoomID = player.RoomId;
            ServerRoom serverRoom = m_RoomManager.GetServerRoom(serverRoomID);

            // 下发缺失帧
            //UnityEngine.Debug.Log($"Before: bufferTick:{serverRoom.bufferTick}, serverTick:{serverRoom.serverTick}");
            var packet = serverRoom.ConvertInputs();
            //UnityEngine.Debug.Log($"After: bufferTick:{serverRoom.bufferTick}, serverTick:{serverRoom.serverTick}");
            peer.Send(WriteSerializable(PacketType.S2C_BattleInputs, packet), DeliveryMethod.ReliableOrdered);
            UnityEngine.Debug.Log($"pack ticks: 1-{packet.frameNumber}");
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
                UserInfo hostPlayer = new UserInfo { PeerId = p1.PeerId, UserName = p1.UserName };
                UserInfo guestPlayer = new UserInfo { PeerId = p2.PeerId, UserName = p2.UserName };
                var packet = new S2C_MatchResultPacket { Code = 0, BattleMode = (byte)BattleMode.Matching, RoomId = (short)serverRoomID, Host = hostPlayer, Guest = guestPlayer };
                var writer = WriteSerializable(PacketType.S2C_MatchResult, packet);
                serverRoom.Send(writer);

                //lock (m_WaitingPeers)
                //{
                m_WaitingPeers.Remove(p1);
                m_WaitingPeers.Remove(p2);
                UnityEngine.Debug.Log($"send ok, waiting count={m_WaitingPeers.Count}");
                //}

                string timeStr = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
                serverRoom.BattleID = $"{timeStr}_{hostPlayer.PeerId}_{guestPlayer.PeerId}";
                serverRoom.MapId = 0; //来自客户端
                serverRoom.BattleMode = BattleMode.Matching;
            }
        }
        #endregion
    }
}