using System;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Touch_Panel.View_Model;

namespace Touch_Panel
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MainViewModel mainViewModel = new MainViewModel();

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = mainViewModel;

            var ver = Assembly.GetExecutingAssembly().GetName().Version;
            this.Title = $"Touch Sensing Tester  v{ver.Major}.{ver.Minor}";
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            mainViewModel.Dispose();
        }

        private void AboutBtn_Click(object sender, RoutedEventArgs e)
        {
            var panel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(20)
            };

            // Logo TNG màu TRẮNG, nền trong: tô trắng khối + dùng logo làm mặt nạ.
            FrameworkElement logo;
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri("pack://application:,,,/CompanyLogo/TNG%20Logo.png", UriKind.Absolute);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                logo = new System.Windows.Shapes.Rectangle
                {
                    Width = 230,
                    Height = 108,
                    Margin = new Thickness(0, 0, 0, 14),
                    Fill = Brushes.White,
                    OpacityMask = new ImageBrush(bmp) { Stretch = Stretch.Uniform }
                };
            }
            catch
            {
                logo = new Image { Width = 230, Margin = new Thickness(0, 0, 0, 14) };
            }
            panel.Children.Add(logo);

            panel.Children.Add(new TextBlock
            {
                Text = "T.N.G TECH Co., Ltd",
                FontWeight = FontWeights.Bold,
                FontSize = 16,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            });
            var ver = Assembly.GetExecutingAssembly().GetName().Version;
            panel.Children.Add(new TextBlock
            {
                Text = $"Touch Sensing Tester  v{ver.Major}.{ver.Minor}",
                FontSize = 12,
                Foreground = Brushes.LightGray,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 4, 0, 0)
            });

            var win = new Window
            {
                Title = "About",
                Width = 480,
                Height = 300,
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                Background = (Brush)FindResource("BaseBrush"),
                Content = panel
            };

            // Tự đóng sau 5s (vẫn đóng sớm được bằng nút X).
            var autoClose = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            autoClose.Tick += (s, ev) => { autoClose.Stop(); win.Close(); };
            win.Loaded += (s, ev) => autoClose.Start();
            win.Closed += (s, ev) => autoClose.Stop();

            win.ShowDialog();
        }
    }
}