using System;
using System.Threading;
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
        
        static public Boolean AdminMode = false;
        static public Boolean first = true;
        static public Boolean isoff = false;
        static System.Threading.Mutex singleton = new Mutex(true, "LastSecur");

        [STAThread]
        static async Task Main()
        {
            if (!singleton.WaitOne(TimeSpan.Zero, true))
            {
                Mylog("Попытка запуска второго экземпляра");
                Application.Exit();
                return;
            } else
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
                    Mylog(e.ToString());
                    Application.Run(new Form1());
                    throw;
                }
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
                var Check = MyIni.Read("numb", "Check");
                Mylog(Check);
                return Int32.Parse(Check);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Произошла ошибка: {ex.Message}");
            }
            return 0;
        }

        public static void Mylog(string LogsText)
        {
            try
            {
                System.IO.File.AppendAllText("C:\\Windows\\secur\\logs.txt", $"\n(Sec)[{DateTime.Now}] {LogsText}");
                Console.WriteLine($"\n[{DateTime.Now}] {LogsText}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Произошла ошибка: {ex.Message}");
            }
            
        }

    }
}
