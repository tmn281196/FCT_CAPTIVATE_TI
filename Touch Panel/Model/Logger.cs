using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows;

namespace Touch_Panel.Model
{
    /// <summary>
    /// Log dùng chung toàn app (singleton), tách 2 kênh theo bên: Log1 (T1) / Log2 (T2).
    /// - AddLog(tester, msg): ghi cho đúng bên (tester = 1 hoặc 2).
    /// - AddLog(msg): sự kiện cấp trạm -> ghi vào cả 2 bên.
    /// Đồng thời tự ghi ra file trong thư mục exe: Logs/T{n}_yyMMdd.txt (append theo ngày).
    /// </summary>
    public class Logger : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly ObservableCollection<string> log1 = new ObservableCollection<string>();
        private readonly ObservableCollection<string> log2 = new ObservableCollection<string>();

        // Thư mục log = <thư mục exe>/Logs
        private static readonly string LogDir = Path.Combine(AppContext.BaseDirectory, "Logs");

        /// <summary>Đường dẫn thư mục log (để UI mở trong Explorer).</summary>
        public static string LogDirectory => LogDir;
        private static readonly object _fileLock = new object();

        // Ngày dùng để đặt tên file log. GHIM theo lúc TEST BẮT ĐẦU (gọi MarkTestStart()):
        // test bắt đầu hôm nay mà kết thúc hôm sau vẫn ghi vào file của HÔM BẮT ĐẦU.
        private static string _logDay = DateTime.Now.ToString("yyMMdd");

        // Đếm số lần chạy FULL TEST đang mở (Auto hoặc Manual All Start).
        // Chỉ ghi log khi > 0 -> KHÔNG ghi khi double-click chạy 1 step, thao tác lẻ, idle...
        private static int _activeRuns = 0;

        private static readonly Logger _instance = new Logger();
        public static Logger Instance => _instance;

        /// <summary>Bắt đầu một lần chạy full test (Auto/Manual All Start) -> mở ghi log + ghim ngày file.</summary>
        public void BeginRun()
        {
            Interlocked.Increment(ref _activeRuns);
            _logDay = DateTime.Now.ToString("yyMMdd");
        }

        /// <summary>Kết thúc một lần chạy full test.</summary>
        public void EndRun()
        {
            if (Interlocked.Decrement(ref _activeRuns) < 0)
                Interlocked.Exchange(ref _activeRuns, 0);
        }

        public ObservableCollection<string> Log1 => log1;
        public ObservableCollection<string> Log2 => log2;

        /// <summary>Ghi log cho 1 bên (tester = 1 hoặc 2).</summary>
        public void AddLog(int tester, string message)
        {
            // Chỉ ghi khi đang trong một lần chạy full test (Auto / Manual All Start).
            if (_activeRuns <= 0) return;

            var target = tester == 2 ? log2 : log1;
            var now = DateTime.Now;
            string uiLine = $"[{now:HH:mm:ss}] {message}";
            string fileLine = $"[{now:yyyy-MM-dd HH:mm:ss}] {message}";

            // Ghi file (trên thread gọi, không chặn UI).
            WriteFile(tester, fileLine);

            // Thêm vào danh sách hiển thị (trên UI thread).
            Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (target.Count >= 500) target.Clear();
                target.Add(uiLine);
            }));
        }

        /// <summary>Ghi log cấp trạm (không thuộc bên nào) -> hiện ở cả 2 bên.</summary>
        public void AddLog(string message)
        {
            AddLog(1, message);
            AddLog(2, message);
        }

        /// <summary>
        /// Ghim ngày file log = ngày TEST BẮT ĐẦU. Gọi lúc test khởi động.
        /// Nhờ vậy test qua nửa đêm vẫn ghi trọn vào file của hôm bắt đầu.
        /// </summary>
        public void MarkTestStart()
        {
            _logDay = DateTime.Now.ToString("yyMMdd");
        }

        private static void WriteFile(int tester, string line)
        {
            try
            {
                Directory.CreateDirectory(LogDir);
                // Dùng ngày đã GHIM lúc test bắt đầu (không đổi giữa chừng dù qua nửa đêm).
                string file = Path.Combine(LogDir, $"T{tester}_{_logDay}.txt");
                lock (_fileLock)
                {
                    File.AppendAllText(file, line + Environment.NewLine);
                }
            }
            catch
            {
                // Ghi file lỗi thì bỏ qua, không ảnh hưởng hiển thị/nghiệp vụ.
            }
        }
    }
}
