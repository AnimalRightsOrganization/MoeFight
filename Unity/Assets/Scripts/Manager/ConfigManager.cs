using System.IO;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif
// 全局配置。不放场景中，通过脚本创建。
public class ConfigManager : MonoBehaviour
{
    static ConfigManager _instance;
    public static ConfigManager Get()
    {
        if (_instance == null)
            _instance = FindObjectOfType<ConfigManager>();
        if (_instance == null)
        {
            var obj = new GameObject("ConfigManager");
            _instance = obj.AddComponent<ConfigManager>();
        }
        return _instance;
    }
    static bool created = false;

    public static ushort DELAY_FRAMES = 5; //延迟模式
    public static ushort ROLLBACK_INTERVAL = 5; //回滚间隔(2-10)
    public static ushort TICK_RATE; //帧率(30/60)，服务器每秒钟接收并运算的次数，与客户端同步
    public static float FIXED_DELTA; //帧间隔
    //public static Fix64 Gravity; //米帧/秒(60:-0.833|50:-1|30:-1.667)
#if UNITY_EDITOR
    public static string REPLAY_FOLDER { get { return $"{Directory.GetParent(Application.dataPath).ToString()}/Replay"; } } //快照
    public static string DUMP_FOLDER { get { return $"{Directory.GetParent(Application.dataPath).ToString()}/Dump"; } } //操作对比
#else
    public static string REPLAY_FOLDER { get { return $"{Application.persistentDataPath}/Replay"; } }
    public static string DUMP_FOLDER { get { return $"{Application.persistentDataPath}/Dump"; } }
#endif
    public const string PREFAB_FOLDER = "Assets/Bundles/Prefabs";
    public const string CONFIG_FOLDER = "Assets/Bundles/Configs";
    public static GlobalConfig m_GlobalConfig;
    public static InputConfig m_InputConfig;
    //public static RoleConfig m_RoleConfig;
    //public static SpriteConfig m_SpriteConfig;

    void Awake()
    {
        if (!created)
        {
            DontDestroyOnLoad(gameObject);
            created = true;
        }
        else
        {
            DestroyImmediate(gameObject, true); //多了一个
            return;
        }

        m_GlobalConfig = ResManager.LoadConfig("configs/globalconfig") as GlobalConfig;
        m_InputConfig = ResManager.LoadConfig("configs/inputconfig") as InputConfig;
        //m_RoleConfig = ResManager.LoadConfig("configs/roleconfig") as RoleConfig;
        //m_SpriteConfig = ResManager.LoadConfig("configs/spriteconfig") as SpriteConfig;

        // 读取配置
        //Debug.unityLogger.logEnabled = false; //release版关闭，debug版打开
        QualitySettings.vSyncCount = 0; //只能是0,1,2，0为不等待垂直同步
        Application.targetFrameRate = m_GlobalConfig.renderFPS;
        TICK_RATE = m_GlobalConfig.logicFPS;
        FIXED_DELTA = 1f / TICK_RATE;
        //Debug.Log($"加载配置：帧率={TICK_RATE}，间隔={FIXED_DELTA}，重力={Gravity}");

        if (Directory.Exists(REPLAY_FOLDER) == false)
            Directory.CreateDirectory(REPLAY_FOLDER);
    }
}