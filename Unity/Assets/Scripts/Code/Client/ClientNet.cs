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
            if (packetType >= 1024) return;

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
                case PacketType.S2C_BattlePause:
                    {
                        var packet = new EmptyPacket();
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
                case PacketType.S2C_LoginResult:
                    {
                        var packet = new S2C_LoginResultPacket();
                        packet.Deserialize(reader);
                        EventManager.Trigger(pt, packet, peer);
                    }
                    break;
                case PacketType.S2C_MatchResult:
                    {
                        var packet = new S2C_MatchResultPacket();
                        packet.Deserialize(reader);
                        EventManager.Trigger(pt, packet, peer);
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

        public void SendTestPVE(C2S_JoinPacket cmd)
        {
            myName = cmd.UserName;
            SendPacketSerializable(PacketType.C2S_TestPVE, cmd);
        }

        public void SendTestPVP(C2S_JoinPacket cmd)
        {
            myName = cmd.UserName;
            SendPacketSerializable(PacketType.C2S_TestPVP, cmd);
        }

        public void SendReady(EmptyPacket cmd)
        {
            SendPacketSerializable(PacketType.C2S_BattleStart, cmd);
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
            var cmd = new EmptyPacket();
            SendPacketSerializable(PacketType.C2S_LogoutReq, cmd);
            //Debug.Log($"[C] 登出请求");
        }

        public void SendGetUserInfo(short peerId)
        {
            C2S_GetUserInfoPacket cmd = new C2S_GetUserInfoPacket
            {
                PeerId = peerId
            };
            SendPacketSerializable(PacketType.C2S_UserInfo, cmd);
        }

        public void SendSettins(Settings cmd)
        {
            SendPacketSerializable(PacketType.C2S_Settings, cmd);
        }

        public void SendMatchRequest()
        {
            Debug.Log($"[C] 请求匹配");
            EmptyPacket cmd = new EmptyPacket();
            SendPacketSerializable(PacketType.C2S_MatchRequest, cmd);

            m_PlayerManager.LocalPlayer.SetStatus(PlayerStatus.Matching);
            UserEventManager.Trigger(m_PlayerManager.LocalPlayer.Status); //通知给UI
        }

        public void SendMatchCancel()
        {
            Debug.Log($"[C] 取消匹配");
            EmptyPacket cmd = new EmptyPacket();
            SendPacketSerializable(PacketType.C2S_MatchCancel, cmd);
        }

        public void SendSelection(int id) { }

        public void SendMatchQuit()
        {
            SendPacketSerializable(PacketType.C2S_MatchQuit, new EmptyPacket());
        }

        public void SendGameReady() { }

        #endregion
    }
}