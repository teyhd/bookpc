using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Management;
using System.ServiceProcess;
using System.Diagnostics.Eventing.Reader;

namespace Security
{
    static class Program
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        /// 
        public static int sec = 60;
        private static Service1 MService;

        [STAThread]
        static async Task Main()
        {
            MService = new Service1();
           // MService.StartService();
            var builder = new HostBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<MyBackgroundService>();
                });
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            // Application.Run(new Form1());

            // Service1.Run();
            // Security.Service1.
            //Security.Service1.



            await builder.RunConsoleAsync();
        }

        public static void LockWorkStation()
        {
            Process.Start("rundll32.exe", "user32.dll,LockWorkStation");
        }

        public static EventLogEntry GetLastLogonEvent(EventLog securityLog)
        {
            EventLogEntry lastLogonEvent = null;

            // Проход по всем записям в журнале
            foreach (EventLogEntry entry in securityLog.Entries)
            {
                // Проверка события входа в систему (EventID=4624)
                if (entry.EventID == 4624)
                {
                    // Если это первое событие входа в систему или более позднее по времени, запоминаем его
                    if (lastLogonEvent == null || entry.TimeGenerated > lastLogonEvent.TimeGenerated)
                    {
                        lastLogonEvent = entry;
                    }
                }
            }

            return lastLogonEvent;
        }

        static string GetUsernameFromLogonEvent(EventLogEntry logonEvent)
        {
            // В данном примере, мы предполагаем, что имя пользователя содержится в поле "Message" события
            // В реальном приложении, вам может потребоваться использовать регулярные выражения или другие методы обработки строки
            return logonEvent?.Message ?? "Не удалось определить имя пользователя.";
        }


    }
}
