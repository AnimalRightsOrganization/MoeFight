namespace Code.Shared
{
    public abstract class BasePlayer
    {
        protected BasePlayer(BasePlayerManager playerManager, string name, byte id)
        {
            _playerManager = playerManager;
            Id = id;
            Name = name;
        }

        private BasePlayerManager _playerManager;
        public readonly string Name;
        public readonly byte Id;

        public int Ping;
        public int RoomId;
        public int SeatId;

        public virtual void Update(float delta)
        {

        }
    }
}