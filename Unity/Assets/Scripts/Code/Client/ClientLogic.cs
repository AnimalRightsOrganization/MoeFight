using System.Collections.Generic;
using UnityEngine;
using Code.Shared;
using LiteNetLib;
using LiteNetLib.Utils;
using HotFix;

namespace Code.Client
{
    public class ClientLogic : MonoBehaviour
    {
        static ClientLogic _instance;
        public static ClientLogic Get
        {
            get
            {
                if (_instance == null)
                    _instance = FindObjectOfType<ClientLogic>();
                return _instance;
            }
        }

        public bool IsStart;
        [SerializeField] uint DELAY_FRAMES = 0;
        [SerializeField] uint sendTick;
        [SerializeField] uint recvTick;
        [SerializeField] uint rendTick;
        private Dictionary<uint, uint[]> ggpo_predict; //预测帧<帧号, 双方操作[]>
        private Dictionary<uint, uint[]> ggpo_recieve; //下发帧<帧号, 双方操作[]>
        private Dictionary<uint, byte[]> cache_buffer; //快照帧<帧号, 场景缓存[]>
        private List<uint> predicted;
        private HitstunRunner runner;

        [SerializeField] int mySeatId;
        [SerializeField] int remoteSeatId;
        [SerializeField] BattleMode myBattleMode;
        private ReplayFormat repInfo;

        #region 内置函数
        void Awake()
        {
            IsStart = false;
            sendTick = 0;
            recvTick = 0;
            rendTick = 0;
            ggpo_predict = new Dictionary<uint, uint[]>(); //4294967295 /50帧每秒 = 85,899,346秒 = 23,860小时 = 994天。4+4+4=12个字节
            ggpo_recieve = new Dictionary<uint, uint[]>();
            cache_buffer = new Dictionary<uint, byte[]>();
            predicted = new List<uint>();
            runner = FindObjectOfType<HitstunRunner>();
            var clientRoom = ClientNet.Get.m_ClientRoom;
            if (clientRoom != null)
            {
                runner.player1Character = (HitstunConstants.CharacterName)clientRoom.HostPlayer.RoleIndex;
                runner.player2Character = (HitstunConstants.CharacterName)clientRoom.GuestPlayer.RoleIndex;
                //Debug.Log($"Awake.p1:{runner.player1Character} vs p2:{runner.player2Character}");

                mySeatId = ClientNet.Get.m_PlayerManager.LocalPlayer.SeatId;
                remoteSeatId = (mySeatId + 1) % 2;
                myBattleMode = clientRoom.BattleMode;
                repInfo = ReplayManager.data;
            }

            gameObject.AddComponent<ClientDebug>();
        }
        void OnEnable()
        {
            EventManager.RegisterEvent(OnNetCallback);
        }
        void OnDisable()
        {
            EventManager.UnRegisterEvent(OnNetCallback);
        }
        void FixedUpdate()
        {
            if (!IsStart) return;

            switch (myBattleMode)
            {
                case BattleMode.Training:
                case BattleMode.Matching:
                    BattleLoop();
                    break;
                case BattleMode.Replay:
                    ReplayLoop();
                    break;
            }
        }
        void BattleLoop()
        {
            //①收集本地按键，发送，预测?
            sendTick++;
            uint input = LocalSession.ReadInputs();
            var cmd = new C2S_InputPacket { frameNumber = sendTick, input = input };
            ClientNet.Get.SendInput(cmd);
            ggpo_predict[sendTick] = new uint[2];
            ggpo_predict[sendTick][mySeatId] = input;
            //Debug.Log($"发送: {sendTick}---{input}");

            //②Delay-Based，要求自己也延迟。
            for (int i = (int)rendTick + 1; i < (int)sendTick - DELAY_FRAMES; i++)
            {
                rendTick = (uint)i;

                //本次Update要求表现的帧，判断是否收到
                if (ggpo_recieve.ContainsKey(rendTick))
                {
                    //因为延迟表现，此时收到了，取出来表现
                    //Debug.Log($"延迟足够，表现{rendTick}");
                    var _inputs = ggpo_recieve[rendTick];
                    Process(rendTick, _inputs);
                }
                else
                {
                    //延迟不够，还未收到，预测。标记为是预测的。
                    //Debug.Log($"延迟不够，发送{sendTick}时，表现{rendTick}，收到{recvTick}");
                    Predict(rendTick);
                    predicted.Add(rendTick);
                }
            }


            //③处理所有新收到的帧
            for (int x = (int)recvTick + 1; x < ggpo_recieve.Count; x++)
            {
                uint i = (uint)x;
                recvTick = i;

                //如果这帧之前是预测的，对比，回滚
                bool needToVerity = predicted.Contains(i);
                if (needToVerity)
                {
                    //Debug.Log($"预测过{i}，需要验证。{ggpo_predict.Count}");
                    //之前标记为预测，判断预测是否准确

                    uint recieve1 = ggpo_recieve[i][0];
                    uint recieve2 = ggpo_recieve[i][1];
                    uint predict1 = ggpo_predict[i][0];
                    uint predict2 = ggpo_predict[i][1];
                    if (recieve1.Equals(predict1) && recieve2.Equals(predict2))
                    {
                        //之前的预测准确。不用更新表现了，预测时已经走过表现逻辑。
                        recvTick = i;
                    }
                    else
                    {
                        // 验证失败
                        uint badTick = i;

                        // 一次性回滚到最早发生错误的地方。
                        Debug.LogError($"{badTick}预测错({recieve1}:{recieve2})，回滚");
                        Rollback(badTick - 1);

                        //追帧到当前渲染帧。
                        Debug.Log($"<color=yellow>追帧，覆盖错误的预测: {badTick}~{rendTick}</color>");
                        for (uint t = badTick; t <= rendTick; t++)
                        {
                            if (rendTick <= ggpo_recieve.Count)
                            {
                                uint[] _inputs = ggpo_recieve[t];
                                ggpo_predict[t] = _inputs;
                                Process(t, _inputs);
                            }
                            else
                            {
                                //recv集合中数据不够了，继续预测
                                Predict(t);
                            }
                        }

                        break; //跳出循环
                    }
                }
            }

            CheckGameEnd();
        }
        void ReplayLoop()
        {
            if (repInfo == null || repInfo.inputs.Count <= recvTick)
                return;

            recvTick++;
            uint[] inputs = repInfo.inputs[recvTick];
            runner.OnReplayUpdate(inputs);

            BattleEvent.doReplayUpdate?.Invoke(recvTick);
        }
        #endregion

