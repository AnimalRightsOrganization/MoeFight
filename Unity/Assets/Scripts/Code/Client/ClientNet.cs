using System;
using System.Net;
using System.Net.Sockets;
using Code.Shared;
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
        private ClientPlayerManager _playerManager;
        public int _ping;
        public string myName;


        #region Inner Method
        void Awake()
        {
            DontDestroyOnLoad(gameObject);

            _writer = new NetDataWriter();
            _playerManager = new ClientPlayerManager(this);
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
            _playerManager.Clear();
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
                case PacketType.S2C_TestX1Result:
                    {
                        var packet = new EmptyPacket();
                        packet.Deserialize(reader);
                        EventManager.Trigger(pt, packet, peer);
                    }
                    break;
                case PacketType.S2C_TestX2Result:
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
            SendPacketSerializable(PacketType.C2S_TestX1Req, cmd);
        }

        public void SendTestPVP(C2S_JoinPacket cmd)
        {
            myName = cmd.UserName;
            SendPacketSerializable(PacketType.C2S_TestX2Req, cmd);
        }

        public void SendReady(EmptyPacket cmd)
        {
            SendPacketSerializable(PacketType.C2S_BattleStart, cmd);
        }

        public void SendInput(C2S_InputPacket cmd)
        {
            SendPacketSerializable(PacketType.C2S_Lockstep, cmd);
        }
        #endregion
    }
}