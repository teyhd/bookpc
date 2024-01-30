using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Win32;
using static MyProg.IniFile;
namespace LastSecur
{
    static class Program
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        /// Pr
        /// IniFile
        
        static public Boolean AdminMode = false;
        static public Boolean first = true;
        static public Boolean isoff = false;

        [STAThread]
        static async Task Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var builder = new HostBuilder()
            .ConfigureServices((hostContext, services) =>
            {
                services.AddHostedService<MyBackgroundService>();
            });
            try
            {
                Events();
                await builder.RunConsoleAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Application.Run(new Form1());
                throw;
            }
            
        }

        public static void Events()
        {
            SystemEvents.SessionSwitch += (sender, args) =>
            {
                if (args.Reason == SessionSwitchReason.SessionLock)
                {
                    Mylog("Экран блокирован.");
                    isoff = true;
                    Db.UpdatePCLock(1);
                }
                else if (args.Reason == SessionSwitchReason.SessionUnlock)
                {
                    Mylog("Экран разблокирован.");
                    isoff = false;
                    Db.UpdatePCLock(0);
                    //checkdb();
                    //Авторизация
                }
            };
        }

        public static int getid()
        {
            try
            {
                var MyIni = new MyProg.IniFile(@"C:\Windows\secur\0\settings.ini");
                var Lapnum = MyIni.Read("numb", "Check");
                Mylog(Lapnum);
                return Int32.Parse(Lapnum);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Произошла ошибка: {ex.Message}");
            }
            return 0;
        }

        public static void Mylog(string LogsText)
        {
            System.IO.File.AppendAllText("C:\\Windows\\secur\\logs.txt", $"\n[{DateTime.Now}] {LogsText}");
            Console.WriteLine($"\n[{DateTime.Now}] {LogsText}");
        }


    }
}
