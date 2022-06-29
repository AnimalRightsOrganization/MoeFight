using System;
using System.Threading.Tasks;
using Code.Server;
using UnityEngine;

namespace Rollback
{
    internal class Program : MonoBehaviour
    {
        static ServerNet server;
        static bool isStarted = false;
        
        async void Start()
        {
            await RunMainAsync();
        }

        static async Task<int> RunMainAsync()
        {
            try
            {
                using (server = new ServerNet())
                {
                    Debug.Log($"StartServer, listen on {ServerNet.Port}");
                    await server.StartProgram();
                    isStarted = true;
                }

                return 0;
            }
            catch (Exception e)
            {
                Debug.Log($"\nException while trying to run client: {e.Message}");
                Console.ReadLine();
                return 1;
            }
        }

        void OnDestroy()
        {
            Debug.Log("Dispose");

            server?.Dispose();
        }
    }
}