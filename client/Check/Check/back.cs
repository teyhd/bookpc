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
            [Obsolete]
            protected override async Task ExecuteAsync(CancellationToken stoppingToken)
            {
                var MyIni = new MyProg.IniFile(@"C:\Windows\secur\settings.ini");
                //MyIni.Write("numb", "11");
                //MyIni.Write("check", "true");
                // MyIni.Write("checklog", "true");

                var Checklog = MyIni.Read("checklog");
                AddToStartup("Check.exe", GetApplicationPath());
                while (!stoppingToken.IsCancellationRequested)
                {
                    var Lapnum = MyIni.Read("numb");
                    var Check = MyIni.Read("check");
                    // Ваш код фоновой задачи здесь
                    //Program.Mylog("Фоновая задача выполняется...");
                    Db.CheckHost();
                    string processName = "LastSecur";
                    if (Check == "true")
                    {
                        Program.Mylog("Check");
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
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
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
