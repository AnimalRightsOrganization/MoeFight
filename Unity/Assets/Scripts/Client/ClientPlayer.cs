using Code.Shared;
using UnityEngine;

namespace Code.Client
{ 
    public class ClientPlayer : BasePlayer
    {
        private PlayerInputPacket _nextCommand;
        private readonly ClientNet _clientLogic;
        private readonly ClientPlayerManager _playerManager;
        private ServerState _lastServerState;
        private const int MaxStoredCommands = 60;
        private bool _firstStateReceived;
        private int _updateCount;

        public Vector2 LastPosition { get; private set; }
        public float LastRotation { get; private set; }

        public ClientPlayer(ClientNet clientLogic, ClientPlayerManager manager, string name, byte id) : base(manager, name, id)
        {
            _playerManager = manager;
            _clientLogic = clientLogic;
        }

        public override void Update(float delta)
        {
        }
    }
}