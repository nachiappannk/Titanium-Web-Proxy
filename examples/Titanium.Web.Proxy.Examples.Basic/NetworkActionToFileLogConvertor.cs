using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Titanium.Web.Proxy.Examples.Basic
{
    public class NetworkActionToFileLogConvertor
    {
        private readonly BlockingCollection<NetworkAction> networkActions = new BlockingCollection<NetworkAction>();
        private readonly Dictionary<int, NetworkTransaction> transactions = new Dictionary<int, NetworkTransaction>();
        
        public void AddNetworkAction(NetworkAction action)
        {
            Task.Run(() => { networkActions.Add(action); });
        }

        public async Task<String> Convert(CancellationToken ct)
        {

            while (true)
            {
                if (ct.IsCancellationRequested)
                {
                    return "bad";
                }

                if (networkActions.TryTake(out NetworkAction info))
                {
                    Console.WriteLine($"{info.Url}\t{info.Body}");
                    AddToTransactions(info);
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
            var call = transactions[info.MappingId];
            return (call.Response != null && call.Request != null);
        }

        private void AddToTransactions(NetworkAction info)
        {
            if (!transactions.ContainsKey(info.MappingId))
            {
                transactions.Add(info.MappingId, new NetworkTransaction());
            }

            var call = transactions[info.MappingId];
            if (info.Type == NetworkActionType.Response)
                call.Response = info;
            else
                call.Request = info;
        }
    }
}