        #region 战斗系统
        private void Predict(uint tick)
        {
            uint remoteInput = (ggpo_recieve.Count == 0) ? 0 : ggpo_recieve[(uint)ggpo_recieve.Count][remoteSeatId];
            var _inputs = ggpo_predict[tick];
            _inputs[remoteSeatId] = remoteInput;
            //Debug.Log($"<color=blue>预测第{tick}帧，远程操作是{remoteInput}</color>");

            //预测完成后，让角色跑预测帧。
            Process(tick, _inputs);
        }
        private void Rollback(uint tick)
        {
            GameState.FromByteArray(LocalSession.gs, cache_buffer[tick]);
            Debug.Log($"回滚到第{tick}帧" +
                $"\nP1:{LocalSession.gs.characters[0].position}---hp:{LocalSession.gs.characters[0].health}" +
                $"\nP2:{LocalSession.gs.characters[1].position}---hp:{LocalSession.gs.characters[1].health}");
        }
        private void Process(uint tick, uint[] inputs) //双方操作
        {
            runner.SaveOldBuffer();
            LocalSession.RunFrame(inputs);
            runner.OnFixedUpdate(inputs);
            //Debug.Log($"执行完第{tick}帧执行后, P1:{LocalSession.gs.characters[0].position}, P2:{LocalSession.gs.characters[1].position}");

            Snapshot(tick);
        }
        private void Snapshot(uint tick)
        {
            //Debug.Log($"快照: {tick}");
            cache_buffer[tick] = GameState.ToByteArray(LocalSession.gs);
        }
        private void CheckGameEnd()
        {
            if (BattleEvent.doSetGameEnd == null) return;

            int passedTime = (int)(rendTick * Time.fixedDeltaTime);
            int leftTime = Mathf.Max(ConstValue.TOTAL_SECOND - passedTime, 0);
            BattleEvent.doSetTimeText?.Invoke($"{leftTime}");

            if (passedTime >= ConstValue.TOTAL_SECOND)
            {
                BattleEvent.doSetGameEnd.Invoke(2);
            }
        }
        #endregion

