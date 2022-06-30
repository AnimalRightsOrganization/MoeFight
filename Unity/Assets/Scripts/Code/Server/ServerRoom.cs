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
        public ServerRoom(int roomId, ServerPlayer host, ServerPlayer guest) : base(roomId, host, guest)
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
        public void Send(NetDataWriter writer)
        {
            (hostPlayer as ServerPlayer).AssociatedPeer.Send(writer, DeliveryMethod.ReliableOrdered);
            (guestPlayer as ServerPlayer).AssociatedPeer.Send(writer, DeliveryMethod.ReliableOrdered);
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
        private Dictionary<uint, Dictionary<int, uint>> dic_recv;
        protected NetPeer[] m_NetPeers;

        public void DoInit()
        {
            var netManager = ServerNet.Get._netManager;
            dic_recv = new Dictionary<uint, Dictionary<int, uint>>();
            m_NetPeers = new NetPeer[]
            {
                netManager.GetPeerById(hostPlayer.PeerId),
                netManager.GetPeerById(guestPlayer.PeerId),
            };
        }

        // 收到帧数据
        public void OnInputReceived(int seatId, C2S_InputPacket cmd)
        {

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