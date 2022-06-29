using UnityEngine;
using Code.Shared;
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
        if (!Initialized)
        {
            DontDestroyOnLoad(gameObject);

            //TODO: ¼ì²é¸üÐÂ
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
}