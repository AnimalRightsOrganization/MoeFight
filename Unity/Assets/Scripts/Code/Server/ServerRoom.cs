using System.IO;
using System.Collections.Generic;
using Code.Shared;
using LiteNetLib;
using LiteNetLib.Utils;
using Debug = UnityEngine.Debug;
using UnityEngine;

namespace Code.Server
{
    public class ServerRoom : BaseRoom
    {
        public ServerPlayer hostPlayer;
        public ServerPlayer guestPlayer;
        public int[] PauseChance = { 1, 1 }; //每局暂停机会

        #region 房间数据
        public ServerRoom(int id, ServerPlayer host, ServerPlayer guest) : base(id)
        {
            //Debug.Log("子类迟");
            hostPlayer = host;
            guestPlayer = guest;

            hostPlayer.SetRoomID(id).SetSeatID(0).SetStatus(PlayerStatus.AtRoomWait);
            guestPlayer.SetRoomID(id).SetSeatID(1).SetStatus(PlayerStatus.AtRoomWait);
        }
        public ServerPlayer GetOtherPlayer(short peerId)
        {
            if (hostPlayer.PeerId == peerId)
                return guestPlayer;
            else if (guestPlayer.PeerId == peerId)
                return hostPlayer;
            return null;
        }
        public void Send(NetDataWriter writer)
        {
            if (hostPlayer.IsBot == false)
                hostPlayer.AssociatedPeer.Send(writer, DeliveryMethod.ReliableOrdered);
            if (guestPlayer.IsBot == false)
                guestPlayer.AssociatedPeer.Send(writer, DeliveryMethod.ReliableOrdered);
        }
        #endregion

        #region 帧同步
        // 跳转场景同步
        //private List<short> stage_0_list = new List<short>();
        //public int Stage_0_Count => stage_0_list.Count;
        // 321倒计时同步
        private List<short> stage_1_list = new List<short>();
        public int Stage_1_Count => stage_1_list.Count;
        public void StageCount(int stage, ServerPlayer player)
        {
            //if (stage == 0)
            //{
            //    if (stage_0_list.Contains(player.PeerId) == false)
            //        stage_0_list.Add(player.PeerId);
            //}
            //else if (stage == 1)
            if (stage == 1)
            {
                if (stage_1_list.Contains(player.PeerId) == false)
                    stage_1_list.Add(player.PeerId);
            }
            else
            {
                Debug.LogError("[BUG] AddPlayerToFight");
            }
        }
        // 结算消息计数，确认收到2条
        public int EndCount = 0;


        // 独立的帧同步对象
        private int delay = 1; //Update时--，==0则执行，否则等待。
        private uint bufferTick;
        private uint serverTick;
        private Dictionary<uint, Dictionary<int, uint>> dic_recv; //从1开始, <座位号, 操作码>


        public void DoInit()
        {
            BattleStage = BattleStage.Paused;
            EndCount = 0;
            serverTick = 0;
            dic_recv = new Dictionary<uint, Dictionary<int, uint>>();
            PauseChance = new int[2] { 1, 1 };

            hostPlayer.SetStatus(PlayerStatus.AtBattle);
            guestPlayer.SetStatus(PlayerStatus.AtBattle);
        }

        public void DoUpdate() //每秒执行60次
        {
            delay--;
            if (delay <= 0)
            {
                uint buffer = (bufferTick - serverTick); //缓存帧数
                if (buffer >= 1)
                {
                    // 用一个loop，发送所有
                    // t = buffer → t = 1
                    for (int t = (int)buffer; t > 0; t--)
                    {
                        serverTick++; //服务器走帧
                        var input = dic_recv[serverTick];
                        var packet = new S2C_InputPacket
                        {
                            frameNumber = serverTick,
                            inputs = new uint[] { input[0], input[1] },
                        };
                        var writer = ServerNet.Get.WriteSerializable(PacketType.S2C_Input, packet);
                        Send(writer);
                    }

                    delay = 1; //服务器已经适应客户端速度，之后保持为1
                }
                else
                {
                    float Delta = Time.fixedDeltaTime * 1000;
                    delay = Mathf.CeilToInt(Mathf.Max(hostPlayer.Ping, guestPlayer.Ping) / Delta); //重新计算
                }
            }
            else
            {
                // 跳过，等待下一帧执行
            }
        }

