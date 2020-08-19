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
        private readonly ProxyServer proxyServer;
        private ExplicitProxyEndPoint explicitEndPoint;
        public event Action<String, long> OnRequest;
        public event Action<String, String, int, String, long> OnResponse;
        public event Action<NetworkInfo> OnNetworkEvent;

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
                var client = e.HttpClient;
                var networkInfo = CreateRequestNetworkInfo(client, processIdValue);
                OnNetworkEvent?.Invoke(networkInfo);

                OnRequest?.Invoke(httpClient.Request.Url, httpClient.Request.ContentLength);
                e.GetState().PipelineInfo.AppendLine(nameof(OnRequestToServer) + ":" + e.HttpClient.Request.RequestUri);

            }
        }

        private static NetworkInfo CreateRequestNetworkInfo(HttpWebClient client, int processIdValue)
        {
            var networkInfo = new NetworkInfo()
            {
                Id = client.GetHashCode(),
                ProcessId = processIdValue,
                PayloadSize = client.Request.ContentLength,
                Time = DateTime.Now,
                Method = client.Request.Method,
                Type = NetworkInfoType.Request,
                Url = client.Request.Url,
            };
            try
            {

                networkInfo.Body = client.Request.BodyString;
            }
            catch (Exception exception)
            {
                networkInfo.Body = "error";
            }

            return networkInfo;
        }

        private bool IsValidHost(string url, int procId)
        {
            return hostNames.Any(x => url.Contains(x));
        }

        private async Task OnResponseFromServer(object sender, SessionEventArgs e)
        {
            int processIdValue = e.HttpClient.ProcessId.Value;
            string requestUrl = e.HttpClient.Request.Url;

            if(IsValidHost(requestUrl, processIdValue))
            {
                var client = e.HttpClient;

                var body = "";
               
                var networkInfo = await CreateResponseNetworkInfo(client, processIdValue);

                OnNetworkEvent?.Invoke(networkInfo);
                OnResponse?.Invoke(e.HttpClient.Request.Url, body, e.HttpClient.Response.StatusCode, e.HttpClient.Request.Method, 0);
            }
        }

        private static async Task<NetworkInfo> CreateResponseNetworkInfo(
            HttpWebClient client, int processIdValue)
        {
            long responseContentLength = client.Response.ContentLength;
            string body;
            if (client.Response.HasBody && responseContentLength > 0)
            {
                try
                {
                    body = client.Response.BodyString;
                }
                catch (Exception ee)
                {
                    body = "error reading body";
                }
            }

            var networkInfo = new NetworkInfo()
            {
                Id = client.GetHashCode(),
                Url = client.Request.Url,
                ProcessId = processIdValue,
                PayloadSize = client.Response.ContentLength,
                Time = DateTime.Now,
                Method = client.Request.Method,
                Type = NetworkInfoType.Response,
            };
            try
            {
                networkInfo.Body = client.Request.BodyString;
            }
            catch (Exception exception)
            {
                networkInfo.Body = "error";
            }

            return networkInfo;
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

        public void Dispose()
        {
            proxyServer?.Dispose();
            
        }


        
    }

    public enum NetworkInfoType
    {
        Request,
        Response,
    }


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
