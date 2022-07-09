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
    //public InputConfig inputConfig;

    // 多语言
    public Languages currentLanguage = Languages.Chinese;
    protected LanguageNode[] m_Languages;
    public void SetLanguage(Languages lang)
    {
        currentLanguage = lang;
    }
    public string GetWord(int key)
    {
        return m_Languages[key].Word[(int)currentLanguage];
    }

    // 角色配置
    public CharacterNode[] m_CharacterList;
    public CharacterNode GetCharacter(HitstunConstants.CharacterName key)
    {
        return m_CharacterList[(int)key];
    }

    void Awake()
    {
        globalConfig = ResManager.LoadConfig("Configs/GlobalConfig") as GlobalConfig;
        //inputConfig = ResManager.LoadConfig("Configs/InputConfig") as InputConfig;

        var langConfig = ResManager.LoadBytes("Configs/Language");
        m_Languages = JsonMapper.ToObject<LanguageNode[]>(langConfig);

        var charConfig = ResManager.LoadBytes("Configs/Character");
        m_CharacterList = JsonMapper.ToObject<CharacterNode[]>(charConfig);
    }
}