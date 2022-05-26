using System;
using System.Threading.Tasks;
using Code.Server;
using UnityEngine;

namespace Rollback
{
    internal class Program : MonoBehaviour
    {
        static ServerLogic server;
        
        async void Start()
        {
            Debug.Log("Start");

            await RunMainAsync();
        }

        private static async Task<int> RunMainAsync()
        {
            try
            {
                using (server = new ServerLogic())
                {
                    Debug.Log($"StartServer, listen on {ServerLogic.Port}");
                    await server.StartServer();
                    //Console.ReadLine();
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