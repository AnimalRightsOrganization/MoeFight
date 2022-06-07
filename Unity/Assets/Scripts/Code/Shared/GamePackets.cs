using LiteNetLib.Utils;

namespace Code.Shared
{
    public enum PacketType : byte
    {
        //////////////
        C2S_TestX1Req       ,   //独立启动加入
        C2S_TestX2Req       ,   //双人启动加入
        C2S_BattleStart     ,   //请求开始战斗
        C2S_BattlePause     ,   //请求暂停战斗
        C2S_BattleQuit      ,   //离开比赛（认输） =>返回大厅
        C2S_BattleEnd       ,   //上报比赛结果（双方都要发，由战斗系统判定）
        C2S_Lockstep        ,   //帧同步
        C2S_LackFrames      ,   //缺失帧
        //////////////
        S2C_ErrorOperate    ,   //错误代码
        S2C_TestX1Result    ,   //独立启动加入
        S2C_TestX2Result    ,   //双人启动加入
        S2C_BattleStart     ,   //开始战斗（第一帧同步）
        S2C_BattlePause     ,   //暂停战斗（暂停帧同步）
        S2C_BattleEnd       ,   //比赛结束，结算
        S2C_Lockstep        ,   //帧同步
        S2C_LackFrames      ,   //丢失帧
    }

    #region 公用
    public struct EmptyPacket : INetSerializable
    {
        public void Serialize(NetDataWriter writer) { }
        public void Deserialize(NetDataReader reader) { }
    }
    #endregion

    #region 上行
    public struct C2S_JoinPacket : INetSerializable
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
    #endregion

    #region 下行
    // 错误码回包
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
    #endregion

    #region 帧同步
    [System.Serializable] //必须序列化才能保存
    public struct InputBuffer : INetSerializable
    {
        public static implicit operator InputBuffer(byte a)
        {
            return new InputBuffer { Dir = 5, Hit = 0, KeyDown = 0 };
        }
        public static InputBuffer Default = 5;

        public byte Dir;
        public byte Hit;
        public byte KeyDown;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Dir);
            writer.Put(Hit);
            writer.Put(KeyDown);
        }
        public void Deserialize(NetDataReader reader)
        {
            Dir = reader.GetByte();
            Hit = reader.GetByte();
            KeyDown = reader.GetByte();
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            var otherBuffer = (InputBuffer)obj;
            bool cond1 = Dir.Equals(otherBuffer.Dir);
            bool cond2 = Hit.Equals(otherBuffer.Hit);
            bool cond3 = KeyDown.Equals(otherBuffer.KeyDown);
            return cond1 && cond2 && cond3;
        }
        public override string ToString()
        {
            return $"dir={Dir}，hit={Hit}，keyDown={KeyDown}";
        }
    }

    // 客户端操作（5字节）
    public struct C2S_InputBufferPacket : INetSerializable
    {
        public ushort Tick;
        public InputBuffer Operation;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Tick);
            Operation.Serialize(writer);
        }
        public void Deserialize(NetDataReader reader)
        {
            Tick = reader.GetUShort();
            Operation.Deserialize(reader);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            var otherBuffer = (C2S_InputBufferPacket)obj;
            bool cond0 = Tick.Equals(otherBuffer.Tick);
            bool cond1 = Operation.Equals(otherBuffer.Operation);
            return cond0 && cond1;
        }
        public override string ToString()
        {
            return $"Tick={Tick}，op={Operation.ToString()}";
        }
    }
    // 请求缺失帧
    public struct C2S_LackFramesPacket : INetSerializable
    {
        public ushort FromFrameID;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(FromFrameID);
        }
        public void Deserialize(NetDataReader reader)
        {
            FromFrameID = reader.GetUShort();
        }
    }

    // 服务器同步操作（8字节）
    public struct S2C_AllPlayerOperationPacket : INetSerializable
    {
        public static implicit operator S2C_AllPlayerOperationPacket(ushort a)
        {
            return new S2C_AllPlayerOperationPacket
            {
                ServerTick = a,
                HostOperation = InputBuffer.Default,
                GuestOperation = InputBuffer.Default,
            };
        }
        public static S2C_AllPlayerOperationPacket Default = 0;

        public ushort ServerTick;
        public InputBuffer HostOperation;
        public InputBuffer GuestOperation;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(ServerTick);
            HostOperation.Serialize(writer);
            GuestOperation.Serialize(writer);
        }
        public void Deserialize(NetDataReader reader)
        {
            ServerTick = reader.GetUShort();
            HostOperation.Deserialize(reader);
            GuestOperation.Deserialize(reader);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        /*
        public override bool Equals(object obj)
        {
            var record = (S2C_AllPlayerOperationPacket)obj;
            bool cond1 = record.ServerTick == ServerTick;
            bool cond2 = HostOperation.Equals(record.HostOperation);
            bool cond3 = GuestOperation.Equals(record.GuestOperation);
            if (!cond2)
            {
                Debug.LogError($"主位不同{ServerTick}，Dir={HostOperation.Dir}/{record.HostOperation.Dir}");
            }
            if (!cond3)
            {
                Debug.LogError($"客位不同{ServerTick}，Dir={GuestOperation.Dir}/{record.GuestOperation.Dir}");
            }
            //return cond1 && cond2 && cond3;
            return cond2 && cond3;
        }
        public override string ToString()
        {
            return $"ServerTick={ServerTick}，Host={HostOperation.ToString()}，Guest={GuestOperation.ToString()}";
        }*/
    }
    // 下发缺失帧
    public struct S2C_LackFramesPacket : INetSerializable
    {
        public int FrameCount;
        public S2C_AllPlayerOperationPacket[] Frames;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(FrameCount);

            for (int i = 0; i < FrameCount; i++)
                Frames[i].Serialize(writer);
        }
        public void Deserialize(NetDataReader reader)
        {
            FrameCount = reader.GetInt();

            if (Frames == null)
                Frames = new S2C_AllPlayerOperationPacket[FrameCount];
            for (int i = 0; i < FrameCount; i++)
                Frames[i].Deserialize(reader);
        }
    }
    #endregion
}