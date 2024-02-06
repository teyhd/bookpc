using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using Microsoft.Win32;
using System.Windows.Forms;

namespace LastSecur
{
    public class MyBackgroundService : BackgroundService
    {
        Form1 myForm = new Form1();
        public static Form globalForm;
        public void checkdb()
        {
            Program.Mylog("Я работаю");
            if (LastSecur.Db.Isauth() == 0)
            {
                try
                {
                    Application.Run(new Form1());
                    Program.Mylog("В БД нет авторизации");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ошибка");
                    Program.Mylog($"Ошибка {ex.ToString()}");
                }
            } else
            {
                Program.Mylog("Я АВТОРИЗОВАН");
            }
        }

        static bool IsProcessRunning(string processName)
        {
            Process[] processes = Process.GetProcessesByName(processName);
            return processes.Length > 0;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Console.WriteLine(LastSecur.Db.Isauth().ToString());
               // Program.Mylog(Program.isoff.ToString());
               // Program.Mylog(LastSecur.Program.AdminMode.ToString());
               // if (!Program.isoff && !LastSecur.Program.AdminMode)
                if (!LastSecur.Program.AdminMode)
                {
                    checkdb();
                } 
                if (Program.first)
                {
                    Program.first = false;
                    Application.Run(new Form1());
                }
                Program.Mylog("Фоновая задача выполняется...");
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
        }
    }
}
