using System;
using System.Net;
using System.Net.Sockets;
using Code.Shared;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;
using Random = System.Random;

namespace Code.Client
{
    public class ClientNet : MonoBehaviour, INetEventListener
    {
        public const string IP = "localhost";
        public const int Port = 5000;
        public const string Key = "ExampleGame";

        private NetPeer _server;
        private NetManager _netManager;
        private NetDataWriter _writer;

        private Action<DisconnectInfo> _onDisconnected;
        private string _userName;
        private ushort _lastServerTick;
        private ClientPlayerManager _playerManager;
        private int _ping;


        #region Inner Method
        void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Random rd = new Random();
            _userName = Environment.MachineName + " " + rd.Next(100000);
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
            //$"Ping: {_ping}");
        }

        void OnDestroy()
        {
            _netManager.Stop();
        }

        void FixedUpdate()
        {
            _playerManager.LogicUpdate();
        }
        #endregion


        #region Interface
        public void SendPacketSerializable<T>(PacketType type, T packet, DeliveryMethod deliveryMethod) where T : INetSerializable
        {
            if (_server == null)
                return;
            _writer.Reset();
            _writer.Put((byte)type);
            packet.Serialize(_writer);
            _server.Send(_writer, deliveryMethod);
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
            if (_onDisconnected != null)
            {
                _onDisconnected(disconnectInfo);
                _onDisconnected = null;
            }
        }

        void INetEventListener.OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            Debug.Log("[C] NetworkError: " + socketError);
        }

        void INetEventListener.OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
        {
            byte packetType = reader.GetByte();
            if (packetType >= NetworkGeneral.PacketTypesCount)
                return;

            PacketType pt = (PacketType)packetType;
            switch (pt)
            {
                case PacketType.S2C_Login:
                    OnLogin(peer, reader);
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
        }

        public void SendLogin()
        {
            var request = new LoginRequest { UserName = _userName };
            SendPacketSerializable(PacketType.C2S_Login, request, DeliveryMethod.Unreliable);
        }

        private void OnLogin(NetPeer peer, NetPacketReader reader)
        {
            LoginResponse resp = new LoginResponse();
            resp.Deserialize(reader);
            Debug.Log($"[S2C] OnLogin: {resp.UserName}, {resp.Token}");
        }
        #endregion
    }
}