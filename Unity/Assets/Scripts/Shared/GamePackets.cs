using System;
using LiteNetLib.Utils;

namespace Code.Shared
{
    public enum PacketType : byte
    {
        //////////////
        C2S_LoginReq        ,   //登录请求
        C2S_BattleStart     ,   //请求开始战斗
        C2S_BattlePause     ,   //请求暂停战斗
        C2S_BattleQuit      ,   //离开比赛（认输） =>返回大厅
        C2S_BattleEnd       ,   //上报比赛结果（双方都要发，由战斗系统判定）
        C2S_Lockstep        ,   //帧同步
        C2S_LackFrames      ,   //缺失帧
        //////////////
        S2C_ErrorOperate    ,   //错误代码
        S2C_LoginResult     ,   //登录结果
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
    public struct LoginRequest : INetSerializable
    {
        public string UserName;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(UserName);
        }

        public void Deserialize(NetDataReader reader)
        {
            UserName = reader.GetString();
        }
    }

    public struct InputPacket : INetSerializable
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

    public struct LoginResponse : INetSerializable
    {
        public string UserName;
        public string Token;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(UserName);
            writer.Put(Token);
        }

        public void Deserialize(NetDataReader reader)
        {
            UserName = reader.GetString();
            Token = reader.GetString();
        }
    }
    #endregion

    [Flags]
    public enum MovementKeys : byte
    {
        Left = 1 << 1,
        Right = 1 << 2,
        Up = 1 << 3,
        Down = 1 << 4,
        Fire = 1 << 5
    }

    public struct PlayerInputPacket : INetSerializable
    {
        public ushort Id;
        public MovementKeys Keys;
        public float Rotation;
        public ushort ServerTick;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put((byte)Keys);
            writer.Put(Rotation);
            writer.Put(ServerTick);
        }

        public void Deserialize(NetDataReader reader)
        {
            Id = reader.GetUShort();
            Keys = (MovementKeys)reader.GetByte();
            Rotation = reader.GetFloat();
            ServerTick = reader.GetUShort();
        }
    }
}