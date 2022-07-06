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
            var p = GetPlayerByUsername(player.UserName);
            if (p == null)
            {
                playerList.Add(player);
                player.ResetToLobby();
            }
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
        public ServerPlayer GetPlayerByUsername(string userName)
        {
            var player = playerList.Find(x => x.UserName == userName);
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


        public void Print()
        {
            string result = $"服务器用户数:{Count}";
            foreach (ServerPlayer p in playerList)
            {
                result += $"\n{p}";
            }
            UnityEngine.Debug.Log(result);
        }
    }
}