using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Titanium.Web.Proxy.Examples.Basic
{
    public class NetworkMonitor
    {

        private static List<string> hostNames1 = new List<string>() { "sharefile", "szchanaa" };
        public async Task<NetworkResult> Monitor(int timeoutInSeconds)
        {
            ProxyTestController controller = new ProxyTestController(hostNames1);
            controller.OnRequest += OnRequest;
            controller.OnResponse += OnResponse;

            controller.StartProxy();
            return await Task.Delay(new TimeSpan(0, 0, 0, timeoutInSeconds))
                .ContinueWith((task) =>
                {
                    controller.Stop();
                    controller.Dispose();
                    return new NetworkResult();
                });
        }


        private void OnRequest(String url, long size)
        {
            Console.WriteLine(DateTime.Now.ToLongTimeString() + "\t" + size + "\t" + url);
        }

        private void OnResponse(String url, String body, int statusCode, String method, long size)
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
    }

    public class NetworkResult  
    {
        public int TimeTakeInSeconds { get; set; }
    }
}
