using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Threading.Tasks;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Examples.Basic.Performance;
using Titanium.Web.Proxy.Helpers;
using Titanium.Web.Proxy.Http;
using Titanium.Web.Proxy.Models;

namespace Titanium.Web.Proxy.Examples.Basic
{
    public class ProxyTestController : IDisposable
    {
        private readonly List<string> hostNames;
        private readonly ProxyServer proxyServer;
        private ExplicitProxyEndPoint explicitEndPoint;
        public event Action<NetworkAction> OnNetworkEvent;

        public ProxyTestController(List<String> hostNames)
        {
            this.hostNames = hostNames;
            proxyServer = new ProxyServer();

            proxyServer.ExceptionFunc = async exception => { };
            proxyServer.TcpTimeWaitSeconds = 10;
            proxyServer.ConnectionTimeOutSeconds = 15;
            proxyServer.ReuseSocket = false;
            proxyServer.EnableConnectionPool = false;
            proxyServer.ForwardToUpstreamGateway = true;
            proxyServer.CertificateManager.SaveFakeCertificates = true;
        }

        public void StartProxy()
        {
            proxyServer.BeforeRequest += OnRequestToServer;
            proxyServer.BeforeResponse += OnResponseFromServer;
            proxyServer.ServerCertificateValidationCallback += OnCertificateValidation;
            
            explicitEndPoint = new ExplicitProxyEndPoint(IPAddress.Any, 8000);
            explicitEndPoint.BeforeTunnelConnectRequest += OnBeforeTunnelConnectRequest;
            proxyServer.AddEndPoint(explicitEndPoint);
            proxyServer.Start();
            
            
            if (RunTime.IsWindows)
            {
                proxyServer.SetAsSystemProxy(explicitEndPoint, ProxyProtocolType.AllHttp);
            }
        }

        public void Stop()
        {
            explicitEndPoint.BeforeTunnelConnectRequest -= OnBeforeTunnelConnectRequest;
            proxyServer.BeforeRequest -= OnRequestToServer;
            proxyServer.BeforeResponse -= OnResponseFromServer;
            proxyServer.ServerCertificateValidationCallback -= OnCertificateValidation;
            proxyServer.Stop();
        }

        private async Task OnBeforeTunnelConnectRequest(object sender, TunnelConnectSessionEventArgs e)
        {
            string hostname = e.HttpClient.Request.RequestUri.Host;

            var clientLocalIp = e.ClientLocalEndPoint.Address;
            if (!clientLocalIp.Equals(IPAddress.Loopback) && !clientLocalIp.Equals(IPAddress.IPv6Loopback))
            {
                e.HttpClient.UpStreamEndPoint = new IPEndPoint(clientLocalIp, 0);
            }

            int processIdValue = e.HttpClient.ProcessId.Value;
            if (!IsValidHost(hostname, processIdValue))
            {
                e.DecryptSsl = false;
            }
        }

        private async Task OnRequestToServer(object sender, SessionEventArgs e)
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
                var networkInfo = CreateRequestNetworkInfo(httpClient, processIdValue);
                try
                {
                    networkInfo.Body = "";
                    if (httpClient.Request.HasBody && httpClient.Request.ContentLength > 0) networkInfo.Body = await e.GetRequestBodyAsString();
                }
                catch (Exception ex)
                {
                    networkInfo.Body = "There is an error in processing";
                }
                OnNetworkEvent?.Invoke(networkInfo);
            }
        }

        private static NetworkAction CreateRequestNetworkInfo(HttpWebClient client, int processIdValue)
        {
            var networkInfo = new NetworkAction()
            {
                MappingId = client.GetHashCode(),
                ProcessId = processIdValue,
                PayloadSize = client.Request.ContentLength,
                Time = DateTime.Now,
                Method = client.Request.Method,
                Type = NetworkActionType.Request,
                Url = client.Request.Url,
            };
            return networkInfo;
        }

        private bool IsValidHost(string url, int procId)
        {
            return hostNames.Any(url.Contains);
        }

        private async Task OnResponseFromServer(object sender, SessionEventArgs e)
        {
            var client = e.HttpClient;
            int processIdValue = client.ProcessId.Value;
            string requestUrl = client.Request.Url;

            if(IsValidHost(requestUrl, processIdValue))
            {
                var networkInfo = await CreateResponseNetworkInfo(client, processIdValue);
                try
                {
                    networkInfo.Body = "";
                    if (client.Response.HasBody && client.Response.ContentLength > 0) networkInfo.Body = await e.GetResponseBodyAsString();
                }
                catch (Exception ex)
                {
                    networkInfo.Body = "There is an error in processing";
                }
                OnNetworkEvent?.Invoke(networkInfo);
            }
        }

        private static async Task<NetworkAction> CreateResponseNetworkInfo(
            HttpWebClient client, int processIdValue)
        {
            long responseContentLength = client.Response.ContentLength;
            var networkInfo = new NetworkAction()
            {
                MappingId = client.GetHashCode(),
                Url = client.Request.Url,
                ProcessId = processIdValue,
                PayloadSize = client.Response.ContentLength,
                Time = DateTime.Now,
                Method = client.Request.Method,
                Type = NetworkActionType.Response,
            };
            return networkInfo;
        }


        public Task OnCertificateValidation(object sender, CertificateValidationEventArgs e)
        {
            if (e.SslPolicyErrors == SslPolicyErrors.None)
                e.IsValid = true;
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            OnNetworkEvent = null;
            proxyServer?.Dispose();
        }
    }
}
