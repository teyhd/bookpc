using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using Microsoft.Win32;
namespace Check
{
    class back
    {
        public class MyBackgroundService : BackgroundService
        {
            private static int timestart = (int)(long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            [Obsolete]
            protected override async Task ExecuteAsync(CancellationToken stoppingToken)
            {
                var MyIni = new MyProg.IniFile(@"C:\Windows\secur\0\settings.ini");
                //MyIni.Write("numb", "11");
                //MyIni.Write("check", "true");
                // MyIni.Write("checklog", "true");

                AddToStartup("Check.exe", GetApplicationPath());
                
                while (!stoppingToken.IsCancellationRequested)
                {
                    var Lapnum = MyIni.Read("numb");
                    var Check = Db.GetCheck();
                    
                    string processName = "LastSecur";
                    if (Check == 0)
                    {
                        Program.Mylog("Check");
                        int timenow = (int)(long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                        int gof = timenow - timestart;
                        Console.WriteLine(gof);
                        if (timenow - timestart >= 10)
                        {
                            Console.WriteLine($"ЗАмена {timestart}");
                            timestart = timenow;
                            Console.WriteLine($"ЗАмена {timestart}");
                            Db.UpdatePC();
                            Db.CheckHost();
                        }
                        if (IsProcessRunning(processName))
                        {
                           
                            Program.Mylog($"Процесс {processName} уже запущен.");
                        }
                        else
                        {
                            Program.Mylog($"Процесс {processName} не запущен. Запускаем...");
                            string processPath = "C:\\Windows\\secur\\LastSecur.exe";
                            StartProcess(processPath);
                        }
                    }
                    // Задержка между выполнениями задачи
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                }
            }
        }

        static string GetApplicationPath()
        {
            // Получение сборки текущего приложения
            var assembly = System.Reflection.Assembly.GetEntryAssembly();

            // Получение пути к исполняемому файлу сборки
            string appPath = assembly.Location;

            // Получение каталога, в котором находится исполняемый файл
            string appDirectory = System.IO.Path.GetDirectoryName(appPath);

            return appDirectory;
        }

        static void AddToStartup(string appName, string appPath)
        {
            try
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                key.SetValue(appName, appPath);
                key.Close();
            }
            catch (Exception ex)
            {
                Program.Mylog($"Ошибка при добавлении в автозагрузку: {ex.Message}");
            }
        }

        static bool IsProcessRunning(string processName)
        {
            Process[] processes = Process.GetProcessesByName(processName);
            return processes.Length > 0;
        }

        static void StartProcess(string processPath)
        {
            try
            {
                Process.Start(processPath);
                Program.Mylog("Процесс успешно запущен.");
            }
            catch (Exception ex)
            {
                Program.Mylog($"Ошибка при запуске процесса: {ex.Message}");
            }
        }
    }
}
