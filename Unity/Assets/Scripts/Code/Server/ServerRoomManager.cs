using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;

namespace Code.Server
{
    public class ServerRoomManager
    {
        const int MAX_ROOMS = 128;
        const int MIN_INDEX = 1;
        protected Dictionary<int, ServerRoom> dic_rooms;
        public int Count => dic_rooms.Count;

        public ServerRoomManager()
        {
            dic_rooms = new Dictionary<int, ServerRoom>();
            dic_battles = new Dictionary<int, ServerRoom>();
        }

        // 获取空闲房间Id
        private int GetAvailableRoomID()
        {
            int id = MIN_INDEX;

            if (dic_rooms.Count == 0)
                return id;

            for (int i = MIN_INDEX; i <= MAX_ROOMS; i++)
            {
                ServerRoom serverRoom = null;
                if (dic_rooms.TryGetValue(i, out serverRoom) == false)
                {
                    id = i;
                    break;
                }
            }
            return id;
        }
        // 增
        public ServerRoom CreateServerRoom(ServerPlayer hostPlayer, ServerPlayer guestPlayer)
        {
            int roomId = GetAvailableRoomID();
            if (dic_rooms.ContainsKey(roomId))
            {
                Debug.Print("严重的错误，创建房间时，ID重复");
                return null;
            }
            if (Count >= MAX_ROOMS)
            {
                Debug.Print("大厅爆满，无法创建新房间");
                return null;
            }

            ServerRoom serverRoom = new ServerRoom(roomId, hostPlayer, guestPlayer);
            dic_rooms.Add(roomId, serverRoom);
            return serverRoom;
        }

        // 删（房主解散或房间结算后执行）
        public void RemoveServerRoom(int roomId)
        {
            ServerRoom serverRoom = null;
            if (dic_rooms.TryGetValue(roomId, out serverRoom))
            {
                if (dic_battles.ContainsKey(roomId))
                {
                    RemoveBattle(serverRoom);
                }
                serverRoom.Dispose();
                dic_rooms.Remove(roomId);
            }
            else
            {
                Debug.Print("严重的错误，无法移除房间");
            }
        }
        // 删（所有）
        public void RemoveAll()
        {
            foreach (var roomItem in dic_rooms)
            {
                roomItem.Value.Dispose();
                dic_rooms.Remove(roomItem.Key);
            }
            foreach (var roomItem in dic_battles)
            {
                roomItem.Value.Dispose();
                dic_rooms.Remove(roomItem.Key);
            }
        }

        // 查
        public ServerRoom GetServerRoom(int roomId)
        {
            ServerRoom serverRoom = null;
            if (dic_rooms.TryGetValue(roomId, out serverRoom) == false)
            {
                Debug.Print("严重的错误，无法移除房间");
            }
            return serverRoom;
        }
        // 查（所有，仅调试用）
        public ServerRoom[] GetAll()
        {
            ServerRoom[] DictionaryToArray = dic_rooms.Values.ToArray();
            return DictionaryToArray;
        }


        protected Dictionary<int, ServerRoom> dic_battles;
        public Dictionary<int, ServerRoom> GetBattles()
        {
            return dic_battles;
        }
        public void SetBattle(ServerRoom room)
        {
            dic_battles.Add(room.RoomID, room);
        }
        public void RemoveBattle(ServerRoom room)
        {
            dic_battles.Remove(room.RoomID);
        }
    }
}