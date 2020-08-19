using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Titanium.Web.Proxy.Examples.Basic
{
    public class NetworkInfoProcessor
    {
        private readonly BlockingCollection<Transaction> networkInfoCollection = new BlockingCollection<Transaction>();
        private Dictionary<int, NetworkRequestResponseInfo> calls = new Dictionary<int, NetworkRequestResponseInfo>();
        private int c = 0;
        
        public void AddInfo(Transaction transaction)
        {
            Task.Run(() => { networkInfoCollection.Add(transaction); });
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
                if (networkInfoCollection.TryTake(out Transaction info))
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

        private bool IsCallComplete(Transaction info)
        {
            var call = calls[info.MappingId];
            return (call.Response != null && call.Request != null);
        }

        private void Add(Transaction info)
        {
            if (!calls.ContainsKey(info.MappingId))
            {
                calls.Add(info.MappingId, new NetworkRequestResponseInfo());
            }

            var call = calls[info.MappingId];
            if (info.Type == TransactionType.Response)
                call.Response = info;
            else
                call.Request = info;
        }
    }
}
