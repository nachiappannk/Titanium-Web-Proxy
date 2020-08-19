using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Titanium.Web.Proxy.Examples.Basic.Performance
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
                if (counter == numFiles)
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
            sessionLog.Actions.Add(action);
            if (endAction.Invoke(action))
            {
                counter++;
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
}
