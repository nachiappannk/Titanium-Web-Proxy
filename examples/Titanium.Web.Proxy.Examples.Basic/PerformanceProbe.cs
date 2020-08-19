using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Titanium.Web.Proxy.Examples.Basic
{
    public class PerformanceProbe
    {
        private static List<string> hostNames1 = new List<string>() { "sharefile", "szchanaa" };

        public static async Task<String> asyncMain(int timeWait)
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
            await  Task.WhenAny(task ,task2);
            source.Cancel();
            await Task.WhenAll(task,task2);
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
        }
    }
}
