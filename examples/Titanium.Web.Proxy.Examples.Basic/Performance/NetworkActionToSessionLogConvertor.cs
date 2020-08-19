using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Titanium.Web.Proxy.Examples.Basic.Performance;

namespace Titanium.Web.Proxy.Examples.Basic
{
    public class NetworkActionToSessionLogConvertor
    {
        private readonly int numFiles;
        private readonly Predicate<NetworkAction> startAction;
        private readonly Predicate<NetworkAction> endAction;
        private readonly BlockingCollection<NetworkAction> networkActions = new BlockingCollection<NetworkAction>();
        private readonly Dictionary<int, NetworkTransaction> transactions = new Dictionary<int, NetworkTransaction>();
        SessionLog sessionLog = new SessionLog();
        private bool started = false;
        private int counter = 0;

        public NetworkActionToSessionLogConvertor(int numFiles, Predicate<NetworkAction> startAction, Predicate<NetworkAction> endAction)
        {
            this.numFiles = numFiles;
            this.startAction = startAction;
            this.endAction = endAction;
        }

        public void AddNetworkAction(NetworkAction action)
        {
            Task.Run(() => { networkActions.Add(action); });
        }

        public async Task<SessionLog> Convert(CancellationToken ct)
        {

            while (true)
            {
                if (ct.IsCancellationRequested) return sessionLog;
                if (!networkActions.TryTake(out NetworkAction info)) await Task.Delay(200);
                if (info == null) continue;

                var action = GetAction();
                action.Invoke(info);
                if (counter != numFiles)
                {
                    sessionLog.IsCancelled = false;
                    sessionLog.EndTime = info.Time;
                    return sessionLog;
                }
            }
        }

        private Action<NetworkAction> GetAction()
        {
            if (started)
                return OnEventInStartedState;
            return OnEventInInitialState;
        }

        private void OnEventInInitialState(NetworkAction action)
        {
            if (startAction.Invoke(action))
            {
                started = true;
                sessionLog.StarTime = action.Time;
                sessionLog.Actions.Add(action);
            }
        }

        private void OnEventInStartedState(NetworkAction action)
        {
            if (endAction.Invoke(action))
            {
                counter++;
                sessionLog.Actions.Add(action);
            }
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

    public class SessionLog
    {
        public SessionLog()
        {
            Actions = new List<NetworkAction>();
            IsCancelled = true;
        }

        public bool IsCancelled { get; set; }
        public DateTime StarTime { get; set; }
        public DateTime EndTime { get; set; }
        public List<NetworkAction> Actions { get; set; }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"Total Time (s) is \t{(EndTime - StarTime).TotalSeconds}");
            builder.AppendLine($"Start Time is \t{StarTime.ToLongTimeString()}");
            builder.AppendLine($"End Time (s) is \t{EndTime.ToLongTimeString()}");
            builder.AppendLine($"Is Cancelled is \t{IsCancelled}");
            foreach (var n in Actions)
            {
                builder.AppendLine(
                    $"{n.Time.ToLongTimeString()}\t{n.Type}\t{n.MappingId}\t{n.Method}\t{n.PayloadSize}\t{n.Url}");
            }
            return builder.ToString();
        }
    }
}
