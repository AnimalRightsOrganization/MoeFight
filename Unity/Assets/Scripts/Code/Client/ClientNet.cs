using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;
using LiteNetLib;
using LiteNetLib.Utils;
using Code.Shared;
using HotFix;

namespace Code.Client
{
    public class ClientNet : MonoBehaviour, INetEventListener
    {
        static ClientNet _instance;
        public static ClientNet Get
        {
            get
            {
                if (_instance == null)
                    _instance = FindObjectOfType<ClientNet>();
                return _instance;
            }
        }

        private NetPeer _server;
        private NetManager _netManager;
        private NetDataWriter _writer;

        private Action<DisconnectInfo> _onDisconnected;
        public Action _onConnected;
        public ClientRoom m_ClientRoom;
        public ClientPlayerManager m_PlayerManager;
        public int _ping;

        [Range(0, 100)]
        public int dropRate; //丢包率，编辑器测试用


        #region Inner Method
        void Awake()
        {
            DontDestroyOnLoad(gameObject);

            _writer = new NetDataWriter();
            m_PlayerManager = new ClientPlayerManager();
            _netManager = new NetManager(this)
            {
                AutoRecycle = true,
                IPv6Mode = IPv6Mode.Disabled
            };
            _netManager.Start();
        }

        void Update()
        {
            _netManager?.PollEvents();
        }

        void OnDestroy()
        {
            _netManager.Stop();
        }
        #endregion


        #region Interface
        public void SendPacketSerializable<T>(PacketType type, T packet) where T : INetSerializable
        {
            if (_server == null)
                return;
            _writer.Reset();
            _writer.Put((byte)type);
            packet.Serialize(_writer);
            _server.Send(_writer, DeliveryMethod.ReliableOrdered);
        }

        void INetEventListener.OnPeerConnected(NetPeer peer)
        {
            Debug.Log($"<color=green>[C] Connected to server: {peer.EndPoint}</color>");
            _server = peer;

            _onConnected?.Invoke();
            _onConnected = null;

            SendLoginByToken();
        }

        void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Debug.Log("[C] Disconnected from server: " + disconnectInfo.Reason);
            m_PlayerManager.RemoveAll();
            _server = null;

            _onDisconnected?.Invoke(disconnectInfo);
            _onDisconnected = null;
            //Debug.Log("清空委托");
        }

