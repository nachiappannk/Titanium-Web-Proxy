﻿using System;
using Titanium.Web.Proxy.Examples.Basic.Performance;

namespace Titanium.Web.Proxy.Examples.Basic
{
    public class NetworkTransaction
    {
        public NetworkAction Request { get; set; }
        public NetworkAction Response { get; set; }
    }
}