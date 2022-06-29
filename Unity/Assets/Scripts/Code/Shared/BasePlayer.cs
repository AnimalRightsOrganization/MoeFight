namespace Code.Shared
{
    public abstract class BasePlayer
    {
        protected BasePlayer(BasePlayerManager playerManager, string name, byte peerid)
        {
            _playerManager = playerManager;
            PeerId = (short)peerid;
            UserName = name;
        }

        private BasePlayerManager _playerManager;

        public int Ping; //延迟
        public int RoomId;
        public int SeatId;

        public byte RoleIndex = 0; //角色编号（默认）

        public readonly short PeerId; //连接ID
        public readonly string UserName; //用户名
        public readonly string NickName; //昵称
    }
}