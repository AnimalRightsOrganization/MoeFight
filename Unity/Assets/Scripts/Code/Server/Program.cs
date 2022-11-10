using System;
using System.Threading.Tasks;
using Code.Server;
using UnityEngine;

namespace Rollback
{
    internal class Program : MonoBehaviour
    {
        static ServerNet server;
        
        async void Start()
        {
            await RunMainAsync();
        }

        static async Task<int> RunMainAsync()
        {
            try
            {
                using (server = ServerNet.Get)
                {
                    Debug.Log($"StartServer, listen on {ServerNet.Port}");
                    await server.StartProgram();
                }

                return 0;
            }
            catch (Exception e)
            {
                //Collection was modified; enumeration operation may not execute.
                Debug.LogError($"\nException while trying to run client: {e.Message}");
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