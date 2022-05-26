using Code.Shared;
using LiteNetLib;

namespace Code.Server
{
    public class ServerPlayer : BasePlayer
    {
        private readonly ServerPlayerManager _playerManager;
        public readonly NetPeer AssociatedPeer;
        public PlayerState NetworkState;
        public ushort LastProcessedCommandId { get; private set; }
        
        public ServerPlayer(ServerPlayerManager playerManager, string name, NetPeer peer) : base(playerManager, name, (byte)peer.Id)
        {
            _playerManager = playerManager;
            peer.Tag = this;
            AssociatedPeer = peer;
            NetworkState = new PlayerState {Id = (byte) peer.Id};
        }

        public override void ApplyInput(PlayerInputPacket command, float delta)
        {
            if (NetworkGeneral.SeqDiff(command.Id, LastProcessedCommandId) <= 0)
                return;
            LastProcessedCommandId = command.Id;
            base.ApplyInput(command, delta);
        }

        public override void Update(float delta)
        {
            base.Update(delta);
            NetworkState.Rotation = _rotation;
            NetworkState.Tick = LastProcessedCommandId;
            
            //Draw cross as server player
            const float sz = 0.1f;
            
        }
    }
}