using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using Code.Shared;
using Newtonsoft.Json;

[System.Serializable]
public class ReplayFormat
{
    public S2C_LoadScenePacket scene;
    public sbyte winnerId;
    public byte battleMode;
    public Dictionary<uint, uint[]> inputs; //下发帧

    public ReplayFormat()
    {
        inputs = new Dictionary<uint, uint[]>();
    }

    public override string ToString()
    {
        string result = string.Empty;
        foreach (var node in inputs)
        {
            string nodeStr = $"\n[{node.Key}] {node.Value[0]}, {node.Value[1]}";
            result += nodeStr;
        }
        return result;
    }
}
public class ReplayManager
{
    public static ReplayFormat data;
    // 写入
    public static async void SaveReplay(ReplayFormat dict)
    {
        string folder = ConstValue.USER_REPLAY_FOLDER;
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        string fileName = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string filePath = $"{folder}/{fileName}.bytes";
        if (File.Exists(filePath))
            File.Delete(filePath);

        string json = JsonConvert.SerializeObject(dict);
        Debug.Log(json);
        await WriteTextAsync(filePath, json);
    }
    static async Task WriteTextAsync(string filePath, string text)
    {
        byte[] data = Encoding.UTF8.GetBytes(text);
        using (var fs = new FileStream(filePath, FileMode.Append, FileAccess.Write))
        {
            await fs.WriteAsync(data, 0, data.Length);
            Debug.Log($"write to: {filePath}");
        };
    }
    // 读取
    public static async Task<ReplayFormat> LoadReplay(string filePath = "")
    {
        string json = await SimpleReadAsync(filePath);
        //Debug.Log(json);
        data = JsonConvert.DeserializeObject<ReplayFormat>(json);
        return data;
    }
    static async Task<string> SimpleReadAsync(string filePath = "")
    {
        return await File.ReadAllTextAsync(filePath, Encoding.UTF8);
    }
}