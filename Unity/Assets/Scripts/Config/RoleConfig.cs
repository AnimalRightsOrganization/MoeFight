using UnityEngine;

public class RoleConfig : ScriptableObject
{
    public RoleAttr[] Roles = new RoleAttr[]
    {
        new RoleAttr { Name = new string[] { "Ken", "肯" },        ID = 0, Mass = 1, HP = 1000, Stun = 1000, Rect = new float[] { 0.5f, 1.6f, 0.8f }, Skills = new int[] { 1, 2, 3, 4, 5, 6, 7, 13, 14, 15, 16 } },
        new RoleAttr { Name = new string[] { "Aoi", "葵" },        ID = 1, Mass = 1, HP = 900,  Stun = 1100, Rect = new float[] { 0.5f, 1.6f, 0.8f }, Skills = new int[] { 1, 2, 3, 4, 5, 6, 7, 13, 14, 15, 16 } },
        new RoleAttr { Name = new string[] { "Honoka", "穗花" },   ID = 2, Mass = 1, HP = 1000, Stun = 1000, Rect = new float[] { 0.5f, 1.6f, 0.8f }, Skills = new int[] { 1, 2, 3, 4, 5, 6, 7, 13, 14, 15, 16 } },
        new RoleAttr { Name = new string[] { "Satomi", "里美" },   ID = 3, Mass = 1, HP = 1000, Stun = 1000, Rect = new float[] { 0.5f, 1.6f, 0.8f }, Skills = new int[] { 1, 2, 3, 4, 5, 6, 7, 13, 14, 15, 16 } },
        new RoleAttr { Name = new string[] { "Taichi", "太一郎" }, ID = 4, Mass = 1, HP = 1000, Stun = 1000, Rect = new float[] { 0.5f, 1.6f, 0.8f }, Skills = new int[] { 1, 2, 3, 4, 5, 6, 7, 13, 14, 15, 16 } },
        //new RoleAttr { Name = new string[] { "YBot", "机器人" },   ID = 5, Mass = 1, HP = 1000, Stun = 1000, Rect = new float[] { 0.5f, 1.6f, 0.8f }, Skills = new int[] { 1, 2, 3, 4, 5, 6, 7, 13, 14, 15, 16 } },
        //new RoleAttr { Name = new string[] { "Puppet", "傀儡" },   ID = 6, Mass = 1, HP = 1000, Stun = 1000, Rect = new float[] { 0.5f, 1.6f, 0.8f }, Skills = new int[] { 1, 2, 3, 4, 5, 6, 7, 13, 14, 15, 16 } },
    };

    [ContextMenu("Reset2")]
    public void Reset2()
    {
        Roles = new RoleAttr[]
        {
            new RoleAttr { Name = new string[] { "Ken", "肯" },        ID = 0, Mass = 1, HP = 1000, Stun = 1000, Rect = new float[] { 0.5f, 1.6f, 0.8f }, Skills = new int[] { 1, 2, 3, 4, 5, 6, 7, 13, 14, 15, 16 } },
            new RoleAttr { Name = new string[] { "Aoi", "葵" },        ID = 1, Mass = 1, HP = 900,  Stun = 1100, Rect = new float[] { 0.5f, 1.6f, 0.8f }, Skills = new int[] { 1, 2, 3, 4, 5, 6, 7, 13, 14, 15, 16 } },
            new RoleAttr { Name = new string[] { "Honoka", "穗花" },   ID = 2, Mass = 1, HP = 1000, Stun = 1000, Rect = new float[] { 0.5f, 1.6f, 0.8f }, Skills = new int[] { 1, 2, 3, 4, 5, 6, 7, 13, 14, 15, 16 } },
            new RoleAttr { Name = new string[] { "Satomi", "里美" },   ID = 3, Mass = 1, HP = 1000, Stun = 1000, Rect = new float[] { 0.5f, 1.6f, 0.8f }, Skills = new int[] { 1, 2, 3, 4, 5, 6, 7, 13, 14, 15, 16 } },
            new RoleAttr { Name = new string[] { "Taichi", "太一郎" }, ID = 4, Mass = 1, HP = 1000, Stun = 1000, Rect = new float[] { 0.5f, 1.6f, 0.8f }, Skills = new int[] { 1, 2, 3, 4, 5, 6, 7, 13, 14, 15, 16 } },
            //new RoleAttr { Name = new string[] { "YBot", "机器人" },   ID = 5, Mass = 1, HP = 1000, Stun = 1000, Rect = new float[] { 0.5f, 1.6f, 0.8f }, Skills = new int[] { 1, 2, 3, 4, 5, 6, 7, 13, 14, 15, 16 } },
            //new RoleAttr { Name = new string[] { "Puppet", "傀儡" },   ID = 6, Mass = 1, HP = 1000, Stun = 1000, Rect = new float[] { 0.5f, 1.6f, 0.8f }, Skills = new int[] { 1, 2, 3, 4, 5, 6, 7, 13, 14, 15, 16 } },
        };
        //EditorUtility.SetDirty(this);
        //AssetDatabase.Refresh();
        //AssetDatabase.SaveAssets();
    }
}
[System.Serializable]
public class RoleAttr
{
    public string[] Name;
    public int ID;
    public int Mass;
    public int HP;
    public int Stun;
    public float[] Rect;
    public int[] Skills;
    //public string HeadImage; //头像图片名
    //public string Model; //模型名
}