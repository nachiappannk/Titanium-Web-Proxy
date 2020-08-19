using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
            if (RunTime.IsWindows)
            {
                ConsoleHelper.DisableQuickEditMode();
            } 
            var result = asyncMain(60).GetAwaiter().GetResult();
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

        private static async Task<String> asyncMain(int timeWait)
        {
            ProxyTestController controller = new ProxyTestController(hostNames1);
            BlockingCollection<NetworkInfo> networkInfoCollection = new BlockingCollection<NetworkInfo>();
            NetworkInfoProcessor processor = new NetworkInfoProcessor(controller, networkInfoCollection);
            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken ct = source.Token;
            var task = processor.Process(ct);
            var task2 = MyDelay(timeWait, ct);
            //Unsubscribe
            controller.OnNetworkEvent += (info) => { Task.Run(() => { networkInfoCollection.Add(info); }); };
            controller.StartProxy();
            await  Task.WhenAny(task, task2);
            source.Cancel();
            await Task.WhenAll(task, task2);
            controller.Stop();
            controller.Dispose();
            await Task.Delay(5000);
            return task.Result;
        }

        private static async Task MyDelay(int timeWait, CancellationToken ct)
        {
            try
            {
                await Task.Delay(new TimeSpan(0, 0, 0, timeWait), ct);
            }
            catch (Exception e)
            {
                
            }

            return;
        }
    }

    public class NetworkInfoProcessor
    {
        private readonly ProxyTestController controller;
        private readonly BlockingCollection<NetworkInfo> networkInfoCollection;
        private Dictionary<int, NetworkRequestResponseInfo> calls = new Dictionary<int, NetworkRequestResponseInfo>();
        private int c = 0;
        public NetworkInfoProcessor(ProxyTestController controller, BlockingCollection<NetworkInfo> networkInfoCollection)
        {
            this.controller = controller;
            this.networkInfoCollection = networkInfoCollection;
        }


        public async Task<String> Process(CancellationToken ct)
        {

            while (true)
            {
                if (ct.IsCancellationRequested)
                {
                    return "bad";
                }

                c++;
                if (c == 10)
                {

                    //return "Good";
                }
                if (networkInfoCollection.TryTake(out NetworkInfo info))
                {
                    Add(info);
                    if(!IsCallComplete(info)) break;

                }
                else
                {
                    await Task.Delay(200);
                    
                }
            }
            return "Should not come here";
        }

        private bool IsCallComplete(NetworkInfo info)
        {
            var call = calls[info.Id];
            return (call.Response != null && call.Request != null);
        }

        private void Add(NetworkInfo info)
        {
            if (!calls.ContainsKey(info.Id))
            {
                calls.Add(info.Id, new NetworkRequestResponseInfo());
            }

            var call = calls[info.Id];
            if (info.Type == NetworkInfoType.Response)
                call.Response = info;
            else
                call.Request = info;
        }
    }

    public class NetworkRequestResponseInfo
    {
        public NetworkInfo Request { get; set; }
        public NetworkInfo Response { get; set; }
    }
}
