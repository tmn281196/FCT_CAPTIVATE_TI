using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace Touch_Panel.Model
{
    /// <summary>
    /// Log dùng chung toàn app (singleton), tách 2 kênh theo bên: Log1 (T1) / Log2 (T2).
    /// - AddLog(tester, msg): ghi cho đúng bên (tester = 1 hoặc 2).
    /// - AddLog(msg): sự kiện cấp trạm -> ghi vào cả 2 bên.
    /// </summary>
    public class Logger : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly ObservableCollection<string> log1 = new ObservableCollection<string>();
        private readonly ObservableCollection<string> log2 = new ObservableCollection<string>();

        private static readonly Logger _instance = new Logger();
        public static Logger Instance => _instance;

        public ObservableCollection<string> Log1 => log1;
        public ObservableCollection<string> Log2 => log2;

        /// <summary>Ghi log cho 1 bên (tester = 1 hoặc 2).</summary>
        public void AddLog(int tester, string message)
        {
            var target = tester == 2 ? log2 : log1;
            string line = $"[{DateTime.Now:HH:mm:ss}] {message}";
            // BeginInvoke (không chặn) vì AddLog bị gọi liên tục từ thread nền test.
            Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (target.Count >= 500) target.Clear();
                target.Add(line);
            }));
        }

        /// <summary>Ghi log cấp trạm (không thuộc bên nào) -> hiện ở cả 2 bên.</summary>
        public void AddLog(string message)
        {
            AddLog(1, message);
            AddLog(2, message);
        }
    }
}
