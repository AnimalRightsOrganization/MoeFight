using System;
using LiteNetLib.Utils;

namespace Code.Shared
{
    public enum PacketType : byte
    {
        //////////////
        C2S_Login,
        C2S_Input,
        //////////////
        S2C_Login,
        S2C_FirstSync,
        S2C_Input,
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