using Code.Shared;

namespace Code.Client
{   
    public class RemotePlayer : BasePlayer
    {
        private float _receivedTime;
        private float _timer;
        private const float BufferTime = 0.1f; //100 milliseconds
        
        public RemotePlayer(ClientPlayerManager manager, string name, PlayerJoinedPacket pjPacket) : base(manager, name, pjPacket.InitialPlayerState.Id)
        {
            _health = pjPacket.Health;
            _rotation = pjPacket.InitialPlayerState.Rotation;
        }
    }
}