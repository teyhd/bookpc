using System;
//using MySql.Data.MySqlClient;
using System.Net;
using System.Diagnostics;
using System.Runtime.InteropServices;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySqlConnector;
namespace Check
{
    class Db
    {
        private static string IPstr = "0";
        public static string connectionString = "server=db.local;database=laptop;uid=teyhd;password=258000;";
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

        [StructLayout(LayoutKind.Sequential)]
        struct INPUT
        {
            public uint type;
            public InputUnion u;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct InputUnion
        {
            [FieldOffset(0)] public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        const int INPUT_KEYBOARD = 1;
        const ushort VK_MENU = 0x12; // ALT key
        const ushort VK_F4 = 0x73; // F4 key
        const uint KEYEVENTF_KEYUP = 0x0002;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("wtsapi32.dll", SetLastError = true)]
        private static extern bool WTSEnumerateSessions(
            IntPtr hServer,
            int Reserved,
            int Version,
            ref IntPtr ppSessionInfo,
            ref int pCount);

        [StructLayout(LayoutKind.Sequential)]
        private struct WTS_SESSION_INFO
        {
            public int SessionID;
            public string pWinStationName;
            public WTS_CONNECTSTATE_CLASS State;
        }

        private enum WTS_CONNECTSTATE_CLASS
        {
            WTSActive,
            WTSConnected,
            WTSConnectQuery,
            WTSShadow,
            WTSDisconnected,
            WTSIdle,
            WTSListen,
            WTSReset,
            WTSDown,
            WTSInit
        }

        [DllImport("wtsapi32.dll", SetLastError = true)]
        private static extern bool WTSQueryUserToken(uint sessionId, out IntPtr Token);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetProcessWindowStation(IntPtr hWinSta);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetThreadDesktop(IntPtr hDesktop);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetProcessWindowStation();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetThreadDesktop(uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr OpenInputDesktop(uint dwFlags, bool fInherit, uint dwDesiredAccess);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr OpenWindowStation(string lpszWinSta, bool fInherit, uint dwDesiredAccess);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr OpenDesktop(string lpszDesktop, uint dwFlags, bool fInherit, uint dwDesiredAccess);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SwitchDesktop(IntPtr hDesktop);

        [DllImport("user32.dll")]
        static extern bool PostMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);
        [DllImport("user32.dll")]
        static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32")]
        public static extern bool ExitWindowsEx(uint Flag, uint Reason); //For signout
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool LockWorkStation();

        [DllImport("user32.dll")]
        static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);
        const int SW_SHOWMINIMIZED = 2;
        const int SW_MINIMIZE = 6;

        static public void ExecCmd(string cmd)
        {

            switch (cmd)
            {
                case "1":
                    Program.Mylog("Выключить ПК");
                    UpdateCmdDB();
                    killpros("PlatonAlarm");
                    Process.Start("shutdown", "/s /f /t 0");
                    break;
                case "2":
                    Program.Mylog("Перезагрузить ПК");
                    UpdateCmdDB();
                    killpros("PlatonAlarm");
                    Process.Start("shutdown", "/r /t 0");
                    break;
                case "3":
                    Program.Mylog("Заблокировать ПК");
                    UpdateCmdDB();
                    qLockWorkStation();
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
                    back.deletetask3 = false;
                    back.AddMyTask(1);
                    //UpdateAll();
                    break;
                case "6":
                    Program.Mylog("Убить LastSecur");
                    back.deletetask3 = false;
                    back.AddMyTask(60);
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
                case "11":
                    Program.Mylog("Закрыть вкладку");
                    CloseTab();
                    UpdateCmdDB();
                    break;
                case "12":
                    Program.Mylog("Починить Права пользователя");
                    RepairFolder();
                    UpdateCmdDB();
                    break;
            }
        }


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

        const int DESKTOP_SWITCHDESKTOP = 0x0100;
        const int WINSTA_ALL_ACCESS = 0x37F;
        static void HideApps()
        {
            try
            {
                IntPtr hWnd = GetForegroundWindow();
                if (hWnd == IntPtr.Zero)
                {
                    Console.WriteLine("Не удалось найти активное окно.");
                    return;
                }

                // Отправить команду свернуть окно
                bool result = ShowWindow(hWnd, SW_MINIMIZE);
                if (result)
                {
                    Console.WriteLine("Окно свернуто.");
                }
                else
                {
                    Console.WriteLine("Не удалось свернуть окно.");
                }
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
                Program.Mylog("Выключить приложение");
                IntPtr hWnd = GetForegroundWindow();
                if (hWnd == IntPtr.Zero)
                {
                    Program.Mylog("Не удалось найти активное окно.");
                    return;
                }

                // Получить ID процесса активного окна
                GetWindowThreadProcessId(hWnd, out int processId);
                if (processId == 0)
                {
                    Program.Mylog("Не удалось получить ID процесса активного окна.");
                    return;
                }

                // Проверить, что процесс не является системным
                Process process = Process.GetProcessById(processId);
                if (process.SessionId == 0)
                {
                    Program.Mylog("Процесс принадлежит системной сессии.");
                    return;
                }
                process.Kill();
                process.WaitForExit();
                process.Dispose();
            }
            catch (Exception e)
            {
                Program.Mylog(e.ToString());
            }
        }

