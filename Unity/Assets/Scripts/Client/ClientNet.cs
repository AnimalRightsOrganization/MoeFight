using System;
using System.Net;
using System.Net.Sockets;
using Code.Shared;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;
using HitstunConstants;

namespace Code.Client
{
    public class ClientNet : MonoBehaviour, INetEventListener
    {
        //public const string IP = "moegijinka.cn";
        public const string IP = "127.0.0.1";
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

            _writer = new NetDataWriter();
            _playerManager = new ClientPlayerManager(this);
            _netManager = new NetManager(this)
            {
                AutoRecycle = true,
                IPv6Mode = IPv6Mode.Disabled
            };
            _netManager.Start();
        }

        void Start()
        {
            //用FixedUpdate代替InvokeRepeating, Stopwatch, 可以在编辑器步进
            //InvokeRepeating("LogicUpdate", 0, ConfigManager.FIXED_DELTA);
        }

        void Update()
        {
            _netManager.PollEvents();

            UI_Main.Instance.Ping(_ping);

            //Debug.Log("<color=green>Update</color>");
        }

        void OnDestroy()
        {
            _netManager.Stop();
        }

        void FixedUpdate()
        {
            _playerManager.LogicUpdate();

            //Debug.Log($"<color=yellow>FixedUpdate: {Time.time}</color>"); //两个Fixed之间有多个Update
            LogicUpdate();
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
                case PacketType.S2C_LoginResult:
                    OnLogin(peer, reader);
                    break;
                case PacketType.S2C_Lockstep:
                    OnLockstep(peer, reader);
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
            //Debug.Log($"OnNetworkLatencyUpdate: {peer.Id} - {latency}ms");
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

        public void SendLogin(C2S_LoginPacket cmd)
        {
            SendPacketSerializable(PacketType.C2S_LoginReq, cmd);
        }

        public void SendInput(C2S_InputPacket cmd)
        {
            //Debug.Log($"[C2S.SendInput] {cmd.frameNumber}---{cmd.input}");
            SendPacketSerializable(PacketType.C2S_Lockstep, cmd);
        }

        private void OnLogin(NetPeer peer, NetPacketReader reader)
        {
            var resp = new S2C_LoginResultPacket();
            resp.Deserialize(reader);
            Debug.Log($"[S2C] OnLogin: code={resp.Code}, peerid={resp.PeerId}, {resp.UserName}");
        }

        private void OnLockstep(NetPeer peer, NetPacketReader reader)
        {
            var resp = new S2C_InputPacket();
            resp.Deserialize(reader);
            Debug.Log($"[S2C.Lockstep] {resp.frameNumber}---{resp.inputs[0]}");
        }
        #endregion


        public void LogicUpdate()
        {
            _lastServerTick++;

            uint _input = ReadInputs(0);
            C2S_InputPacket cmd = new C2S_InputPacket { frameNumber = _lastServerTick, input = _input };
            SendInput(cmd);
        }

        // 键盘输入，左右两边控制
        static uint ReadInputs(int controllerId)
        {
            uint input = 0;

            if (controllerId == 0)
            {
                if (Input.GetKey(KeyCode.W))
                {
                    input |= (uint)KeyPress.KEY_UP;
                }
                if (Input.GetKey(KeyCode.S))
                {
                    input |= (uint)KeyPress.KEY_DOWN;
                }
                if (Input.GetKey(KeyCode.A))
                {
                    input |= (uint)KeyPress.KEY_LEFT;
                }
                if (Input.GetKey(KeyCode.D))
                {
                    input |= (uint)KeyPress.KEY_RIGHT;
                }
                if (Input.GetKey(KeyCode.U))
                {
                    input |= (uint)KeyPress.KEY_LP;
                }
                if (Input.GetKey(KeyCode.I))
                {
                    input |= (uint)KeyPress.KEY_MP;
                }
                if (Input.GetKey(KeyCode.O))
                {
                    input |= (uint)KeyPress.KEY_HP;
                }
                if (Input.GetKey(KeyCode.J))
                {
                    input |= (uint)KeyPress.KEY_LK;
                }
                if (Input.GetKey(KeyCode.K))
                {
                    input |= (uint)KeyPress.KEY_MK;
                }
                if (Input.GetKey(KeyCode.L))
                {
                    input |= (uint)KeyPress.KEY_HK;
                }
            }
            else if (controllerId == 1)
            {
                if (Input.GetKey(KeyCode.UpArrow))
                {
                    input |= (uint)KeyPress.KEY_UP;
                }
                if (Input.GetKey(KeyCode.DownArrow))
                {
                    input |= (uint)KeyPress.KEY_DOWN;
                }
                if (Input.GetKey(KeyCode.LeftArrow))
                {
                    input |= (uint)KeyPress.KEY_LEFT;
                }
                if (Input.GetKey(KeyCode.RightArrow))
                {
                    input |= (uint)KeyPress.KEY_RIGHT;
                }
                if (Input.GetKey(KeyCode.RightControl))
                {
                    input |= (uint)KeyPress.KEY_MK;
                }
            }
            return input;
        }
    }
}