namespace Code.Shared
{
    public abstract class BasePlayer
    {
        public int Ping; //延迟

        private int _roomid = -1; //-1是在Lobby
        public int RoomId => _roomid; //1开始
        private SeatInfo _seatid = SeatInfo.NONE; //0是主位，1是客位
        public sbyte SeatId => (sbyte)_seatid;
        private PlayerStatus _status = PlayerStatus.Offline;
        public PlayerStatus Status => _status;

        public byte RoleIndex = 0; //角色编号（默认）
        public byte RoleColor = 0; //角色颜色
        public byte RoleCloth = 0; //角色时装

        public readonly string UserName; //用户名
        public readonly string NickName; //昵称
        public readonly short PeerId; //连接ID
        protected BasePlayer(string userName, int peerid)
        {
            UserName = userName;
            PeerId = (short)peerid;
            ResetToLobby(); //登录成功创建的，已经在大厅
        }

        // 回到大厅后重置
        public virtual void ResetToLobby()
        {
            this.SetRoomID(-1)
                .SetSeatID(-1)
                .SetStatus(PlayerStatus.AtLobby);
            RoleIndex = 0;
            RoleColor = 0;
            RoleCloth = 0;
        }
        public virtual BasePlayer SetRoomID(int roomId)
        {
            this._roomid = roomId;
            return this;
        }
        public virtual BasePlayer SetSeatID(int seatid)
        {
            this._seatid = (SeatInfo)seatid;
            return this;
        }
        public virtual BasePlayer SetStatus(PlayerStatus status)
        {
            //UnityEngine.Debug.Log($"SetStatus: {UserName}@{status}");
            this._status = status;
            return this;
        }

        public void CopyFrom(BasePlayer lastPlayer)
        {
            SetRoomID(lastPlayer.RoomId);
            SetSeatID(lastPlayer.SeatId);
            SetStatus(lastPlayer.Status);
            RoleIndex = lastPlayer.RoleIndex;
            RoleColor = lastPlayer.RoleColor;
            RoleCloth = lastPlayer.RoleCloth;
        }

        public override string ToString()
        {
            string str = $"[peer:{PeerId}]{UserName} @Room#{RoomId} @Seat={SeatId} @Status={Status}";
            return str;
        }
    }
}