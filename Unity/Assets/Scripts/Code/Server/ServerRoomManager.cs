using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;

namespace Code.Server
{
    public class ServerRoomManager
    {
        const int MAX_ROOMS = 64;
        const int MIN_INDEX = 1;
        protected Dictionary<int, ServerRoom> dic_rooms;
        public int Count => dic_rooms.Count;

        public ServerRoomManager()
        {
            dic_rooms = new Dictionary<int, ServerRoom>();
            //m_RoomList = new List<ServerRoom>();
            m_BattleRoomList = new List<ServerRoom>();
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
                if (m_BattleRoomList.Contains(serverRoom))
                {
                    RemoveBattleRoom(serverRoom);
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

        // 战场逻辑
        protected List<ServerRoom> m_BattleRoomList;
        public List<ServerRoom> GetAllBattleRoom()
        {
            return m_BattleRoomList;
        }
        public void AddBattleRoom(ServerRoom room)
        {
            if (m_BattleRoomList.Contains(room))
            {
                UnityEngine.Debug.LogError("已经存在，无法添加战场");
                return;
            }
            m_BattleRoomList.Add(room);
        }
        // 主动退出/断线/正常比赛结算
        void RemoveBattleRoom(ServerRoom room)
        {
            if (m_BattleRoomList.Contains(room) == false)
            {
                UnityEngine.Debug.LogError("错误的战场房，无法移除");
                return;
            }
            m_BattleRoomList.Remove(room);
        }
    }
}