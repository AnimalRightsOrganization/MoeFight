using UnityEngine;
using Code.Shared;

namespace Code.Client
{
    public class ClientRoom : BaseRoom
    {
        #region 房间数据
        public ClientRoom(int roomId, ClientPlayer host, ClientPlayer guest) : base(roomId, host, guest)
        {
            m_PlayerList = new ClientPlayer[] { host, guest };
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

        public void DoInit(S2C_LoadScenePacket packet)
        {
            //RoomID //构造函数中已赋值
            BattleID = packet.BattleId;
            Seed = packet.Seed;
            MapId = packet.MapId;
            BattleMode = (BattleMode)packet.BattleMode;
            hostPlayer.RoleIndex = packet.Host.RoleIndex;
            guestPlayer.RoleIndex = packet.Guest.RoleIndex;
        }
    }
}