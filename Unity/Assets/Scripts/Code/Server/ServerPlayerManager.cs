using System.Collections.Generic;
using Code.Shared;

namespace Code.Server
{
    public class ServerPlayerManager
    {
        public ServerPlayerManager()
        {
            playerList = new List<ServerPlayer>();
        }

        private List<ServerPlayer> playerList;
        public int Count => playerList.Count;

        // 登录成功
        public void AddPlayer(ServerPlayer player)
        {
            playerList.Add(player);
            player.ResetToLobby();
        }
        // 登出/断线/踢人
        public bool RemovePlayer(int peerId)
        {
            var player = playerList.Find(x => x.PeerId == peerId);
            if (player == null) 
                return false;

            playerList.Remove(player);
            return true;
        }
        // 关服
        public void RemoveAll()
        {
            for (int i = playerList.Count - 1; i >= 0; i--)
            {
                playerList[i] = null;
                playerList.RemoveAt(i);
            }
        }

        // 获取指定玩家
        public ServerPlayer GetPlayerByPeerId(short peerId)
        {
            var player = playerList.Find(x => x.PeerId == peerId);
            return player;
        }
        // 获取所有玩家
        public ServerPlayer[] GetPlayersAll()
        {
            return playerList.ToArray();
        }
        // 获取大厅内玩家
        public ServerPlayer[] GetPlayersByLobby()
        {
            return playerList.FindAll(x => x.Status == PlayerStatus.AtLobby).ToArray();
        }
    }
}