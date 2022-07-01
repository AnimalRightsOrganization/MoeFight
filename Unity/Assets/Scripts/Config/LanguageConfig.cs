using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LanguageConfig : ScriptableObject
{
    public List<UINode> uiList = new List<UINode>();
}
[System.Serializable]
public class UINode
{
    public string Title;
    public List<UINodeItem> itemList = new List<UINodeItem>();
}
[System.Serializable]
public class UINodeItem
{
    public string Word;
    public string EnglishWord;
    public string ChineseWord;
    public string JapaneseWord;
    public string EpanishWord;
}