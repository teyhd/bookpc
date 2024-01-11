using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Management;

namespace Security
{
    public partial class Service1 : ServiceBase
    {
        private ManagementEventWatcher watcher;

        public Service1()
        {
            InitializeComponent();
        }

        public void StartService()
        {
            OnStart(null);
        }

        public void StopService()
        {
            OnStop();
        }

        protected override void OnStart(string[] args)
        {
            // Инициализация и запуск мониторинга
            WqlEventQuery query = new WqlEventQuery("SELECT * FROM Win32_LogonSession");
            watcher = new ManagementEventWatcher(query);
            watcher.EventArrived += OnLogonEventArrived;
            watcher.Start();
            Console.WriteLine("Пользователь!");
            Console.WriteLine("Пользователь!");
        }

        protected override void OnStop()
        {
            // TODO: Добавьте код, выполняющий подготовку к остановке службы.
        }

        private void OnLogonEventArrived(object sender, EventArrivedEventArgs e)
        {
            // Обработка события входа/выхода пользователя
            Console.WriteLine("Пользователь вошел в систему!");
        }

        internal static void Run()
        {
            throw new NotImplementedException();
        }
    }
}
