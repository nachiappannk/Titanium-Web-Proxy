using System;
using System.Text;
using System.Threading.Tasks;
using Titanium.Web.Proxy.Examples.Basic.Performance;
using Titanium.Web.Proxy.Helpers;

namespace Titanium.Web.Proxy.Examples.Basic
{
    public class Program
    {
        private static String[] domainNames = new[] { "sharefile", "szchanaa" };

        public static void Main(string[] args)
        {
            //FileCreator is also available in the project

            //TestASingleFileWebAppUpload().GetAwaiter().GetResult();
            TestTwoFileWebAppUpload().GetAwaiter().GetResult();
        }


        private static async Task TestASingleFileWebAppUpload()
        {
            var performanceProbe = new PerformanceProbe(domainNames);
            Console.WriteLine("Do preparation");
            var pt = performanceProbe.GetFileLogs(300, 1, StartCounter, EndCounter);
            Console.WriteLine("Do the test");
            var result = await pt;
            Console.WriteLine(result);
            Console.WriteLine("Do Clean up");
        }

        private static async Task TestTwoFileWebAppUpload()
        {
            var performanceProbe = new PerformanceProbe(domainNames);
            Console.WriteLine("Do preparation");
            var pt = performanceProbe.GetFileLogs(300, 2, StartCounter, EndCounter);
            Console.WriteLine("Do the test");
            var result = await pt;
            Console.WriteLine(result);
            Console.WriteLine("Do Clean up");
        }

        private static bool StartCounter(NetworkAction a)
        {
            if (a.Type != NetworkActionType.Request)
                return false;
            if (!a.Url.Contains("Upload2"))
                return false;
            return true;
        }

        private static bool EndCounter(NetworkAction a)
        {
            if (!a.Url.Contains("upload-threaded-3.aspx"))
                return false;
            if (a.Type != NetworkActionType.Response)
                return false;
            return true;
        }

    }
}
