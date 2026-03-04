using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Windows;

namespace Touch_Panel
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            string currentProcessName = Process.GetCurrentProcess().ProcessName;
            var processes = Process.GetProcessesByName(currentProcessName);

            if (processes.Length > 1)
            {
                MessageBox.Show("Application is already running.");
                Shutdown();
                return;
            }

     
        }
    }

}
