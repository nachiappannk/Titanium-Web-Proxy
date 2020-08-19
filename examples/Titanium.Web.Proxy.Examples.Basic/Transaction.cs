using System;

namespace Titanium.Web.Proxy.Examples.Basic
{
    public class Transaction
    {
        public int ProcessId { get; set; }
        public TransactionType Type { get; set; }
        public int MappingId { get; set; }
        public String Url { get; set; }
        public DateTime Time { get; set; }
        public long PayloadSize { get; set; }
        public String Method { get; set; }
        public String Body { get; set; }
    }
}
