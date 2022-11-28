using System.IO;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using Code.Client;
using HotFix;
using LitJson;
using HitstunConstants;

public class GameManager : MonoBehaviour
{
    static GameManager _instance;
    public static GameManager Get
    {
        get
        {
            if (_instance == null)
                _instance = FindObjectOfType<GameManager>();
            return _instance;
        }
    }

    private static bool Initialized = false;
    public static Present present; //通过请求返回
    public static string Token { get; private set; }

    private ClientLogic logic;

    void Awake()
    {
        if (!Initialized)
        {
            DontDestroyOnLoad(gameObject);

            // 系统设置
            Time.timeScale = 1.0f;
            Time.fixedDeltaTime = 1f / Constants.FPS;
            Application.targetFrameRate = Constants.FPS; //锁定渲染帧60，不锁是-1
            QualitySettings.vSyncCount = 0; //只能是0/1/2，0是不等待垂直同步
            Screen.fullScreen = false;
            //Screen.SetResolution(540, 960);
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            //Debug.unityLogger.logEnabled = false; //release版关闭
            //Application.systemLanguage;

#if UNITY_EDITOR && !USE_ASSETBUNDLE
            // 不检查更新
            present = new Present();
            OnInited();
#else
            // 加载配置（需要启动资源服务器）
            GetConfig();
#endif
        }
        else
        {
            OnInited();
        }
    }

    // 请求游戏配置
    async void GetConfig()
    {
        string text = await HttpHelper.TryGetAsync(ConstValue.PRESENT_GET);
        if (string.IsNullOrEmpty(text))
        {
            Debug.LogError("配置请求失败");
            return;
        }
        Debug.Log($"config: {text}");
        var obj = JsonMapper.ToObject<ServerResponse>(text);
        present = JsonMapper.ToObject<Present>(obj.data);

        StartCoroutine(CheckUpdateAsync(OnInited));
    }

    IEnumerator CheckUpdateAsync(System.Action action)
    {
        if (!Directory.Exists(ConstValue.AB_AppPath))
            Directory.CreateDirectory(ConstValue.AB_AppPath);

        Transform root = GameObject.Find("Canvas").transform;
        var request = Resources.LoadAsync<GameObject>("UI_CheckUpdate");
        yield return request;

        var asset = request.asset as GameObject;
        GameObject prefab = Instantiate(asset, root);
        var ui_checkupdate = prefab.AddComponent<UI_CheckUpdate>();

        yield return ui_checkupdate.StartCheck(action);
    }

    void OnInited()
    {
        Initialized = true;

        // 进入HotFix代码

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

        // 加载第一个UI
        UIManager.Get().Push<UI_Login>();
    }

    public async void LoadBattleAsync(System.Action action = null)
    {
        var asset = ResManager.LoadPrefab("Prefabs/ClientLogic");
        logic = Instantiate(asset).GetComponent<ClientLogic>();
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