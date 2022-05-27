using System;
using System.IO;
using System.Diagnostics;
using UnityEngine;
using UnityEditor;
using Debug = UnityEngine.Debug;

public class EditorTools : Editor
{
    //% (ctrl on Windows and Linux, cmd on macOS),
    //^ (ctrl on Windows, Linux, and macOS),
    //# (shift),
    //& (alt)
    [MenuItem("Tools/启动/客户端 %_F11", false)]
    static void RunClient()
    {
        string filepath = $"D:\\Documents\\GitHub\\MoeFight\\Unity\\Build\\Client\\{Application.productName}.exe";
        Process.Start(filepath);
    }
    [MenuItem("Tools/启动/服务器 %_F12", false)]
    static void RunServer()
    {
        string filepath = $"D:\\Documents\\GitHub\\MoeFight\\Unity\\Build\\Server\\{Application.productName}.exe";
        Process.Start(filepath);
    }

    [MenuItem("Tools/打包/客户端", false)]
    static void BuildClient()
    {
        string curr_dir = Environment.CurrentDirectory;
        var curr_info = new DirectoryInfo("build_dir");

        string build_root = $"{curr_info.Parent}/Build";
        if (!Directory.Exists(build_root))
            Directory.CreateDirectory(build_root);

        string build_dir = $"{build_root}/Client";
        if (Directory.Exists(build_dir))
            Directory.Delete(build_dir, true);
        Directory.CreateDirectory(build_dir);

        //BuildTarget buildTarget = (BuildTarget)System.Enum.Parse(typeof(BuildTarget), ConstValue.PLATFORM_NAME);

        BuildPlayerOptions opt = new BuildPlayerOptions();
        opt.scenes = new string[] { "Assets/Scenes/Client.unity" };
        opt.locationPathName = $"{build_dir}/{Application.productName}.exe";
        opt.target = BuildTarget.StandaloneWindows64;
        opt.options = BuildOptions.None;

        BuildPipeline.BuildPlayer(opt);

        Debug.Log($"打包成功: {opt.locationPathName}");
    }
    [MenuItem("Tools/打包/服务器", false)]
    static void BuildServer()
    {
        string curr_dir = Environment.CurrentDirectory;
        var curr_info = new DirectoryInfo("build_dir");

        string build_root = $"{curr_info.Parent}/Build";
        if (!Directory.Exists(build_root))
            Directory.CreateDirectory(build_root);

        string build_dir = $"{build_root}/Server";
        try
        {
            if (Directory.Exists(build_dir))
                Directory.Delete(build_dir, true);
        }
        catch (Exception e)
        {
            Debug.LogError($"无法删除: {e}");
        }
        Directory.CreateDirectory(build_dir);

        //BuildTarget buildTarget = (BuildTarget)System.Enum.Parse(typeof(BuildTarget), ConstValue.PLATFORM_NAME);

        BuildPlayerOptions opt = new BuildPlayerOptions();
        opt.scenes = new string[] { "Assets/Scenes/Server.unity" };
        opt.locationPathName = $"{build_dir}/{Application.productName}.exe";
        opt.target = BuildTarget.StandaloneWindows64;
        opt.options = BuildOptions.EnableHeadlessMode;

        BuildPipeline.BuildPlayer(opt);

        Debug.Log($"打包成功: {opt.locationPathName}");
    }
    [MenuItem("Tools/打包/资源", false)]
    static void BuildRes() { }
}