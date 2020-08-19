using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Titanium.Web.Proxy.Examples.Basic
{
    public class FileCreator
    {
        private static Random random = new Random();

        public static async Task CreateFile(String path, int size)
        {
            if (File.Exists(path)) File.Delete(path);
            using (StreamWriter sw = File.CreateText(path))
            {
                int count = size / 1000;
                if (count == 0)
                    count++;
                for (int i = 0; i < count; i++)
                {
                    await sw.WriteAsync((string)RandomString(1000));
                }

                await sw.FlushAsync();
            }
            
        }

        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}