using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Touch_Panel.Model
{
    public partial class UserSelectOptions : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<string> partNumberList = new();

        [ObservableProperty]
        private ObservableCollection<string> partnerCodeList = new();

        [ObservableProperty]
        private ObservableCollection<string> countryCodeList = new();

        [ObservableProperty]
        private ObservableCollection<string> lineCodeList = new();

        [ObservableProperty]
        private ObservableCollection<string> equipmentSerialList = new();

        public static string CacheDir =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".TouchPanelSystem");

        public static string CacheFile => Path.Combine(CacheDir, "user_select_options.json");

        public static UserSelectOptions Load()
        {
            try
            {
                if (File.Exists(CacheFile))
                {
                    var json = File.ReadAllText(CacheFile);
                    return JsonSerializer.Deserialize<UserSelectOptions>(json) ?? new UserSelectOptions();
                }
            }
            catch
            {
            }
            return new UserSelectOptions();
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
            }
        }

        public bool AddIfNew(string fieldName, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            var list = GetList(fieldName);
            if (list == null) return false;
            if (list.Contains(value)) return false;
            list.Add(value);
            return true;
        }

        public bool RemoveOption(string fieldName, string value)
        {
            var list = GetList(fieldName);
            if (list == null) return false;
            return list.Remove(value);
        }

        private ObservableCollection<string> GetList(string fieldName) => fieldName switch
        {
            nameof(Settings.PartNumber) => PartNumberList,
            nameof(Settings.PartnerCode) => PartnerCodeList,
            nameof(Settings.CountryCode) => CountryCodeList,
            nameof(Settings.LineCode) => LineCodeList,
            nameof(Settings.EquipmentSerial) => EquipmentSerialList,
            _ => null
        };
    }
}
