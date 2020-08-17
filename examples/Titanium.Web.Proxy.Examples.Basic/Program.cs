using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Titanium.Web.Proxy.Examples.Basic.Helpers;
using Titanium.Web.Proxy.Helpers;

namespace Titanium.Web.Proxy.Examples.Basic
{
    public class Program
    {
        private static List<string> hostNames1 = new List<string>() { "google", "ndtv" };
        private static string outputFileName1 = "proxy.txt";
        private static string workingDirectory1 = @"C:\Data\";
        private static int KB = 1024;
        private static int MB = KB * KB;

        public static void Main(string[] args)
        {
            asyncMain().GetAwaiter().GetResult();
            CreateFile("somfile.txt", 4 * MB).GetAwaiter().GetResult();
        }

        private static Random random = new Random();

        public static  async Task CreateFile(String file, int size)
        {
            string path = file;
            // This text is added only once to the file.
            if (!File.Exists(path))
            {
                // Create a file to write to.
                using (StreamWriter sw = File.CreateText(path))
                {
                    int count = size / 1000;
                    if (count == 0)
                        count++;
                    for (int i = 0; i < count; i++)
                    {
                        await sw.WriteAsync(RandomString(1000));
                    }
                    await sw.FlushAsync();
                }
            }
        }


        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private static async Task asyncMain()
        {
            ProxyTestController controller = new ProxyTestController(hostNames1);

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
            System.IO.File.WriteAllLines(workingDirectory1+outputFileName1, logs);
            controller.Dispose();
        }
    }
}
