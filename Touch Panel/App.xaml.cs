using System;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using Touch_Panel.View;

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

            // Hiện splash trước; sau ~2.5s tạo & hiện MainWindow rồi đóng splash.
            var splash = new SplashWindow();
            splash.Show();

            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.5) };
            timer.Tick += (s, ev) =>
            {
                timer.Stop();
                var main = new MainWindow();   // load model/COM diễn ra khi splash còn hiện
                MainWindow = main;
                main.Show();
                splash.Close();
            };
            timer.Start();
        }
    }

}
