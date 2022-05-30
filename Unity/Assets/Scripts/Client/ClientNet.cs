using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using Code.Shared;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

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
            IsStart = false;
            sendTick = 0;
            recvTick = 0;
            rendTick = 0;
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
        }
        #endregion

        public uint DELAY_FRAMES = 0;
        public bool IsStart;
        public uint sendTick;
        public uint recvTick;
        public uint rendTick;
        private Dictionary<uint, uint[]> ggpo_predict; //预测帧
        private Dictionary<uint, uint[]> ggpo_recieve; //下发帧
        private Queue<C2S_InputPacket> dic_delay; //延迟帧
        public HitstunRunner runner;

        void FixedUpdate()
        {
            if (!IsStart) return;


            //①收集本地按键，发送，预测
            sendTick++;
            uint[] inputs = LocalSession.CreateInputs();
            uint input = inputs[0];
            var cmd = new C2S_InputPacket { frameNumber = sendTick, input = input };
            SendInput(cmd);
            ggpo_predict[sendTick] = new uint[2];
            ggpo_predict[sendTick][0] = inputs[0];


            //②Delay-Based，要求自己也延迟。
            for (int i = (int)rendTick + 1; i < (int)sendTick - DELAY_FRAMES; i++)
            {
                rendTick = (uint)i; //本次Update要求表现的帧，不一定收到了

                //send=20，recv=15，delay=10(DELAY_FRAMES)。要求表现第10帧，剩余的存着。
                //send=20，recv=15，delay=2。要求表现第18帧，不够，则要预测。
                if (ggpo_recieve.ContainsKey(rendTick))
                {
                    //因为延迟表现，此时收到了，取出来表现
                    var _inputs = ggpo_recieve[rendTick];
                    Process(_inputs);
                }
                else
                {
                    //延迟不够，还未收到，预测。标记为是预测的。
                    Predict(rendTick);
                }
            }


            //③处理所有新收到的帧
            for (uint i = recvTick + 1; i < ggpo_recieve.Count; i++)
            {
                //如果这帧之前是预测的，对比，回滚
                bool needToVerity = ggpo_predict.ContainsKey(i);
                if (needToVerity)
                {
                    //之前标记为预测，判断预测是否准确

                    var recieve1 = ggpo_recieve[i][0];
                    var recieve2 = ggpo_recieve[i][1];
                    var predict1 = ggpo_predict[i][0];
                    var predict2 = ggpo_predict[i][1];
                    if (recieve1.Equals(predict1) && recieve2.Equals(predict2))
                    {
                        //之前的预测准确。不用更新表现了，预测时已经走过表现逻辑。
                        recvTick = i;
                    }
                    else
                    {
                        //verity = false;
                        uint badTick = i;
                        Debug.Log($"预测错误：\nP1:{predict1}\nP2:{predict2}");


                        // 验证失败处理
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

                        break; //跳出循环
                    }
                }
            }
        }
        // 预测
        private void Predict(uint tick)
        {
            uint lastTick = tick - 1;
            //uint[] try_inputs;
            //if (ggpo_predict.TryGetValue(lastTick, out try_inputs) == false)
            //{
            //    ggpo_predict[lastTick] = new uint[2];
            //    Debug.Log($"没有上一帧：{lastTick}，创建{ggpo_predict[lastTick][0]}, {ggpo_predict[lastTick][1]}");
            //}

            uint remoteInput = (ggpo_predict.ContainsKey(lastTick) == false) ? 0 : ggpo_predict[lastTick][1]; //取上一帧作为预测
            ggpo_predict[tick][1] = remoteInput;
            Debug.Log($"<color=blue>预测第{tick}帧，远程操作是{remoteInput}</color>");

            //预测完成后，让角色跑预测帧。
            var _inputs = ggpo_predict[tick];
            Process(_inputs);
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
            runner.SaveOldBuffer();
            LocalSession.RunFrameNext(inputs);
            runner.OnFixedUpdate(inputs);
        }
    }
}