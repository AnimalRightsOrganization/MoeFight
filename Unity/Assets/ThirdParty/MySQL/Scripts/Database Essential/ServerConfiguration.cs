using UnityEngine;

namespace DatabaseEssential
{
    [CreateAssetMenu(fileName = "ServerConfig", menuName = "Database/Server Configuration", order = 0)]
    public class ServerConfiguration : ScriptableObject
    {
        public string host, database, user, password;
    }
    public class SQLConfiguration
    {
        public string host, database, user, password;
    }
}