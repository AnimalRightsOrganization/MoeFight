using System;
using System.Net;
using System.Net.Sockets;
using Code.Shared;
using HotFix;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

namespace Code.Client
{
    public class ClientNet : MonoBehaviour, INetEventListener
    {
        static ClientNet _get;
        public static ClientNet Get
        {
            get
            {
                if (_get == null)
                    _get = FindObjectOfType<ClientNet>();
                return _get;
            }
        }

        public const string IP = "192.168.1.101";
        public const int Port = 5000;
        public const string Key = "ExampleGame";

        private NetPeer _server;
        private NetManager _netManager;
        private NetDataWriter _writer;

        private Action<DisconnectInfo> _onDisconnected;
        public ClientRoom m_ClientRoom;
        public ClientPlayerManager m_PlayerManager;
        public int _ping;
        public string myName;


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
            _netManager.PollEvents();
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
            Debug.Log("[C] Connected to server: " + peer.EndPoint);
            _server = peer;
        }

        void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            m_PlayerManager.Clear();
            _server = null;

            Debug.Log("[C] Disconnected from server: " + disconnectInfo.Reason);
            _onDisconnected?.Invoke(disconnectInfo);
            _onDisconnected = null;
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
            switch (pt)
            {
                case PacketType.S2C_TestPVE:
                    {
                        var packet = new EmptyPacket();
                        packet.Deserialize(reader);
                        EventManager.Trigger(pt, packet, peer);
                    }
                    break;
                case PacketType.S2C_TestPVP:
                    {
                        var packet = new S2C_JoinResultPacket();
                        packet.Deserialize(reader);
                        EventManager.Trigger(pt, packet, peer);
                    }
                    break;
                case PacketType.S2C_Lockstep:
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
                        EventManager.Trigger(pt, packet, peer); //派发
                        if (packet.Code == 0)
                            OnUserStatusChanged(pt, packet); //登录成功才改变用户状态
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
                        EventManager.Trigger(pt, packet, peer);
                        OnUserStatusChanged(pt, packet);
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
                        EventManager.Trigger(pt, packet, peer);

                        m_PlayerManager.LocalPlayer.m_Settings = packet;
                        AudioManager.musicVolume = packet.MusicVolume / 100f;
                        AudioManager.soundVolume = packet.SoundVolume / 100f;
                        AudioManager.Get().ApplyToCurrent();
                    }
                    break;
                case PacketType.S2C_GameReady:
                    {
                        var packet = new S2C_GameReadyPacket();
                        packet.Deserialize(reader);
                        EventManager.Trigger(pt, packet, peer);
                        OnUserStatusChanged(pt, packet);
                    }
                    break;
                case PacketType.S2C_LoadScene:
                    {
                        var packet = new S2C_LoadScenePacket();
                        packet.Deserialize(reader);
                        EventManager.Trigger(pt, packet, peer);
                        OnUserStatusChanged(pt, packet);
                    }
                    break;
                case PacketType.S2C_BattleStart:
                    {
                        var packet = new S2C_BattleStartPacket();
                        packet.Deserialize(reader);
                        EventManager.Trigger(pt, packet, peer);
                    }
                    break;
                case PacketType.S2C_BattlePause:
                    {
                        var packet = new EmptyPacket();
                        packet.Deserialize(reader);
                        EventManager.Trigger(pt, packet, peer);
                    }
                    break;
                case PacketType.S2C_BattleEnd:
                    {
                        var packet = new S2C_BattleEndPacket();
                        packet.Deserialize(reader);
                        EventManager.Trigger(pt, packet, peer); //UI_GameResult这接收
                        OnUserStatusChanged(pt, packet);
                    }
                    break;
                default:
                    Debug.Log("Unhandled packet: " + pt);
                    break;
            }
        }

        void INetEventListener.OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {

        }

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
        public void Connect(Action<DisconnectInfo> onDisconnected)
        {
            _onDisconnected = onDisconnected;
            _netManager.Connect(IP, Port, Key);
            Debug.Log($"Connect to: {IP}: {Port}, key={Key}");
        }

        public void SendTestPVE()
        {
            System.Random rd = new System.Random();
            string _userName = System.Environment.MachineName + " " + rd.Next(100000);
            var cmd = new C2S_JoinPacket { UserName = _userName };

            myName = cmd.UserName;

            SendPacketSerializable(PacketType.C2S_TestPVE, cmd);
        }

        public void SendTestPVP()
        {
            System.Random rd = new System.Random();
            string _userName = System.Environment.MachineName + " " + rd.Next(100000);
            var cmd = new C2S_JoinPacket { UserName = _userName };

            myName = cmd.UserName;

            SendPacketSerializable(PacketType.C2S_TestPVP, cmd);
        }

        public void SendReady()
        {
            SendPacketSerializable(PacketType.C2S_BattleStart, new EmptyPacket());
        }

        public void SendInput(C2S_InputPacket cmd)
        {
            SendPacketSerializable(PacketType.C2S_Lockstep, cmd);
        }

        public void SendLogin(string userName, string password)
        {
            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
            {
                Debug.LogError("用户名或密码未填");
                var ui = UIManager.Get().Push<UI_Toast>();
                ui.Show("用户名或密码未填");
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

        public void SendLogout()
        {
            Debug.Log($"[C] 登出请求");
            var cmd = new EmptyPacket();
            SendPacketSerializable(PacketType.C2S_LogoutReq, cmd);
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

            m_PlayerManager.LocalPlayer.SetStatus(PlayerStatus.Matching);
            UserEventManager.Trigger(m_PlayerManager.LocalPlayer.Status); //通知给UI
        }

        public void SendMatchCancel()
        {
            Debug.Log($"[C] 取消匹配");
            SendPacketSerializable(PacketType.C2S_MatchCancel, new EmptyPacket());
        }

        public void SendSelection(int id)
        {
            if (m_PlayerManager.LocalPlayer.Status == PlayerStatus.AtRoomReady ||
                m_PlayerManager.LocalPlayer.Status == PlayerStatus.AtBattle)
            {
                Debug.LogError("准备好了，不能再选择");
                return;
            }
            Debug.Log($"[C] 我({m_PlayerManager.LocalPlayer.Status})，选择角色: {id}");
            C2S_RoleSelectPacket cmd = new C2S_RoleSelectPacket { Index = (byte)id };
            SendPacketSerializable(PacketType.C2S_RoleSelect, cmd);
        }

        public void SendMatchQuit()
        {
            SendPacketSerializable(PacketType.C2S_MatchQuit, new EmptyPacket());
        }

        public void SendGameReady()
        {
            SendPacketSerializable(PacketType.C2S_GameReady, new EmptyPacket());
        }

        public void SendBattleStart()
        {

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
                            //ReconnectTimes = 2; //登录成功，补充重连次数
                            m_PlayerManager.LocalPlayer.ResetToLobby();
                        }
                    }
                    break;
                case PacketType.S2C_MatchResult:
                    {
                        S2C_MatchResultPacket packet = (S2C_MatchResultPacket)reader;
                        if (packet.Code == 0) //匹配成功
                        {
                            ClientPlayer host = new ClientPlayer(packet.Host.UserName, packet.Host.PeerId);
                            ClientPlayer guest = new ClientPlayer(packet.Guest.UserName, packet.Guest.PeerId);
                            m_ClientRoom = new ClientRoom(packet.RoomId, host, guest);
                            m_PlayerManager.LocalPlayer.SetStatus(PlayerStatus.AtRoomWait);
                        }
                        else if (packet.Code == 1) //匹配取消
                        {
                            m_PlayerManager.LocalPlayer.ResetToLobby();
                            m_ClientRoom?.Dispose();
                            m_ClientRoom = null;
                        }
                        else if (packet.Code == 2) //匹配后退出
                        {
                            m_PlayerManager.LocalPlayer.ResetToLobby();
                            m_PlayerManager.ResetRival();
                            m_ClientRoom.Dispose();
                            m_ClientRoom = null;
                        }
                    }
                    break;
                case PacketType.S2C_GameReady:
                    {
                        S2C_GameReadyPacket packet = (S2C_GameReadyPacket)reader;
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
                        //GameManager.Instance.DumpInputs();
                        m_PlayerManager.LocalPlayer.ResetToLobby();
                        m_PlayerManager.ResetRival();
                    }
                    break;
            }
            UserEventManager.Trigger(m_PlayerManager.LocalPlayer.Status); //通知给UI
        }

        // 自己登出
        void OnLogoutResult(INetSerializable reader)
        {
            Debug.Log($"<color=red>[C] {m_PlayerManager.LocalPlayer.UserName}登出重置</color>");
            m_PlayerManager.Clear();
        }

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