using System.Collections;
using UnityEngine;
using Code.Client;
using HotFix;

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
    public static string Token { get; private set; }

    void Start()
    {
        // 系统设置
        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = 0.02f;
        Screen.fullScreen = false;
        //Screen.SetResolution(540, 960);
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        //Application.targetFrameRate = 60; //锁定渲染帧
        QualitySettings.vSyncCount = 0;

        if (!Initialized)
        {
            DontDestroyOnLoad(gameObject);

            //TODO: 检查更新
            OnInit();
        }
        else
        {
            OnInit();
        }
    }

    void OnInit()
    {
        Initialized = true;

        GameObject uimanager = new GameObject("UIManager");
        uimanager.transform.SetParent(this.transform);
        uimanager.AddComponent<UIManager>();

        GameObject clientNet = new GameObject("ClientNet");
        clientNet.transform.SetParent(this.transform);
        clientNet.AddComponent<ClientNet>();

        UIManager.Get().Push<UI_Login>();
    }


    public void LoadBattle(System.Action action = null)
    {
        StartCoroutine(LoadBattleAsync(action));
    }
    IEnumerator LoadBattleAsync(System.Action action = null)
    {
        yield return new WaitForSeconds(2);
        var asset = ResManager.LoadPrefab("Prefabs/ClientLogic");
        UnityEngine.Object.Instantiate(asset);
        yield return new WaitForSeconds(1);
        {
            action?.Invoke();
        }
    }
}