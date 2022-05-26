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
            NetworkState = new PlayerState { Id = (byte)peer.Id };
        }

        public override void Update(float delta)
        {
            base.Update(delta);
            NetworkState.Tick = LastProcessedCommandId;
        }
    }
}