using System;
using System.Collections.Generic;
using System.Text;

namespace Titanium.Web.Proxy.Examples.Basic.Performance
{
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
            var time = "";
            if (!IsCancelled)
                time = (EndTime - StarTime).TotalSeconds.ToString();

            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"Total Time (s) is \t{time}");
            builder.AppendLine($"Start Time is \t{StarTime.ToLongTimeString()}");
            builder.AppendLine($"End Time is \t{EndTime.ToLongTimeString()}");
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
