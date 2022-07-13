using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;
using UnityEngine;
using Code.Client;
#if UNITY_EDITOR
using UnityEditor;
public class ReplayWindow : EditorWindow
{
    static ReplayWindow window;

    private ClientLogic logic;
    private ReplayFormat replay;
    private Vector2 scroll;

    public static void ShowWindow()
    {
        window = (ReplayWindow)GetWindow(typeof(ReplayWindow));
        window.titleContent = new GUIContent("回放窗口");
        window.Show();
    }

    void OnEnable()
    {
        logic = FindObjectOfType<ClientLogic>();
        replay = new ReplayFormat(logic);
        scroll = new Vector2(0, 100);
    }

    void OnGUI()
    {
        if (logic == null || replay == null) return;
        scroll = GUILayout.BeginScrollView(scroll, "回放");
        GUILayout.Label(replay.PrintPredict());
        GUILayout.EndScrollView();
    }
}
#endif

[System.Serializable]
public class ReplayFormat
{
    //public S2C_LoadScenePacket sceneData;
    //public Dictionary<int, GameState> storeBuffer; //运行时快照
    //public int WinnerSeatId;
    public ReplayFormat()
    {
        gs = new GameState();
        ggpo_predict = new Dictionary<uint, uint[]>();
        ggpo_recieve = new Dictionary<uint, uint[]>();
        cache_buffer = new Dictionary<uint, byte[]>();
    }
    public ReplayFormat(ClientLogic logic)
    {
        gs = new GameState();
        ggpo_predict = logic.ggpo_predict;
        ggpo_recieve = logic.ggpo_recieve;
        cache_buffer = logic.cache_buffer;
    }
    public GameState gs;
    public Dictionary<uint, uint[]> ggpo_predict; //预测帧
    public Dictionary<uint, uint[]> ggpo_recieve; //下发帧
    public Dictionary<uint, byte[]> cache_buffer; //快照缓存

    public void BufferToGS(byte[] bytes)
    {
        using (var memoryStream = new MemoryStream(bytes))
        {
            using (var reader = new BinaryReader(memoryStream))
            {
                gs.Deserialize(reader);
            }
        }
    }
    public byte[] GSToBuffer()
    {
        using (var memoryStream = new MemoryStream())
        {
            using (var writer = new BinaryWriter(memoryStream))
            {
                gs.Serialize(writer);
            }
            return memoryStream.ToArray();
        }
    }
    public string PrintPredict()
    {
        string result = string.Empty;
        foreach (var node in ggpo_predict)
        {
            string nodeStr = $"\n[{node.Key}] {node.Value[0]}, {node.Value[1]}";
            result += nodeStr;
        }
        return result;
    }
    public string PrintRecieve()
    {
        string result = string.Empty;
        foreach (var node in ggpo_recieve)
        {
            string nodeStr = $"\n[{node.Key}] {node.Value[0]}, {node.Value[1]}";
            result += nodeStr;
        }
        return result;
    }
    public override string ToString()
    {
        string result = string.Empty;
        foreach (var node in cache_buffer)
        {
            string nodeStr = $"\n[{node.Key}] {node.Value[0]}, {node.Value[1]}";
            result += nodeStr;
        }
        return result;
    }
}
public class ReplayManager
{
    // 写入文件
    public static void SaveReplay(Dictionary<uint, byte[]> buffer)
    {
        var obj = new ReplayFormat
        {
            cache_buffer = buffer,
        };
        byte[] bytes = ObjectToBytes(obj);

        string fileName = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string folder = $"{ConstValue.REPLAY_FOLDER}";
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        string filePath = $"{folder}/{fileName}.bytes";
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            Debug.LogError("先删除，再保存");
        }

        File.WriteAllBytes(filePath, bytes);
        Debug.Log($"replay saved in: {filePath}");
    }
    public static string MyDictionaryToJson(Dictionary<uint, uint[]> dict)
    {
        var entries = dict.Select(d => $"\"{d.Key}\": [{string.Join(",", d.Value)}]");
        return "{" + string.Join(",", entries) + "}";
    }
    // 读取文件
    public static void LoadReplay(string filePath = "")
    {

    }
    
    public static byte[] ObjectToBytes(object obj)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            IFormatter formatter = new BinaryFormatter();
            formatter.Serialize(ms, obj);
            return ms.GetBuffer();
        }
    }
    public static object BytesToObject(byte[] Bytes)
    {
        using (MemoryStream ms = new MemoryStream(Bytes))
        {
            IFormatter formatter = new BinaryFormatter();
            return formatter.Deserialize(ms);
        }
    }
}