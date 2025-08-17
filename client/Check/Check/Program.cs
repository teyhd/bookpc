using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Check
{
    static class Program
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        /// 

        static System.Threading.Mutex singleton = new Mutex(true, "Check");
        [STAThread]
        static async Task Main()
        {
            //Console.WriteLine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location));
            if (!singleton.WaitOne(TimeSpan.Zero, true))
            {
                Mylog("Открыт второй экзмепляр");
                Application.Exit();
                return;
            }
            var builder = new HostBuilder()
                 .ConfigureServices((hostContext, services) =>
                 {
                     services.AddHostedService<back.MyBackgroundService>();
                 });

            await builder.RunConsoleAsync();
        }
        public static void Mylog(string LogsText)
        {
            System.IO.Directory.CreateDirectory("C:\\Windows\\secur\\logs");
            System.IO.File.AppendAllText($"C:\\Windows\\secur\\logs\\{DateTime.Now:M} logs.txt", $"\n(Check)[{DateTime.Now}] {LogsText}");
            Console.WriteLine($"\n[{DateTime.Now}] {LogsText}");
        }
        static public string GetPath()
        {
            return System.IO.Path.GetDirectoryName(Application.ExecutablePath);
        }

    }
}
