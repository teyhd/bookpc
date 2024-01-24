using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;
using Microsoft.Win32;

namespace LastSecur
{
    static class Program
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
       
        static public Boolean AdminMode = false;
        static public Boolean first = true;
        static public Boolean isoff = false;
        [STAThread]
        static async Task Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var builder = new HostBuilder()
            .ConfigureServices((hostContext, services) =>
            {
                services.AddHostedService<MyBackgroundService>();
            });
            try
            {
                Events();
                await builder.RunConsoleAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Application.Run(new Form1());
                throw;
            }
            
        }

        public static void Events()
        {
            SystemEvents.SessionSwitch += (sender, args) =>
            {
                if (args.Reason == SessionSwitchReason.SessionLock)
                {
                    Console.WriteLine("Экран блокирован.");
                    isoff = true;
                }
                else if (args.Reason == SessionSwitchReason.SessionUnlock)
                {
                    Console.WriteLine("Экран разблокирован.");
                    isoff = false;
                    //checkdb();
                    //Авторизация
                }
            };
        }

        public static int getid()
        {
            try
            {
                // Путь к вашему файлу
                string filePath = "C:\\Windows\\secur\\info.txt";

                // Проверка существования файла
                if (File.Exists(filePath))
                {
                    // Чтение содержимого файла
                    string content = File.ReadAllText(filePath);

                    // Вывод содержимого на консоль
                    Console.WriteLine("Содержимое файла:");
                    Console.WriteLine(content);
                    
                    return Int32.Parse(content);
                }
                else
                {
                    Console.WriteLine("Ошибка конфигурации");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Произошла ошибка: {ex.Message}");
            }
            return 0;
        }

        public static void Mylog(string LogsText)
        {
            System.IO.File.AppendAllText("C:\\Windows\\secur\\logs.txt", $"\n{LogsText}");
        }
    }
}
