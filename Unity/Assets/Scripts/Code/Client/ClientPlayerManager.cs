using System.Collections.Generic;
using Code.Shared;
using UnityEngine;

namespace Code.Client
{
    public struct PlayerHandler
    {
        public readonly BasePlayer Player;

        public PlayerHandler(BasePlayer player)
        {
            Player = player;
        }

        public void Update(float delta)
        {
            Player.Update(delta);
        }
    }

    public class ClientPlayerManager : BasePlayerManager
    {
        private readonly Dictionary<byte, PlayerHandler> _players;
        private readonly ClientNet _clientLogic;
        private ClientPlayer _clientPlayer;

        public ClientPlayer OurPlayer => _clientPlayer;
        public override int Count => _players.Count;

        public ClientPlayerManager(ClientNet clientLogic)
        {
            _clientLogic = clientLogic;
            _players = new Dictionary<byte, PlayerHandler>();
        }
        
        public override IEnumerator<BasePlayer> GetEnumerator()
        {
            foreach (var ph in _players)
                yield return ph.Value.Player;
        }

        public BasePlayer GetById(byte id)
        {
            return _players.TryGetValue(id, out var ph) ? ph.Player : null;
        }

        public BasePlayer RemovePlayer(byte id)
        {
            if (_players.TryGetValue(id, out var handler))
            {
                _players.Remove(id);
            }
        
            return handler.Player;
        }

        public override void LogicUpdate()
        {
            //foreach (var kv in _players)
            //    kv.Value.Update(LogicTimer.FixedDelta);
        }

        public void AddClientPlayer(ClientPlayer player)
        {
            _clientPlayer = player;
        }
        
        public void Clear()
        {
            _players.Clear();
        }
    }
}