        // 收到帧数据
        public void OnInputReceived(int seatId, C2S_InputPacket cmd)
        {
            switch (BattleMode)
            {
                case BattleMode.Training: //单人，收到就下发
                    {
                        var packet = new S2C_InputPacket
                        {
                            frameNumber = cmd.frameNumber,
                            inputs = new uint[] { cmd.input, 0 }
                        };
                        var writer = ServerNet.Get.WriteSerializable(PacketType.S2C_Input, packet);
                        Send(writer);

                        dic_recv[cmd.frameNumber] = new Dictionary<int, uint>();
                        dic_recv[cmd.frameNumber][seatId] = cmd.input;
                        dic_recv[cmd.frameNumber][1] = 0;

                        serverTick = cmd.frameNumber;
                        //Debug.Log($"server tick: {Tick}");
                    }
                    break;
                case BattleMode.Matching:
                    {
                        /*
                        if (dic_recv.ContainsKey(cmd.frameNumber) == false)
                        {
                            //Debug.Log($"[C2S.Input.111] {seatId}: {cmd.frameNumber}---{cmd.input}");
                            dic_recv[cmd.frameNumber] = new Dictionary<int, uint>();
                            dic_recv[cmd.frameNumber][seatId] = cmd.input;
                        }
                        else
                        {
                            // 同一个帧号，集齐两人份就下发
                            //Debug.Log($"[C2S.Input.222] {seatId}: {cmd.frameNumber}---{cmd.input}");
                            dic_recv[cmd.frameNumber][seatId] = cmd.input;

                            var packet = new S2C_InputPacket
                            {
                                frameNumber = cmd.frameNumber,
                                inputs = new uint[] { dic_recv[cmd.frameNumber][0], dic_recv[cmd.frameNumber][1] }
                            };
                            var writer = ServerNet.Get.WriteSerializable(PacketType.S2C_Input, packet);
                            Send(writer);

                            serverTick = cmd.frameNumber;
                        }
                        */

                        //②这里仅收集，Update中下发
                        //dic_recv.TryAdd(cmd.frameNumber, new Dictionary<int, uint>());
                        if (dic_recv.ContainsKey(cmd.frameNumber) == false)
                        {
                            dic_recv[cmd.frameNumber] = new Dictionary<int, uint>();
                            dic_recv[cmd.frameNumber][seatId] = cmd.input; //快的
                        }
                        else
                        {
                            dic_recv[cmd.frameNumber][seatId] = cmd.input; //慢的

                            bufferTick = cmd.frameNumber; //缓存到第几帧
                        }
                    }
                    break;
            }
        }

        // 把帧集合打包成下发的格式
        public S2C_LackInputPacket ConvertInputs()
        {
            S2C_LackInputPacket packet = new S2C_LackInputPacket();

            try
            {
                S2C_InputPacket[] array = new S2C_InputPacket[serverTick + 1]; //多一个废帧[0]

                for (int i = 0; i < array.Length; i++)
                {
                    if (i == 0)
                    {
                        array[0] = new S2C_InputPacket();
                    }
                    else
                    {
                        uint tick = (uint)i;
                        var item = dic_recv[tick];
                        uint[] _inputs = new uint[2] { item[0], item[1] };
                        array[i] = new S2C_InputPacket { frameNumber = tick, inputs = _inputs };
                    }
                }

                packet.frameNumber = serverTick;
                packet.inputs = array;
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
            }

            return packet;
        }

        // 打印服务器帧
        public void Dump()
        {
            string root = ConstValue.DUMP_FOLDER;
            if (!Directory.Exists(root))
            {
                Directory.CreateDirectory(root);
            }

            string folder = $"{root}/{BattleID}";
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
                //Debug.Log($"create folder: {folder}");
            }

            string savePath = $"{folder}/server.txt";
            List<string> lines = new List<string>();
            File.WriteAllLines(savePath, lines);
            Debug.Log($"saved in: {savePath}");
        }
        #endregion
    }
}