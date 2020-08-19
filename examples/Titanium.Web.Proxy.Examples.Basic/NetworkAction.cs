using System;

namespace Titanium.Web.Proxy.Examples.Basic
{
    public class NetworkAction
    {
        public int ProcessId { get; set; }
        public NetworkActionType Type { get; set; }
        public int MappingId { get; set; }
        public String Url { get; set; }
        public DateTime Time { get; set; }
        public long PayloadSize { get; set; }
        public String Method { get; set; }
        public String Body { get; set; }
    }
}
