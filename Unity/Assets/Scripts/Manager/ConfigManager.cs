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

    //public static GlobalConfig m_GlobalConfig;
    //public static InputConfig m_InputConfig;
    //public static RoleConfig m_RoleConfig;

    public LanguageNode[] m_NodeList;
    public Languages currentLanguage = Languages.Chinese;

    void Awake()
    {
        //m_GlobalConfig = ResManager.LoadConfig("configs/globalconfig") as GlobalConfig;
        //m_InputConfig = ResManager.LoadConfig("configs/inputconfig") as InputConfig;
        //m_RoleConfig = ResManager.LoadConfig("configs/roleconfig") as RoleConfig;

#if UNITY_EDITOR
        var ta = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Editor/ExcelTool/Excel/Language.json");
        m_NodeList = JsonMapper.ToObject<LanguageNode[]>(ta.text);
#endif
    }

    public string GetWord(int key)
    {
        return m_NodeList[key].Word[(int)currentLanguage];
    }
}