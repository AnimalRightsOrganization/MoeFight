using UnityEngine;
using Code.Shared;

namespace Code.Client
{
    public class ClientRoom : BaseRoom
    {
        public ClientPlayer HostPlayer;
        public ClientPlayer GuestPlayer;

        // 匹配成功创建
        public ClientRoom(int id, ClientPlayer host, ClientPlayer guest) : base(id)
        {
            HostPlayer = host;
            GuestPlayer = guest;

            HostPlayer.SetRoomID(id).SetSeatID(0).SetStatus(PlayerStatus.AtRoomWait);
            GuestPlayer.SetRoomID(id).SetSeatID(1).SetStatus(PlayerStatus.AtRoomWait);
        }

        // 双方准备，初始化比赛
        public void DoInit(S2C_LoadScenePacket packet)
        {
            BattleID = packet.BattleId;
            MapId = packet.MapId;
            HostPlayer.RoleIndex = packet.Host.RoleIndex;
            GuestPlayer.RoleIndex = packet.Guest.RoleIndex;

            Debug.Log($"房间初始化: {HostPlayer.RoleIndex} vs {GuestPlayer.RoleIndex}");
        }
    }
}