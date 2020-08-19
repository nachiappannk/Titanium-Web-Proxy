using System;
using System.Collections.Generic;
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
        private static List<string> hostNames1 = new List<string>() { "sharefile", "szchanaa" };
        private static string outputFileName1 = "proxy.txt";
        private static string workingDirectory1 = @"C:\Data\";
        private static int KB = 1024;
        private static int MB = KB * KB;

        public static void Main(string[] args)
        {

            //NetworkMonitor nm = new NetworkMonitor();
            //nm.Monitor(300).GetAwaiter().GetResult();

            //Code to intercept the network traffic.
            asyncMain().GetAwaiter().GetResult();

            //Code to generate a random file
            //CreateFile(workingDirectory1+"somfile.txt", 4 * MB).GetAwaiter().GetResult();
        }

        private static void OnRequest(String url, long size)
        {
            Console.WriteLine(DateTime.Now.ToLongTimeString()+"\t"+size+"\t"+url);
        }

        private static  void OnResponse(String url, String body, int statusCode, String method, long size)
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
            var consoleLock = new object();
            ;
            ProxyTestController controller = new ProxyTestController(hostNames1);

            if (RunTime.IsWindows)
            {
                // fix console hang due to QuickEdit mode
                ConsoleHelper.DisableQuickEditMode();
            }

            //controller.OnRequest += OnRequest;
            //controller.OnResponse += OnResponse;
            // Start proxy controller

            List<String> logs = new List<string>();
            controller.OnNetworkEvent += (info) =>
            {
                Task.Run(() =>
                {
                    var b = "converting error";
                    try
                    {
                        //b = Encoding.UTF8.GetString(info.BodyBytes, 0, info.BodyBytes.Length);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                    lock (consoleLock)
                    {
                        var log =
                            $"{info.Time.ToLongTimeString()}\t{info.ProcessId}\t{info.Id}\t{info.Type}\t{info.Method}\t{info.Url}\t{info.PayloadSize}\t{info.Body}\t{b}";
                        logs.Add(log);
                    }
                });
            };
            controller.StartProxy();

            Console.WriteLine("Hit any key to exit..");
            Console.WriteLine();
            Console.Read();

            controller.Stop();
            await Task.Delay(5000);
            File.WriteAllLines(@"C:\Data\logs11a.txt", logs);
            controller.Dispose();
        }
    }
}
