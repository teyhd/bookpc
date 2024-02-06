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

namespace Check
{
    class back
    {
        public class MyBackgroundService : BackgroundService
        {
            private static int timestart = (int)(long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            [Obsolete]
            protected override async System.Threading.Tasks.Task ExecuteAsync(CancellationToken stoppingToken)
            {
                var MyIni = new MyProg.IniFile(@"C:\Windows\secur\0\settings.ini");
                //MyIni.Write("numb", "11");
                //MyIni.Write("check", "true");
                // MyIni.Write("checklog", "true");

                AddToStartup("Check.exe", GetApplicationPath());
                
                while (!stoppingToken.IsCancellationRequested)
                {
                    var Lapnum = Int32.Parse(MyIni.Read("numb"));
                    var Check = Db.GetCheck();
                    
                    string processName = "LastSecur";
                    if (Check == 0)
                    {
                        
                        int timenow = (int)(long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                        int gof = timenow - timestart;
                        Console.WriteLine(gof);
                        if (timenow - timestart >= 10)
                        {
                            Program.Mylog("Check");
                            Program.Mylog(gof.ToString());
                            Console.WriteLine($"ЗАмена {timestart}");
                            timestart = timenow;
                            Console.WriteLine($"ЗАмена {timestart}");
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
                if (taskService.GetTask(taskName)!=null)
                {
                    TaskDefinition taskDefinition = taskService.NewTask();
                    taskDefinition.RegistrationInfo.Description = taskDescription;
                    taskDefinition.Triggers.Add(new LogonTrigger());
                    taskDefinition.Actions.Add(new ExecAction(taskExecutablePath));
                    taskService.RootFolder.RegisterTaskDefinition(taskName, taskDefinition);
                    Program.Mylog("Задача успешно добавлена в планировщик задач.");
                }
                else
                {
                    Program.Mylog("Задача с таким именем уже существует в планировщике задач.");
                }
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
