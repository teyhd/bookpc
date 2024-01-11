using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using System.Management;
using System.Windows.Forms;

using System.ServiceProcess;
using System.Diagnostics.Eventing.Reader;
using System.Diagnostics;

namespace Security
{

    public class MyBackgroundService : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            while (!stoppingToken.IsCancellationRequested)
            {

                string securityLogName = "Security";
                
                // Поиск последнего события входа/выхода пользователя
                EventLog securityLog = new EventLog("Security", ".");
                EventLogEntry lastLogonEvent = Security.Program.GetLastLogonEvent(securityLog);

                if (lastLogonEvent != null)
                {
                    Console.WriteLine($"Время последнего входа в систему: {lastLogonEvent.TimeGenerated}");
                    //Console.WriteLine($"Имя пользователя: {GetUsernameFromLogonEvent(lastLogonEvent)}");
                }
                else
                {
                    Console.WriteLine("Событие входа в систему не найдено.");
                }
                //  Security.Program.sec = 50;
                // Ваш код фоновой задачи здесь
                Console.WriteLine("Фоновая задача выполняется...");
                //Security.Program.LockWorkStation();
                // Задержка между выполнениями задачи
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}

