using System.IO;
using System.Collections.Generic;
using Code.Shared;
using LiteNetLib;
using Debug = UnityEngine.Debug;

namespace Code.Server
{
    /* 远程房间 */
    public class ServerRoom : BaseRoom
    {
        #region 房间数据

        public ServerRoom(int roomId, ServerPlayer host, ServerPlayer guest) : base(roomId, host, guest)
        {
            //Debug.LogError("测试先执行.ServerRoom"); //子类迟
            m_PlayerList = new ServerPlayer[] { host, guest };
            EndCount = 0;
        }

        public override BasePlayer[] m_PlayerList { get; protected set; }
        public override void Dispose() { }
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
                return null;
            }
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
        private ushort Tick;
        private Dictionary<ushort, Dictionary<int, InputBuffer>> dic_recv;
        protected NetPeer[] m_NetPeers;

        // 收到帧数据
        public void OnInputReceived(int seatId, C2S_InputBufferPacket cmd)
        {
            int pid = seatId + 1;
            if (dic_recv.ContainsKey(cmd.Tick) == false)
            {
                dic_recv[cmd.Tick] = new Dictionary<int, InputBuffer>();
                dic_recv[cmd.Tick][pid] = cmd.Operation;
            }
            else
            {
                //<注意>不可靠模式下，这里可能收到重复的冗余帧，排除掉。
                if (dic_recv[cmd.Tick].ContainsKey(pid))
                    return;

                // 之前建过了，是第二个玩家提交
                dic_recv[cmd.Tick][pid] = cmd.Operation;

                Tick = cmd.Tick; //纯转发，不计算
                var keys1 = dic_recv[Tick][1];
                var keys2 = dic_recv[Tick][2];

                // 下发逻辑直接在这里写。
                var packet = new S2C_AllPlayerOperationPacket
                {
                    ServerTick = Tick,
                    HostOperation = keys1,
                    GuestOperation = keys2,
                };
                //ServerNet.Get.BroadcastLockstep(m_NetPeers, packet);
                string time = System.DateTime.Now.ToString("yyyy-mm-dd hh:mm:ss:fff");
                Debug.Log($"<color=green>服务器下发帧：{Tick}，缓存帧：{dic_recv.Count}/(余{dic_recv.Count - Tick})------{time}</color>");
            }
        }
        // 收到缺失帧请求
        public void OnLackFramesReceived(C2S_LackFramesPacket data)
        {
            ushort from = data.FromFrameID;

            int count = Tick - from + 1;
            Debug.Log($"[S]请求{from}~{Tick}，共{count}个缺失帧");
            var ops = new S2C_AllPlayerOperationPacket[count];
            for (ushort i = 0; i < count; i++)
            {
                ushort index = (ushort)(from + i);
                //var role1 = dic_host[index];
                //var role2 = dic_guest[index];
                var op = new S2C_AllPlayerOperationPacket
                {
                    ServerTick = index,
                    //HostOperation = role1,
                    //GuestOperation = role2,
                };
                ops[i] = op;
            }

            S2C_LackFramesPacket packet = new S2C_LackFramesPacket
            {
                FrameCount = count,
                Frames = ops,
            };
            //ServerNet.Get.BroadcastLackFrames(m_NetPeers, packet);
        }

        public void DoInit()
        {
            var netManager = ServerNet.Get._netManager;
            dic_recv = new Dictionary<ushort, Dictionary<int, InputBuffer>>();
            m_NetPeers = new NetPeer[]
            {
                netManager.GetPeerById(hostPlayer.PeerId),
                netManager.GetPeerById(guestPlayer.PeerId),
            };
        }

        public const string DUMP_FOLDER = "";

        public void Dump()
        {
            string root = DUMP_FOLDER;
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