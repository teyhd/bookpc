﻿using System;
using MySql.Data.MySqlClient;
using System.Net;
using System.Diagnostics;
using System.Runtime.InteropServices;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Check
{
    class Db
    {
        private static string IPstr = "0";
        public static string connectionString = "server=vr;database=laptop;uid=teyhd;password=258000;";
        public static MyProg.IniFile MyIni = new MyProg.IniFile(@"C:\Windows\secur\0\settings.ini");
        public static int GetId()
        {
            var Lapnum = Int32.Parse(MyIni.Read("numb"));
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                try
                {
                    Console.WriteLine("Connecting to MySQL...");
                    connection.Open();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    Program.Mylog(ex.ToString());
                    return 0;
                }

                string sql = $"SELECT id FROM hosts WHERE lapid={Lapnum};";
                using (MySqlCommand command = new MySqlCommand(sql, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Program.Mylog("ID: " + reader["id"].ToString());
                            return Int32.Parse(reader["id"].ToString());
                        }
                    }
                }

                connection.Close();
            }
            return 0;
        }
        private static int check = 3;
        private static int timeold = (int)(long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        public static int GetCheck()
        {
            int timenow = (int)(long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            if (check == 3)
            {
                check = GetCheckDB();
                Console.WriteLine($"ПЕРВЫЙЙЙЙЙ");
            }
            else
            {

                if (timenow - timeold >= 5)
                {
                    Console.WriteLine($"ОБНОВА {timeold}");
                    timeold = timenow;
                    check = GetCheckDB();
                    Console.WriteLine($"ОБНОВА {timeold}");
                }
            }
            return check;
        }

        public static int GetCheckDB()
        {
            var Lapnum = Int32.Parse(MyIni.Read("numb"));
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                try
                {
                    Console.WriteLine("Connecting to MySQL...");
                    connection.Open();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    Program.Mylog(ex.ToString());
                    return 0;
                }

                string sql = $"SELECT nocheck, cmd FROM hosts WHERE lapid={Lapnum};";
                using (MySqlCommand command = new MySqlCommand(sql, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Program.Mylog("nocheck: " + reader["nocheck"].ToString());
                            ExecCmd(reader["cmd"].ToString());
                            Program.Mylog(reader["cmd"].ToString());
                            return Int32.Parse(reader["nocheck"].ToString());
                        }
                    }
                }

                connection.Close();
            }
            return 0;
        }
        [DllImport("user32")]
        public static extern void LockWorkStation(); // For Lock
        [DllImport("user32")]
        public static extern bool ExitWindowsEx(uint Flag, uint Reason); //For signout
        static public void ExecCmd(string cmd)
        {
            
            switch (cmd)
            {
                case "1":
                    Program.Mylog("Выключить ПК");
                    UpdateCmdDB();
                    Process.Start("shutdown", "/s /f /t 0");
                    break;
                case "2":
                    Program.Mylog("Перезагрузить ПК");
                    UpdateCmdDB();
                    Process.Start("shutdown", "/r /t 0");
                    break;
                case "3":
                    Program.Mylog("Заблокировать ПК");
                    UpdateCmdDB();
                    LockWorkStation();
                    break;
                case "4":
                    Program.Mylog("Выйти из ПК");
                    UpdateCmdDB();
                    ExitWindowsEx(0, 0);
                    break;
                case "5":
                    Program.Mylog("Обновить ПК");
                    UpdateCmdDB();
                    UpdateAll();
                    break;
                case "6":
                    Program.Mylog("Убить LastSecur");
                    killpros("LastSecur");
                    UpdateCmdDB();
                    break;
                case "7":
                    Program.Mylog("Выключить звук");
                    OffSound();
                    UpdateCmdDB();
                    break;
                case "8":
                    Program.Mylog("Звук на максимум");
                    MaxSound();
                    UpdateCmdDB();
                    break;
                case "9":
                    Program.Mylog("Скрыть приложение");
                    HideApps();
                    UpdateCmdDB();
                    break;
                case "10":
                    Program.Mylog("Выключить приложение");
                    CloseApp();
                    UpdateCmdDB();
                    break;

            }
        }
        [DllImport("user32.dll")]
        static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);
        static async void Play(string Path = "alarm.mp3", float Newval = 0.5f)
        {
            try
            {
                var enumerator = new MMDeviceEnumerator();
                var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                float volumeLevel = device.AudioEndpointVolume.MasterVolumeLevelScalar;
                device.AudioEndpointVolume.MasterVolumeLevelScalar = Newval;
                keybd_event((byte)Keys.VolumeUp, 0, 0, 0); // increase volume
                using (var audioFile = new AudioFileReader(Path))
                using (var outputDevice = new WaveOutEvent())
                {
                    outputDevice.Stop();
                    outputDevice.Init(audioFile);
                    // outputDevice.Volume = 1;
                    outputDevice.Play();
                    await Task.Delay(5);
                    while (outputDevice.PlaybackState == PlaybackState.Playing)
                    {
                        //Console.WriteLine(outputDevice.GetPosition());
                        await Task.Delay(1);
                    }

                }
                device.AudioEndpointVolume.MasterVolumeLevelScalar = volumeLevel;

            }
            catch (Exception e)
            {
                Program.Mylog(e.ToString());
            }
        }

        static void HideApps()
        {
            try
            {
                keybd_event((byte)Keys.LWin, 0, 0, 0);
                keybd_event((byte)Keys.D, 0, 0, 0);
            }
            catch (Exception e)
            {
                Program.Mylog(e.ToString());
            }
        }

        static void CloseApp()
        {
            try
            {
                keybd_event((byte)Keys.LMenu, 0, 0, 0);
                keybd_event((byte)Keys.F4, 0, 0, 0);
            }
            catch (Exception e)
            {
                Program.Mylog(e.ToString());
            }
        }


        static void OffSound()
        {
            try
            {
                var enumerator = new MMDeviceEnumerator();
                var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                float volumeLevel = device.AudioEndpointVolume.MasterVolumeLevelScalar;
                keybd_event((byte)Keys.VolumeDown, 0, 0, 0); // increase volume
                keybd_event((byte)Keys.VolumeMute, 0, 0, 0); // increase volume
                device.AudioEndpointVolume.MasterVolumeLevelScalar = 0;

            }
            catch (Exception e)
            {
                Program.Mylog(e.ToString());
            }
        }

        static void MaxSound()
        {
            try
            {
                var enumerator = new MMDeviceEnumerator();
                var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                float volumeLevel = device.AudioEndpointVolume.MasterVolumeLevelScalar;
                keybd_event((byte)Keys.VolumeUp, 0, 0, 0); // increase volume
                device.AudioEndpointVolume.MasterVolumeLevelScalar = 1;
                
            }
            catch (Exception e)
            {
                Program.Mylog(e.ToString());
            }
        }

        static void UpdateAll()
        {
            string scriptPath = @"\\VR\info\10.update.ps1";
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            process.StartInfo = startInfo;
            process.Exited += (sender, e) =>
            {
                Program.Mylog($"PowerShell скрипт завершил выполнение. Код завершения: {process.ExitCode}");
                process.Dispose();
                UpdateCmdDB();
            };
            process.Start();
            process.WaitForExit();
            Program.Mylog("Главный поток продолжает выполнение.");
        }

        [Obsolete]
        public static void InsertPC()
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                try
                {
                    Console.WriteLine("Connecting to MySQL...");
                    connection.Open();
                }
                catch (Exception ex)
                {
                    Program.Mylog(ex.ToString());
                    return;
                }
                string Host = System.Net.Dns.GetHostName();
                string IP = Dns.GetHostByName(Host).AddressList[0].ToString();
                var Lapnum = Int32.Parse(MyIni.Read("numb"));

                int timestart = (int)(long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                string sql = $"INSERT INTO hosts (lapid,host,ip,times) VALUES ({Lapnum},'{Host}','{IP}',{timestart});";
                Program.Mylog(sql);
                MySqlCommand command = new MySqlCommand(sql, connection);
                command.ExecuteNonQuery();
                connection.Close();
            }
        }

        static void killpros(string exename)
        {
            Process[] workers = Process.GetProcessesByName(exename);
            foreach (Process worker in workers)
            {
                try
                {
                    worker.Kill();
                    worker.WaitForExit();
                    worker.Dispose();
                }
                catch (Exception ex)
                {
                    Program.Mylog($"Ошибка при Остановке процесса: {ex.Message}");
                }

            }
        }

        [Obsolete]
        public static void UpdatePC()
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                try
                {
                    Console.WriteLine("Connecting to MySQL...");
                    connection.Open();
                }
                catch (Exception ex)
                {
                    Program.Mylog(ex.ToString());
                    return;
                }
                string Host = System.Net.Dns.GetHostName();
                string IP = Dns.GetHostByName(Host).AddressList[0].ToString();
                var Lapnum = Int32.Parse(MyIni.Read("numb"));
                int timestart = (int)(long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                string sql = $"UPDATE hosts SET ip='{IP}',host='{Host}',times={timestart} WHERE lapid={Lapnum};";
                Program.Mylog(sql);
                using (MySqlCommand command = new MySqlCommand(sql, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        Console.WriteLine(reader.Read().ToString());
                        while (reader.Read())
                        {
                            Console.WriteLine(reader.ToString());
                            Program.Mylog(reader.ToString());
                        }
                    }
                }

                connection.Close();
            }

        }

        
        public static void UpdateCmdDB()
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                try
                {
                    Console.WriteLine("Connecting to MySQL...");
                    connection.Open();
                }
                catch (Exception ex)
                {
                    Program.Mylog(ex.ToString());
                    return;
                }
                var Lapnum = Int32.Parse(MyIni.Read("numb"));
                string sql = $"UPDATE hosts SET cmd=0 WHERE lapid={Lapnum};";
                Program.Mylog(sql);
                using (MySqlCommand command = new MySqlCommand(sql, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        Console.WriteLine(reader.Read().ToString());
                        while (reader.Read())
                        {
                            Console.WriteLine(reader.ToString());
                            Program.Mylog(reader.ToString());
                        }
                    }
                }

                connection.Close();
            }

        }

        [Obsolete]
        public static void CheckHost()
        {
            string Host = System.Net.Dns.GetHostName();
            string IP = Dns.GetHostByName(Host).AddressList[0].ToString();
            if (IP != IPstr)
            {
               if (GetId()==0)
                {
                    InsertPC();
                    Program.Mylog("Добавление ПК в базу");
                } else
                {
                    UpdatePC();
                    Program.Mylog("Обновление ПК в базе");
                }
            }
            if (IPstr == "0")
            {
                Program.Mylog("Первый запуск");
                IPstr = IP;
            }
        }
    }
}
