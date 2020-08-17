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
                    await writeToConsole("Exception", exception.Message + ": " + phex.InnerException?.Message);
                }
                else
                {
                    await writeToConsole("Exception", exception.Message);
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

            if (!IsValidHost(hostname, e.HttpClient.ProcessId.Value))
            {
                // Exclude Https addresses you don't want to proxy
                // Useful for clients that use certificate pinning
                // for example dropbox.com
                
                e.DecryptSsl = false;
            }
            else
            {
                await writeToConsole("Tunnel to" , hostname);
                await writeToConsole("Tunnel to ", e.HttpClient.Request.Url);
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
                writeToConsole("socket comm", "is Sent :"+sent + frame.OpCode).Wait();
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
            if (IsValidHost(url, e.HttpClient.ProcessId.Value))
            {
                e.GetState().PipelineInfo.AppendLine(nameof(onRequest) + ":" + e.HttpClient.Request.RequestUri);
                await writeToConsole("request", url);
                await writeToConsole("request","ProcessID " + httpClient.ProcessId.Value.ToString());
            }
        }

        private bool IsValidHost(string url, int procId)
        {
            return hostNames.Any(x => url.Contains(x));
        }

        private async Task onResponse(object sender, SessionEventArgs e)
        {

            if (e.HttpClient.ConnectRequest?.TunnelType == TunnelType.Websocket)
            {
                if (IsValidHost(e.HttpClient.Request.Url, e.HttpClient.ProcessId.Value))
                {
                    e.DataSent += WebSocket_DataSent;
                    e.DataReceived += WebSocket_DataReceived;
                }
            }

            if(IsValidHost(e.HttpClient.Request.Url, e.HttpClient.ProcessId.Value))
            {
                e.GetState().PipelineInfo.AppendLine(nameof(onResponse));
                string ext = System.IO.Path.GetExtension(e.HttpClient.Request.RequestUri.AbsolutePath);
                await writeToConsole("response","response " + e.HttpClient.Request.Url);
                await writeToConsole("response", "response 1" + ext);
                await writeToConsole("response", "ProcessID " + e.HttpClient.ProcessId.Value.ToString());
            }
        }

        private async Task onAfterResponse(object sender, SessionEventArgs e)
        {
            if (IsValidHost(e.HttpClient.Request.Url, e.HttpClient.ProcessId.Value))
            {
                await writeToConsole("Pipelineinfo", $"{e.GetState().PipelineInfo}");
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

        private async Task writeToConsole(string type, string message)
        {
            var mes = DateTime.Now.ToLongTimeString() +"\t"+ type + "\t" + message;
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
