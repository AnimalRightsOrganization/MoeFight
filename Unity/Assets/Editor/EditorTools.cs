using System;
using System.IO;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using Debug = UnityEngine.Debug;
using Code.Client;
using Code.Server;
using Code.Shared;
using HitstunConstants;

public class TestWindow : EditorWindow
{
    static TestWindow window;

    public bool UseOpening;

    public static void ShowWindow()
    {
        window = (TestWindow)GetWindow(typeof(TestWindow));
        window.titleContent = new GUIContent("调试窗口");
        window.Show();
    }

    void OnGUI()
    {
        switch (SceneManager.GetActiveScene().name)
        {
            case "Client": ClientGUI(); break;
            case "Server": ServerGUI(); break;
            case "Photo": PhotoGUI(); break;
            default: GUILayout.Label("空"); break;
        }
    }

    HotFix.UI_Login FillLogin(string uesr)
    {
        var login = HotFix.UIManager.Get().GetUI<HotFix.UI_Login>();
        login.m_UserNameField.text = uesr;
        login.m_PasswordField.text = "123456";
        return login;
    }
    void ClientGUI()
    {
        if (GUILayout.Button("登录.自动填写.test1"))
        {
            FillLogin("test1");
        }
        if (GUILayout.Button("登录.自动填写.test2"))
        {
            FillLogin("test2");
        }
        if (GUILayout.Button("Client Print"))
        {
            var str = ClientNet.Get.m_PlayerManager.LocalPlayer.ToString();
            Debug.Log(str);
        }
        GUILayout.BeginHorizontal();
        GUILayout.Space(20);
        UseOpening = GUILayout.Toggle(UseOpening, "使用过场", GUILayout.Height(30));
        if (GUILayout.Button("单机调试", GUILayout.Height(30)))
        {
            ClientNet.Get.Disconnect();
            //await Task.Delay(1);

            ClientPlayer host = new ClientPlayer("test1", 0);
            ClientPlayer guest = new ClientPlayer("bot", 1);
            ClientNet.Get.m_PlayerManager.AddClientPlayer(host, true);
            ClientNet.Get.m_PlayerManager.AddClientPlayer(guest, false);
            ClientNet.Get.m_ClientRoom = new ClientRoom(0, host, guest);
            var room = ClientNet.Get.m_ClientRoom;
            room.BattleMode = BattleMode.Training;
            var packet = new S2C_LoadScenePacket
            {
                RoomId = (short)room.RoomID,
                BattleId = room.BattleID,
                MapId = room.MapId,
                Host = new PlayerLoadPacket { UserName = host.UserName, PeerId = host.PeerId, RoleIndex = (int)CharacterName.KEN },
                Guest = new PlayerLoadPacket { UserName = guest.UserName, PeerId = guest.PeerId, RoleIndex = (int)CharacterName.SATOMI },
            };
            Debug.Log($"调试模式:\n{packet}");

            GameManager.Get.OnLoadScene(packet, UseOpening);
        }
        GUILayout.EndHorizontal();
        if (GUILayout.Button("重载配置"))
        {
            ClientLogic.Get.runner.Reload();
        }
    }
    void ServerGUI()
    {
        if (GUILayout.Button("Server Print"))
        {
            ServerNet.Get.m_PlayerManager.Print();
            ServerNet.Get.m_RoomManager.Print();
        }
        if (GUILayout.Button("Room Print"))
        {
            ServerRoom room = ServerNet.Get.m_RoomManager.GetAll()[0];
            Debug.Log($"room#{room.RoomID}:{room.BattleStage}:{room.hostPlayer.RoleIndex} vs {room.guestPlayer.RoleIndex}" +
                $"\nbufferTick:{room.bufferTick}, serverTick:{room.serverTick}, dic_recv:{room.dic_recv.Count}");
        }
    }
    void PhotoGUI()
    {
        if (GUILayout.Button("SnapShot"))
        {
            string fileName = $"{Application.streamingAssetsPath}/Actor_{DateTime.Now.ToString("yyyyMMddhhmmss")}.png";
            ScreenCapture.CaptureScreenshot(fileName);
            Debug.Log(fileName);
            AssetDatabase.Refresh();
        }
    }
}
[InitializeOnLoad]
public class EditorTools : Editor
{
    static string kShowedReadmeSessionStateName = "ReadmeEditor.TestWindow";

    static EditorTools()
    {
        EditorApplication.delayCall += SelectReadmeAutomatically; //Inspector刷新时
    }
    static void SelectReadmeAutomatically()
    {
        if (!SessionState.GetBool(kShowedReadmeSessionStateName, false))
        {
            SessionState.SetBool(kShowedReadmeSessionStateName, true);

            RunEditor();
        }
    }