        #region 回放系统
        public void InitReplay()
        {
            // 所有帧跑一遍，生成快照
            for (int i = 1; i <= repInfo.inputs.Count; i++)
            {
                uint tick = (uint)i;
                var inputs = repInfo.inputs[tick];
                ProcessReplay(tick, inputs);
            }
            // 返回第一帧
            RollbackReplay(1);

            // 血条
            int hp1 = LocalSession.gs.characters[0].health;
            int hp2 = LocalSession.gs.characters[1].health;
            BattleEvent.doSetCurrentHp?.Invoke(1, hp1);
            BattleEvent.doSetCurrentHp?.Invoke(2, hp2);
        }
        public void PlayReplay()
        {
            BattleEvent.doSetAnimeSpeed?.Invoke(1f);
            IsStart = true;
        }
        public void PauseReplay()
        {
            BattleEvent.doSetAnimeSpeed?.Invoke(0);
            IsStart = false;
        }
        public void RollbackReplay(uint tick)
        {
            recvTick = tick;
            uint lastTick = (tick == 1) ? 1 : tick - 1;
            uint[] inputs = (tick == 1) ? new uint[2] { 0, 0 } : repInfo.inputs[tick];
            Rollback(lastTick);
            ProcessReplay(tick, inputs);
        }
        public void ProcessReplay(uint tick, uint[] inputs)
        {
            runner.OnReplayUpdate(inputs);
            Snapshot(tick);
        }
        #endregion

        #region 网络消息
        private void OnNetCallback(PacketType eventID, INetSerializable reader, NetPeer peer)
        {
            switch (eventID)
            {
                case PacketType.S2C_TestPVE:
                    OnTestPVE(reader);
                    break;
                case PacketType.S2C_TestPVP:
                    OnTestPVP(reader);
                    break;
                case PacketType.S2C_Input:
                    OnRecvInput(reader);
                    break;
                case PacketType.S2C_BattleStart:
                    OnBattleStart(reader);
                    break;
                case PacketType.S2C_BattlePause:
                    OnBattlePause(reader);
                    break;
                case PacketType.S2C_BattleEnd: //断线/主动认输/游戏结果上报
                    OnBattleEnd(reader);
                    break;
            }
        }
        private void OnTestPVE(INetSerializable reader)
        {
            var packet = (S2C_JoinResultPacket)reader;
            Debug.Log($"[S2C] 单人测试: code={packet.Code}, peerid={packet.HostId}, {packet.HostName}");
            if (packet.Code == 0)
            {
                UIManager.Get().Push<UI_GameMenu>();
                IsStart = true;
            }
        }
        private void OnTestPVP(INetSerializable reader)
        {
            var packet = (S2C_JoinResultPacket)reader;
            Debug.Log($"[S2C] 双人测试: code={packet.Code}, peerid={packet.HostId}, {packet.HostName}");
            if (packet.Code == 0)
            {
                IsStart = true;
            }
        }
        private void OnRecvInput(INetSerializable reader)
        {
            var packet = (S2C_InputPacket)reader;

            uint server_tick = packet.frameNumber;
            ggpo_recieve[server_tick] = packet.inputs;
            //Debug.Log($"<color=grey>---收到第{server_tick}帧</color>");
        }
        private void OnBattleStart(INetSerializable reader)
        {
            var packet = (S2C_BattleStartPacket)reader;
            Debug.Log($"[C] 战斗开始, 阶段: {packet.Stage}");

            if (packet.Stage == 0) //场景加载完成上报，服务器集齐后下发
            {
                //UI：3、2、1
            }
            else if (packet.Stage == 1) //倒计时完成上报，服务器集齐后下发
            {
                IsStart = true; //开始发送帧数据
            }
            else if (packet.Stage == 2) //从暂停恢复
            {
                IsStart = true;
            }
        }
        private void OnBattlePause(INetSerializable reader)
        {
            IsStart = false;
        }
        private void OnBattleEnd(INetSerializable reader)
        {
            Debug.Log("OnBattleEnd: save replay");
            var packet = (S2C_BattleEndPacket)reader;

            var clientRoom = ClientNet.Get.m_ClientRoom;
            var hostPlayer = clientRoom.HostPlayer;
            var guestPlayer = clientRoom.GuestPlayer;
            var scene = new S2C_LoadScenePacket
            {
                RoomId = (short)clientRoom.RoomID,
                BattleId = clientRoom.BattleID,
                MapId = clientRoom.MapId,
                Host = new PlayerLoadPacket { RoleIndex = hostPlayer.RoleIndex, UserName = hostPlayer.UserName },
                Guest = new PlayerLoadPacket { RoleIndex = guestPlayer.RoleIndex, UserName = guestPlayer.UserName },
            };
            var rep = new ReplayFormat { scene = scene, battleMode = (byte)clientRoom.BattleMode, winnerId = packet.WinnerSeatId, inputs = ggpo_recieve };
            ReplayManager.SaveReplay(rep);

            //IsStart = false;
            //Application.targetFrameRate = 15;
            //Time.fixedDeltaTime = 1f / 15;
        }
        #endregion
    }
}