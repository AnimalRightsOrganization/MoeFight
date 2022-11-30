using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using Debug = UnityEngine.Debug;
using Code.Client;
using Code.Server;

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
        /*
        if (GUILayout.Button("PVE"))
        {
            if (ClientNet.Get.m_PlayerManager.LocalPlayer == null)
            {
                Debug.LogError("请先登录");
                return;
            }
            ClientPlayer host = new ClientPlayer("test1", 1);
            ClientPlayer guest = new ClientPlayer("BOT", -1);
            ClientNet.Get.m_ClientRoom = new ClientRoom(1, host, guest);
            ClientNet.Get.m_ClientRoom.BattleMode = Code.Shared.BattleMode.Training;

            System.Action action = () =>
            {
                HotFix.UIManager.Get().PopAll();
                HotFix.UIManager.Get().Push<HotFix.UI_GameMenu>();
                ClientNet.Get.SendTestPVE();
            };
            GameManager.Get.LoadBattleAsync(action); //测试PVE
        }
        if (GUILayout.Button("PVP"))
        {
            if (ClientNet.Get.m_PlayerManager.LocalPlayer == null)
            {
                Debug.LogError("请先登录");
                return;
            }
            System.Action action = () =>
            {
                HotFix.UIManager.Get().PopAll();
                HotFix.UIManager.Get().Push<HotFix.UI_GameMenu>();
                ClientNet.Get.SendTestPVP();
            };
            GameManager.Get.LoadBattleAsync(action); //测试PVP
        }*/
        switch (SceneManager.GetActiveScene().name)
        {
            case "Client": ClientGUI(); break;
            case "Server": ServerGUI(); break;
            case "Photo": PhotoGUI(); break;
            default: break;
        }
    }

    HotFix.UI_Login FillLogin(string uesr)
    {
        var login = HotFix.UIManager.Get().GetUI<HotFix.UI_Login>();
        login.m_UserNameField.text = uesr;
        login.m_PasswordField.text = "123456";
        return login;
    }
    async void ClientGUI()
    {
        if (GUILayout.Button("登录.自动填写.test1"))
        {
            FillLogin("test1");
        }
        if (GUILayout.Button("登录.自动填写.test2"))
        {
            FillLogin("test2");
        }
        if (GUILayout.Button("登录.test1.匹配"))
        {
            FillLogin("test1").SendLogin();
            await Task.Delay(1000);

            var lobby = HotFix.UIManager.Get().GetUI<HotFix.UI_Lobby>();
            lobby.RequestMatch();
        }
        if (GUILayout.Button("Client Print"))
        {
            var str = ClientNet.Get.m_PlayerManager.LocalPlayer.ToString();
            Debug.Log(str);
        }
        if (GUILayout.Button("Battle Print"))
        {
            var str = $"IsStart:{ClientLogic.Get.IsStart}, ";
            Debug.Log(str);
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
            var room = ServerNet.Get.m_RoomManager.GetAll()[0];
            Debug.Log($"{room.hostPlayer.RoleIndex} vs {room.guestPlayer.RoleIndex}");
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
        var curr_info = new DirectoryInfo(Environment.CurrentDirectory);
        string filepath = $"{curr_info}/Builds/Client/Client.exe";

        Process.Start(filepath);
    }
    [MenuItem("Tools/启动/服务器 %_F12", false)]
    static void RunServer()
    {
        var curr_info = new DirectoryInfo(Environment.CurrentDirectory);
        string filepath = $"{curr_info}/Builds/Server/GameServer.exe";

        Process.Start(filepath);
    }

    [MenuItem("Tools/打包/服务器", false)]
    static void BuildServer_Win64()
    {
        //EditorBuildSettings.scenes = new EditorBuildSettingsScene[] { new EditorBuildSettingsScene("Assets/Scenes/Server.unity", true) };
        //EditorUserBuildSettings.SwitchActiveBuildTarget(NamedBuildTarget.Server, BuildTarget.StandaloneWindows64);

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
    }
    [MenuItem("Tools/打包/客户端", false)]
    static void BuildClient_Win64()
    {
        //EditorBuildSettings.scenes = new EditorBuildSettingsScene[] { new EditorBuildSettingsScene("Assets/Scenes/Client.unity", true) };
        //EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);

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
    [MenuItem("Tools/打包/资源", false)]
    static void BuildRes()
    {
        BuildTarget target = (BuildTarget)System.Enum.Parse(typeof(BuildTarget), ConstValue.PLATFORM_NAME);
        Debug.Log($"打包{target}平台资源");
        BundleTools.Build_Target(target);
    }
}