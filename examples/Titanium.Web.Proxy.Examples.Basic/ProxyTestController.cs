using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Exceptions;
using Titanium.Web.Proxy.Helpers;
using Titanium.Web.Proxy.Http;
using Titanium.Web.Proxy.Models;
using Titanium.Web.Proxy.StreamExtended.Network;

namespace Titanium.Web.Proxy.Examples.Basic
{
    public class ProxyTestController : IDisposable
    {
        private readonly List<string> hostNames;
        private readonly SemaphoreSlim @lock = new SemaphoreSlim(1);
        private readonly ProxyServer proxyServer;
        private ExplicitProxyEndPoint explicitEndPoint;
        private List<String> logs = new List<String>();
        private HashSet<HttpWebClient> httpWebClients = new HashSet<HttpWebClient>();

        public ProxyTestController(List<String> hostNames)
        {
            this.hostNames = hostNames;
            proxyServer = new ProxyServer();

            proxyServer.ExceptionFunc = async exception =>
            {
                if (exception is ProxyHttpException phex)
                {
                    await WriteToConsole("Exception", exception.Message + ": " + phex.InnerException?.Message, -1);
                }
                else
                {
                    await WriteToConsole("Exception", exception.Message, -1);
                }
            };

            proxyServer.TcpTimeWaitSeconds = 10;
            proxyServer.ConnectionTimeOutSeconds = 15;
            proxyServer.ReuseSocket = false;
            proxyServer.EnableConnectionPool = false;
            proxyServer.ForwardToUpstreamGateway = true;
            proxyServer.CertificateManager.SaveFakeCertificates = true;
        }

        public void StartProxy()
        {
            proxyServer.BeforeRequest += onRequest;
            proxyServer.BeforeResponse += onResponse;
            proxyServer.AfterResponse += onAfterResponse;
            proxyServer.ServerCertificateValidationCallback += OnCertificateValidation;
            proxyServer.ClientCertificateSelectionCallback += OnCertificateSelection;
            explicitEndPoint = new ExplicitProxyEndPoint(IPAddress.Any, 8000);
            explicitEndPoint.BeforeTunnelConnectRequest += onBeforeTunnelConnectRequest;
            explicitEndPoint.BeforeTunnelConnectResponse += onBeforeTunnelConnectResponse;
            proxyServer.AddEndPoint(explicitEndPoint);
            proxyServer.Start();
            foreach (var endPoint in proxyServer.ProxyEndPoints)
            {
                Console.WriteLine("Listening on '{0}' endpoint at Ip {1} and port: {2} ", endPoint.GetType().Name,
                    endPoint.IpAddress, endPoint.Port);
            }
            if (RunTime.IsWindows)
            {
                proxyServer.SetAsSystemProxy(explicitEndPoint, ProxyProtocolType.AllHttp);
            }
        }

        public List<String> Stop()
        {
            explicitEndPoint.BeforeTunnelConnectRequest -= onBeforeTunnelConnectRequest;
            explicitEndPoint.BeforeTunnelConnectResponse -= onBeforeTunnelConnectResponse;

            proxyServer.BeforeRequest -= onRequest;
            proxyServer.BeforeResponse -= onResponse;
            proxyServer.ServerCertificateValidationCallback -= OnCertificateValidation;
            proxyServer.ClientCertificateSelectionCallback -= OnCertificateSelection;

            proxyServer.Stop();

            return logs;
        }

        private async Task onBeforeTunnelConnectRequest(object sender, TunnelConnectSessionEventArgs e)
        {
            string hostname = e.HttpClient.Request.RequestUri.Host;
            e.GetState().PipelineInfo.AppendLine(nameof(onBeforeTunnelConnectRequest) + ":" + hostname);
            

            var clientLocalIp = e.ClientLocalEndPoint.Address;
            if (!clientLocalIp.Equals(IPAddress.Loopback) && !clientLocalIp.Equals(IPAddress.IPv6Loopback))
            {
                e.HttpClient.UpStreamEndPoint = new IPEndPoint(clientLocalIp, 0);
            }

            int processIdValue = e.HttpClient.ProcessId.Value;
            if (!IsValidHost(hostname, processIdValue))
            {
                // Exclude Https addresses you don't want to proxy
                // Useful for clients that use certificate pinning
                // for example dropbox.com
                
                e.DecryptSsl = false;
            }
            else
            {
                await WriteToConsole("Tunnel to ", e.HttpClient.Request.Url, processIdValue);
            }
        }

