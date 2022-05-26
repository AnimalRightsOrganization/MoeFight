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
    public class ClientLogic : MonoBehaviour, INetEventListener
    {
        public const string IP = "localhost";
        public const int Port = 5000;
        public const string Key = "ExampleGame";

        private Action<DisconnectInfo> _onDisconnected;

        private NetManager _netManager;
        private NetDataWriter _writer;

        private string _userName;
        private ushort _lastServerTick;
        private NetPeer _server;
        private ClientPlayerManager _playerManager;
        private int _ping;

        public static LogicTimer LogicTimer { get; private set; }

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Random rd = new Random();
            _userName = Environment.MachineName + " " + rd.Next(100000);
            LogicTimer = new LogicTimer(OnLogicUpdate);
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
            LogicTimer.Update();
            //$"Ping: {_ping}");
        }

        void OnDestroy()
        {
            _netManager.Stop();
        }

        void FixedUpdate()
        {
            
        }

        private void OnLogicUpdate()
        {
            _playerManager.LogicUpdate();
        }


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

        public void SendPacket<T>(T packet, DeliveryMethod deliveryMethod) where T : class, new()
        {
            if (_server == null)
                return;
            _writer.Reset();
            _writer.Put((byte)PacketType.Serialized);
            //_packetProcessor.Write(_writer, packet);
            _server.Send(_writer, deliveryMethod);
        }

        void INetEventListener.OnPeerConnected(NetPeer peer)
        {
            Debug.Log("[C] Connected to server: " + peer.EndPoint);
            _server = peer;

            LogicTimer.Start();
        }

        void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            _playerManager.Clear();
            _server = null;
            LogicTimer.Stop();
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

        private void OnPlayerJoined(PlayerJoinedPacket packet)
        {
            Debug.Log($"[C] Player joined: {packet.UserName}");
        }

        private void OnPlayerLeaved(PlayerLeavedPacket packet)
        {
            var player = _playerManager.RemovePlayer(packet.Id);
            if (player != null)
                Debug.Log($"[C] Player leaved: {player.Name}");
        }

        private void OnJoinAccept(JoinAcceptPacket packet)
        {
            Debug.Log("[C] Join accept. Received player id: " + packet.Id);
            _lastServerTick = packet.ServerTick;
            var clientPlayer = new ClientPlayer(this, _playerManager, _userName, packet.Id);
            //var view = ClientPlayerView.Create(_clientPlayerViewPrefab, clientPlayer);
            //_playerManager.AddClientPlayer(clientPlayer, view);
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