using System.IO;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using Code.Client;
using Code.Shared;
using Newtonsoft.Json;
using HitstunConstants;
using HotFix;

public class GameManager : MonoBehaviour
{
    public static GameManager Get;

    private static bool Initialized = false;
    private readonly IPC _ipc = new IPC { ReceiveTimeout = 1000 };
    public static string Token { get; private set; }
    public static Present present; //通过请求返回

    private ClientLogic logic;
    private Transform canvasRoot;
    private UI_CheckUpdate ui_check;

    void Awake()
    {
#if USE_ASSETBUNDLE
        Debug.Log($"渠道:{ConstValue.CHANNEL_NAME}，使用热更，初始化:{Initialized}");
#else
        Debug.Log($"渠道:{ConstValue.CHANNEL_NAME}，不是热更，初始化:{Initialized}");
#endif

        if (!Initialized)
        {
            Get = this;
            DontDestroyOnLoad(gameObject);

            SystemSetting();

            BindAssets();

#if Channel_101 //内测PC，从大厅启动
            IPC_Login();
#endif

            GetConfig();
        }
        else
        {
            OnInited();
        }
    }

    void OnApplicationQuit()
    {
        Initialized = false;
    }

    // 系统设置
    void SystemSetting()
    {
        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = 1f / Constants.FPS;
        Application.targetFrameRate = Constants.FPS; //锁定渲染帧60，不锁是-1
        QualitySettings.vSyncCount = 0; //只能是0/1/2，0是不等待垂直同步
        Screen.fullScreen = false;
        //Screen.SetResolution(540, 960, false);
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        //Debug.unityLogger.logEnabled = false; //release版关闭
    }

    // 绑定组件
    void BindAssets()
    {
        // 初始化目录
        if (!Directory.Exists(ConstValue.AB_AppPath))
            Directory.CreateDirectory(ConstValue.AB_AppPath);

        // 初始化各种管理器
        GameObject clientNet = new GameObject("ClientNet");
        clientNet.transform.SetParent(this.transform);
        clientNet.AddComponent<ClientNet>();

        GameObject uiManager = new GameObject("UIManager");
        uiManager.transform.SetParent(this.transform);
        uiManager.AddComponent<UIManager>();

        GameObject configManager = new GameObject("ConfigManager");
        configManager.transform.SetParent(this.transform);
        configManager.AddComponent<ConfigManager>();

        GameObject audioManager = new GameObject("AudioManager");
        audioManager.transform.SetParent(this.transform);
        audioManager.AddComponent<AudioManager>();

        //transform.Find("ILGlobal").gameObject.AddComponent<ILGlobal>();

        // 初始UI
        canvasRoot = GameObject.Find("Canvas").transform;
        //Debug.Assert(canvasRoot);
        string ui_name = "UI_CheckUpdate";
        GameObject asset = Resources.Load<GameObject>(ui_name);
        //Debug.Assert(asset);
        GameObject obj = Instantiate(asset, canvasRoot);
        //Debug.Assert(obj);
        obj.name = ui_name;
        if (obj.GetComponent<UI_CheckUpdate>() == false)
            obj.AddComponent<UI_CheckUpdate>();
        ui_check = obj.GetComponent<UI_CheckUpdate>();
        //Debug.Assert(ui_check);
    }

    // 请求游戏配置
    async void GetConfig()
    {
        string text = await HttpHelper.TryGetAsync(ConstValue.PRESENT_GET);
        if (string.IsNullOrEmpty(text))
        {
            Debug.LogError($"配置请求失败: {ConstValue.PRESENT_GET}");
            return;
        }
        Debug.Log($"success: {text}");
        var obj = JsonConvert.DeserializeObject<ServerResponse>(text);
        present = JsonConvert.DeserializeObject<Present>(obj.data);


#if UNITY_EDITOR && !USE_ASSETBUNDLE
        // 不检查更新
        OnInited();
#else
        // 加载配置（需要启动资源服务器）
        StartCoroutine(ui_check.StartCheck(OnInited));
#endif
    }

    void OnInited()
    {
        Initialized = true;

        // 进入HotFix代码
        ui_check.gameObject.SetActive(false);
        // 加载第一个UI
        UIManager.Get().Push<UI_Login>();
    }

    async void IPC_Login()
    {
        try
        {
            string result = await _ipc.Send("Login 0");
            Token = result;
            Debug.Log($"IPC返回：{result}");
        }
        catch (System.Exception e)
        {
            Debug.Log($"未启动大厅：{e.Message}");
        }
    }

    // 该游戏独立业务
    public async void OnLoadScene(S2C_LoadScenePacket packet, bool opening = true)
    {
        ClientNet.Get.m_ClientRoom.DoInit(packet);
        //Debug.Log($"比赛模式：{ClientNet.Get.m_ClientRoom.BattleMode}");

        // 转场动画
        var ui_versus = UIManager.Get().Push<UI_Versus>();
        int left = packet.Host.RoleIndex;
        int right = packet.Guest.RoleIndex;
        ui_versus.FadeIn(left, right);
        await Task.Delay(2000);

        // 加载场景
        System.Action action = () =>
        {
            UIManager.Get().PopAll();
            UIManager.Get().Push<UI_GameMenu>(); //战斗UI

            if (opening && ClientNet.Get.m_ClientRoom.BattleMode != BattleMode.Training)
                logic.Opening(); //开场动画
            else
                logic.PlayLoop(); //直接开始
        };
        LoadBattleAsync(action); //创建模型
    }
    public async void LoadBattleAsync(System.Action action = null)
    {
        var asset = ResManager.LoadPrefab("Prefabs/ClientLogic");
        logic = Instantiate(asset).GetComponent<ClientLogic>();
        logic.name = typeof(ClientLogic).Name;
        await Task.Delay(1000);
        action?.Invoke();
    }
    public void CleanBattle()
    {
        if (logic != null)
            Destroy(logic.gameObject);
        logic = null;
    }
}