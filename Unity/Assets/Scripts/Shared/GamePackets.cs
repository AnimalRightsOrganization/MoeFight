using System;
using LiteNetLib.Utils;

namespace Code.Shared
{
    public enum PacketType : byte
    {
        Movement,
        Spawn,
        ServerState,
        Serialized,
        Shoot,
        C2S_Login,
        S2C_Login,
    }

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


    //Auto serializable packets
    public class JoinPacket
    {
        public string UserName { get; set; }
    }

    public class JoinAcceptPacket
    {
        public byte Id { get; set; }
        public ushort ServerTick { get; set; }
    }

    public class PlayerJoinedPacket
    {
        public string UserName { get; set; }
        public bool NewPlayer { get; set; }
        public byte Health { get; set; }
        public ushort ServerTick { get; set; }
        public PlayerState InitialPlayerState { get; set; }
    }

    public class PlayerLeavedPacket
    {
        public byte Id { get; set; }
    }

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
    
    public struct PlayerState : INetSerializable
    {
        public byte Id;
        public float Rotation;
        public ushort Tick;

        public const int Size = 1 + 8 + 4 + 2;
        
        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put(Rotation);
            writer.Put(Tick);
        }

        public void Deserialize(NetDataReader reader)
        {
            Id = reader.GetByte();
            Rotation = reader.GetFloat();
            Tick = reader.GetUShort();
        }
    }

    public struct ServerState : INetSerializable
    {
        public ushort Tick;
        public ushort LastProcessedCommand;
        
        public int PlayerStatesCount;
        public int StartState; //server only
        public PlayerState[] PlayerStates;
        
        //tick
        public const int HeaderSize = sizeof(ushort)*2;
        
        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Tick);
            writer.Put(LastProcessedCommand);
            
            for (int i = 0; i < PlayerStatesCount; i++)
                PlayerStates[StartState + i].Serialize(writer);
        }

        public void Deserialize(NetDataReader reader)
        {
            Tick = reader.GetUShort();
            LastProcessedCommand = reader.GetUShort();
            
            PlayerStatesCount = reader.AvailableBytes / PlayerState.Size;
            if (PlayerStates == null || PlayerStates.Length < PlayerStatesCount)
                PlayerStates = new PlayerState[PlayerStatesCount];
            for (int i = 0; i < PlayerStatesCount; i++)
                PlayerStates[i].Deserialize(reader);
        }
    }
}