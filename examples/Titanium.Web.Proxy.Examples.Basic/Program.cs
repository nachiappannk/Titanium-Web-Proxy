using System;
using System.Text;
using System.Threading.Tasks;
using Titanium.Web.Proxy.Examples.Basic.Performance;
using Titanium.Web.Proxy.Helpers;

namespace Titanium.Web.Proxy.Examples.Basic
{
    public class Program
    {
        private static int OneKb = 1024;
        private static int OneMb = OneKb * OneKb;

        public static void Main(string[] args)
        {

            Test().GetAwaiter().GetResult();
            
        }


        private static async Task Test()
        {
            var performanceProbe = new PerformanceProbe("sharefile", "szchanaa");


            Console.WriteLine("Do preparation");
            var pt = performanceProbe.GetFileLogs(300, 1, StartCounter, EndCounter);
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
            if (a.Type != NetworkActionType.Request)
                return false;
            if (!a.Url.Contains("upload-threaded-3.aspx"))
                return false;
            return true;
        }

    }
}
