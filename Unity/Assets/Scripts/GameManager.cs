using UnityEngine;
using HotFix;

public class GameManager : MonoBehaviour
{
    static GameManager _get;
    public static GameManager Get
    {
        get
        {
            if (_get == null)
                _get = FindObjectOfType<GameManager>();
            return _get;
        }
    }

    void Start()
    {
        GameObject uimanager = new GameObject("UIManager");
        uimanager.AddComponent<UIManager>();

        //GameObject eventmanager = new GameObject("EventManager");
        //eventmanager.AddComponent<EventManager>();

        UIManager.Get().Push<UI_Login>();
    }
}