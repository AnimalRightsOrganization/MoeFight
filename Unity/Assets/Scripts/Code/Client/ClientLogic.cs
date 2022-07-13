using System.Collections.Generic;
using UnityEngine;
using Code.Shared;
using LiteNetLib;
using LiteNetLib.Utils;

namespace Code.Client
{
    public class ClientLogic : MonoBehaviour
    {
        private int mySeatId;
        private int remoteSeatId;

        public uint DELAY_FRAMES = 0;
        public bool IsStart;
        public uint sendTick;
        public uint recvTick;
        public uint rendTick;
        public Dictionary<uint, uint[]> ggpo_predict; //预测帧<帧号, 双方操作[]>
        public Dictionary<uint, uint[]> ggpo_recieve; //下发帧<帧号, 双方操作[]>
        public Dictionary<uint, byte[]> cache_buffer; //快照帧<帧号, 场景缓存[]>
        private List<uint> predicted;
        private HitstunRunner runner;

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
            if (ClientNet.Get.m_ClientRoom != null)
            {
                runner.player1Character = (HitstunConstants.CharacterName)ClientNet.Get.m_ClientRoom.HostPlayer.RoleIndex;
                runner.player2Character = (HitstunConstants.CharacterName)ClientNet.Get.m_ClientRoom.GuestPlayer.RoleIndex;
                Debug.Log($"Awake.p1:{runner.player1Character} vs p2:{runner.player2Character}");
            }

            EventManager.RegisterEvent(OnNetCallback);
        }

        void OnGUI()
        {
            var char0 = LocalSession.gs.characters[0];
            var data0 = LocalSession.gs.characterDatas[0];
            var currentState0 = char0.state;
            var currentAnimation0 = char0.isAttacking() ? data0.attacks[currentState0.ToString()] : data0.animations[currentState0.ToString()];
            int currentFrame0 = (int)char0.framesInState % currentAnimation0.totalFrames;

            var char1 = LocalSession.gs.characters[1];
            var data1 = LocalSession.gs.characterDatas[1];
            var currentState1 = char1.state;
            var currentAnimation1 = char1.isAttacking() ? data1.attacks[currentState1.ToString()] : data1.animations[currentState1.ToString()];
            int currentFrame1 = (int)char1.framesInState % currentAnimation1.totalFrames;

            string log = $"Tick: {LocalSession.gs.frameNumber}" +
                $"\nping: {ClientNet.Get._ping}";
            GUI.Label(new Rect(10, 10, 100, 50), log, style1);


            string state1 = $"{currentState0}: {currentFrame0}";
            GUI.Label(new Rect(posX1, posY, 100, 50), state1, style1);
            string state2 = $"{currentState1}: {currentFrame1}";
            GUI.Label(new Rect(posX2, posY, 100, 50), state2, style1);
        }

