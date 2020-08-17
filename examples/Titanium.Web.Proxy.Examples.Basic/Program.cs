using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Titanium.Web.Proxy.Examples.Basic.Helpers;
using Titanium.Web.Proxy.Helpers;

namespace Titanium.Web.Proxy.Examples.Basic
{
    public class Program
    {
        

        public static void Main(string[] args)
        {
            asyncMain().GetAwaiter().GetResult();
        }

        private static async Task asyncMain()
        {
            List<String> hostNames = new List<string>() { "google", "ndtv" };

            ProxyTestController controller = new ProxyTestController(hostNames);

            if (RunTime.IsWindows)
            {
                // fix console hang due to QuickEdit mode
                ConsoleHelper.DisableQuickEditMode();
            }

            // Start proxy controller
            controller.StartProxy();

            Console.WriteLine("Hit any key to exit..");
            Console.WriteLine();
            Console.Read();

            var logs = controller.Stop();
            await Task.Delay(5000);
            foreach (string log in logs)
            {
                Console.WriteLine(log);
            }

            controller.Dispose();
        }
    }
}
