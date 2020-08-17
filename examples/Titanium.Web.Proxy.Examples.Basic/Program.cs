using System;
using Titanium.Web.Proxy.Examples.Basic.Helpers;
using Titanium.Web.Proxy.Helpers;

namespace Titanium.Web.Proxy.Examples.Basic
{
    public class Program
    {
        

        public static void Main(string[] args)
        {
            ProxyTestController controller = new ProxyTestController();

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

            controller.Stop();
            controller.Dispose();
        }
    }
}
