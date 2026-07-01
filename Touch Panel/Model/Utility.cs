using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace Touch_Panel.Model
{
    public static class Utility
    {
        private static string _currentModelFilePath;

        /// <summary>
        /// Đường dẫn file model đang mở. Dùng cho lệnh Save (ghi đè file cũ).
        /// Được set khi mở model hoặc sau khi Save As thành công.
        /// Mỗi lần set sẽ lưu thầm vào file chung (AppState) để khởi động lần sau load lại.
        /// </summary>
        public static string CurrentModelFilePath
        {
            get => _currentModelFilePath;
            set
            {
                _currentModelFilePath = value;
                AppState.SetRecentModelPath(value);
            }
        }

        /// <summary>
        /// Đọc đường dẫn model mở gần nhất. Trả null nếu chưa có hoặc file không còn tồn tại.
        /// </summary>
        public static string GetRecentModelPath() => AppState.GetRecentModelPath();

        public static void SaveModel<T>(this T source, string filePath, string fileName)
        {
            try
            {
                if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    filePath += ".json";
                }
                string json = JsonSerializer.Serialize(source, new JsonSerializerOptions
                {
                    WriteIndented = true // Makes JSON output more readable
                });

                File.WriteAllText(filePath, json);
                MessageBox.Show("Model Save Success!");
            }
            catch (Exception)
            {
                MessageBox.Show("Model Save Failed!");
            }
        }

        public static T ConvertFromJson<T>(string jsonStr)
        {
            JsonSerializerOptions options = new JsonSerializerOptions()
            {
                MaxDepth = 1024,
                //WriteIndented = true
            };
            return JsonSerializer.Deserialize<T>(jsonStr, options);
        }
    }
}
