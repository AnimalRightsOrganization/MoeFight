using Code.Shared;
using UnityEngine;

namespace Code.Client
{
    public class ClientPlayer : BasePlayer
    {
        private readonly ClientNet _clientLogic;
        private readonly ClientPlayerManager _playerManager;

        public ClientPlayer(ClientNet clientLogic, ClientPlayerManager manager, string name, byte id) : base(manager, name, id)
        {
            _playerManager = manager;
            _clientLogic = clientLogic;
        }
    }
}