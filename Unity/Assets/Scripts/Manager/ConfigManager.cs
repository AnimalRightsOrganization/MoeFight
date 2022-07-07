using UnityEngine;
using LitJson;

public class ConfigManager : MonoBehaviour
{
    static ConfigManager _instance;
    public static ConfigManager Get()
    {
        if (_instance == null)
            _instance = FindObjectOfType<ConfigManager>();
        return _instance;
    }

    public GlobalConfig globalConfig;
    public RoleConfig roleConfig;
    //public InputConfig inputConfig;

    protected LanguageNode[] m_NodeList;
    protected Languages currentLanguage = Languages.Chinese;

    void Awake()
    {
        globalConfig = ResManager.LoadConfig("Configs/GlobalConfig") as GlobalConfig;
        roleConfig = ResManager.LoadConfig("Configs/RoleConfig") as RoleConfig;
        //inputConfig = ResManager.LoadConfig("Configs/InputConfig") as InputConfig;

        var json = ResManager.LoadBytes("Configs/Language");
        m_NodeList = JsonMapper.ToObject<LanguageNode[]>(json);
    }

    public string GetWord(int key)
    {
        return m_NodeList[key].Word[(int)currentLanguage];
    }
}