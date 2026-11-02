using System;
using System.Threading;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using Microsoft.Win32;
using Microsoft.Win32.TaskScheduler;
using System.Runtime.InteropServices;
using System.ComponentModel;
using BenchmarkDotNet.Extensions;

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
                // SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, path, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
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
                    Program.Mylog(Lapnum.ToString());
                    Console.WriteLine(Lapnum.ToString());
                    int Check = 2;

                    int timenow = (int)(long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                    if (timenow - timestart >= 2)
                    {
                        Check = Db.GetCheckDB();
                        Program.Mylog("Check");
                    }
                    if (timenow - timestart >= 6)
                    {
                        Console.WriteLine($"Замена {timestart}");
                        timestart = timenow;
                        Console.WriteLine($"На {timestart}");
                        Db.CheckHost();
                        AddTask();
                        AddRebootTask();
                        AddMyTask();
                        Program.Mylog("Проверка:");
                        Program.Mylog(Check.ToString());
                        // SetWallpaper(wallpaperPath);
                    }

                    if (Check == 0) //Запустить все
                    {
                        StartProg("1\\PlatonAlarm");
                        StartProg("LastSecur");
                    }
                    if (Check == 1) //Запустить звонок
                    {
                        StartProg("1\\PlatonAlarm");
                    }
                    if (Check == 2) //Ничего не запускать
                    {
                        //
                    }
                    // Задержка между выполнениями задачи
                    await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
                }
            }
        }

        static public Boolean deletetask = false;
        static public Boolean deletetask1 = false;
        static public Boolean deletetask3 = false;

        static void AddRebootTask()
        {
            string taskName = "Перезагрузка";
            string taskDescription = "Проверка обновлений системы";
            try
            {
                using (TaskService taskService = new TaskService())
                {
                    // Проверяем, существует ли задача с таким именем
                    if (taskService.GetTask(taskName) != null)
                    {
                        if (!deletetask1)
                        {
                            taskService.RootFolder.DeleteTask(taskName);
                            Program.Mylog("Удалил задачу из планировщика.");
                            deletetask1 = true;
                        }

                    }
                    else
                    {
                        TaskDefinition taskDefinition = taskService.NewTask();
                        taskDefinition.RegistrationInfo.Description = taskDescription;
                        taskDefinition.Triggers.Add(new DailyTrigger { StartBoundary = DateTime.Today.AddHours(21).AddMinutes(10) });
                        taskDefinition.Settings.DisallowStartIfOnBatteries = false;
                        taskDefinition.Settings.StopIfGoingOnBatteries = false;
                        taskDefinition.Settings.WakeToRun = true;
                        taskDefinition.Settings.DeleteExpiredTaskAfter = TimeSpan.Zero;
                        taskDefinition.Settings.ExecutionTimeLimit = TimeSpan.Zero;
                        taskDefinition.Actions.Add(new ExecAction("shutdown", "-s -f -t 30"));
                        taskDefinition.Settings.AllowDemandStart = true;
                        taskDefinition.Principal.RunLevel = TaskRunLevel.Highest;
                        taskDefinition.Principal.UserId = ""; // Пустое значение позволяет использовать текущего пользователя
                        //taskDefinition.Principal.UserId = System.Security.Principal.WindowsIdentity.GetCurrent().Name;

                       /// taskDefinition.Principal.LogonType = TaskLogonType.InteractiveToken;
                        taskDefinition.Settings.MultipleInstances = TaskInstancesPolicy.Parallel;
                        //taskService.RootFolder.RegisterTaskDefinition(taskName, taskDefinition, TaskCreation.CreateOrUpdate, "NT AUTHORITY\\NETWORKSERVICE", null, TaskLogonType.ServiceAccount);
                        taskService.RootFolder.RegisterTaskDefinition(taskName, taskDefinition);

                        Program.Mylog("Задача успешно добавлена в планировщик.");

                    }



                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Program.Mylog(ex.ToString());
            }

            // Создаем экземпляр планировщика задач

        }
        
        public static void AddMyTask(int sinterv = 60)
        {
            string taskName = "Скрипт";
           // string taskDescription = "Проверка обновлений системы";
            try
            {
                using (TaskService taskService = new TaskService())
                {
                    // Проверяем, существует ли задача с таким именем
                    if (taskService.GetTask(taskName) != null)
                    {
                        if (!deletetask3)
                        {
                            taskService.RootFolder.DeleteTask(taskName);
                            Program.Mylog("Удалил задачу из планировщика.");
                            deletetask3 = true;
                        }

                    }
                    else
                    {
                     /*   TaskDefinition taskDefinition = taskService.NewTask();
                        taskDefinition.RegistrationInfo.Description = taskDescription;

                        TimeTrigger minuteTrigger = new TimeTrigger();
                        minuteTrigger.StartBoundary = DateTime.Parse("08:00:00");
                        minuteTrigger.Repetition.Interval = TimeSpan.FromMinutes(sinterv);
                       // minuteTrigger.StartBoundary = DateTime.Now;
                        // minuteTrigger.DaysInterval = 1; 
                        taskDefinition.Triggers.Add(minuteTrigger);
                        taskDefinition.Triggers.Add(new LogonTrigger());
                        taskDefinition.Triggers.Add(new BootTrigger());
                        taskDefinition.Settings.DisallowStartIfOnBatteries = false;
                        taskDefinition.Settings.StopIfGoingOnBatteries = false;
                        taskDefinition.Settings.WakeToRun = true;
                        taskDefinition.Settings.StartWhenAvailable = true;
                        taskDefinition.Settings.DeleteExpiredTaskAfter = TimeSpan.Zero;
                        taskDefinition.Settings.ExecutionTimeLimit = TimeSpan.Zero;
                        taskDefinition.Actions.Add(new ExecAction("cmd.exe", "/c start /min \"\" powershell.exe -WindowStyle Hidden -ExecutionPolicy Bypass -File \"\\\\fs.local\\info\\scripts\\start.ps1\""));

                        // taskDefinition.Actions.Add(new ExecAction("powershell.exe", "-WindowStyle Hidden -ExecutionPolicy Bypass -File \"\\\\fs.local\\info\\scripts\\start.ps1\""));
                        //taskDefinition.Actions.Add(new ExecAction("schtasks.exe", "/create /tn \"MyTask\" /tr \"powershell.exe -ExecutionPolicy Bypass -File \"\\\\fs.local\\info\\scripts\\start.ps1\"\" /sc once /st 00:00 /f"));

                        taskDefinition.Settings.AllowDemandStart = true;
                        taskDefinition.Principal.RunLevel = TaskRunLevel.Highest;
                        taskDefinition.Principal.UserId = ""; // Пустое значение позволяет использовать текущего пользователя
                                                              //taskDefinition.Principal.UserId = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
                                                              // taskDefinition.Principal.UserId = $"{Environment.MachineName}\\{Environment.UserName}";

                        taskDefinition.Principal.LogonType = TaskLogonType.InteractiveToken;
                        taskDefinition.Settings.MultipleInstances = TaskInstancesPolicy.Parallel;
                        //taskService.RootFolder.RegisterTaskDefinition(taskName, taskDefinition, TaskCreation.CreateOrUpdate, "NT AUTHORITY\\NETWORKSERVICE", null, TaskLogonType.ServiceAccount);
                        taskService.RootFolder.RegisterTaskDefinition(taskName, taskDefinition);

                        Program.Mylog("Задача успешно добавлена в планировщик."); */

                    }



                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Program.Mylog(ex.ToString());
            }

            // Создаем экземпляр планировщика задач

        }

        static void AddTask()
        {
            string taskName = "Проверка";
            string taskDescription = "Проверка обновлений системы";
            try
            {
                using (TaskService taskService = new TaskService())
                {
                    // Проверяем, существует ли задача с таким именем
                    if (taskService.GetTask(taskName) != null)
                    {
                        if (!deletetask)
                        {
                            //taskService.RootFolder.DeleteTask(taskName);
                            Program.Mylog("Удалил задачу из планировщика.");
                            deletetask = true;
                        }

                    }
                    else
                    {
                        TaskDefinition taskDefinition = taskService.NewTask();
                        taskDefinition.RegistrationInfo.Description = taskDescription;

                        TimeTrigger minuteTrigger = new TimeTrigger();
                        minuteTrigger.Repetition.Interval = TimeSpan.FromMinutes(1);
                        minuteTrigger.StartBoundary = DateTime.Now;
                       // minuteTrigger.DaysInterval = 1; 
                        taskDefinition.Triggers.Add(minuteTrigger);

                        taskDefinition.Triggers.Add(new LogonTrigger());
                        taskDefinition.Triggers.Add(new BootTrigger());
                        taskDefinition.Settings.DisallowStartIfOnBatteries = false;
                        taskDefinition.Settings.StopIfGoingOnBatteries = false;
                        taskDefinition.Settings.WakeToRun = true;
                        taskDefinition.Settings.StartWhenAvailable = true;

                        taskDefinition.Settings.DeleteExpiredTaskAfter = TimeSpan.Zero;
                        taskDefinition.Settings.ExecutionTimeLimit = TimeSpan.Zero;
                        taskDefinition.Actions.Add(new ExecAction(@"C:\Windows\secur\Release\Check.exe"));
                        taskDefinition.Actions.Add(new ExecAction(@"C:\Windows\secur\1\PlatonAlarm.exe"));
                        taskDefinition.Settings.AllowDemandStart = true;
                        taskDefinition.Principal.RunLevel = TaskRunLevel.Highest;
                        taskDefinition.Principal.UserId = ""; // Пустое значение позволяет использовать текущего пользователя
                        //taskDefinition.Principal.UserId = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
                       // taskDefinition.Principal.UserId = $"{Environment.MachineName}\\{Environment.UserName}";

                        taskDefinition.Principal.LogonType = TaskLogonType.InteractiveToken;
                        taskDefinition.Settings.MultipleInstances = TaskInstancesPolicy.Parallel;
                        //taskService.RootFolder.RegisterTaskDefinition(taskName, taskDefinition, TaskCreation.CreateOrUpdate, "NT AUTHORITY\\NETWORKSERVICE", null, TaskLogonType.ServiceAccount);
                        taskService.RootFolder.RegisterTaskDefinition(taskName, taskDefinition);

                        Program.Mylog("Задача успешно добавлена в планировщик.");

                    }



                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Program.Mylog(ex.ToString());
            }

            // Создаем экземпляр планировщика задач

        }
       
        static public void StartProg(string processName)
        {
            if (IsProcessRunning(processName))
            {
                Program.Mylog($"Процесс {processName} уже запущен.");
            }
            else
            {
                Program.Mylog($"Процесс {processName} не запущен. Запускаем...");
                string processPath = $"C:\\Windows\\secur\\{processName}.exe";
                if (processName == "PlatonAlarm")
                {
                    processPath = $"C:\\Windows\\secur\\1\\{processName}.exe";
                }
                StartProcess(processPath);
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
       // private readonly IProcessServices _processServices;

        static void StartProcess(string processPath)
        {
            try
            {
                Process proc = new Process();
                proc.StartInfo.FileName = processPath;
                proc.StartInfo.UseShellExecute = true;
                proc.StartInfo.Verb = "runas";
                //_processServices.StartProcessAsCurrentUser(processPath);
                ProcessServices.StartProcessAsCurrentUser(processPath);
                proc.Start();
                Process.Start(processPath);
                //  ProcessExtensions.StartProcessAsCurrentUser(processPath);

                Program.Mylog("Процесс успешно запущен.");
            }
            catch (Exception ex)
            {
                Program.Mylog($"Ошибка при запуске процесса: {ex.Message}");
                Db.RepairFolder();
            }
        }


    }
}

