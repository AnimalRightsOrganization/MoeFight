using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using Code.Shared;
using LiteNetLib;
using LiteNetLib.Utils;
using HitstunConstants;

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

        // 线程中运行，编辑器暂停时无法停止
        // 为了保证真机切后台，依然正常运行
        public static LogicTimer LogicTimer { get; private set; }

        private void HandleDevKeys()
        {
            // quit
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Application.Quit();
            }
            // toggle hitboxes
            if (Input.GetKeyDown(KeyCode.F1))
            {
                runner.showHitboxes = !runner.showHitboxes;
                if (runner.showHitboxes)
                {
                    Debug.Log("Hitboxes ON");
                }
                else
                {
                    Debug.Log("Hitboxes OFF");
                }
            }
            // pause / running
            if (Input.GetKeyDown(KeyCode.F2))
            {
                if (IsStart == true)
                    PauseLoop();
                else
                    PlayLoop();
                Debug.Log($"paused: {!IsStart}");
            }
            // hp recover
            if (Input.GetKeyDown(KeyCode.F3))
            {
                DebugHeal();
            }
            // pause
            if (Input.GetKeyDown(KeyCode.F10))
            {
                DebugStop();
            }
            // resume
            if (Input.GetKeyDown(KeyCode.F11))
            {
                DebugStart();
            }
            // frame by frame
            if (Input.GetKeyDown(KeyCode.F12))
            {
                DebugStep();
            }
        }
        private void DebugHeal()
        {
            if (m_ClientRoom.BattleMode == BattleMode.Training)
            {
                for (int i = 0; i < Constants.NUM_PLAYERS; i++)
                {
                    LocalSession.gs.characters[i].health = 1000;
                    BattleEvent.doSetCurrentHp.Invoke(i, 1000);
                }
            }
        }
        private void DebugStop()
        {
            PauseLoop();
            LogicTimer.Stop();
        }
        private void DebugStart()
        {
            PlayLoop();
            LogicTimer.Start();
        }
        private async void DebugStep()
        {
            LogicTimer.Start();
            PlayLoop();
            await Task.Delay((int)(LogicTimer.FixedDelta * 1000));
            PauseLoop();
            LogicTimer.Stop();
        }

        // Timeline
        public async void Opening()
        {
            runner.showHitboxes = false;
            for (int i = 0; i < Constants.NUM_PLAYERS; ++i)
            {
                runner.characterViews[i].showHitboxes = false;
                runner.characterViews[i].UpdateCharacterView(LocalSession.gs.characters[i]);
                await Task.Delay(1);
            }

            await Opening_i(0);
            await Opening_i(1);
        }
        async Task Opening_i(int i)
        {
            var opening = runner.characterViews[i].GetDirector("Opening");
            int duration = (int)(opening.duration * 1000);
            if (i == 0)
                duration = duration - 1000;
            else
                duration = duration - 100; //早点发开始，避免卡顿？
            opening.Play();
            Debug.Log($"opening_{i}: {duration}, {m_ClientRoom.BattleStage}");
            await Task.Delay(duration);

            if (i == 1)
            {
                runner.showHitboxes = true;
                if (m_ClientRoom.BattleMode == BattleMode.Matching)
                {
                    ClientNet.Get.SendBattleStart(0); //切换场景完，同步
                    Debug.Log(System.DateTime.Now.ToString("HH:mm:ss.fff"));
                }
                else
                {
                    PlayLoop();
                }
            }
        }


        public bool IsStart; //running
        [SerializeField] uint DELAY_FRAMES = 0;
        [SerializeField] uint sendTick;
        [SerializeField] uint recvTick;
        [SerializeField] uint rendTick;
        private Dictionary<uint, uint[]> ggpo_predict; //预测帧<帧号, 双方操作[2]>
        private Dictionary<uint, uint[]> ggpo_recieve; //下发帧<帧号, 双方操作[2]>
        private Dictionary<uint, byte[]> cache_buffer; //快照帧<帧号, 场景缓存[2]>
        private List<uint> predicted;
        public HitstunRunner runner;

        // 缓存变量
        private ClientRoom _clientRoom;
        private ClientRoom m_ClientRoom
        {
            get
            {
                if (_clientRoom == null)
                    _clientRoom = ClientNet.Get.m_ClientRoom;
                return _clientRoom;
            }
        }
        private BattleMode m_BattleMode;
        private int localSeatId;
        private int remoteSeatId;
        private ReplayFormat repInfo;

        #region 内置函数
        void Awake()
        {
            LogicTimer = new LogicTimer(OnLogicUpdate);

            IsStart = false;
            sendTick = 0;
            recvTick = 0;
            rendTick = 0;
            ggpo_predict = new Dictionary<uint, uint[]>(); //4294967295 /50帧每秒 = 85,899,346秒 = 23,860小时 = 994天。4+4+4=12个字节
            ggpo_recieve = new Dictionary<uint, uint[]>();
            cache_buffer = new Dictionary<uint, byte[]>();
            predicted = new List<uint>();
            runner = FindObjectOfType<HitstunRunner>();
            if (m_ClientRoom != null)
            {
                runner.player1Character = (CharacterName)m_ClientRoom.HostPlayer.RoleIndex;
                runner.player2Character = (CharacterName)m_ClientRoom.GuestPlayer.RoleIndex;
                //Debug.Log($"Awake.p1:{runner.player1Character} vs p2:{runner.player2Character}");

                localSeatId = ClientNet.Get.m_PlayerManager.LocalPlayer.SeatId;
                remoteSeatId = (localSeatId + 1) % 2;
                m_BattleMode = m_ClientRoom.BattleMode;
                repInfo = ReplayManager.data;
            }

            //BindSignal();

            gameObject.AddComponent<ClientDebug>();
        }
        void OnEnable()
        {
            EventManager.RegisterEvent(OnNetCallback);
            LogicTimer.Start();
        }
        void OnDisable()
        {
            EventManager.UnRegisterEvent(OnNetCallback);
            LogicTimer.Stop();
        }
        void Update()
        {
            // handles function key debugging inputs
            HandleDevKeys();

            LogicTimer.Update();
        }
        void OnApplicationPause(bool pause)
        {
            Debug.Log($"<color=green>OnApplicationPause: {pause}</color>");
            if (pause)
            {
                ClientNet.Get.SendBattlePause(); //掉线处理
            }
            else
            {
                ClientNet.Get.SendBattleStart(2); //断线重连
            }
        }
        #endregion

        #region 战斗系统
        void OnLogicUpdate()
        {
            if (m_ClientRoom.BattleStage == BattleStage.End)
            {
                // 保证动画播放完
                var inputs = new uint[] { 0, 0 };
                runner.SaveOldBuffer();
                LocalSession.RunFrame(inputs);
                runner.OnFixedUpdate(inputs); //游戏结束，动画不停
                return;
            }

            if (!IsStart) return;

            switch (m_BattleMode)
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
            //①收集本地按键，发送。
            sendTick++;

            //uint input = LocalSession.ReadInputs();
            //ggpo_predict[sendTick] = new uint[2];
            //ggpo_predict[sendTick][localSeatId] = input;
            //Debug.Log($"发送: {sendTick}---{input}");
            uint[] inputs = CollectInputs(sendTick);
            uint input = inputs[0];

            var cmd = new C2S_InputPacket
            {
                frameNumber = sendTick,
                input = input,
            };
            ClientNet.Get.SendInput(cmd);

            //②Delay-Based，本地模拟延迟。
            for (int i = (int)rendTick + 1; i < (int)sendTick - DELAY_FRAMES; i++)
            {
                rendTick = (uint)i;

                //本次Update要求表现的帧，判断是否收到
                if (ggpo_recieve.ContainsKey(rendTick))
                {
                    //因为延迟表现，此时收到了，取出来表现
                    //Debug.Log($"延迟足够，发送{sendTick}时，表现{rendTick}");
                    var _inputs = ggpo_recieve[rendTick];
                    Process(rendTick, _inputs);
                }
                else
                {
                    //延迟不够，还未收到，预测。标记为是预测的。
                    //Debug.Log($"[渲染] 延迟不够，发送{sendTick}时，表现{rendTick}，收到{recvTick}");
                    Predict(rendTick);
                    predicted.Add(rendTick);
                }
            }


            //③处理所有新收到的帧，校验回滚
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
                        Debug.LogError($"{badTick}预测错({recieve1}:{predict1})({recieve2}:{predict2})，回滚到{badTick - 1}");
                        Rollback(badTick - 1);

                        //追帧到当前渲染帧。
                        Debug.Log($"<color=yellow>追帧，覆盖错误的预测: {badTick}~{rendTick}({rendTick - badTick}个)</color>");
                        for (uint t = badTick; t <= rendTick; t++)
                        {
                            if (t <= ggpo_recieve.Count)
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

            //④检查游戏结束
            CheckGameEnd();
        }
        void ReplayLoop()
        {
            if (repInfo == null || repInfo.inputs.Count <= recvTick)
            {
                //Debug.LogError($"{repInfo == null} || {repInfo.inputs.Count} <= {recvTick}");
                return;
            }

            recvTick++;
            uint[] inputs = repInfo.inputs[recvTick];
            runner.OnReplayUpdate(inputs);

            //Debug.Log($"ReplayLoop: {recvTick}");
            BattleEvent.doReplayUpdate?.Invoke(recvTick);
        }
        public void PlayLoop()
        {
            IsStart = true; //播放
            BattleEvent.doSetAnimeSpeed?.Invoke(0);
            m_ClientRoom.SetStage(BattleStage.Running);
        }
        public void PauseLoop()
        {
            IsStart = false; //暂停
            BattleEvent.doSetAnimeSpeed?.Invoke(0);
            m_ClientRoom.SetStage(BattleStage.Paused);
        }

        public Queue<uint> custom = new Queue<uint>();
        uint[] CollectInputs(uint server_tick)
        {
            uint[] inputs;

            if (m_ClientRoom.BattleMode == BattleMode.Matching)
            {
                uint input = LocalSession.ReadInputs();
                ggpo_predict[sendTick] = new uint[2];
                ggpo_predict[sendTick][localSeatId] = input;
                inputs = new uint[] { input };
            }
            else
            {
                // 训练或调试，机器人接受实时指令
                uint input_0 = LocalSession.ReadInputs(); //此模式玩家只能是〇号位
                uint input_1 = custom.Count > 0 ? custom.Dequeue() : 0;
                inputs = new uint[] { input_0, input_1 };
                ggpo_recieve[server_tick] = inputs;
                ggpo_predict[server_tick] = inputs;
            }
            return inputs;
        }

        private void Predict(uint tick)
        {
            uint remoteInput = (ggpo_recieve.Count == 0) ? 0 : ggpo_recieve[(uint)ggpo_recieve.Count][remoteSeatId];
            var _inputs = ggpo_predict[tick];
            _inputs[remoteSeatId] = remoteInput;
            //Debug.Log($"<color=blue>[预测] 第{tick}帧: ({_inputs[0]})({_inputs[1]})</color>");

            //预测完成后，让角色跑预测帧。
            Process(tick, _inputs);
        }
        private void Rollback(uint tick)
        {
            GameState.FromByteArray(LocalSession.gs, cache_buffer[tick]);
            Debug.Log($"[回滚] 到第{tick}帧" +
                $"\nP1:{LocalSession.gs.characters[0].position}---hp:{LocalSession.gs.characters[0].health}" +
                $"\nP2:{LocalSession.gs.characters[1].position}---hp:{LocalSession.gs.characters[1].health}");
        }
        public void Process(uint tick, uint[] inputs) //双方操作
        {
            runner.SaveOldBuffer();
            LocalSession.RunFrame(inputs);
            runner.OnFixedUpdate(inputs);//推进逻辑
            //Debug.Log($"[执行] 第{tick}帧执行后, P1:{LocalSession.gs.characters[0].position}, P2:{LocalSession.gs.characters[1].position}");

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
            if (m_ClientRoom.BattleMode == BattleMode.Training) return;

            int passedTime = (int)(rendTick * Time.fixedDeltaTime);
            int leftTime = Mathf.Max(ConstValue.TOTAL_SECOND - passedTime, 0);
            BattleEvent.doSetTimeText?.Invoke($"{leftTime}");

            if (passedTime >= ConstValue.TOTAL_SECOND)
            {
                BattleEvent.doSetGameEnd.Invoke(0);
            }
        }
        // 重连恢复数据
        public void Reconnect(S2C_LackInputPacket packet)
        {
            //var speed = runner.characterViews[0].animator.speed;
            Debug.Log($"追帧模拟: IsStart:{IsStart}, " +
                //$"speed:{speed}" +
                $"\n服务器收到: {packet.frameNumber}" +
                $"\nggpo_predict:{ggpo_predict.Count}, ggpo_recieve:{ggpo_recieve.Count}, cache_buffer:{cache_buffer.Count}");

            IsStart = false;
            // 客户端发的一定＞服务器收的，所以
            // ①预测客户端发的帧数
            // ②通过请求对方，得知对方当前sendTick
            for (int i = 1; i < packet.frameNumber; i++)
            {
                S2C_InputPacket resp = packet.inputs[i];
                Process(resp.frameNumber, resp.inputs);

                ggpo_predict[resp.frameNumber] = resp.inputs;
                ggpo_recieve[resp.frameNumber] = resp.inputs;
            }
            sendTick = packet.frameNumber; //+ DELAY_FRAMES;
            recvTick = packet.frameNumber;
            rendTick = packet.frameNumber; //客户端追帧表现到这帧
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
            BattleEvent.doSetCurrentHp?.Invoke(0, hp1);
            BattleEvent.doSetCurrentHp?.Invoke(1, hp2);
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
        private void OnRecvInput(INetSerializable reader)
        {
            var packet = (S2C_InputPacket)reader;
            uint server_tick = packet.frameNumber;
            ggpo_recieve[server_tick] = packet.inputs;
            //Debug.Log($"<color=grey> << 收到: {server_tick}---({packet.inputs[0]})({packet.inputs[1]}) << </color>");
        }
        private void OnBattleStart(INetSerializable reader)
        {
            var packet = (S2C_BattleStartPacket)reader;
            Debug.Log($"[C] 战斗开始, 阶段: {packet.Stage}, 此时比赛: {IsStart}");

            if (packet.Stage == 0) //场景加载完同步
            {
                //UI: 3,2,1,Start
            }
            else if (packet.Stage == 1) //倒计时完同步
            {
                PlayLoop(); //开始发送帧数据
            }
            else if (packet.Stage == 2)
            {
                PlayLoop(); //从暂停恢复
            }
        }
        private void OnBattlePause(INetSerializable reader)
        {
            var packet = (S2C_BattlePausePacket)reader;
            Debug.Log($"{packet.SeatID}提出暂停: {packet.Duration}s");
            if (packet.Duration > 0)
            {
                PauseLoop();
            }
        }
        private void OnBattleEnd(INetSerializable reader)
        {
            var packet = (S2C_BattleEndPacket)reader; //训练结束没有此消息

            IsStart = false;
            m_ClientRoom.SetStage(BattleStage.End);

            var hostPlayer = m_ClientRoom.HostPlayer;
            var guestPlayer = m_ClientRoom.GuestPlayer;
            var scene = new S2C_LoadScenePacket
            {
                RoomId = (short)m_ClientRoom.RoomID,
                BattleId = m_ClientRoom.BattleID,
                MapId = m_ClientRoom.MapId,
                Host = new PlayerLoadPacket { RoleIndex = hostPlayer.RoleIndex, UserName = hostPlayer.UserName },
                Guest = new PlayerLoadPacket { RoleIndex = guestPlayer.RoleIndex, UserName = guestPlayer.UserName },
            };
            var rep = new ReplayFormat { scene = scene, battleMode = (byte)m_ClientRoom.BattleMode, winnerId = packet.WinnerSeatId, inputs = ggpo_recieve };
            ReplayManager.SaveReplay(rep);
            Debug.Log("OnBattleEnd: save replay");
        }
        #endregion
    }
}