using UnityEngine;

[System.Serializable]
public class GlobalConfig : ScriptableObject
{
    public string IP = "192.168.1.101";
    public int Port = 5000;      //端口(0~65535)
    public string Key = "ExampleGame";

    public ushort renderFPS = 60;   //渲染频率(30/60/90)
    public ushort logicFPS = 60;    //帧同步频率(30/60)
    public int TotalSecond = 99;    //比赛时长99(s)
}