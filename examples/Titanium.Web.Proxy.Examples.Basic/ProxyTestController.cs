﻿using System;
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
                    //TBD await writeToConsole(exception.Message + ": " + phex.InnerException?.Message, ConsoleColor.Red);
                }
                else
                {
                    //TBD await writeToConsole(exception.Message, ConsoleColor.Red);
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

            //proxyServer.EnableWinAuth = true;

            explicitEndPoint = new ExplicitProxyEndPoint(IPAddress.Any, 8000);

            // Fired when a CONNECT request is received
            explicitEndPoint.BeforeTunnelConnectRequest += onBeforeTunnelConnectRequest;
            explicitEndPoint.BeforeTunnelConnectResponse += onBeforeTunnelConnectResponse;

            // An explicit endpoint is where the client knows about the existence of a proxy
            // So client sends request in a proxy friendly manner
            proxyServer.AddEndPoint(explicitEndPoint);
            proxyServer.Start();

            // Transparent endpoint is useful for reverse proxy (client is not aware of the existence of proxy)
            // A transparent endpoint usually requires a network router port forwarding HTTP(S) packets or DNS
            // to send data to this endPoint
            //var transparentEndPoint = new TransparentProxyEndPoint(IPAddress.Any, 443, true)
            //{ 
            //    // Generic Certificate hostname to use
            //    // When SNI is disabled by client
            //    GenericCertificateName = "google.com"
            //};

            //proxyServer.AddEndPoint(transparentEndPoint);
            //proxyServer.UpStreamHttpProxy = new ExternalProxy("localhost", 8888);
            //proxyServer.UpStreamHttpsProxy = new ExternalProxy("localhost", 8888);

            // SOCKS proxy
            //proxyServer.UpStreamHttpProxy = new ExternalProxy("127.0.0.1", 1080)
            //    { ProxyType = ExternalProxyType.Socks5, UserName = "User1", Password = "Pass" };
            //proxyServer.UpStreamHttpsProxy = new ExternalProxy("127.0.0.1", 1080)
            //    { ProxyType = ExternalProxyType.Socks5, UserName = "User1", Password = "Pass" };


            //var socksEndPoint = new SocksProxyEndPoint(IPAddress.Any, 1080, true)
            //{
            //    // Generic Certificate hostname to use
            //    // When SNI is disabled by client
            //    GenericCertificateName = "google.com"
            //};

            //proxyServer.AddEndPoint(socksEndPoint);

            foreach (var endPoint in proxyServer.ProxyEndPoints)
            {
                Console.WriteLine("Listening on '{0}' endpoint at Ip {1} and port: {2} ", endPoint.GetType().Name,
                    endPoint.IpAddress, endPoint.Port);
            }

            // Only explicit proxies can be set as system proxy!
            //proxyServer.SetAsSystemHttpProxy(explicitEndPoint);
            //proxyServer.SetAsSystemHttpsProxy(explicitEndPoint);
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

        private async Task<IExternalProxy> onGetCustomUpStreamProxyFunc(SessionEventArgsBase arg)
        {
            arg.GetState().PipelineInfo.AppendLine(nameof(onGetCustomUpStreamProxyFunc));

            // this is just to show the functionality, provided values are junk
            return new ExternalProxy
            {
                BypassLocalhost = false,
                HostName = "127.0.0.9",
                Port = 9090,
                Password = "fake",
                UserName = "fake",
                UseDefaultCredentials = false
            };
        }

        private async Task<IExternalProxy> onCustomUpStreamProxyFailureFunc(SessionEventArgsBase arg)
        {
            arg.GetState().PipelineInfo.AppendLine(nameof(onCustomUpStreamProxyFailureFunc));

            // this is just to show the functionality, provided values are junk
            return new ExternalProxy
            {
                BypassLocalhost = false,
                HostName = "127.0.0.10",
                Port = 9191,
                Password = "fake2",
                UserName = "fake2",
                UseDefaultCredentials = false
            };
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

            if (!IsValidHost(hostname))
            {
                // Exclude Https addresses you don't want to proxy
                // Useful for clients that use certificate pinning
                // for example dropbox.com
                
                e.DecryptSsl = false;
            }
            else
            {
                await writeToConsole("Tunnel to: " + hostname);
                await writeToConsole("Tunnel to: " + e.HttpClient.Request.RequestUri);
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
            var color = sent ? ConsoleColor.Green : ConsoleColor.Blue;

            foreach (var frame in args.WebSocketDecoder.Decode(e.Buffer, e.Offset, e.Count))
            {
                if (frame.OpCode == WebsocketOpCode.Binary)
                {
                    var data = frame.Data.ToArray();
                    string str = string.Join(",", data.ToArray().Select(x => x.ToString("X2")));
                    //TBD writeToConsole(str, color).Wait();
                }

                if (frame.OpCode == WebsocketOpCode.Text)
                {
                    //TBD writeToConsole(frame.GetText(), color).Wait();
                }
            }
        }

        private Task onBeforeTunnelConnectResponse(object sender, TunnelConnectSessionEventArgs e)
        {
            e.GetState().PipelineInfo.AppendLine(nameof(onBeforeTunnelConnectResponse) + ":" + e.HttpClient.Request.RequestUri);

            return Task.CompletedTask;
        }

        // intercept & cancel redirect or update requests
        private async Task onRequest(object sender, SessionEventArgs e)
        {
            e.GetState().PipelineInfo.AppendLine(nameof(onRequest) + ":" + e.HttpClient.Request.RequestUri);

            var clientLocalIp = e.ClientLocalEndPoint.Address;
            if (!clientLocalIp.Equals(IPAddress.Loopback) && !clientLocalIp.Equals(IPAddress.IPv6Loopback))
            {
                e.HttpClient.UpStreamEndPoint = new IPEndPoint(clientLocalIp, 0);
            }

            var url = e.HttpClient.Request.Url;
            if (IsValidHost(url))
            {
                //httpWebClients.Add(e.HttpClient);
                await writeToConsole("Active Client Connections:" + ((ProxyServer)sender).ClientConnectionCount);
                await writeToConsole(e.HttpClient.Request.Url);
                await writeToConsole("ProcessID " + e.HttpClient.ProcessId.Value.ToString());
            }


            // store it in the UserData property
            // It can be a simple integer, Guid, or any type
            //e.UserData = new CustomUserData()
            //{
            //    RequestHeaders = e.HttpClient.Request.Headers,
            //    RequestBody = e.HttpClient.Request.HasBody ? e.HttpClient.Request.Body:null,
            //    RequestBodyString = e.HttpClient.Request.HasBody? e.HttpClient.Request.BodyString:null
            //};

            ////This sample shows how to get the multipart form data headers
            //if (e.HttpClient.Request.Host == "mail.yahoo.com" && e.HttpClient.Request.IsMultipartFormData)
            //{
            //    e.MultipartRequestPartSent += MultipartRequestPartSent;
            //}



        }

        private bool IsValidHost(string url)
        {
            return hostNames.Any(x => url.Contains(x));
        }

        // Modify response
        private async Task multipartRequestPartSent(object sender, MultipartRequestPartSentEventArgs e)
        {
            e.GetState().PipelineInfo.AppendLine(nameof(multipartRequestPartSent));

            var session = (SessionEventArgs)sender;
            //TBD await writeToConsole("Multipart form data headers:");
            foreach (var header in e.Headers)
            {
               //TBD await writeToConsole(header.ToString());
            }
        }

        private async Task onResponse(object sender, SessionEventArgs e)
        {
            e.GetState().PipelineInfo.AppendLine(nameof(onResponse));


            if (e.HttpClient.ConnectRequest?.TunnelType == TunnelType.Websocket)
            {
                e.DataSent += WebSocket_DataSent;
                e.DataReceived += WebSocket_DataReceived;
            }

            if(IsValidHost(e.HttpClient.Request.Url))
            {
                await writeToConsole("Active Server Connections:" + ((ProxyServer)sender).ServerConnectionCount);
            
                string ext = System.IO.Path.GetExtension(e.HttpClient.Request.RequestUri.AbsolutePath);
                await writeToConsole("response " + e.HttpClient.Request.Url);
                await writeToConsole("response 1" + ext);
                await writeToConsole("ProcessID " + e.HttpClient.ProcessId.Value.ToString());

            }
            // access user data set in request to do something with it
            //var userData = e.HttpClient.UserData as CustomUserData;

            //if (ext == ".gif" || ext == ".png" || ext == ".jpg")
            //{ 
            //    byte[] btBody = Encoding.UTF8.GetBytes("<!DOCTYPE html>" +
            //                                           "<html><body><h1>" +
            //                                           "Image is blocked" +
            //                                           "</h1>" +
            //                                           "<p>Blocked by Titanium</p>" +
            //                                           "</body>" +
            //                                           "</html>");

            //    var response = new OkResponse(btBody);
            //    response.HttpVersion = e.HttpClient.Request.HttpVersion;

            //    e.Respond(response);
            //    e.TerminateServerConnection();
            //} 

            //// print out process id of current session
            ////WriteToConsole($"PID: {e.HttpClient.ProcessId.Value}");

            ////if (!e.ProxySession.Request.Host.Equals("medeczane.sgk.gov.tr")) return;
            //if (e.HttpClient.Request.Method == "GET" || e.HttpClient.Request.Method == "POST")
            //{ 
            //    if (e.HttpClient.Response.StatusCode == (int)HttpStatusCode.OK)
            //    {
            //        if (e.HttpClient.Response.ContentType != null && e.HttpClient.Response.ContentType.Trim().ToLower().Contains("text/html"))
            //        {
            //            var bodyBytes = await e.GetResponseBody();
            //            await e.SetResponseBody(bodyBytes);

            //            string body = await e.GetResponseBodyAsString();
            //            await e.SetResponseBodyString(body);
            //        }
            //    }
            //} 
        }

        private async Task onAfterResponse(object sender, SessionEventArgs e)
        {
            //TBD await writeToConsole($"Pipelineinfo: {e.GetState().PipelineInfo}", ConsoleColor.Yellow);
        }

        /// <summary>
        ///     Allows overriding default certificate validation logic
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public Task OnCertificateValidation(object sender, CertificateValidationEventArgs e)
        {
            e.GetState().PipelineInfo.AppendLine(nameof(OnCertificateValidation));

            // set IsValid to true/false based on Certificate Errors
            if (e.SslPolicyErrors == SslPolicyErrors.None)
            {
                e.IsValid = true;
            }

            return Task.CompletedTask;
        }

        /// <summary>
        ///     Allows overriding default client certificate selection logic during mutual authentication
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public Task OnCertificateSelection(object sender, CertificateSelectionEventArgs e)
        {
            e.GetState().PipelineInfo.AppendLine(nameof(OnCertificateSelection));

            // set e.clientCertificate to override

            return Task.CompletedTask;
        }

        private async Task writeToConsole(string message, ConsoleColor? consoleColor = null)
        {
            await @lock.WaitAsync();
            logs.Add(message);
            Console.WriteLine(message);
            @lock.Release();
        }


        public void Dispose()
        {
            @lock?.Dispose();
            proxyServer?.Dispose();
        }
    }
}
