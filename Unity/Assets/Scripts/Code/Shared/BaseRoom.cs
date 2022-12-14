namespace Code.Shared
{
    [System.Serializable]
    public abstract class BaseRoom
    {
        public BaseRoom(int id)
        {
            //Debug.Log("基类先");
            RoomID = id;
        }
        public readonly int RoomID;     //房间ID（1~65535）
        public string BattleID;         //服务器战斗编号
        public byte MapId = 0;          //地图编号
        public BattleMode BattleMode;   //房间模式
        public BattleStage BattleStage;

        public override string ToString()
        {
            string str = $"Room#{RoomID}，Mode={BattleMode}";
            //str += $"[主位][{Players[0][PEER_ID_INDEX]}]({(PlayerStatus)Players[0][STATUS_INDEX]})，";
            //str += $"[客位][{Players[1][PEER_ID_INDEX]}]({(PlayerStatus)Players[1][STATUS_INDEX]})";
            return str;
        }
    }
}