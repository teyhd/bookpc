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
        
        //public Boolean isoff = false;
        Form1 myForm = new Form1();
        public static Form globalForm;
        public void checkdb()
        {
            if (LastSecur.Db.Isauth() == 0)
            {
                try
                {
                    Console.WriteLine("В БД нет авторизации");
                   // Console.WriteLine(Program.first);
                    Application.Run(new Form1());
                    Program.Mylog("В БД нет авторизации");
                }
                catch (Exception ex)
                {
                    //myForm.Show();
                    Console.WriteLine("Ошибка");
                    Console.WriteLine(ex.ToString());
                    Program.Mylog($"Ошибка {ex.ToString()}");
                }

            }
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
           while (!stoppingToken.IsCancellationRequested)
            {
                // Console.WriteLine(LastSecur.Db.Isauth().ToString());
                Console.WriteLine(Program.isoff);
                Console.WriteLine(LastSecur.Program.AdminMode);
                if (!Program.isoff && !LastSecur.Program.AdminMode)
                {
                    checkdb();
                } 
                Console.WriteLine("Фоновая задача выполняется...");
                Program.Mylog("Фоновая задача выполняется...");
                // Задержка между выполнениями задачи
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
