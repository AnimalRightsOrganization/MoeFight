using Code.Shared;
using LiteNetLib;

namespace Code.Server
{
    public class ServerPlayer : BasePlayer
    {
        public readonly NetPeer AssociatedPeer;

        public ServerPlayer(string name, NetPeer peer) : base(name, peer.Id)
        {
            peer.Tag = this;
            AssociatedPeer = peer;
        }
    }
}