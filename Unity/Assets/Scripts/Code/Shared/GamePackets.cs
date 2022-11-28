using LiteNetLib.Utils;

namespace Code.Shared
{
    public enum Languages
    {
        English,
        简体中文,
        日本語,
        Español,
    }

    public enum ErrorCode : byte
    {
        LOBBY_IS_FULL,  //大厅爆满
        ROOM_IS_FULL,   //房间爆满
        USERNAME_USED,  //账号已经注册
        BE_KICKED,      //被踢了（顶号/GM）
        HAS_LOGIN,      //已登录
        //PAUSE_USED,     //暂停次数用尽
    }
    
    public enum PlayerStatus : byte
    {
        Offline     = 0,    //离线
        AtLobby     = 1,    //在大厅
        Matching    = 2,    //匹配中
        AtRoomWait  = 3,    //在房间
        AtRoomReady = 4,    //在房间
        AtBattle    = 5,    //在战场
        Reconnect   = 6,    //比赛中掉线，标记为重连
    }

    public enum SeatInfo : sbyte
    {
        NONE        = -1,   //没人或不在房间
        HOST        = 0,    //主位
        GUEST       = 1,    //客位
    }

    public enum PacketType : byte
    {
        // C2S /////////////
        C2S_TestPVE         ,   //独立启动加入
        C2S_TestPVP         ,   //双人启动加入
        C2S_Input           ,   //
        C2S_RegisterReq     ,   //注册请求
        C2S_LoginReq        ,   //登录请求
        C2S_LogoutReq       ,   //登出请求
        C2S_UserInfo        ,   //请求用户信息
        C2S_Settings        ,   //设置选项
        C2S_MatchRequest    ,   //请求匹配
        C2S_MatchCancel     ,   //请求匹配中离开
        C2S_MatchQuit       ,   //匹配成功后离开
        C2S_RoleSelect      ,   //匹配成功后选择角色
        C2S_GameReady       ,   //请求准备
        C2S_BattleStart     ,   //请求开始战斗
        C2S_BattlePause     ,   //请求暂停（客户端主动）
        C2S_BattleInputs    ,   //缺失帧（重连）
        C2S_BattleQuit      ,   //比赛中、重连后（认输）
        C2S_BattleEnd       ,   //上报比赛结果（双方都要发，由战斗系统判定）
        // S2C /////////////
        S2C_TestPVE         ,   //独立启动加入
        S2C_TestPVP         ,   //双人启动加入
        S2C_Input           ,   //
        S2C_LoginResult     ,   //登录结果
        S2C_LogoutResult    ,   //登出结果
        S2C_UserInfo        ,   //下发用户信息
        S2C_Settings        ,   //设置选项
        S2C_ErrorOperate    ,   //错误代码
        S2C_MatchResult     ,   //匹配结果
        S2C_RoleSelect      ,   //选择角色
        S2C_GameReady       ,   //准备结果
        S2C_LoadScene       ,   //跳转场景（双方都准备后，服务器主动下发）
        S2C_BattleStart     ,   //比赛开始（第一帧同步）
        S2C_BattlePause     ,   //比赛暂停（客户端主动，每人每局一次机会）
        S2C_BattleLostNet   ,   //断线暂停（服务器主动，一方断线）
        S2C_BattleReconnect ,   //比赛重连
        S2C_BattleInputs    ,   //比赛帧数据
        S2C_BattleEnd       ,   //比赛结束，结算
    }

    public enum BattleStage
    {
        Paused      = 0, //暂停/准备
        Running     = 1, //游戏
        End         = 2, //结束
        //Process     = 3, //追帧
        //LostNet     = 4, //掉线
        //Replaying   = 5, //回放中
        //ReplayPause = 6, //回放暂停
    }
    public enum BattleMode
    {
        Editor      = 0, //编辑器调试（单机、hitstun）
        Training    = 1, //训练（PVE）
        Matching    = 2, //匹配（PVP）
        Replay      = 3, //回放
        //Arcade      = 4, //剧情（人机）
    }
    public enum BattleResult
    {
        Lose        = 0,
        Draw        = 1,
        Win         = 2,
    }

