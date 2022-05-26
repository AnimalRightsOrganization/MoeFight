namespace Code.Shared
{
    public abstract class BasePlayer
    {
        public readonly string Name;

        private float _speed = 3f;
        private GameTimer _shootTimer = new GameTimer(0.2f);
        private BasePlayerManager _playerManager;

        public readonly byte Id;
        public int Ping;

        protected BasePlayer(BasePlayerManager playerManager, string name, byte id)
        {
            Id = id;
            Name = name;
            _playerManager = playerManager;
        }

        public virtual void Update(float delta)
        {
            _shootTimer.UpdateAsCooldown(delta);
        }
    }
}