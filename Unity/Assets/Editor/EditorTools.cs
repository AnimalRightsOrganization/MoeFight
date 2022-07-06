using System;
using System.IO;
using System.Diagnostics;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using Code.Client;
using Code.Server;
using Debug = UnityEngine.Debug;

public class TestWindow : EditorWindow
{
    static TestWindow window;

    public static void ShowWindow()
    {
        window = (TestWindow)GetWindow(typeof(TestWindow));
        window.titleContent = new GUIContent("调试窗口");
        window.Show();
    }
    
    void OnGUI()
    {
        if (GUILayout.Button("登录.自动填写.test1"))
        {
            var login = HotFix.UIManager.Get().GetUI<HotFix.UI_Login>();
            login.m_UserNameField.text = "test1";
            login.m_PasswordField.text = "123456";
        }
        if (GUILayout.Button("登录.自动填写.test2"))
        {
            var login = HotFix.UIManager.Get().GetUI<HotFix.UI_Login>();
            login.m_UserNameField.text = "test2";
            login.m_PasswordField.text = "123456";
        }
        if (GUILayout.Button("PVE"))
        {
            var asset = ResManager.LoadPrefab("Prefabs/ClientLogic");
            UnityEngine.Object.Instantiate(asset);
            HotFix.UIManager.Get().PopAll();

            ClientNet.Get.SendTestPVE();
        }
        if (GUILayout.Button("PVP"))
        {
            var asset = ResManager.LoadPrefab("Prefabs/ClientLogic");
            UnityEngine.Object.Instantiate(asset);
            HotFix.UIManager.Get().PopAll();

            ClientNet.Get.SendTestPVP();
        }

        if (GUILayout.Button("打印预测"))
        {
            ReplayWindow.ShowWindow();
        }

        if (GUILayout.Button("打印服务器"))
        {
            ServerNet.Get.m_PlayerManager.Print();
        }
    }
}
public class EditorTools : Editor
{
    //% (ctrl on Windows and Linux, cmd on macOS),
    //^ (ctrl on Windows, Linux, and macOS),
    //# (shift),
    //& (alt)
    [MenuItem("Tools/启动/调试 %_F10", false)]
    static void RunEditor()
    {
        TestWindow.ShowWindow();
    }
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
    static void BuildWindows()
    {
        //EditorBuildSettings.scenes = new EditorBuildSettingsScene[] { new EditorBuildSettingsScene("Assets/Scenes/Client.unity", true) };
        //EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);

        string curr_dir = Environment.CurrentDirectory;
        var curr_info = new DirectoryInfo(curr_dir);
        string build_root = $"{curr_info}/Build";
        if (!Directory.Exists(build_root))
            Directory.CreateDirectory(build_root);
        string build_dir = $"{build_root}/Client";
        if (Directory.Exists(build_dir))
            Directory.Delete(build_dir, true);
        Directory.CreateDirectory(build_dir);

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
        //EditorBuildSettings.scenes = new EditorBuildSettingsScene[] { new EditorBuildSettingsScene("Assets/Scenes/Server.unity", true) };
        //EditorUserBuildSettings.SwitchActiveBuildTarget(NamedBuildTarget.Server, BuildTarget.StandaloneWindows64);

        string curr_dir = Environment.CurrentDirectory;
        var curr_info = new DirectoryInfo(curr_dir);
        string build_root = $"{curr_info}/Build";
        if (!Directory.Exists(build_root))
            Directory.CreateDirectory(build_root);
        string build_dir = $"{build_root}/Server";
        if (Directory.Exists(build_dir))
            Directory.Delete(build_dir, true);
        Directory.CreateDirectory(build_dir);

        BuildPlayerOptions opt = new BuildPlayerOptions();
        opt.scenes = new string[] { "Assets/Scenes/Server.unity" };
        opt.locationPathName = $"{build_dir}/{Application.productName}.exe";
        opt.target = BuildTarget.StandaloneWindows64;
        opt.options = BuildOptions.None;
        BuildPipeline.BuildPlayer(opt);
        Debug.Log($"打包成功: {opt.locationPathName}");
    }
    static void BuildAndroid()
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(NamedBuildTarget.Android, BuildTarget.Android);

        string defines = "USE_ASSETBUNDLE;CHANNEL_11011";
        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, defines);
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.moegijinka.moefight"); //不同渠道包名不一样
        //PlayerSettings.bundleVersion = string.Format("{0}.{1}.{2}", GameConfig.clientVersions[0],
        //    GameConfig.clientVersions[1] * 100 + GameConfig.clientVersions[2], GameConfig.clientVersions[3]);
    }
    static void BuildiOS()
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(NamedBuildTarget.iOS, BuildTarget.iOS);

        string defines = "USE_ASSETBUNDLE;CHANNEL_11011";
        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS, defines);
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, "com.moegijinka.moefight"); //不同渠道包名不一样
        //PlayerSettings.bundleVersion = string.Format("{0}.{1}.{2}", GameConfig.clientVersions[0],
        //    GameConfig.clientVersions[1] * 100 + GameConfig.clientVersions[2], GameConfig.clientVersions[3]);

        int code;
        if (int.TryParse(PlayerSettings.iOS.buildNumber, out code) == false)
        {
            code = 0;
        }
        PlayerSettings.iOS.buildNumber = (code + 1).ToString();
    }
    [MenuItem("Tools/打包/资源", false)]
    static void BuildRes()
    {
        BuildTarget target = (BuildTarget)System.Enum.Parse(typeof(BuildTarget), ConstValue.PLATFORM_NAME);
        Debug.Log($"打包{target}平台资源");
        BundleTools.Build_Target(target);
    }
}