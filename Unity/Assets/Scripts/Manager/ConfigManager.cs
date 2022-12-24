using UnityEngine;
using Newtonsoft.Json;

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

    // 多语言
    public Languages currentLanguage = Languages.简体中文;
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

        var langConfig = ResManager.LoadBytes("Configs/Language");
        m_Languages = JsonConvert.DeserializeObject<LanguageNode[]>(langConfig);

        var charConfig = ResManager.LoadBytes("Configs/Character");
        m_CharacterList = JsonConvert.DeserializeObject<CharacterNode[]>(charConfig);
    }
}