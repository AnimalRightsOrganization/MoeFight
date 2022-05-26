using System;
using System.Threading.Tasks;
using Code.Server;

namespace Rollback
{
    internal class Program
    {
        static int Main(string[] args)
        {
            return RunMainAsync().Result;
        }

        private static async Task<int> RunMainAsync()
        {
            try
            {
                using (ServerLogic server = new ServerLogic())
                {
                    Console.WriteLine($"StartServer, listen on {ServerLogic.Port}");
                    await server.StartServer();
                    Console.ReadLine();
                }

                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine($"\nException while trying to run client: {e.Message}");
                Console.ReadLine();
                return 1;
            }
        }
    }
}