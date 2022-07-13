using System;
using Debug = UnityEngine.Debug;

namespace Code.Shared
{
    public class BaseRoom : IDisposable
    {
        public readonly int RoomID;     //当前房间ID（1~65535）
        public string BattleID;         //服务器战斗编号
        public int Seed;                //随机种子
        public byte MapId = 0;          //地图编号
        public BattleMode BattleMode;   //房间模式

        public BaseRoom(int id)
        {
            //Debug.Log("基类先");
            RoomID = id;
        }

        public virtual void Dispose() { }

        //public static int CheckWinnerSeatId(int hostHP, int guestHP)
        //{
        //    int winnerSeatId = 0;
        //    if (hostHP == guestHP)
        //        winnerSeatId = -1;
        //    else
        //        winnerSeatId = hostHP > guestHP ? 0 : 1;
        //    return winnerSeatId;
        //}

        public override string ToString()
        {
            string str = $"Room#{RoomID}，";
            //str += $"[主位][{Players[0][PEER_ID_INDEX]}]({(PlayerStatus)Players[0][STATUS_INDEX]})，";
            //str += $"[客位][{Players[1][PEER_ID_INDEX]}]({(PlayerStatus)Players[1][STATUS_INDEX]})";
            return str;
        }
    }
}