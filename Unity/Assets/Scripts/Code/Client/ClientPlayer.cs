using Code.Shared;

namespace Code.Client
{
    /* 本地用户 */
    [System.Serializable]
    public class ClientPlayer : BasePlayer
    {
        public ClientPlayer(string name, short peerId) : base(name, peerId) { }

        public Settings m_Settings;
    }
}