using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

//class的名称要与file名称一致，不然会None (Mono script)
//[CreateAssetMenu(fileName = "Data", menuName = "Config/Character", order = 1)] //右键创建
public class CharacterConfig : ScriptableObject
{
    public string Name = string.Empty;
    public Params constants = new Params();
    public List<AnimationEx> animations = new List<AnimationEx>();
    public List<Attack> attacks = new List<Attack>();
    public List<ProjectileData> projectiles = new List<ProjectileData>();

    // json转asset
    public void FromJson()
    {
        var ta = Resources.Load<TextAsset>("CharacterData/KEN");
        string json = ta.text;
        CharacterData data = JsonConvert.DeserializeObject<CharacterData>(json);

        Name = data.name;
        constants = data.constants;

        animations = new List<AnimationEx>();
        foreach (var item in data.animations)
        {
            //string key = item.Key; //STAND, CROUCH, ..
            //Debug.Log($"key={key}--->{animations.Count}");
            var anime = new AnimationEx(item.Value);
            animations.Add(anime); //还是按照书写顺序的
        }

        attacks = new List<Attack>();
        foreach (var item in data.attacks)
        {
            attacks.Add(item.Value);
        }

        projectiles = new List<ProjectileData>();
        foreach (var item in data.projectiles)
        {
            projectiles.Add(item.Value);
        }
    }
}