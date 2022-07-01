[System.Serializable]
public class LanguageNode
{
    public string Id;
    public string English;
    public string Chinese;
    public string Japanese;
    //public string Remark;

    public string[] Word => new string[] { English, Chinese, Japanese };
}
public enum Languages : int
{
    English = 0,
    Chinese,
    Japanese,
}