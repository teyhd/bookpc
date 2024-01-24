using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Check
{
    static class Program
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        [STAThread]
        static async Task Main()
        {
            var builder = new HostBuilder()
                 .ConfigureServices((hostContext, services) =>
                 {
                     services.AddHostedService<back.MyBackgroundService>();
                 });

            await builder.RunConsoleAsync();
        }
    }
}
