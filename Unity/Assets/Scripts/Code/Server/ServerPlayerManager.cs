using System.Collections.Generic;
using Code.Shared;

namespace Code.Server
{
    public class ServerPlayerManager : BasePlayerManager
    {
        private readonly ServerPlayer[] _players;

        private int _playersCount;

        public override int Count => _playersCount;

        public ServerPlayerManager(ServerNet serverLogic)
        {
            _players = new ServerPlayer[ServerNet.MaxPlayers];
        }

        public override IEnumerator<BasePlayer> GetEnumerator()
        {
            int i = 0;
            while (i < _playersCount)
            {
                yield return _players[i];
                i++;
            }
        }

        public void AddPlayer(ServerPlayer player)
        {
            for (int i = 0; i < _playersCount; i++)
            {
                if (_players[i].PeerId == player.PeerId)
                {
                    _players[i] = player;
                    return;
                }
            }

            _players[_playersCount] = player;
            _playersCount++;
        }

        public override void LogicUpdate()
        {
            for (int i = 0; i < _playersCount; i++)
            {
                var p = _players[i];
                //p.Update(LogicTimer.FixedDelta);
            }
        }

        public bool RemovePlayer(byte playerId)
        {
            for (int i = 0; i < _playersCount; i++)
            {
                if (_players[i].PeerId == playerId)
                {
                    _playersCount--;
                    _players[i] = _players[_playersCount];
                    _players[_playersCount] = null;
                    return true;
                }
            }
            return false;
        }

        public ServerPlayer GetPlayer(int playerId)
        {
            return _players[playerId];
        }

        public ServerPlayer[] GetPlayersAll()
        {
            return _players;
        }
    }
}