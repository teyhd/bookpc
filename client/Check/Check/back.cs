using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using Microsoft.Win32;
using Microsoft.Win32.TaskScheduler;
using System.Runtime.InteropServices;

namespace Check
{
    class back
    {
        public class MyBackgroundService : BackgroundService
        {
            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

            const int SPI_SETDESKWALLPAPER = 20;
            const int SPIF_UPDATEINIFILE = 0x01;
            const int SPIF_SENDCHANGE = 0x02;
            static void SetWallpaper(string path)
            {
                // Вызываем функцию SystemParametersInfo, чтобы установить заставку
                SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, path, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
            }
            private static int timestart = (int)(long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            [Obsolete]
            protected override async System.Threading.Tasks.Task ExecuteAsync(CancellationToken stoppingToken)
            {
                var MyIni = new MyProg.IniFile(@"C:\Windows\secur\0\settings.ini");
                string wallpaperPath = @"C:\Windows\secur\wallpapersvet.png";
                //MyIni.Write("numb", "11");
                //MyIni.Write("check", "true");
                // MyIni.Write("checklog", "true");

                AddToStartup("Check.exe", GetApplicationPath());
                
                while (!stoppingToken.IsCancellationRequested)
                {
                    var Lapnum = Int32.Parse(MyIni.Read("numb"));
                    var Check = Db.GetCheck();
                    
                    string processName = "PlatonAlarm";
                    if (IsProcessRunning(processName))
                    {
                        Program.Mylog($"Процесс {processName} уже запущен.");
                    }
                    else
                    {
                        Program.Mylog($"Процесс {processName} не запущен. Запускаем...");
                        string processPath = "C:\\Windows\\secur\\1\\PlatonAlarm.exe";
                        StartProcess(processPath);
                        StartProcess(@"C:\Windows\secur\1\PlatonAlarm.exe");
                    }
                    processName = "LastSecur";
                    int timenow = (int)(long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                    if (timenow - timestart >= 5)
                    {
                        AddTask();
                        SetWallpaper(wallpaperPath);
                    }
                        
                    if (Check == 0)
                    {
                        timenow = (int)(long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                        int gof = timenow - timestart;
                        Console.WriteLine(gof);
                        if (timenow - timestart >= 5)
                        {
                            Program.Mylog("Check");
                            Program.Mylog(gof.ToString());
                            Console.WriteLine($"Замена {timestart}");
                            timestart = timenow;
                            Console.WriteLine($"Замена {timestart}");
                            Db.UpdatePC();
                            Db.CheckHost();
                            AddTask();
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
                            StartProcess(@"C:\Windows\secur\LastSecur.exe");
                        }
                    }
                    else
                    {
                        timenow = (int)(long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                        if (timenow - timestart >= 10)
                        {
                            timestart = timenow;
                            Console.WriteLine($"ЗАмена {timestart}");
                            Db.UpdatePC();
                        }
                    }
                    // Задержка между выполнениями задачи
                    await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                }
            }
        }
        

        static void AddTask()
        {
            string taskName = "Проверка";
            string taskDescription = "Проверка обновлений системы";
            string taskExecutablePath = @"C:\Windows\secur\Release\Check.exe";
            // Создаем экземпляр планировщика задач
            using (TaskService taskService = new TaskService())
            {
                // Проверяем, существует ли задача с таким именем
                // if (taskService.GetTask(taskName)!=null)
               
                    TaskDefinition taskDefinition = taskService.NewTask();
                    taskDefinition.RegistrationInfo.Description = taskDescription;

                    TimeTrigger minuteTrigger = new TimeTrigger();
                    minuteTrigger.Repetition.Interval = TimeSpan.FromMinutes(1);
                    minuteTrigger.StartBoundary = DateTime.Now;

                    taskDefinition.Triggers.Add(minuteTrigger);

                    taskDefinition.Triggers.Add(new LogonTrigger());
                    taskDefinition.Triggers.Add(new BootTrigger());
                    taskDefinition.Settings.DisallowStartIfOnBatteries = false;
                    taskDefinition.Settings.StopIfGoingOnBatteries = false;
                    taskDefinition.Settings.WakeToRun = true;
                    taskDefinition.Settings.DeleteExpiredTaskAfter = TimeSpan.Zero;
                    taskDefinition.Settings.ExecutionTimeLimit = TimeSpan.Zero;
                    taskDefinition.Actions.Add(new ExecAction(taskExecutablePath));
                    taskDefinition.Actions.Add(new ExecAction(@"C:\Windows\secur\1\PlatonAlarm.exe"));
                    taskService.RootFolder.RegisterTaskDefinition(taskName, taskDefinition);
                 
                    Program.Mylog("Задача успешно добавлена в планировщик задач.");
               
            }
        }

        static string GetApplicationPath()
        {
            var assembly = System.Reflection.Assembly.GetEntryAssembly();
            string appPath = assembly.Location;
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
