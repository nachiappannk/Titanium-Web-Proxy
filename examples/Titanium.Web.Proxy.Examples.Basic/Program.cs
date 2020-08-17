using System;
using System.Collections.Generic;
using Titanium.Web.Proxy.Examples.Basic.Helpers;
using Titanium.Web.Proxy.Helpers;

namespace Titanium.Web.Proxy.Examples.Basic
{
    public class Program
    {
        

        public static void Main(string[] args)
        {
            List<String> hostNames = new List<string>() { "google.com", "ndtv.com" };

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
            foreach (string log in logs)
            {
                Console.WriteLine(log);
            }
            controller.Dispose();
        }
    }
}
