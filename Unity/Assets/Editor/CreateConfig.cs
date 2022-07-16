using System.IO;
using UnityEngine;
using UnityEditor;

public class CreateConfig : Editor
{
    static void CreateAsset<Type>() where Type : ScriptableObject
    {
        Type asset = ScriptableObject.CreateInstance<Type>();

        string bundles_dir = $"{Application.dataPath}/Bundles";
        if (!Directory.Exists(bundles_dir))
            Directory.CreateDirectory(bundles_dir);

        string config_dir = $"{bundles_dir}/Configs";
        if (!Directory.Exists(config_dir))
            Directory.CreateDirectory(config_dir);

        string path = AssetDatabase.GenerateUniqueAssetPath("Assets/Bundles/Configs/" + typeof(Type) + ".asset");
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;
    }

    [MenuItem("Tools/创建配置/GlobalConfig")]
    static void CreateGlobalConfig()
    {
        CreateAsset<GlobalConfig>();
    }

    [MenuItem("Tools/创建配置/选中角色配置")]
    static void SelectCharacterConfig()
    {
        // 搜索 type:CharacterConfig
        var ids = AssetDatabase.FindAssets("* t:CharacterConfig");
        if (ids.Length == 1)
        {
            var readmeObject = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(ids[0]));

            Selection.objects = new UnityEngine.Object[] { readmeObject };
        }
        else
        {
            Debug.LogError("Couldn't find a CharacterConfig");
        }
    }
}