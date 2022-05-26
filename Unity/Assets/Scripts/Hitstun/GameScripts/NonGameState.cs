using HitstunConstants;

public struct PlayerConnectionInfo
{
    public int handle;
    public PlayerType type; //自己还是对方
    public PlayerConnectState connectState; //是否掉线
    public int controllerId;
};

public struct ChecksumInfo
{
    public int frameNumber;
    public int checksum;
};

public class NonGameState
{
    public PlayerConnectionInfo[] players;
    public string status; //没用
    public ChecksumInfo currentChecksum; //没用

    public void SetConnectState(int handle, PlayerConnectState state)
    {
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i].handle == handle)
            {
                players[i].connectState = state;
                break;
            }
        }
    }
}