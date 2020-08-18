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
        private static List<string> hostNames1 = new List<string>() { "sharefile", "szchanaa" };
        private static string outputFileName1 = "proxy.txt";
        private static string workingDirectory1 = @"C:\Data\";
        private static int KB = 1024;
        private static int MB = KB * KB;

        public static void Main(string[] args)
        {
            //Code to intercept the network traffic.
            asyncMain().GetAwaiter().GetResult();

            //Code to generate a random file
            CreateFile(workingDirectory1+"somfile.txt", 4 * MB).GetAwaiter().GetResult();
        }

        private async Task TestCase(int fileSize)
        {
            //Generate a file name
            //Delete file if present
            await CreateFile(workingDirectory1 + "someFile.txt", fileSize);
            await Part1();
            await Part2();
        }

        private static void OnRequest(String s)
        {
            if (s.Contains("Upload2"))
            {
                Console.WriteLine(DateTime.Now.ToLongTimeString()+ " "+ s);
            }
        }

        private static  void OnResponse(String s, String s2, int status)
        {
            if (s.Contains("upload-threaded-3"))
            {
                Console.WriteLine(DateTime.Now.ToLongTimeString() + " " + s);
            }

            if (s.Contains("Upload2"))
            {
                Console.WriteLine(DateTime.Now.ToLongTimeString() + " " + s);
                Console.WriteLine(DateTime.Now.ToLongTimeString() + " " + s2);
                Console.WriteLine(DateTime.Now.ToLongTimeString() + " " + status);


            }
        }


        private async Task Part2()
        {
            ProxyTestController controller = new ProxyTestController(hostNames1);

            controller.StartProxy();
            await CopyFileViaSelenium();
            //Wait for sometime
            var logs = controller.Stop();
            await Task.Delay(5000);
            System.IO.File.WriteAllLines(workingDirectory1 + outputFileName1, logs);
            controller.Dispose();
        }

        private async Task CopyFileViaSelenium()
        {
            throw new NotImplementedException();
        }

        private async Task Part1()
        {
            ProxyTestController controller = new ProxyTestController(hostNames1);
            await CopyToFolderForUpload();
            controller.StartProxy();
            //Wait for sometime
            var logs = controller.Stop();
            await Task.Delay(5000);
            System.IO.File.WriteAllLines(workingDirectory1 + outputFileName1, logs);
            controller.Dispose();
        }

        private async Task CopyToFolderForUpload()
        {
            throw new NotImplementedException();
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

            controller.OnRequest += OnRequest;
            controller.OnResponse += OnResponse;
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
