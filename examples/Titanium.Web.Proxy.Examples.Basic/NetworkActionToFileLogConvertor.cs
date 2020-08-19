using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Titanium.Web.Proxy.Examples.Basic
{
    public class NetworkActionToFileLogConvertor
    {
        private readonly BlockingCollection<NetworkAction> transactions = new BlockingCollection<NetworkAction>();
        private Dictionary<int, NetworkRequestResponseInfo> calls = new Dictionary<int, NetworkRequestResponseInfo>();
        private int c = 0;
        
        public void AddInfo(NetworkAction transaction)
        {
            Task.Run(() => { transactions.Add(transaction); });
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
                if (transactions.TryTake(out NetworkAction info))
                {
                    Console.WriteLine($"{info.Url}\t{info.Body}");
                    Add(info);
                    if(!IsCallComplete(info)) continue;

                }
                else
                {
                    await Task.Delay(200);
                    
                }
            }
            return "Should not come here";
        }

        private bool IsCallComplete(NetworkAction info)
        {
            var call = calls[info.MappingId];
            return (call.Response != null && call.Request != null);
        }

        private void Add(NetworkAction info)
        {
            if (!calls.ContainsKey(info.MappingId))
            {
                calls.Add(info.MappingId, new NetworkRequestResponseInfo());
            }

            var call = calls[info.MappingId];
            if (info.Type == NetworkActionType.Response)
                call.Response = info;
            else
                call.Request = info;
        }
    }
}