        void FixedUpdate()
        {
            if (!IsStart) return;

            //①收集本地按键，发送，预测?
            sendTick++;
            uint input = LocalSession.GetInput();
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

        // 预测
        private void Predict(uint tick)
        {
            uint remoteInput = (ggpo_recieve.Count == 0) ? 0 : ggpo_recieve[(uint)ggpo_recieve.Count][remoteSeatId];
            var _inputs = ggpo_predict[tick];
            _inputs[remoteSeatId] = remoteInput;
            //Debug.Log($"<color=blue>预测第{tick}帧，远程操作是{remoteInput}</color>");

            //预测完成后，让角色跑预测帧。
            Process(tick, _inputs);
        }
        // 回滚
        private void Rollback(uint tick)
        {
            GameState.FromByteArray(LocalSession.gs, cache_buffer[tick]);
            Debug.Log($"回滚到第{tick}帧状态: P1:{LocalSession.gs.characters[0].position}, P2:{LocalSession.gs.characters[1].position}");
        }
        // 追帧
        private void Process(uint tick, uint[] inputs) //双方操作
        {
            runner.SaveOldBuffer();
            LocalSession.RunFrameNext(inputs);
            runner.OnFixedUpdate(inputs);
            //Debug.Log($"执行完第{tick}帧执行后: P1:{LocalSession.gs.characters[0].position}, P2:{LocalSession.gs.characters[1].position}");

            Snapshot(tick);
        }
        // 快照
        private void Snapshot(uint tick)
        {
            //Debug.Log($"快照: {tick}");
            cache_buffer[tick] = GameState.ToByteArray(LocalSession.gs);
        }

        // 结束判定（①时间，②血量）
        private void CheckGameEnd()
        {
            int passedTime = (int)(rendTick * Time.fixedDeltaTime);
            int leftTime = Mathf.Max(ConstValue.TOTAL_SECOND - passedTime, 0);
            HotFix.UIManager.doSetTimeText?.Invoke($"{leftTime}");
            if (passedTime >= ConstValue.TOTAL_SECOND)
            {
                HotFix.UIManager.doSetGameEnd?.Invoke(2);
            }
        }


        void OnNetCallback(PacketType eventID, INetSerializable reader, NetPeer peer)
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
                case PacketType.S2C_LackInput:
                    OnLackInput(reader);
                    break;
            }
        }

        private void OnTestPVE(INetSerializable reader)
        {
            Debug.Log("[S2C] 单人测试");

            mySeatId = 0;
            remoteSeatId = 1;

            IsStart = true;

            HotFix.UIManager.Get().Push<HotFix.UI_GameMenu>();
        }

        private void OnTestPVP(INetSerializable reader)
        {
            var packet = (S2C_JoinResultPacket)reader;
            Debug.Log($"[S2C] 双人测试: code={packet.Code}, peerid={packet.HostId}, {packet.HostName}");

            if (packet.Code == 0)
            {
                mySeatId = packet.HostName.Equals(ClientNet.Get.m_PlayerManager.LocalPlayer.UserName) ? packet.HostId : packet.GuestId;
                remoteSeatId = (mySeatId + 1) % 2;

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
            //DoGameEnd();
            //SaveReplay();
        }

        // 重连恢复场景
        public static Dictionary<uint, uint[]> ConvertRecv(S2C_LackInputPacket packet)
        {
            Dictionary<uint, uint[]> recv = new Dictionary<uint, uint[]>();

            for (int i = 0; i < packet.inputs.Length; i++)
            {
                if (i == 0) continue; //删掉废帧0

                S2C_InputPacket input = packet.inputs[i];

                uint tick = (uint)i;
                uint[] _inputs = input.inputs;
                recv[tick] = _inputs;
            }

            return recv;
        }
        private void OnLackInput(INetSerializable reader)
        {
            var packet = (S2C_LackInputPacket)reader;
            Debug.Log($"缺失帧: {packet.frameNumber}/{packet.inputs.Length}个"); //63/64

            ggpo_recieve = ConvertRecv(packet);

            for (uint t = 1; t < packet.frameNumber; t++)
            {
                uint[] _inputs = ggpo_recieve[t];
                ggpo_predict[t] = _inputs;
                Process(t, _inputs);
            }

            sendTick = packet.frameNumber;
            recvTick = packet.frameNumber;
            rendTick = packet.frameNumber;
            //IsStart= true;
            Debug.Log($"pred:{ggpo_predict.Count}, recv:{ggpo_recieve.Count}, ");
        }

        private GUIStyle _style1;
        private GUIStyle style1
        {
            get
            {
                if (_style1 == null)
                {
                    _style1 = new GUIStyle();
                    _style1.fontSize = 25;
                    _style1.normal.textColor = Color.red;
                }
                return _style1;
            }
        }
        private Transform _view0;
        private Transform view0
        {
            get
            {
                if (_view0 == null)
                {
                    _view0 = runner.transform.GetChild(0);
                }
                return _view0;
            }
        }
        private Transform _view1;
        private Transform view1
        {
            get
            {
                if (_view1 == null)
                {
                    _view1 = runner.transform.GetChild(1);
                }
                return _view1;
            }
        }
        private int _posX1;
        private int posX1
        {
            get
            {
                if (_posX1 == 0)
                {
                    _posX1 = Screen.width / 4;
                }
                return _posX1;
            }
        }
        private int _posX2;
        private int posX2
        {
            get
            {
                if (_posX2 == 0)
                {
                    _posX2 = Screen.width / 4 * 3;
                }
                return _posX2;
            }
        }
        private int _posY;
        private int posY
        {
            get
            {
                if (_posY == 0)
                {
                    _posY = Screen.height - 50;
                }
                return _posY;
            }
        }
#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            //if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Client") return;
            //if (_netManager == null || _netManager.IsRunning == false) return;
            if (!IsStart) return;

            //Gizmos.color = Color.yellow;
            //Gizmos.DrawSphere(transform.position + Vector3.up * 2, 0.1f);
            var x0 = LocalSession.gs.characters[0].position.x.ToString();
            var x1 = LocalSession.gs.characters[1].position.x.ToString();
            UnityEditor.Handles.Label(view0.position, x0, style1);
            UnityEditor.Handles.Label(view1.position, x1, style1);
        }
#endif
    }
}