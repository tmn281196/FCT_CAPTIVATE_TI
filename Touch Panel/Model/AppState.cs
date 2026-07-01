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

        // ===== Serial theo tên model =====

        /// <summary>Lấy serial đã cache cho model; trả null nếu chưa có.</summary>
        public static SerialInfo GetSerial(string modelName)
        {
            if (string.IsNullOrEmpty(modelName)) return null;
            var s = Load();
            return s.Serials.TryGetValue(modelName, out var e) ? e : null;
        }

        /// <summary>Lưu thầm serial + ngày in cuối cho model.</summary>
        public static void SetSerial(string modelName, int serialNumber, string lastPrintDate)
        {
            if (string.IsNullOrEmpty(modelName)) return;
            var s = Load();
            s.Serials[modelName] = new SerialInfo { SerialNumber = serialNumber, LastPrintDate = lastPrintDate };
            s.Save();
        }
    }
}
