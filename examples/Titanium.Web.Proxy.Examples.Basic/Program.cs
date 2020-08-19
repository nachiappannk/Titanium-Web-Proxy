using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Titanium.Web.Proxy.Examples.Basic.Helpers;
using Titanium.Web.Proxy.Helpers;

namespace Titanium.Web.Proxy.Examples.Basic
{
    public class Program
    {
        private static string outputFileName1 = "proxy.txt";
        private static string workingDirectory1 = @"C:\Data\";
        private static int KB = 1024;
        private static int MB = KB * KB;

        public static void Main(string[] args)
        {

            //NetworkMonitor nm = new NetworkMonitor();
            //nm.Monitor(300).GetAwaiter().GetResult();

            //Code to intercept the network traffic.
            if (RunTime.IsWindows)
            {
                ConsoleHelper.DisableQuickEditMode();
            } 
            var result = new PerformanceProbe("sharefile", "szchanaa").asyncMain(60).GetAwaiter().GetResult();
            Console.WriteLine(result);

            //Code to generate a random file
            //CreateFile(workingDirectory1+"somfile.txt", 4 * MB).GetAwaiter().GetResult();
        }

        private static void OnRequest(String url, long size)
        {
            Console.WriteLine(DateTime.Now.ToLongTimeString() + "\t" + size + "\t" + url);
        }

        private static void OnResponse(String url, String body, int statusCode, String method, long size)
        {

            if (url.Contains("upload-threaded-3"))
            {
                Console.WriteLine(DateTime.Now.ToLongTimeString() + " " + url);
            }

            if (url.Contains("Upload2"))
            {
                Console.WriteLine(DateTime.Now.ToLongTimeString() + "response 1 " + url);
                Console.WriteLine(DateTime.Now.ToLongTimeString() + "response 2 " + body);
                Console.WriteLine(DateTime.Now.ToLongTimeString() + "response 3 " + statusCode);
                Console.WriteLine(DateTime.Now.ToLongTimeString() + "response 4 " + method);
                Console.WriteLine(DateTime.Now.ToLongTimeString() + "response 5 " + size);

            }
        }

        private static Random random = new Random();

        public static async Task CreateFile(String file, int size)
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
    }
}