        private void WebSocket_DataSent(object sender, DataEventArgs e)
        {
            var args = (SessionEventArgs)sender;
            WebSocketDataSentReceived(args, e, true);
        }

        private void WebSocket_DataReceived(object sender, DataEventArgs e)
        {
            var args = (SessionEventArgs)sender;
            WebSocketDataSentReceived(args, e, false);
        }

        private void WebSocketDataSentReceived(SessionEventArgs args, DataEventArgs e, bool sent)
        {
            foreach (var frame in args.WebSocketDecoder.Decode(e.Buffer, e.Offset, e.Count))
            {
                WriteToConsole("socket comm", "is Sent :"+sent + frame.OpCode, -1).Wait();
            }
        }

        private Task onBeforeTunnelConnectResponse(object sender, TunnelConnectSessionEventArgs e)
        {
            e.GetState().PipelineInfo.AppendLine(nameof(onBeforeTunnelConnectResponse) + ":" + e.HttpClient.Request.RequestUri);
            return Task.CompletedTask;
        }

        private async Task onRequest(object sender, SessionEventArgs e)
        {
            var clientLocalIp = e.ClientLocalEndPoint.Address;
            if (!clientLocalIp.Equals(IPAddress.Loopback) && !clientLocalIp.Equals(IPAddress.IPv6Loopback))
            {
                e.HttpClient.UpStreamEndPoint = new IPEndPoint(clientLocalIp, 0);
            }
            var httpClient = e.HttpClient;
            var url = httpClient.Request.Url;
            int processIdValue = e.HttpClient.ProcessId.Value;
            if (IsValidHost(url, processIdValue))
            {
                e.GetState().PipelineInfo.AppendLine(nameof(onRequest) + ":" + e.HttpClient.Request.RequestUri);
                await WriteToConsole("request", url, processIdValue);
            }
        }

        private bool IsValidHost(string url, int procId)
        {
            return hostNames.Any(x => url.Contains(x));
        }

        private async Task onResponse(object sender, SessionEventArgs e)
        {
            int processIdValue = e.HttpClient.ProcessId.Value;
            string requestUrl = e.HttpClient.Request.Url;
  

            if(IsValidHost(requestUrl, processIdValue))
            {
                if (e.HttpClient.ConnectRequest?.TunnelType == TunnelType.Websocket)
                {
                    e.DataSent += WebSocket_DataSent;
                    e.DataReceived += WebSocket_DataReceived;
                }

                e.GetState().PipelineInfo.AppendLine(nameof(onResponse));
                string ext = System.IO.Path.GetExtension(e.HttpClient.Request.RequestUri.AbsolutePath);
                await WriteToConsole("response",requestUrl, processIdValue);
            }
        }

        private async Task onAfterResponse(object sender, SessionEventArgs e)
        {
            int processIdValue = e.HttpClient.ProcessId.Value;
            if (IsValidHost(e.HttpClient.Request.Url, processIdValue))
            {
                await WriteToConsole("Pipelineinfo", $"{e.GetState().PipelineInfo}", processIdValue);
            }
        }

        public Task OnCertificateValidation(object sender, CertificateValidationEventArgs e)
        {
            e.GetState().PipelineInfo.AppendLine(nameof(OnCertificateValidation));
            if (e.SslPolicyErrors == SslPolicyErrors.None)
            {
                e.IsValid = true;
            }
            return Task.CompletedTask;
        }

        public Task OnCertificateSelection(object sender, CertificateSelectionEventArgs e)
        {
            e.GetState().PipelineInfo.AppendLine(nameof(OnCertificateSelection));
            return Task.CompletedTask;
        }

        private async Task WriteToConsole(string type, string message, int processId)
        {
            var mes = message + "\t" + DateTime.Now.ToLongTimeString() + "\t" + type + "\t"+ processId;  
            await @lock.WaitAsync();
            logs.Add(mes);
            @lock.Release();
        }

        public void Dispose()
        {
            @lock?.Dispose();
            proxyServer?.Dispose();
        }
    }
}
