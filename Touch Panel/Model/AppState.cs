using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Touch_Panel.Model
{
    /// <summary>
    /// Trạng thái app lưu THẦM ra đĩa trong MỘT file chung (~/.TouchPanelSystem/app_state.json):
    ///  - RecentModelPath: file model mở gần nhất (để khởi động tự load lại).
    ///  - Serials: serial + ngày in cuối, theo TÊN MODEL (mỗi model một bộ đếm).
    /// Mọi thao tác đều im lặng, lỗi đọc/ghi bỏ qua, không chặn nghiệp vụ chính.
    /// </summary>
    public class AppState
    {
        public string RecentModelPath { get; set; } = string.Empty;
        public Dictionary<string, SerialInfo> Serials { get; set; } = new();

        public class SerialInfo
        {
            public int SerialNumber { get; set; }
            public string LastPrintDate { get; set; } = string.Empty;
            // Thống kê chạy theo model (lưu Pass/NG; Total và % suy ra được).
            public int Pass { get; set; }
            public int Fail { get; set; }
        }

        public static string CacheDir =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".TouchPanelSystem");

        public static string CacheFile => Path.Combine(CacheDir, "app_state.json");

        public static AppState Load()
        {
            try
            {
                if (File.Exists(CacheFile))
                {
                    var json = File.ReadAllText(CacheFile);
                    return JsonSerializer.Deserialize<AppState>(json) ?? new AppState();
                }
            }
            catch
            {
                // Lỗi đọc thì coi như chưa có state.
            }
            return new AppState();
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(CacheDir);
                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(CacheFile, json);
            }
            catch
            {
                // Lưu thầm thất bại thì bỏ qua.
            }
        }

        // ===== Recent model path =====

        /// <summary>Đường dẫn model mở gần nhất; trả null nếu chưa có hoặc file không còn tồn tại.</summary>
        public static string GetRecentModelPath()
        {
            var path = Load().RecentModelPath;
            return (!string.IsNullOrEmpty(path) && File.Exists(path)) ? path : null;
        }

        public static void SetRecentModelPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            var s = Load();
            s.RecentModelPath = path;
            s.Save();
        }

        // ===== Cache theo ĐƯỜNG DẪN FILE MODEL (serial + thống kê) =====

        /// <summary>Lấy cache (serial + thống kê) cho model; trả null nếu chưa có.</summary>
        public static SerialInfo GetSerial(string modelKey)
        {
            if (string.IsNullOrEmpty(modelKey)) return null;
            var s = Load();
            return s.Serials.TryGetValue(modelKey, out var e) ? e : null;
        }

        /// <summary>Lưu thầm serial + ngày in cuối; GIỮ NGUYÊN Pass/NG đang có.</summary>
        public static void SetSerial(string modelKey, int serialNumber, string lastPrintDate)
        {
            if (string.IsNullOrEmpty(modelKey)) return;
            var s = Load();
            if (!s.Serials.TryGetValue(modelKey, out var e) || e == null) e = new SerialInfo();
            e.SerialNumber = serialNumber;
            e.LastPrintDate = lastPrintDate;
            s.Serials[modelKey] = e;
            s.Save();
        }

        /// <summary>Lưu thầm thống kê Pass/NG; GIỮ NGUYÊN serial đang có.</summary>
        public static void SetStats(string modelKey, int pass, int fail)
        {
            if (string.IsNullOrEmpty(modelKey)) return;
            var s = Load();
            if (!s.Serials.TryGetValue(modelKey, out var e) || e == null) e = new SerialInfo();
            e.Pass = pass;
            e.Fail = fail;
            s.Serials[modelKey] = e;
            s.Save();
        }
    }
}
