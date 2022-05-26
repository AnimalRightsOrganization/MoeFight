using UnityEngine;
//using FPLibrary;
using Unity.Collections;

[System.Serializable]
public class GlobalConfig : ScriptableObject
{
    public string IP = "127.0.0.1";
    public ushort Port = 9050;      //端口(0~65535)
    public string Key = "ExampleGame";

    public ushort renderFPS = 60;   //渲染频率(30/60/90)
    public ushort logicFPS = 50;    //帧同步频率(30/60)
    public int TotalSecond = 99;    //比赛时长99(s)
}