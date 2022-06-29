using UnityEngine;
using Code.Shared;
using LiteNetLib;
using LiteNetLib.Utils;

namespace Code.Client
{
    /* 本地房间 */
    public class ClientRoom : BaseRoom
    {
        #region 房间数据
        public ClientRoom(int roomId, ClientPlayer host, ClientPlayer guest) : base(roomId, host, guest)
        {
            //Debug.LogError("测试先执行.ClientRoom"); //子类迟
            m_PlayerList = new ClientPlayer[] { host, guest };
        }

        public override BasePlayer[] m_PlayerList { get; protected set; }
        public override void Dispose()
        {
            // 清空帧同步，清空数据
            DoStop(); //Dispose房间
            m_PlayerList = null;
            //m_SendTimer = null;
        }
        public ClientPlayer GetOtherPlayer(short peerId)
        {
            if (m_PlayerList[0].PeerId == peerId && m_PlayerList[1].PeerId != peerId)
            {
                return m_PlayerList[1] as ClientPlayer;
            }
            else if (m_PlayerList[0].PeerId != peerId && m_PlayerList[1].PeerId == peerId)
            {
                return m_PlayerList[0] as ClientPlayer;
            }
            else
            {
                return null;
            }
        }
        #endregion

        #region 帧同步

        void OnNetCallback(PacketType eventID, INetSerializable reader, NetPeer peer)
        {
            switch (eventID)
            {
                case PacketType.S2C_Lockstep:
                    OnLockstep(reader);
                    break;
            }
        }
        void OnLockstep(INetSerializable reader)
        {
            var packet = (S2C_InputPacket)reader;
            //GameManager.Instance.RecvOperation(packet);
        }


        // 计时器，获得指定的更新频率
        //protected LogicTimer m_SendTimer;
        public bool IsHost = true; //我是否房主
        public S2C_LoadScenePacket sceneData;

        // 用于创建场景
        public void DoInit(S2C_LoadScenePacket packet)
        {
            sceneData = packet;

            //RoomID //已经有了
            BattleID = packet.BattleId;
            Seed = packet.Seed;
            MapId = packet.MapId;
            BattleMode = (BattleMode)packet.BattleMode;
            hostPlayer.RoleIndex = packet.Host.RoleIndex;
            guestPlayer.RoleIndex = packet.Guest.RoleIndex;

            // 初始化计时器
            //m_SendTimer = new LogicTimer(OnSendUpdate);

            // 判断我的主客位
            if (BattleMode == BattleMode.Matching)
            {
                IsHost = ClientNet.Get.m_PlayerManager.LocalPlayer.SeatId == 0;
            }
        }

        // 客户端启动计时器
        public void DoStart()
        {
            //Debug.Log($"<color=blue>客户端启动帧同步：</color>");
            EventManager.RegisterEvent(OnNetCallback);
            //m_SendTimer.Start();
        }
        public void DoStop()
        {
            EventManager.UnRegisterEvent(OnNetCallback);
            //m_SendTimer?.Stop();
        }
        public void DoUpdate()
        {
            //m_SendTimer?.Update();
        }

        void OnSendUpdate()
        {
            //GameManager.Instance.LogicUpdate();
        }

        #endregion
    }
}