using System.Diagnostics;
using UnityEditor;

public class EditorTools : Editor
{
    [MenuItem("Tools/Æô¶¯·þÎñÆ÷")]
    static void RunServer()
    {
        string filepath = "D:\\Documents\\GitHub\\MoeFight\\Unity\\Build\\Server\\moefight.exe";
        Process.Start(filepath);
    }
}