using System.IO;
using System.Collections.Generic;
using Code.Shared;
using LiteNetLib;
using LiteNetLib.Utils;
using Debug = UnityEngine.Debug;

namespace Code.Server
{
    public class ServerRoom : BaseRoom
    {
        #region 房间数据
        public ServerRoom(int id, ServerPlayer host, ServerPlayer guest) : base(id, host, guest)
        {
            //Debug.Log("子类迟");
            m_PlayerList = new ServerPlayer[] { host, guest };
            EndCount = 0;
        }
        public ServerPlayer GetOtherPlayer(short peerId)
        {
            if (m_PlayerList[0].PeerId == peerId && m_PlayerList[1].PeerId != peerId)
            {
                return m_PlayerList[1] as ServerPlayer;
            }
            else if (m_PlayerList[0].PeerId != peerId && m_PlayerList[1].PeerId == peerId)
            {
                return m_PlayerList[0] as ServerPlayer;
            }
            else
            {
                return null; //要找的用户不在当前房间
            }
        }
        protected ServerPlayer serverHost;
        protected ServerPlayer serverGuest;
        public void Send(NetDataWriter writer)
        {
            if (serverHost.IsBot == false)
                serverHost.AssociatedPeer.Send(writer, DeliveryMethod.ReliableOrdered);
            if (serverGuest.IsBot == false)
                serverGuest.AssociatedPeer.Send(writer, DeliveryMethod.ReliableOrdered);
        }
        #endregion

        #region 帧同步
        // 开始战斗计数，重连时不需要
        private List<short> stage_0_list = new List<short>();
        public int Stage_0_Count => stage_0_list.Count;
        private List<short> stage_1_list = new List<short>();
        public int Stage_1_Count => stage_1_list.Count;
        public void StageCount(int stage, ServerPlayer player)
        {
            if (stage == 0)
            {
                if (stage_0_list.Contains(player.PeerId) == false)
                    stage_0_list.Add(player.PeerId);
            }
            else if (stage == 1)
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
        public uint Tick;
        public Dictionary<uint, Dictionary<int, uint>> dic_recv; //从1开始

        public void DoInit()
        {
            serverHost = hostPlayer as ServerPlayer;
            serverGuest = guestPlayer as ServerPlayer;

            dic_recv = new Dictionary<uint, Dictionary<int, uint>>();
        }

        // 收到帧数据
        public void OnInputReceived(int seatId, C2S_InputPacket cmd)
        {
            switch (BattleMode)
            {
                case BattleMode.Editor:
                    break;
                case BattleMode.TestPVE:
                    {
                        // 只有一人，收到就下发
                        var packet = new S2C_InputPacket
                        {
                            frameNumber = cmd.frameNumber,
                            inputs = new uint[] { cmd.input, 0 }
                        };
                        var writer = ServerNet.Get.WriteSerializable(PacketType.S2C_Input, packet);
                        Send(writer);

                        Tick = cmd.frameNumber;
                        //Debug.Log($"server tick: {Tick}");
                    }
                    break;
                case BattleMode.TestPVP:
                case BattleMode.Matching:
                    {
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

                            Tick = cmd.frameNumber;
                        }
                    }
                    break;
            }
        }

        // 掉线倒计时
        public void CutDown()
        {
            //ConstValue.DROP_WAIT_TIME;
        }

        // 把帧集合打包成下发的格式
        public S2C_LackInputPacket ConvertInputs()
        {
            S2C_InputPacket[] array = new S2C_InputPacket[Tick + 1];
            array[0] = new S2C_InputPacket(); //填充废帧[0]

            for (int i = 1; i < array.Length; i++)
            {
                uint tick = (uint)i;
                Dictionary<int, uint> item = dic_recv[tick];
                uint[] _inputs = new uint[2] { item[0], item[1] };
                array[i] = new S2C_InputPacket { frameNumber = tick, inputs = _inputs };
            }

            S2C_LackInputPacket packet = new S2C_LackInputPacket
            {
                frameNumber = Tick,
                inputs = array,
            };

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