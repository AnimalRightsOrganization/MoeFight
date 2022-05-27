using System.IO;
using UnityEngine;
using UnityEditor;

public class CreateConfig : Editor
{
    static void CreateAsset<Type>() where Type : ScriptableObject
    {
        Type asset = ScriptableObject.CreateInstance<Type>();

        string bundles_dir = Application.dataPath + "/Bundles";
        if (!Directory.Exists(bundles_dir))
            Directory.CreateDirectory(bundles_dir);

        string config_dir = bundles_dir + "/Config";
        if (!Directory.Exists(config_dir))
            Directory.CreateDirectory(config_dir);

        string path = AssetDatabase.GenerateUniqueAssetPath("Assets/Bundles/Config/" + typeof(Type) + ".asset");

        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;
    }

    [MenuItem("Tools/CreateConfig/InputConfig")]
    static void CreateInputConfig()
    {
        CreateAsset<InputConfig>();
    }

    [MenuItem("Tools/CreateConfig/GlobalConfig")]
    static void CreateGlobalConfig()
    {
        CreateAsset<GlobalConfig>();
    }

    [MenuItem("Tools/CreateConfig/CharacterData")]
    static void CreateCharacterConfig()
    {
        CreateAsset<CharacterConfig>();
    }
    [MenuItem("Tools/CreateConfig/选中角色配置")]
    static void LoadJson()
    {
        // 搜索 type:CharacterConfig
        var ids = AssetDatabase.FindAssets("* t:CharacterConfig");
        if (ids.Length == 1)
        {
            var readmeObject = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(ids[0]));

            Selection.objects = new UnityEngine.Object[] { readmeObject };

            (Selection.objects[0] as CharacterConfig).FromJson();
        }
        else
        {
            Debug.Log("Couldn't find a CharacterConfig");
        }
    }
}