        static void CloseTab()
        {
            try
            {
                // Определяем виртуальные коды клавиш
                const byte VK_CONTROL = 0x11;
                const byte VK_W = 0x57;

                // Определяем флаги для keybd_event
                const uint KEYEVENTF_KEYDOWN = 0x0000;
                const uint KEYEVENTF_KEYUP = 0x0002;
                // Нажатие клавиши Ctrl
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYDOWN, 0);
                // Нажатие клавиши W
                keybd_event(VK_W, 0, KEYEVENTF_KEYDOWN, 0);

                // Отпускание клавиши W
                keybd_event(VK_W, 0, KEYEVENTF_KEYUP, 0);
                // Отпускание клавиши Ctrl
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, 0);

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
            killpros("LastSecur");
            killpros("PlatonAlarm");
            string scriptPath = @"\\fs.local\info\scripts\10.update.ps1";
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            process.StartInfo.Verb = "runas";
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

        static public void RepairFolder()
        {
            string scriptPath = @"\\fs.local\info\scripts\repair.ps1";
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
            process.StartInfo.Verb = "runas";
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
                IPAddress[] address = Dns.GetHostByName(Host).AddressList;
                string IP = "";
                for (int index = 0; index < address.Length; index++)
                {
                    IP += $"{address[index]} ";
                    Console.WriteLine(address[index]);
                }
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

           /* Process[] allworkers = Process.GetProcesses();
            foreach (Process worker in allworkers)
            {
                Console.WriteLine(worker.ProcessName);
            } */
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
                IPAddress[] address = Dns.GetHostByName(Host).AddressList;
                string IP = "";
                for (int index = 0; index < address.Length; index++)
                {
                    IP += $"{address[index]} ";
                    Console.WriteLine(address[index]);
                }
                
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
            IPAddress[] address = Dns.GetHostByName(Host).AddressList;
            string IP = "";
            for (int index = 0; index < address.Length; index++)
            {
                IP += $"{address[index]} ";
                Console.WriteLine(address[index]);
            }
            if (GetId() == 0)
            {
                InsertPC();
                Program.Mylog("Добавление ПК в базу");
            }
            else
            {
                UpdatePC();
                Program.Mylog("Обновление ПК в базе");
            }
            if (IP != IPstr)
            {
                Program.Mylog("Смена адреса");
                UpdatePC();
            }
            if (IPstr == "0")
            {
                Program.Mylog("Первый запуск");
                UpdatePC();
                IPstr = IP;
            }
        }


        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool DuplicateTokenEx(
            IntPtr hExistingToken,
            uint dwDesiredAccess,
            IntPtr lpTokenAttributes,
            int ImpersonationLevel,
            int TokenType,
            out IntPtr phNewToken);


        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool CreateProcessAsUser(
            IntPtr hToken,
            string lpApplicationName,
            string lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);



        [StructLayout(LayoutKind.Sequential)]
        private struct STARTUPINFO
        {
            public int cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public int dwX;
            public int dwY;
            public int dwXSize;
            public int dwYSize;
            public int dwXCountChars;
            public int dwYCountChars;
            public int dwFillAttribute;
            public int dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public uint dwProcessId;
            public uint dwThreadId;
        }

        private static void StartProcessAsUser(IntPtr token, string appPath, string cmdLine)
        {
            PROCESS_INFORMATION pi = new PROCESS_INFORMATION();
            STARTUPINFO si = new STARTUPINFO();
            si.cb = Marshal.SizeOf(si);
            si.lpDesktop = "winsta0\\default";

            bool result = CreateProcessAsUser(
                token,
                appPath,
                cmdLine,
                IntPtr.Zero,
                IntPtr.Zero,
                false,
                0,
                IntPtr.Zero,
                null,
                ref si,
                out pi);

            if (result)
            {
                Console.WriteLine("Процесс успешно запущен.");
                CloseHandle(pi.hProcess);
                CloseHandle(pi.hThread);
            }
            else
            {
                Console.WriteLine("Не удалось запустить процесс.");
            }
        }
        private static void qLockWorkStation()
        {
            try
            {
                // Выполнить необходимые действия для блокировки рабочей станции
                IntPtr tokenHandle = IntPtr.Zero;
                IntPtr ppSessionInfo = IntPtr.Zero;
                int sessionCount = 0;

                // Перечислить сессии и получить активную сессию пользователя
                if (WTSEnumerateSessions(IntPtr.Zero, 0, 1, ref ppSessionInfo, ref sessionCount))
                {
                    IntPtr currentSession = ppSessionInfo;
                    for (int i = 0; i < sessionCount; i++)
                    {
                        WTS_SESSION_INFO sessionInfo = (WTS_SESSION_INFO)Marshal.PtrToStructure(currentSession, typeof(WTS_SESSION_INFO));
                        currentSession = (IntPtr)((long)currentSession + Marshal.SizeOf(typeof(WTS_SESSION_INFO)));

                        if (sessionInfo.State == WTS_CONNECTSTATE_CLASS.WTSActive)
                        {
                            if (WTSQueryUserToken((uint)sessionInfo.SessionID, out tokenHandle))
                            {
                                StartProcessAsUser(tokenHandle, "cmd.exe", "/c LockWorkStation");
                                CloseHandle(tokenHandle);
                                Program.Mylog("Сессия пользователя найдена и токен получен.");
                                break;
                            }
                        }
                    }
                }


                else
                {
                    Program.Mylog("Не удалось получить токен сессии пользователя.");
                }
            }
            catch (Exception ex)
            {
                Program.Mylog(ex.ToString());
                return;
            }
        }
    }
}
