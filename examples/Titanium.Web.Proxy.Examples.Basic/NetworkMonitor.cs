using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Titanium.Web.Proxy.Examples.Basic
{
    public class NetworkMonitor
    {

        private static List<string> hostNames1 = new List<string>() { "sharefile", "szchanaa.sf" };
        CancellationTokenSource source = new CancellationTokenSource();
        public async Task<Object> Monitor(int timeoutInSeconds)
        {
            ProxyTestController controller = new ProxyTestController(hostNames1);
     
            controller.StartProxy();
            return await Task.Delay(new TimeSpan(0, 0, 0, timeoutInSeconds), source.Token)
                .ContinueWith((task) =>
                {
                     controller.Stop();
                    return new Object();
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
                Console.WriteLine(DateTime.Now.ToLongTimeString() + " " + url + " "+ method);
               // CancelWait();
            }

            if (url.Contains("Upload2"))
            {
                Console.WriteLine(DateTime.Now.ToLongTimeString() + "response 1 \n" + url);
                Console.WriteLine(DateTime.Now.ToLongTimeString() + "response 2 \n" + body);
               // Console.WriteLine(DateTime.Now.ToLongTimeString() + "response 3 " + statusCode);
               // Console.WriteLine(DateTime.Now.ToLongTimeString() + "response 4 " + method);
               // Console.WriteLine(DateTime.Now.ToLongTimeString() + "response 5 " + size);

            }
        }

        private async void CancelWait()
        {
            await Task.Delay(1000);
            source.Cancel();

        }
    }



}
