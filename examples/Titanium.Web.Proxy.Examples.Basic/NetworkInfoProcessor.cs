using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Titanium.Web.Proxy.Examples.Basic
{
    public class NetworkInfoProcessor
    {
        private readonly BlockingCollection<NetworkInfo> networkInfoCollection = new BlockingCollection<NetworkInfo>();
        private Dictionary<int, NetworkRequestResponseInfo> calls = new Dictionary<int, NetworkRequestResponseInfo>();
        private int c = 0;
        
        public void AddInfo(NetworkInfo networkInfo)
        {
            Task.Run(() => { networkInfoCollection.Add(networkInfo); });
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
}