        void INetEventListener.OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            Debug.Log("[C] NetworkError: " + socketError);
        }

        void INetEventListener.OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
        {
            byte packetType = reader.GetByte();
            if (packetType >= Enum.GetValues(typeof(PacketType)).Length) return;

            PacketType pt = (PacketType)packetType;
            Debug.Log($"<color=yellow>{pt}</color>");
            switch (pt)
            {
                case PacketType.S2C_Input:
                    {
                        var packet = new S2C_InputPacket();
                        packet.Deserialize(reader);
                        EventManager.Trigger(pt, packet, peer);
                    }
                    break;
                //以上是测试，保留
                case PacketType.S2C_LoginResult:
                    {
                        var packet = new S2C_LoginResultPacket();
                        packet.Deserialize(reader); //解包
                        OnUserStatusChanged(pt, packet); //Offline→AtLobby
                        EventManager.Trigger(pt, packet, peer); //派发
                    }
                    break;
                case PacketType.S2C_LogoutResult:
                    {
                        EmptyPacket packet = new EmptyPacket();
                        packet.Deserialize(reader);
                        EventManager.Trigger(pt, packet, peer);
                        OnLogoutResult(packet);
                    }
                    break;
                case PacketType.S2C_MatchResult:
                    {
                        var packet = new S2C_MatchResultPacket();
                        packet.Deserialize(reader);
                        OnUserStatusChanged(pt, packet); //AtLobby→AtRoomWait
                        EventManager.Trigger(pt, packet, peer);
                    }
                    break;
                case PacketType.S2C_RoleSelect:
                    {
                        var packet = new S2C_RoleSelectPacket();
                        packet.Deserialize(reader);
                        EventManager.Trigger(pt, packet, peer);
                    }
                    break;
                case PacketType.S2C_ErrorOperate:
                    {
                        var packet = new S2C_ErrorPacket();
                        packet.Deserialize(reader);
                        OnErrorOperate(packet);
                    }
                    break;
                case PacketType.S2C_UserInfo:
                    {
                        var packet = new S2C_GetUserInfoPacket();
                        packet.Deserialize(reader);
                        EventManager.Trigger(pt, packet, peer);
                    }
                    break;
                case PacketType.S2C_Settings:
                    {
                        var packet = new Settings();
                        packet.Deserialize(reader);
                        Debug.Log($"[S2C_Settings] music={packet.MusicVolume}, sound={packet.SoundVolume}, lang={(Languages)packet.Language}");

                        m_PlayerManager.LocalPlayer.m_Settings = packet;
                        AudioManager.Get().musicVolume = packet.MusicVolume / 100f;
                        AudioManager.Get().soundVolume = packet.SoundVolume / 100f;
                        AudioManager.Get().ApplyToCurrent();
                        ConfigManager.Get().SetLanguage((Languages)packet.Language);

                        EventManager.Trigger(pt, packet, peer);
                    }
                    break;
                case PacketType.S2C_GameReady:
                    {
                        var packet = new S2C_GameReadyPacket();
                        packet.Deserialize(reader);
                        OnUserStatusChanged(pt, packet); //AtRoomWait→AtRoomReady
                        EventManager.Trigger(pt, packet, peer);
                    }
                    break;
                case PacketType.S2C_LoadScene:
                    {
                        var packet = new S2C_LoadScenePacket();
                        packet.Deserialize(reader);
                        OnUserStatusChanged(pt, packet); //AtRoomReady→AtBattle
                        EventManager.Trigger(pt, packet, peer);
                    }
                    break;
                case PacketType.S2C_BattleStart:
                    {
                        var packet = new S2C_BattleStartPacket();
                        packet.Deserialize(reader);
                        EventManager.Trigger(pt, packet, peer);
                    }
                    break;
                case PacketType.S2C_BattlePause: //请求暂停比赛，等待30秒
                    {
                        var packet = new S2C_BattlePausePacket();
                        packet.Deserialize(reader);
                        EventManager.Trigger(pt, packet, peer);
                    }
                    break;
                case PacketType.S2C_BattleLostNet: //对方掉线，等待60秒
                    {
                        var packet = new S2C_BattlePausePacket();
                        packet.Deserialize(reader);
                        EventManager.Trigger(pt, packet, peer);
                    }
                    break;
                case PacketType.S2C_BattleReconnect: //掉线方登录，提示返回比赛
                    {
                        Debug.Log($"[C] 收到重连消息，弹窗是否返回比赛");
                        var packet = new S2C_LoadScenePacket();
                        packet.Deserialize(reader);
                        EventManager.Trigger(pt, packet, peer);
                    }
                    break;
                case PacketType.S2C_BattleInputs:
                    {
                        Debug.Log($"[C] Battle Lack Inputs");
                        var packet = new S2C_LackInputPacket();
                        packet.Deserialize(reader);
                        EventManager.Trigger(pt, packet, peer);
                    }
                    break;
                case PacketType.S2C_BattleEnd:
                    {
                        var packet = new S2C_BattleEndPacket();
                        packet.Deserialize(reader);
                        EventManager.Trigger(pt, packet, peer); //用UI_GameResult接收
                        OnUserStatusChanged(pt, packet); //AtBattle→AtLobby
                    }
                    break;
                default:
                    Debug.Log("Unhandled packet: " + pt);
                    break;
            }
        }

        void INetEventListener.OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { }

        void INetEventListener.OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
            _ping = latency;
        }

        void INetEventListener.OnConnectionRequest(ConnectionRequest request)
        {
            request.Reject();
        }
        #endregion


        #region Functions
        public bool IsConnect()
        {
            if (_netManager.ConnectedPeersCount > 0)
                return true;
            return false;
        }

        public void Connect(Action<DisconnectInfo> onDisconnected)
        {
            if (IsConnect()) return;
            //Debug.Log("挂载委托");
            _onDisconnected = onDisconnected;

            string IP = GameManager.present.gate;
            int Port = ConfigManager.Get().globalConfig.Port;
            string Key = ConfigManager.Get().globalConfig.Key;
            _netManager.Connect(IP, Port, Key);
            Debug.Log($"Connect to: {IP}: {Port}, key={Key}");
        }

        public void Disconnect()
        {
            _netManager.DisconnectAll();
            //Debug.Log("disconnect:" + _netManager.ConnectedPeersCount);
        }

        public void SendInput(C2S_InputPacket cmd)
        {
            if (m_ClientRoom.BattleStage != BattleStage.Running)
            {
                //Debug.Log($"{m_ClientRoom.BattleStage} return");
                return; 
            }

#if UNITY_EDITOR
            // 模拟丢包
            //int rd = UnityEngine.Random.Range(0, 100);
            //if (rd < dropRate)
            //{
            //    Debug.LogError($"丢包:{cmd.frameNumber}");
            //    return;
            //}
#endif

            if (m_ClientRoom.BattleMode == BattleMode.Matching)
            {
                SendPacketSerializable(PacketType.C2S_Input, cmd);
                Debug.Log($" >> 发送: {cmd.frameNumber}---({cmd.input}) >>");
            }
            else
            {
                // 不需要发
            }
        }
        //以上是测试，保留

        public void SendRegister(string userName, string password)
        {
            Debug.Log($"[C] 注册请求");
            var cmd = new C2S_LoginPacket
            {
                UserName = userName,
                Password = password,
            };
            SendPacketSerializable(PacketType.C2S_RegisterReq, cmd);
        }

        public void SendLogin(string userName, string password)
        {
            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
            {
                Debug.LogError("用户名或密码未填");
                var connect = UIManager.Get().Push<UI_Connect>();
                connect.Pop();
                var toast = UIManager.Get().Push<UI_Toast>();
                toast.Show("用户名或密码未填");
                return;
            }

            var cmd = new C2S_LoginPacket
            {
                UserName = userName,
                Password = Md5Utils.GetMD5String(password),
            };
            SendPacketSerializable(PacketType.C2S_LoginReq, cmd);
            Debug.Log($"[C] 登录请求：用户名={cmd.UserName}，密码={cmd.Password}");
        }

        // 连接成功，自动用令牌登录
        async void SendLoginByToken()
        {
            string token = GameManager.Token;
            Debug.Log($"连接服务器成功，尝试读取Token：'{token}'");

            var connect = UIManager.Get().Push<UI_Connect>();
            await Task.Delay(500); //转一下

            // 使用Token登录
            if (!string.IsNullOrEmpty(token))
            {
                var cmd = new C2S_LoginByTokenPacket { Token = token };
                SendPacketSerializable(PacketType.C2S_LoginByToken, cmd);
            }
            else
            {
                connect.Pop();
            }
        }

        public void SendLogout()
        {
            Debug.Log($"[C] 登出请求");
            SendPacketSerializable(PacketType.C2S_LogoutReq, new EmptyPacket());
        }

        public void SendGetUserInfo(short peerId)
        {
            var cmd = new C2S_GetUserInfoPacket { PeerId = peerId };
            SendPacketSerializable(PacketType.C2S_UserInfo, cmd);
        }

        public void SendSettins(Settings cmd)
        {
            SendPacketSerializable(PacketType.C2S_Settings, cmd);
        }

        public void SendMatchRequest()
        {
            Debug.Log($"[C] 请求匹配");
            SendPacketSerializable(PacketType.C2S_MatchRequest, new EmptyPacket());

            var localPlayer = m_PlayerManager.LocalPlayer;
            localPlayer.SetStatus(PlayerStatus.Matching);
            UserEventManager.Trigger(localPlayer.Status); //通知给UI
        }

        public void SendMatchCancel()
        {
            Debug.Log($"[C] 取消匹配");
            SendPacketSerializable(PacketType.C2S_MatchCancel, new EmptyPacket());
        }

        public void SendMatchQuit()
        {
            SendPacketSerializable(PacketType.C2S_MatchQuit, new EmptyPacket());
        }

        public void SendSelection(int id)
        {
            if (m_PlayerManager.LocalPlayer.Status == PlayerStatus.AtRoomReady ||
                m_PlayerManager.LocalPlayer.Status == PlayerStatus.AtBattle)
            {
                Debug.LogError("准备好了，不能再选择");
                return;
            }
            //Debug.Log($"[C] 我({m_PlayerManager.LocalPlayer.Status})，选择角色: {id}");
            var cmd = new C2S_RoleSelectPacket { Index = (byte)id };
            SendPacketSerializable(PacketType.C2S_RoleSelect, cmd);
        }

        public void SendGameReady()
        {
            SendPacketSerializable(PacketType.C2S_GameReady, new EmptyPacket());
        }
        
        private string[] battleStartDebug = new string[]
        {
            "开始跳转场景了，通知服务器启动，准备同步",
            "倒计时结束，出Fight，通知服务器第一帧同步",
            "从暂停恢复游戏",
        };
        public void SendBattleStart(byte stage)
        {
            Debug.Log($"<color=yellow>[C] SendBattleStart: {stage}---{battleStartDebug[stage]}</color>");
            var cmd = new C2S_BattleStartPacket { Stage = stage };
            SendPacketSerializable(PacketType.C2S_BattleStart, cmd);
        }

        public void SendBattlePause()
        {
            SendPacketSerializable(PacketType.C2S_BattlePause, new EmptyPacket());
        }

        public void SendBattleQuit()
        {
            SendPacketSerializable(PacketType.C2S_BattleQuit, new EmptyPacket());
        }

        public void SendBattleEnd(int winner)
        {
            var cmd = new C2S_BattleEndPacket { Winner = (sbyte)winner };
            SendPacketSerializable(PacketType.C2S_BattleEnd, cmd);
        }

        public void SendLackInput(uint start = 0, uint end = 0)
        {
            var cmd = new C2S_LackInputPacket { startTick = start, endTick = end };
            SendPacketSerializable(PacketType.C2S_BattleInputs, cmd);
        }

        // 统一处理用户状态变化，并派发出去
        void OnUserStatusChanged(PacketType type, INetSerializable reader)
        {
            switch (type)
            {
                case PacketType.S2C_LoginResult:
                    {
                        var packet = (S2C_LoginResultPacket)reader;
                        if (packet.Code == 0)
                        {
                            // 创建用户对象
                            var clientPlayer = new ClientPlayer(packet.UserName, packet.PeerId);
                            m_PlayerManager.AddClientPlayer(clientPlayer, true);
                            m_PlayerManager.LocalPlayer.ResetToLobby();
                        }
                    }
                    break;
                case PacketType.S2C_MatchResult:
                    {
                        var packet = (S2C_MatchResultPacket)reader;
                        if (packet.Code == 0) //匹配成功0
                        {
                            // 创建用户管理
                            bool localIsHost = m_PlayerManager.LocalPlayer.PeerId == packet.Host.PeerId;
                            string rivalName = localIsHost ? packet.Guest.UserName : packet.Host.UserName;
                            short rivalPeer = localIsHost ? packet.Guest.PeerId : packet.Host.PeerId;
                            ClientPlayer rivalPlayer = new ClientPlayer(rivalName, rivalPeer);
                            m_PlayerManager.AddClientPlayer(rivalPlayer, false);

                            // 创建房间管理
                            ClientPlayer host = localIsHost ? m_PlayerManager.LocalPlayer : m_PlayerManager.RivalPlayer;
                            ClientPlayer guest = localIsHost ? m_PlayerManager.RivalPlayer : m_PlayerManager.LocalPlayer;
                            m_ClientRoom = new ClientRoom(packet.RoomId, host, guest);
                            m_ClientRoom.BattleMode = (BattleMode)packet.BattleMode;
                        }
                        else //匹配取消1、匹配后退出2
                        {
                            m_ClientRoom = null;
                            m_PlayerManager.LocalPlayer.ResetToLobby();
                            m_PlayerManager.RemoveRival();
                        }
                    }
                    break;
                case PacketType.S2C_GameReady:
                    {
                        var packet = (S2C_GameReadyPacket)reader;
                        //Debug.Log($"<color=red>准备好了：[主位]{(PlayerStatus)packet.HostStatus}，[客位]{(PlayerStatus)packet.GuestStatus}。[我的座位]{m_PlayerManager.LocalPlayer.SeatId}</color>");
                        if ((PlayerStatus)packet.HostStatus == PlayerStatus.AtRoomReady)
                        {
                            if (m_PlayerManager.LocalPlayer.SeatId == 0)
                                m_PlayerManager.LocalPlayer.SetStatus(PlayerStatus.AtRoomReady);
                            else if (m_PlayerManager.RivalPlayer.SeatId == 0)
                                m_PlayerManager.RivalPlayer.SetStatus(PlayerStatus.AtRoomReady);
                        }
                        if ((PlayerStatus)packet.GuestStatus == PlayerStatus.AtRoomReady)
                        {
                            if (m_PlayerManager.LocalPlayer.SeatId == 1)
                                m_PlayerManager.LocalPlayer.SetStatus(PlayerStatus.AtRoomReady);
                            else if (m_PlayerManager.RivalPlayer.SeatId == 1)
                                m_PlayerManager.RivalPlayer.SetStatus(PlayerStatus.AtRoomReady);
                        }
                    }
                    break;
                case PacketType.S2C_LoadScene:
                    {
                        m_PlayerManager.LocalPlayer.SetStatus(PlayerStatus.AtBattle);
                    }
                    break;
                case PacketType.S2C_BattleEnd:
                    {
                        m_PlayerManager.LocalPlayer.ResetToLobby();
                        m_PlayerManager.RemoveRival();
                    }
                    break;
            }
            if (m_PlayerManager.LocalPlayer != null)
                UserEventManager.Trigger(m_PlayerManager.LocalPlayer.Status); //通知给UI
        }

        // 自己登出
        void OnLogoutResult(INetSerializable reader)
        {
            Debug.Log($"<color=red>[C] {m_PlayerManager.LocalPlayer.UserName}登出重置</color>");
            m_PlayerManager.RemoveAll();
        }

        // 错误码
        void OnErrorOperate(INetSerializable reader)
        {
            var packet = (S2C_ErrorPacket)reader;
            Debug.Log($"错误操作：{(ErrorCode)packet.ErrorCode}");

            var toast = UIManager.Get().Push<UI_Toast>();
            toast.Show($"{(ErrorCode)packet.ErrorCode}");
        }
        #endregion
    }
}