    const string notepad = @"C:\Program Files\Notepad++\notepad++.exe";
    const string hosts_path = @"C:\Windows\System32\drivers\etc\hosts";
    const string hosts_www = "Assets/Editor/etc/hosts_www"; //外网,原始hosts
    const string hosts_lan = "Assets/Editor/etc/hosts_lan"; //局域网,域名映射
    [MenuItem("Tools/运行/开发环境", false)]
    static void ModifyHost()
    {
        //string src = Path.Combine(ConstValue.UnityDir, hosts_lan);
        //Debug.Log($"{src}→{hosts_path}");
        //File.Move(src, hosts_path); //IOException: 文件存在。文件其实存在，但没有权限。

        //string content = File.ReadAllText(hosts_path);
        //Debug.Log(content);

        //Process.Start("notepad.exe", hosts_path); //系统记事本
        Process.Start(notepad, hosts_path); //notepad++
    }
    //% (ctrl on Windows and Linux, cmd on macOS),
    //^ (ctrl on Windows, Linux, and macOS),
    //# (shift),
    //& (alt)
    [MenuItem("Tools/运行/命令面板 %_F10", false)]
    static void RunEditor()
    {
        TestWindow.ShowWindow();
    }
    [MenuItem("Tools/运行/客户端 %_F11", false)]
    static void RunClient()
    {
        var curr_info = new DirectoryInfo(Environment.CurrentDirectory);
        string filepath = $"{curr_info}/Builds/Client/Client.exe";

        Process.Start(filepath);
    }
    [MenuItem("Tools/运行/服务器 %_F12", false)]
    static void RunServer()
    {
        var curr_info = new DirectoryInfo(Environment.CurrentDirectory);
        string filepath = $"{curr_info}/Builds/Server/GameServer.exe";

        Process.Start(filepath);
    }

    [MenuItem("Tools/打包/热更新", false, 1)]
    static void BuildRes()
    {
        BuildTarget target = (BuildTarget)System.Enum.Parse(typeof(BuildTarget), ConstValue.PLATFORM_NAME);
        Debug.Log($"打包{target}平台资源");
        BundleTools.Build_Target(target);
    }
    [MenuItem("Tools/打包/服务器", false, 2)]
    static void BuildServer_Win64()
    {
        RemoveIcon();

        var curr_info = new DirectoryInfo(Environment.CurrentDirectory);
        string builds_dir = $"{curr_info}/Builds/Server";

        BuildPlayerOptions opt = new BuildPlayerOptions
        {
            scenes = new string[] { "Assets/Scenes/Server.unity" },
            locationPathName = $"{builds_dir}/GameServer.exe",
            target = BuildTarget.StandaloneWindows64,
#if UNITY_2021_1_OR_NEWER
            options = BuildOptions.ShowBuiltPlayer | BuildOptions.Development | BuildOptions.EnableDeepProfilingSupport,
            subtarget = (int)StandaloneBuildSubtarget.Server,
#else
            options = BuildOptions.EnableHeadlessMode | BuildOptions.ShowBuiltPlayer | BuildOptions.Development
#endif
        };

        BuildReport report = BuildPipeline.BuildPlayer(opt);

        BuildSummary summary = report.summary;
        if (summary.result == BuildResult.Succeeded)
            Debug.Log($"打包成功: {opt.locationPathName}");
        if (summary.result == BuildResult.Failed)
            Debug.LogError("打包失败");

        SetIcon();
    }
    [MenuItem("Tools/打包/客户端", false, 3)]
    static void BuildClient_Win64()
    {
        SetIcon();

        var curr_info = new DirectoryInfo(Environment.CurrentDirectory);
        string builds_dir = $"{curr_info}/Builds/Client";

        BuildPlayerOptions opt = new BuildPlayerOptions
        {
            scenes = new string[] { "Assets/Scenes/Client.unity" },
            locationPathName = Path.Combine(builds_dir, "Client.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.ShowBuiltPlayer | BuildOptions.Development,
        };

        BuildReport report = BuildPipeline.BuildPlayer(opt);

        BuildSummary summary = report.summary;
        if (summary.result == BuildResult.Succeeded)
            Debug.Log($"打包成功: {opt.locationPathName}");
        if (summary.result == BuildResult.Failed)
            Debug.LogError("打包失败");
    }
    static void BuildClient_Android()
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(NamedBuildTarget.Android, BuildTarget.Android);

        string defines = "USE_ASSETBUNDLE;CHANNEL_11011";
        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, defines);
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.moegijinka.moefight"); //不同渠道包名不一样
        //PlayerSettings.bundleVersion = string.Format("{0}.{1}.{2}", GameConfig.clientVersions[0],
        //    GameConfig.clientVersions[1] * 100 + GameConfig.clientVersions[2], GameConfig.clientVersions[3]);
    }
    static void BuildClient_iOS()
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
    [MenuItem("Tools/图标/SetIcon", true)]
    static void SetIcon()
    {
        string filePath = $"Assets/Arts/Icon/Wikipe-tan_cropped.png";
        Texture2D t2d = AssetDatabase.LoadAssetAtPath<Texture2D>(filePath);

        Texture2D[] array_1 = new Texture2D[] { t2d };
        PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, array_1); //default

        Texture2D[] array_8 = new Texture2D[] { t2d, t2d, t2d, t2d, t2d, t2d, t2d, t2d };
        //PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Standalone, array_8); //override, 会覆盖

        AssetDatabase.Refresh();
    }
    [MenuItem("Tools/图标/RemoveIcon", true)]
    static void RemoveIcon()
    {
        Texture2D[] array_1 = new Texture2D[] { null };
        PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, null); //default

        Texture2D[] array_8 = new Texture2D[] { null, null, null, null, null, null, null, null };
        //PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Standalone, array_8); //override, 会覆盖

        AssetDatabase.Refresh();
    }
}