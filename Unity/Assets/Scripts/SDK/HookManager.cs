using UnityEngine;
using Newtonsoft.Json;

public class HookManager : MonoBehaviour
{
    public static HookManager Get;

    private AndroidHook hook;

    void Awake()
    {
        Get = this;
        DontDestroyOnLoad(this.gameObject);

#if UNITY_ANDROID
        hook = new AndroidHook(gameObject);
#elif UNITY_IOS
        //hook = new iOSHook(gameObject);
#else
        hook = null;
#endif
    }

    void OnDestroy()
    {
        hook?.Dispose();
    }

    public bool CheckInstall()
    {
#if UNITY_ANDROID
        return hook.CheckInstall();
#else
        return false;
#endif
    }

    public void JumpActivity()
    {
#if UNITY_ANDROID
        hook.JumpActivity();
#endif
    }

    // 字符串消息返回
    public void JavaToUnity(string message)
    {
        Debug.Log($"Unity recv: {message}");
    }
    // json消息返回
    public void JsonToUnity(string json)
    {
        var obj = JsonConvert.DeserializeObject<MoeCallback>(json);
        Debug.Log($"JsonToUnity: {obj.code}, {obj.data}");
        switch (obj.code)
        {
            case 0: //0:大厅主动发
                GameManager.Token = obj.data;
                break;
            case 1: //1:游戏请求后发
                GameManager.Token = obj.data;
                Code.Client.ClientNet.Get.SendLoginByToken();
                break;
        }
    }
}