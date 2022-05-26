using UnityEngine;

public class ExampleConfig : ScriptableObject
{
    public string IP = "localhost";
    public int PORT = 5000;
    public string KEY = "ExampleGame";
    public int DelayFrame = 2; //延迟帧数，让本地操作感受到延迟。根据Ping值动态调整，覆盖掉网络抖动和延迟。Ping(ms) = DelayFrame * 20ms
    public int RollbackFrame = 2; //回滚帧数，发生错误后非立即，而是延迟几帧进行回滚，避免密集的画面跳动。
}