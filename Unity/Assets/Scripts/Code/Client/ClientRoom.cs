using UnityEngine;
using Code.Shared;

namespace Code.Client
{
    public class ClientRoom : BaseRoom
    {
        public ClientPlayer hostPlayer;
        public ClientPlayer guestPlayer;

        public ClientRoom(int roomId, ClientPlayer host, ClientPlayer guest) : base(roomId, host, guest)
        {
            //m_PlayerList = new ClientPlayer[] { host, guest };
            hostPlayer = host;
            guestPlayer = guest;
        }

        public void DoInit(S2C_LoadScenePacket packet)
        {
            BattleID = packet.BattleId;
            MapId = packet.MapId;
            BattleMode = (BattleMode)packet.BattleMode;
            hostPlayer.RoleIndex = packet.Host.RoleIndex;
            guestPlayer.RoleIndex = packet.Guest.RoleIndex;
            Debug.Log($"客户端初始化: {hostPlayer.RoleIndex} vs {guestPlayer.RoleIndex}");
        }
    }
}