    #region 公用
    public struct EmptyPacket : INetSerializable
    {
        public void Serialize(NetDataWriter writer) { }
        public void Deserialize(NetDataReader reader) { }
    }

    [System.Serializable]
    public struct Settings : INetSerializable
    {
        public byte ScreenSize;
        public byte FullScreen;
        public byte MusicVolume;
        public byte SoundVolume;
        public byte Language;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(ScreenSize);
            writer.Put(FullScreen);
            writer.Put(MusicVolume);
            writer.Put(SoundVolume);
            writer.Put(Language);
        }
        public void Deserialize(NetDataReader reader)
        {
            ScreenSize = reader.GetByte();
            FullScreen = reader.GetByte();
            MusicVolume = reader.GetByte();
            SoundVolume = reader.GetByte();
            Language = reader.GetByte();
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            var other = (Settings)obj;
            bool cond3 = MusicVolume == other.MusicVolume;
            bool cond4 = SoundVolume == other.SoundVolume;
            bool cond5 = Language == other.Language;
            return cond3 && cond4 && cond5;
        }
        public override string ToString()
        {
            return $"cond1={ScreenSize}, cond2={FullScreen}, cond3={MusicVolume}, cond4={SoundVolume}, cond5={Language}";
        }
    }
    #endregion

    #region 上行
    public struct C2S_InputPacket : INetSerializable
    {
        public uint frameNumber;
        public uint input; //按键压制

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(frameNumber);
            writer.Put(input);
        }

        public void Deserialize(NetDataReader reader)
        {
            frameNumber = reader.GetUInt();
            input = reader.GetUInt();
        }
    }

    public struct C2S_LackInputPacket : INetSerializable
    {
        public uint startTick;
        public uint endTick;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(startTick);
            writer.Put(endTick);
        }
        public void Deserialize(NetDataReader reader)
        {
            startTick = reader.GetUInt();
            endTick = reader.GetUInt();
        }
    }

    public struct C2S_LoginPacket : INetSerializable
    {
        public string UserName; //账号
        public string Password;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(UserName);
            writer.Put(Password);
        }
        public void Deserialize(NetDataReader reader)
        {
            UserName = reader.GetString();
            Password = reader.GetString();
        }
    }

    public struct C2S_GetUserInfoPacket : INetSerializable
    {
        public short PeerId;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(PeerId);
        }
        public void Deserialize(NetDataReader reader)
        {
            PeerId = reader.GetShort();
        }
    }
    
    public struct C2S_RoleSelectPacket : INetSerializable
    {
        public byte Index;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Index);
        }
        public void Deserialize(NetDataReader reader)
        {
            Index = reader.GetByte();
        }
    }

    public struct C2S_GameReadyPacket : INetSerializable
    {
        public bool IsReady;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(IsReady);
        }
        public void Deserialize(NetDataReader reader)
        {
            IsReady = reader.GetBool();
        }
    }

    public struct C2S_BattleStartPacket : INetSerializable
    {
        public byte Stage; //阶段：〇倒计时前/~~①倒计时后~~~/②比赛暂停后恢复

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Stage);
        }
        public void Deserialize(NetDataReader reader)
        {
            Stage = reader.GetByte();
        }
    }

    public struct C2S_BattleReconnectPacket : INetSerializable
    {
        public bool Select;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Select);
        }
        public void Deserialize(NetDataReader reader)
        {
            Select = reader.GetBool();
        }
    }

    public struct C2S_BattleEndPacket : INetSerializable
    {
        public sbyte Winner;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Winner);
        }
        public void Deserialize(NetDataReader reader)
        {
            Winner = reader.GetSByte();
        }
    }
    #endregion

    #region 下行
    public struct S2C_ErrorPacket : INetSerializable
    {
        public byte ErrorCode; //错误码

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(ErrorCode);
        }
        public void Deserialize(NetDataReader reader)
        {
            ErrorCode = reader.GetByte();
        }
    }

    public struct S2C_JoinResultPacket : INetSerializable
    {
        public byte Code; //255
        public short HostId; //65535
        public short GuestId; //65535
        public string HostName;
        public string GuestName;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Code);
            writer.Put(HostId);
            writer.Put(GuestId);
            writer.Put(HostName);
            writer.Put(GuestName);
        }
        public void Deserialize(NetDataReader reader)
        {
            Code = reader.GetByte();
            HostId = reader.GetShort();
            GuestId = reader.GetShort();
            HostName = reader.GetString();
            GuestName = reader.GetString();
        }
    }

    public struct S2C_InputPacket : INetSerializable
    {
        public uint frameNumber;
        public uint[] inputs; //数组长度为2，双方的操作

        public const int Size = 4 + 4 * 2; //整个结构体长度

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(frameNumber);

            if (inputs == null)
                inputs = new uint[2];

            for (int i = 0; i < 2; i++)
            {
                writer.Put(inputs[i]);
            }
        }
        public void Deserialize(NetDataReader reader)
        {
            frameNumber = reader.GetUInt();

            if (inputs == null)
                inputs = new uint[2] { 0, 0 }; //注意：嵌套结构体，内层数组默认是Null，要初始化一下！！

            for (int i = 0; i < 2; i++)
            {
                inputs[i] = reader.GetUInt();
            }
        }
    }

    public struct S2C_LackInputPacket : INetSerializable
    {
        public uint frameNumber;
        public S2C_InputPacket[] inputs;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(frameNumber);

            if (inputs == null)
                inputs = new S2C_InputPacket[frameNumber + 1];

            for (int i = 0; i < frameNumber; i++)
            {
                inputs[i].Serialize(writer);
            }
        }
        public void Deserialize(NetDataReader reader)
        {
            frameNumber = reader.GetUInt();

            if (inputs == null)
                inputs = new S2C_InputPacket[frameNumber + 1]; //注意：嵌套结构体，内层数组默认是Null，要初始化一下！！

            for (int i = 0; i < frameNumber; i++)
            {
                inputs[i].Deserialize(reader);
            }
        }
    }

    public struct S2C_GetRoomPacket : INetSerializable
    {
        public short RoomId;       // 房间ID
        public short[] Peers;    // 座位上的玩家PeerID

        public const int Size = 2 + 2 * 2;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(RoomId);

            if (Peers == null)
                Peers = new short[2] { -1, -1 };

            for (int i = 0; i < 2; i++)
            {
                writer.Put(Peers[i]);
            }
        }
        public void Deserialize(NetDataReader reader)
        {
            RoomId = reader.GetShort();

            if (Peers == null)
                Peers = new short[2] { -1, -1 }; //注意点：嵌套结构体，内层数组默认是Null，要初始化一下！！

            for (int i = 0; i < 2; i++)
            {
                var t = reader.GetShort();
                Peers[i] = t;
            }
        }

        public override string ToString()
        {
            string str = $"房间#{RoomId}, [主位]={Peers[0]}, [客位]={Peers[1]}";
            return str;
        }
    }

    public struct S2C_LoginResultPacket : INetSerializable
    {
        public byte Code; //255
        public short PeerId; //65535
        public string UserName; //账号
        public string NickName; //昵称

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Code);
            writer.Put(PeerId);
            writer.Put(UserName);
            writer.Put(NickName);
        }
        public void Deserialize(NetDataReader reader)
        {
            Code = reader.GetByte();
            PeerId = reader.GetShort();
            UserName = reader.GetString();
            NickName = reader.GetString();
        }
    }

    public struct S2C_GetUserInfoPacket : INetSerializable
    {
        public short PeerId;
        public string UserName;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(PeerId);
            writer.Put(UserName);
        }
        public void Deserialize(NetDataReader reader)
        {
            PeerId = reader.GetShort();
            UserName = reader.GetString();
        }
    }

    // 请求匹配回包
    public struct UserInfo : INetSerializable
    {
        public short PeerId;
        public string UserName;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(PeerId);
            writer.Put(UserName);
        }
        public void Deserialize(NetDataReader reader)
        {
            PeerId = reader.GetShort();
            UserName = reader.GetString();
        }
    }
    public struct S2C_MatchResultPacket : INetSerializable
    {
        public byte Code; //结果码：匹配成功(0)，取消(1)，退出(2)，重连?(3)
        public byte BattleMode; //游戏模式
        public short RoomId; //房间ID
        public UserInfo Host;
        public UserInfo Guest;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Code);
            writer.Put(BattleMode);
            writer.Put(RoomId);
            Host.Serialize(writer);
            Guest.Serialize(writer);
        }
        public void Deserialize(NetDataReader reader)
        {
            Code = reader.GetByte();
            BattleMode = reader.GetByte();
            RoomId = reader.GetShort();
            Host.Deserialize(reader);
            Guest.Deserialize(reader);
        }

        public override string ToString()
        {
            string str = $"匹配结果: {Code}, 房间#{RoomId}, 主位#{Host.UserName}, 客位#{Guest.UserName}";
            return str;
        }
    }

    public struct S2C_RoleSelectPacket : INetSerializable
    {
        public byte SeatId;     //操作者
        public byte RoleIndex;  //选择的角色

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(SeatId);
            writer.Put(RoleIndex);
        }
        public void Deserialize(NetDataReader reader)
        {
            SeatId = reader.GetByte();
            RoleIndex = reader.GetByte();
        }
    }

    public struct S2C_GameReadyPacket : INetSerializable
    {
        public byte HostStatus; //主位状态
        public byte GuestStatus; //客位状态

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(HostStatus);
            writer.Put(GuestStatus);
        }
        public void Deserialize(NetDataReader reader)
        {
            HostStatus = reader.GetByte();
            GuestStatus = reader.GetByte();
        }
    }

    // 角色加载所需信息
    public struct PlayerLoadPacket : INetSerializable
    {
        public string UserName; // 玩家昵称
        public short PeerId;    // 玩家Id
        //public int Score;       // 玩家积分
        //public int Rank;        // 玩家排名
        public byte RoleIndex;  // 角色编号
        //public byte RoleColor;  // 角色颜色(双方角色相同时区分)
        //public byte RoleCloth;  // 角色时装
        public void Serialize(NetDataWriter writer)
        {
            writer.Put(UserName);
            writer.Put(PeerId);
            writer.Put(RoleIndex);
        }
        public void Deserialize(NetDataReader reader)
        {
            UserName = reader.GetString();
            PeerId = reader.GetShort();
            RoleIndex = reader.GetByte();
        }

        public override string ToString()
        {
            string stringBuild = $"{PeerId} select {RoleIndex}";
            return stringBuild;
        }
    }

    // 下发初始化场景所需的参数。服务器房间内也要备份。
    public struct S2C_LoadScenePacket : INetSerializable
    {
        public short RoomId;    //要加入的房间号
        public string BattleId; //服务器战斗编号
        public byte MapId;      //地图
        public PlayerLoadPacket Host;
        public PlayerLoadPacket Guest;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(RoomId);
            writer.Put(BattleId);
            writer.Put(MapId);
            Host.Serialize(writer);
            Guest.Serialize(writer);
        }
        public void Deserialize(NetDataReader reader)
        {
            RoomId = reader.GetShort();
            BattleId = reader.GetString();
            MapId = reader.GetByte();
            Host.Deserialize(reader);
            Guest.Deserialize(reader);
        }

        public override string ToString()
        {
            string stringBuild = $"RoomId={RoomId}, P1=[{Host.ToString()}], P2=[{Guest.ToString()}]";
            return stringBuild;
        }
    }

    // 第一帧同步，战斗开始
    public struct S2C_BattleStartPacket : INetSerializable
    {
        public byte Stage; //阶段：[0]加载场景完成；[1]倒计时后；[2]恢复战斗；

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Stage);
        }
        public void Deserialize(NetDataReader reader)
        {
            Stage = reader.GetByte();
        }
    }

    // ①玩家请求暂停(30s)
    // ②玩家掉线服务器通知暂停(60s)
    public struct S2C_BattlePausePacket : INetSerializable
    {
        public byte Duration; //暂停时间(秒)

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Duration);
        }
        public void Deserialize(NetDataReader reader)
        {
            Duration = reader.GetByte();
        }
    }

    // 比赛结算（认输/战死/时间到）
    public struct S2C_BattleEndPacket : INetSerializable
    {
        public sbyte WinnerSeatId; //获胜方的座位Id
        //public int Score; //得分

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(WinnerSeatId);
        }
        public void Deserialize(NetDataReader reader)
        {
            WinnerSeatId = reader.GetSByte();
        }
    }
    #endregion
}