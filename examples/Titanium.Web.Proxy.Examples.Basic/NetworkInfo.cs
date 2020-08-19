using System;

namespace Titanium.Web.Proxy.Examples.Basic
{
    public class NetworkInfo
    {
        public int Id { get; set; }
        public String Url { get; set; }
        public int ProcessId { get; set; }
        public NetworkInfoType Type { get; set; }
        public DateTime Time { get; set; }
        public long PayloadSize { get; set; }
        public String Method { get; set; }
        public String Body { get; set; }
    }
}
