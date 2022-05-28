using System;
using System.Net;
using System.Net.Sockets;
using Code.Shared;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;
using HitstunConstants;
using System.Collections.Generic;

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

            IsStart = false;
            sendTick = 0;
            recvTick = 0;
            ggpo_predict = new Dictionary<uint, uint[]>(); //4294967295 /50帧每秒 = 85,899,346秒 = 23,860小时 = 994天。4+4+4=12个字节
            ggpo_recieve = new Dictionary<uint, uint[]>();
            dic_delay = new Queue<C2S_InputPacket>();
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
                case PacketType.S2C_BattleStart:
                    OnBattleStart(peer, reader);
                    break;
                case PacketType.S2C_Lockstep:
                    OnRecvLockstep(peer, reader);
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

        public void SendReady(EmptyPacket cmd)
        {
            SendPacketSerializable(PacketType.C2S_BattleStart, cmd);
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

            IsStart = true;
        }

        private void OnBattleStart(NetPeer peer, NetPacketReader reader)
        {

        }

        private void OnRecvLockstep(NetPeer peer, NetPacketReader reader)
        {
            var resp = new S2C_InputPacket();
            resp.Deserialize(reader);
            //Debug.Log($"[S2C.Lockstep] {resp.frameNumber}---{resp.inputs[0]}({resp.inputs.Length})");
            //Debug.Log($"Left => {(resp.inputs[0] & (uint)KeyPress.KEY_LEFT) != 0}"); //判断是否按了左键
            //Debug.Log($"Right => {(resp.inputs[0] & (uint)KeyPress.KEY_RIGHT) != 0}");

            uint server_tick = resp.frameNumber;
            ggpo_recieve[server_tick] = resp.inputs;


            //一定不能在这里更逻辑
            //Process(resp.inputs);
            //Execute(resp.frameNumber, resp.inputs);
        }
        #endregion

        public int DELAY_FRAMES = 0;
        public bool IsStart;
        public ushort sendTick;
        public ushort recvTick;
        private Dictionary<uint, uint[]> ggpo_predict; //预测帧
        private Dictionary<uint, uint[]> ggpo_recieve; //下发帧
        private Queue<C2S_InputPacket> dic_delay; //延迟帧
        public HitstunRunner runner;

        void FixedUpdate()
        {
            if (!IsStart) return;

            //Debug.Log("新的一帧----------------------------------------------------");
            S2C_InputPacket op = new S2C_InputPacket();
            runner.SaveOldBuffer();

            //①收集本地按键，发送，预测
            sendTick++;
            uint[] inputs = LocalSession.RunFrame();
            uint input = inputs[0];
            var cmd = new C2S_InputPacket { frameNumber = sendTick, input = input };
            SendInput(cmd);

            //②Delay模式缓冲帧（设为0时，不走这块逻辑）
            if (DELAY_FRAMES > 0)
            {
                dic_delay.Enqueue(cmd);
                if (dic_delay.Count <= DELAY_FRAMES)
                    return;
            }
            if (dic_delay.Count > 0)
            {
                cmd = dic_delay.Dequeue();
                //Debug.Log($"逻辑帧更新：{sendTick}/{myPacket.Tick}");
            }


            //③对比逻辑，不是在OnRecv做，在主循环做可控。
            bool verity = true;
            uint badTick = 0; //最早发生预测错误的帧

            //发送完看下有没有收到新的帧(recvTick + 1)开始，可能有多个，对之前的预测进行验证。
            for (int i = recvTick + 1; i < ggpo_recieve.Count; i++)
            {
                ushort _serverTick = (ushort)i;
                Debug.Log($"[C] 本地更{cmd.frameNumber}时，发现有可用的服务器帧：{_serverTick}");

                //是否执行验证取决于是否预测过（Delay大于延迟，就不用预测）
                bool needToVerity = ggpo_predict.ContainsKey(_serverTick);
                if (needToVerity)
                {
                    var recieve1 = ggpo_recieve[_serverTick][0];
                    var recieve2 = ggpo_recieve[_serverTick][1];
                    var predict1 = ggpo_predict[_serverTick][0];
                    var predict2 = ggpo_predict[_serverTick][1];
                    if (recieve1.Equals(predict1) && recieve2.Equals(predict2))
                    {
                        //之前的预测准确。不用更新表现了，预测时已经走过表现逻辑。
                        recvTick = _serverTick;
                    }
                    else
                    {
                        verity = false;
                        badTick = _serverTick;
                        Debug.Log($"预测错误：\nP1:{predict1}\nP2:{predict2}");
                        break;
                    }
                }
                else //delay很长，期间已经收到包了，没有预测过
                {
                    Debug.Log("delay很长");
                    uint[] _ops = ggpo_recieve[_serverTick]; //双方的操作
                    Process(_ops);

                    //没预测过，也用真实的数据覆盖一下预测容器
                    ggpo_predict[_serverTick] = _ops;
                    recvTick = _serverTick;

                    // 快照
                    //op = new S2C_InputPacket
                    //{
                    //    frameNumber = _serverTick,
                    //    inputs = _ops,
                    //};
                    //Snapshot(op);
                }
            }

            // 验证失败处理
            if (verity == false)
            {
                //一次性回滚到最早发生错误的地方。
                Debug.LogError($"[C] {badTick}预测错，回滚");
                Rollback(badTick);

                //用收到的帧，覆盖错误的预测。
                ushort goodTick = (ushort)ggpo_recieve.Count;
                Debug.Log($"<color=yellow>[C] 覆盖错误的预测: {badTick}~{goodTick}</color>");
                for (uint t = badTick; t <= goodTick; t++)
                {
                    uint[] _inputs = ggpo_recieve[t];
                    ggpo_predict[t] = _inputs;
                    Process(_inputs);
                }
                recvTick = goodTick;

                //追帧预测到本地的前一帧。本地的当前帧，在最后单独处理预测。
                //Debug.Log($"<color=yellow>[C] 追帧预测: {(ushort)(goodTick + 1)}~{packet.Tick - 1}</color>");
                for (ushort t = (ushort)(goodTick + 1); t < cmd.frameNumber; t++)
                {
                    Predict(t); //走到验证错误，说明本方操作已经存进去了。只需要预测对方即可。
                }
            }


            //④帧传给逻辑层，推进

            //客户端推进帧，要走(cmd.frameNumber)这一帧了，判断是否预测。
            if (ggpo_recieve.ContainsKey(cmd.frameNumber) == false)
            {
                //没有帧，走预测
                ggpo_predict[cmd.frameNumber] = new uint[2];
                ggpo_predict[cmd.frameNumber][0] = cmd.input; //本地帧塞进去
                Predict(cmd.frameNumber);

                // 快照
                //op = new S2C_AllPlayerOperationPacket
                //{
                //    ServerTick = myPacket.Tick,
                //    HostOperation = ggpo_predict[myPacket.Tick][1],
                //    GuestOperation = ggpo_predict[myPacket.Tick][2],
                //};
                //Snapshot(op);
            }
        }
        // 预测
        private void Predict(uint tick)
        {
            uint lastTick = (uint)(tick - 1);
            var remoteInput = (ggpo_predict.ContainsKey(lastTick) == false) ? (uint)0 : ggpo_predict[lastTick][1]; //取上一帧作为预测
            ggpo_predict[tick][1] = remoteInput;
            Debug.Log($"<color=blue>预测第{tick}帧，远程操作是{remoteInput}</color>");
            //预测完成后，让角色跑预测帧。
            var _ops = ggpo_predict[tick];
            Process(_ops);
        }
        // 回滚
        private void Rollback(uint tick)
        {
            /*
            GameState shot = null;
            shot = storeBuffer[tick];

            var role1 = GetRole(1);
            var role2 = GetRole(2);
            role1.RollBack(shot.role1);
            role2.RollBack(shot.role2);
            for (int i = 0; i < dic_bullet.Count; i++)
            {
                var bullet = dic_bullet[i];
                bullet.RollBack(shot.bullets[i]);
            }
            Debug.Log($"<color=#9500FF>快照退回到第{tick}帧</color>");
            */
        }
        // 追帧
        private void Process(uint[] inputs) //双方操作
        {
            Debug.Log($"FixedUpdate: <color=yellow>{LocalSession.gs.frameNumber}</color>");
            runner.OnFixedUpdate(inputs);
        }
    }
}