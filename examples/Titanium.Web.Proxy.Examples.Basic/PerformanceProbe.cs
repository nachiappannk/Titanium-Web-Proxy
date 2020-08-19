using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Titanium.Web.Proxy.Examples.Basic
{
    public class PerformanceProbe
    {
        private static List<string> hostNames;

        public PerformanceProbe(params String[] names)
        {
            hostNames = names.ToList();
        }

        public async Task<String> asyncMain(int timeWait)
        {
            ProxyTestController controller = new ProxyTestController(hostNames);
            NetworkActionToFileLogConvertor processor = new NetworkActionToFileLogConvertor();
            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken ct = source.Token;

            var task = processor.Convert(ct);
            var task2 = MyDelay(timeWait, ct);
            controller.OnNetworkEvent += processor.AddInfo;
            controller.StartProxy();
            await  Task.WhenAny(task ,task2);
            source.Cancel();
            await Task.WhenAll(task,task2);
            controller.Stop();
            await Task.Delay(5000);
            controller.OnNetworkEvent -= processor.AddInfo;
            controller.Dispose();
            return task.Result;
        }

        private async Task MyDelay(int timeWait, CancellationToken ct)
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
