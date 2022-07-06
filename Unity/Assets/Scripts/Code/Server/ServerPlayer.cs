using Code.Shared;
using LiteNetLib;

namespace Code.Server
{
    public class ServerPlayer : BasePlayer
    {
        public readonly NetPeer AssociatedPeer;

        public readonly bool IsBot;

        public ServerPlayer(string name, NetPeer peer) : base(name, peer.Id)
        {
            peer.Tag = this;
            AssociatedPeer = peer;
            IsBot = name.Equals("BOT");
        }
    }
}