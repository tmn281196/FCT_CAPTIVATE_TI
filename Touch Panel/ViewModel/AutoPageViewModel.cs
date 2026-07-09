using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Text;
using System.Timers;
using System.Windows;
using Touch_Panel.Model;
using static Touch_Panel.View_Model.AutoPageViewModel;
using Timer = System.Timers.Timer;

namespace Touch_Panel.View_Model
{
    public enum TestState
    {
        Wait,
        Ready,
        Testing,
        NG,
        OK,
        Stop
    }

    public partial class AutoState : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsTesting))]
        [NotifyPropertyChangedFor(nameof(IsNotTesting))]
        [NotifyPropertyChangedFor(nameof(IsBusy))]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        private TestState test;

        // Manual Full Test đang chạy (auto không dùng cờ này).
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsBusy))]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        private bool manualTesting;

        // Đang GHI firmware -> cấm test (auto & manual) và khóa sửa model.
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsBusy))]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        private bool firmwareWriting;

        // Auto đang test (state Testing = nhấn START hoặc auto main-down). Đây MỚI là "test".
        public bool IsTesting => Test == TestState.Testing;
        public bool IsNotTesting => Test != TestState.Testing;

        // Bận = auto đang test HOẶC manual đang chạy HOẶC đang ghi firmware. KHÓA sửa model ở mọi trang.
        public bool IsBusy => Test == TestState.Testing || ManualTesting || FirmwareWriting;
        public bool IsNotBusy => !IsBusy;

        // MICOM1/2 có thể ghi firmware SONG SONG -> đếm để chỉ mở khóa khi TẤT CẢ xong.
        private int _fwWriteCount;
        public void BeginFirmwareWrite()
        {
            System.Threading.Interlocked.Increment(ref _fwWriteCount);
            FirmwareWriting = true;
        }
        public void EndFirmwareWrite()
        {
            if (System.Threading.Interlocked.Decrement(ref _fwWriteCount) <= 0)
            {
                _fwWriteCount = 0;
                FirmwareWriting = false;
            }
        }
    }
    public partial class AutoPageViewModel : ObservableObject
    {
        public bool runWhileLoop;

        // Chỉ cho phép KÍCH HOẠT test (Wait->Ready khi main down) khi đang ở trang Auto.
        // Loop vẫn chạy nền ở trang khác (giữ state/kết quả) nhưng KHÔNG tự trigger test.
        // [ObservableProperty] để UI (nút START/STOP global) bám theo: chỉ bật khi ở trang AUTO.
        [ObservableProperty]
        private bool autoPageActive;

        // Khởi tạo test (halt/reset/xóa kết quả) chỉ chạy 1 lần; tránh xóa kết quả khi chuyển trang.
        private bool testInitialized = false;
        // Chặn spawn nhiều vòng lặp TestStart cùng lúc khi qua lại trang Home.
        private bool isTestLoopRunning = false;

        [ObservableProperty]
        private Model.Model model;

        [ObservableProperty]
        private string elapsedTime = "0.0";


        [ObservableProperty]
        private AutoState state;

        [RelayCommand]
        private async Task ForceStart()
        {
            // Đang ghi firmware -> cấm khởi động test.
            if (State.FirmwareWriting) return;

            Model.Devices.ConnectorAllDown();
            await Task.Delay(Touch_Panel.Model.TestTiming.ConnectorSettleDelayMs);

            State.Test = TestState.Testing;
            Model.Devices.SystemData.MainUpFlag = false;

        }

        [RelayCommand]
        private async Task ForceStop()
        {

            testLogic.Tester1.stopTest = true;
            testLogic.Tester2.stopTest = true;
        }

        // Đang RESET tay -> không cộng dồn Total khi Pass/Fail bị đưa về 0.
        private bool _resettingStats;

        [ObservableProperty]
        private int pass;

        partial void OnPassChanged(int oldValue, int newValue)
        {
            if (_resettingStats) return;
            Total += 1;
            AppState.SetStats(Utility.CurrentModelFilePath, Pass, Fail);
        }

        [ObservableProperty]
        private int fail;

        partial void OnFailChanged(int oldValue, int newValue)
        {
            if (_resettingStats) return;
            Total += 1;
            AppState.SetStats(Utility.CurrentModelFilePath, Pass, Fail);
        }

        [ObservableProperty]
        private int total;

        partial void OnTotalChanged(int oldValue, int newValue)
        {
            if (newValue <= 0)
            {
                PassPercent = 0;
                return;
            }
            PassPercent = Math.Round(((double)Pass / Total) * 100);
        }

        [ObservableProperty]
        private double passPercent;

        [ObservableProperty]
        private string status;

        [ObservableProperty]
        private string stringTestResult = string.Empty;

        [ObservableProperty]
        private TestLogic testLogic;

        [ObservableProperty]
        private int nextSerialNumber = 1;

        public event EventHandler EscapTimeChange;

        private Stopwatch stopwatch = new Stopwatch();

        private DateTime testStartTime;
        private DateTime testFinishTime;

        private Timer taktTimer = new Timer()
        {
            Interval = 100,
            Enabled = true
        };

        public AutoPageViewModel(Model.Model sharedModel)
        {
            taktTimer.Stop();
            stopwatch.Stop();
            taktTimer.Elapsed += TaktTimer_Elapsed;

            this.Model = sharedModel; // OnModelChanged sẽ subscribe events và RefreshNextSerial
        }

        partial void OnModelChanged(Model.Model oldValue, Model.Model newValue)
        {
            if (oldValue != null)
            {
                oldValue.Settings.PropertyChanged -= OnSettingsPropertyChanged;
                oldValue.PropertyChanged -= OnModelPropertyChanged;
            }

            if (newValue != null)
            {
                newValue.Settings.PropertyChanged += OnSettingsPropertyChanged;
                newValue.PropertyChanged += OnModelPropertyChanged;
            }

            RefreshNextSerial();
            LoadStatsFromCache();
        }

        // Nạp lại COUNTER từ cache theo đường dẫn file model: Pass/FAIL (Total, % suy ra) + serial.
        private void LoadStatsFromCache()
        {
            var cache = AppState.GetSerial(Utility.CurrentModelFilePath);
            _resettingStats = true;
            Pass = cache?.Pass ?? 0;
            Fail = cache?.Fail ?? 0;
            Total = Pass + Fail; // OnTotalChanged tự tính PassPercent
            _resettingStats = false;

            // Khôi phục serial đã in gần nhất từ cache (nếu có) -> NextSerial đúng.
            if (cache != null && Model != null)
            {
                Model.Settings.SerialNumber = cache.SerialNumber;
                Model.LastPrintDate = cache.LastPrintDate;
            }
            RefreshNextSerial();
        }

        private void OnSettingsPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Settings.SerialNumber)) RefreshNextSerial();
        }

        private void OnModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Model.LastPrintDate)) RefreshNextSerial();
        }

        private void RefreshNextSerial()
        {
            // Không còn tự reset theo ngày: serial chạy tiếp cho tới khi bấm RESET tay.
            int next = Model.Settings.SerialNumber + 1;
            NextSerialNumber = next > 9999 ? 1 : next;
        }

        // RESET TAY 1: xóa thống kê (Pass/NG/Total/%). Lưu thầm để sống sót qua restart.
        [RelayCommand]
        private void ResetStatistics()
        {
            _resettingStats = true;
            Pass = 0;
            Fail = 0;
            Total = 0;
            PassPercent = 0;
            _resettingStats = false;

            AppState.SetStats(Utility.CurrentModelFilePath, 0, 0);
        }

        // RESET TAY 2: đưa serial kế tiếp về 0001 (SerialNumber = số VỪA in; +1 = kế tiếp).
        [RelayCommand]
        private void ResetSerial()
        {
            Model.Settings.SerialNumber = 0;
            RefreshNextSerial();
            AppState.SetSerial(Utility.CurrentModelFilePath, 0, Model.LastPrintDate);
        }

        // -1 FAIL: trừ 1 khỏi số Fail (và Total) BẤT KỂ kết quả gần nhất là gì.
        // Bấm nhiều lần được, còn Fail thì trừ tiếp; hết Fail (=0) thì thôi.
        [RelayCommand]
        private void UndoLastResult()
        {
            if (Fail <= 0) return; // không còn Fail để trừ

            _resettingStats = true;
            Fail -= 1;
            if (Total > 0) Total -= 1;
            PassPercent = Total > 0 ? Math.Round((double)Pass / Total * 100) : 0;
            _resettingStats = false;

            AppState.SetStats(Utility.CurrentModelFilePath, Pass, Fail);
        }

        private async Task PrintNextLabelAsync()
        {
            if (!Model.Settings.ShouldPrintQr)
            {
                return;
            }

            string today = $"{DateTime.Now:yyMMdd}";

            Model.Settings.SerialNumber += 1;
            if (Model.Settings.SerialNumber > 9999)
            {
                Model.Settings.SerialNumber = 1;
            }

            string qrString = BuildQrString();
            bool printed = await Model.Devices.PrintAsync(Model.Settings.SerialNumber, Model.Settings.PartNumber, qrString);
            if (!printed)
            {
                Model.Settings.SerialNumber -= 1;
                Logger.Instance.AddLog("QR print FAILED - check printer");
                MessageBox.Show(
                    "QR label was NOT printed. Check the printer connection and retry.",
                    "Print Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            else
            {
                Model.LastPrintDate = today;
                Logger.Instance.AddLog($"QR printed - SN {Model.Settings.SerialNumber:D4}");

                // Lưu thầm serial + ngày in theo ĐƯỜNG DẪN FILE MODEL để sống sót qua restart.
                AppState.SetSerial(Utility.CurrentModelFilePath, Model.Settings.SerialNumber, Model.LastPrintDate);
            }
        }

        private void TaktTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ElapsedTime = $"{stopwatch.Elapsed.TotalSeconds:0.0}";
            });
        }

        public void Dispose()
        {
            taktTimer.Elapsed -= TaktTimer_Elapsed;

        }

        public async void START()
        {
            // Đang có vòng lặp chạy thì thôi, tránh spawn trùng khi qua lại trang.
            if (isTestLoopRunning) return;
            await Task.Run(TestStart);
        }


        private async void TestStart()
        {
            isTestLoopRunning = true;
            try
            {
            // Chỉ khởi tạo (halt/reset/XÓA kết quả) LẦN ĐẦU. Các lần chuyển trang sau chỉ
            // chạy tiếp vòng lặp, KHÔNG xóa kết quả test đang hiển thị.
            if (!testInitialized)
            {
                State.Test = TestState.Ready;
                StringTestResult = "BEGIN";

                await Task.WhenAll(
                    Model.Devices.HaltMICOM(1),
                    Model.Devices.HaltMICOM(2)
                );

                await Task.WhenAll(
                    Model.Devices.ResetSolenoid(TestLogic.Tester1),
                    Model.Devices.ResetSolenoid(TestLogic.Tester2)
                );

                TestLogic.Tester1.ClearSteps();
                TestLogic.Tester2.ClearSteps();

                testInitialized = true;
            }


            while (runWhileLoop)
            {
                switch (State.Test)
                {
                    case TestState.Wait:
                        Status = "Wait";

                            if (!(Model.Devices.DeviceManager.MicomPort1 == null && Model.Devices.DeviceManager.MicomPort2 == null))
                            {
                                if (TestLogic.Tester1.Steps == null || TestLogic.Tester2.Steps == null) break; // bỏ qua vòng này, KHÔNG thoát loop (giữ loop sống qua lúc mở model)
                            bool micom1 = TestLogic.Tester1.Steps.Count > 0 ? DeviceManager.IsPortOpen(Model.Devices.DeviceManager.MicomPort1) : true;
                            bool micom2 = TestLogic.Tester2.Steps.Count > 0 ? DeviceManager.IsPortOpen(Model.Devices.DeviceManager.MicomPort2) : true;
                            bool soleinod1 = TestLogic.Tester1.Steps.Count > 0 ? DeviceManager.IsPortOpen(Model.Devices.DeviceManager.Solenoid1Port) : true;
                            bool soleinod2 = TestLogic.Tester2.Steps.Count > 0 ? DeviceManager.IsPortOpen(Model.Devices.DeviceManager.Solenoid2Port) : true;
                            bool soleinod3 = DeviceManager.IsPortOpen(Model.Devices.DeviceManager.Solenoid3Port);
                            bool system = DeviceManager.IsPortOpen(Model.Devices.DeviceManager.SystemPort);


                            bool allDevicesConnected = micom1 && soleinod1 && micom2 && soleinod2 && soleinod3 && system;


                            if (autoPageActive && allDevicesConnected && Model.Settings.LogDir != "" && !State.FirmwareWriting)
                            {
                                if (Model.Devices.SystemData.MainDirection == Direction.Up)
                                {

                                    await Task.WhenAll(
                                          Model.Devices.ResetSolenoid(TestLogic.Tester1),
                                          Model.Devices.ResetSolenoid(TestLogic.Tester2)
                                    );

                                    Model.Devices.ConnectorAllUp();
                                    State.Test = TestState.Ready;
                                }
                            }
                        }
                        break;



                    case TestState.Ready:
                        Status = "Ready";
                        // Rời trang Auto lúc đang Ready -> không kích hoạt, đưa về Wait.
                        if (!autoPageActive)
                        {
                            State.Test = TestState.Wait;
                            break;
                        }
                        if (Model.Devices.SystemData.MainUpFlag == true && Model.Devices.SystemData.MainBottom && !State.FirmwareWriting)
                        {

                            Model.Devices.ConnectorAllDown();

                            StringTestResult = "BEGIN";

                            State.Test = TestState.Testing;
                            Model.Devices.SystemData.MainUpFlag = false;
                            await Task.Delay(Touch_Panel.Model.TestTiming.ConnectorSettleDelayMs);

                        }
                        break;

                    case TestState.Testing:

                   

                        await Task.WhenAll(
                          Model.Devices.ResumeMICOM(1),
                          Model.Devices.ResumeMICOM(2)
                        );


                        Status = "Testing";

                        stopwatch.Start();
                        taktTimer.Start();
                        testStartTime = DateTime.Now;

                        await TestLogic.RunAllTestSteps();

                        testFinishTime = DateTime.Now;

                        stopwatch.Stop();
                        taktTimer.Stop();


                        await Task.WhenAll(
                            Model.Devices.ResetSolenoid(TestLogic.Tester1),
                            Model.Devices.ResetSolenoid(TestLogic.Tester2)
                        );


                        await Task.Delay(100);
                        await Task.WhenAll(
                            Model.Devices.HaltMICOM(1),
                            Model.Devices.HaltMICOM(2)
                        );
                        await Task.Delay(100);

                        if (TestLogic.TestResult == TestResult.Pass)
                        {
                            State.Test = TestState.OK;
                        }
                        if (TestLogic.TestResult == TestResult.Fail)
                        {
                            State.Test = TestState.NG;
                        }
                        if (TestLogic.TestResult == TestResult.Unknown)
                        {
                            State.Test = TestState.Stop;
                        }
                        break;

                    case TestState.NG:

                        if (Model.Settings.ShouldMainResetWhenFailTest)
                        {
                            await Model.Devices.ResetMainCylinder();



                        }

                        await Task.Delay(100);

                        if (Model.Settings.ShouldBuzzerWhenFailTest)
                        {
                            await Model.Devices.TurnOnBuzzer();
                        }


                        StringTestResult = "Fail";
                        await Task.Delay(500);

                        // Chỉ in QR cho hàng đạt (OK) -> case Fail không in.

                        Fail += 1;
                        SaveLog(false);
                        stopwatch.Reset();
                        State.Test = TestState.Wait;
                        break;

                    case TestState.OK:

                  

                        if (Model.Settings.ShouldMainResetWhenPassTest)
                        {
                            await Model.Devices.ResetMainCylinder();

                        }

                        StringTestResult = "Pass";
                        await Task.Delay(500);

                        await PrintNextLabelAsync();

                        Pass += 1;
                        SaveLog(true);
                        stopwatch.Reset();
                        State.Test = TestState.Wait;
                        break;

                    case TestState.Stop:
                        Status = "Stop";
                        await Task.Delay(500);
                        stopwatch.Reset();
                        State.Test = TestState.Wait;
                        break;

                    default:
                        break;
                }
                await Task.Delay(1);
            }
            }
            finally
            {
                isTestLoopRunning = false;
            }
        }


        private void SaveLog(bool pass)
        {
            if (!Model.Settings.ShouldSaveLog) return;
            string dir = Model.Settings.LogDir;
            if (dir == null) return;

            var nowDt = DateTime.Now;
            // Cấu trúc mới: LogDir / YYYY / <Tháng January..December> / ddMM / (file từng test).
            // 12 folder tháng được TẠO SẴN khi thư mục năm mới được tạo.
            string yearFolder = Path.Combine(dir, nowDt.ToString("yyyy"));
            if (!Directory.Exists(yearFolder))
            {
                Directory.CreateDirectory(yearFolder);
                foreach (var m in MonthFolderNames)
                    Directory.CreateDirectory(Path.Combine(yearFolder, m));
            }
            string monthFolder = Path.Combine(yearFolder, MonthFolderNames[nowDt.Month - 1]);
            string dayFolder = Path.Combine(monthFolder, nowDt.ToString("ddMM"));
            Directory.CreateDirectory(dayFolder); // tạo cả monthFolder nếu còn thiếu

            string now = nowDt.ToString("HHmmss");

            var data1 = Model.Micom1TestStep.Steps as IEnumerable<Step>;
            var data2 = Model.Micom2TestStep.Steps as IEnumerable<Step>;

            // ===== 1) LOG 1 SAMPLE = 1 FILE: PASS_/FAIL_HHmmss.csv trong folder ngày (ddMM) =====
            string filePath = Path.Combine(dayFolder, (pass ? "PASS_" : "FAIL_") + now + ".csv");
            using (StreamWriter sw = new StreamWriter(filePath, true, Encoding.UTF8))
            {
                sw.WriteLine("Group,No,CMD Name,Object ID,Spec Value,Value,Result");
                foreach (var row in data1)
                    sw.WriteLine($"L,{row.No},{row.Test},{row.Objectid},{row.Specvalue},{row.Value},{row.Result}");
                foreach (var row in data2)
                    sw.WriteLine($"R,{row.No},{row.Test},{row.Objectid},{row.Specvalue},{row.Value},{row.Result}");
            }

            // ===== 2) LOG GỘP: 1 CSV mỗi ngày, để ở cấp THÁNG: DAILY_ddMM.csv (vd DAILY_0907.csv) =====
            string dailyPath = Path.Combine(monthFolder, "DAILY_" + nowDt.ToString("ddMM") + ".csv");
            bool dailyExists = File.Exists(dailyPath);
            string timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string serial = $"{Model.Settings.SerialNumber:D4}";
            string overall = pass ? "PASS" : "FAIL";
            using (StreamWriter sw = new StreamWriter(dailyPath, true, Encoding.UTF8))
            {
                if (!dailyExists)
                    sw.WriteLine("DateTime,Serial,Overall,Group,No,CMD Name,Object ID,Spec Value,Value,Result");
                foreach (var row in data1)
                    sw.WriteLine($"{timeStamp},{serial},{overall},L,{row.No},{row.Test},{row.Objectid},{row.Specvalue},{row.Value},{row.Result}");
                foreach (var row in data2)
                    sw.WriteLine($"{timeStamp},{serial},{overall},R,{row.No},{row.Test},{row.Objectid},{row.Specvalue},{row.Value},{row.Result}");
                sw.WriteLine(); // dòng trắng ngăn cách giữa các sample
            }
        }

        private static readonly Dictionary<int, char> YearCodes = new()
        {
            [2021] = 'R', [2022] = 'T', [2023] = 'W', [2024] = 'X', [2025] = 'Y',
            [2026] = 'L', [2027] = 'P', [2028] = 'Q', [2029] = 'S', [2030] = 'Z',
            [2031] = 'B', [2032] = 'C', [2033] = 'D', [2034] = 'E', [2035] = 'F',
            [2036] = 'G', [2037] = 'H', [2038] = 'J', [2039] = 'K', [2040] = 'M',
        };

        // Tên 12 folder tháng (index 0..11 = Tháng 1..12), tạo sẵn trong folder năm.
        private static readonly string[] MonthFolderNames =
        {
            "January", "February", "March", "April", "May", "June",
            "July", "August", "September", "October", "November", "December"
        };

        private static char EncodeMonth(int m) => m <= 9 ? (char)('0' + m) : (char)('A' + m - 10);
        private static char EncodeDay(int d) => d <= 9 ? (char)('0' + d) : (char)('A' + d - 10);

        private static string EncodeProductionDate(DateTime d)
        {
            char year = YearCodes.TryGetValue(d.Year, out var y) ? y : '?';
            return $"{year}{EncodeMonth(d.Month)}{EncodeDay(d.Day)}";
        }

        private string BuildQrString()
        {
            var s = Model.Settings;
            var sb = new StringBuilder();

            sb.Append(s.UnitCode);
            sb.Append((s.PartNumber ?? string.Empty).Replace("-", ""));
            sb.Append(s.PartnerCode);
            sb.Append(EncodeProductionDate(DateTime.Now));
            sb.Append($"{s.SerialNumber:D4}");
            sb.Append("QR");
            sb.Append(s.CountryCode);
            sb.Append(s.LineCode);
            sb.Append(s.EquipmentSerial);

            var steps = new List<Step>();
            if (TestLogic?.Tester1?.Steps != null) steps.AddRange(TestLogic.Tester1.Steps);
            if (TestLogic?.Tester2?.Steps != null) steps.AddRange(TestLogic.Tester2.Steps);

            var measSteps = steps.Where(x => x.Test == "MEAS").Take(3).ToList();

            // Danh sách (giá trị đo, spec) lấy từ bước MEAS thật
            var measData = measSteps
                .Select(x => (
                    measured: string.IsNullOrEmpty(x.Value) ? "0" : x.Value,
                    spec: string.IsNullOrEmpty(x.Specvalue) ? "0" : x.Specvalue))
                .ToList();

#if DEBUG
            // CHỈ khi build DEBUG và model KHÔNG có bước MEAS nào -> chèn 3 data MEAS GIẢ để có QR text hợp lệ khi test.
            // Bản Release (sản xuất) KHÔNG chèn giả. Model có MEAS thật cũng giữ nguyên.
            if (measData.Count == 0)
            {
                string[,] dummy = { { "123.45", "120.00" }, { "-67.89", "-70.00" }, { "999.99", "1000.0" } };
                for (int i = 0; i < 3; i++)
                    measData.Add((dummy[i, 0], dummy[i, 1]));
            }
#endif

            sb.Append($"{measData.Count:D2}");
            sb.Append($"/{testStartTime:yyyyMMddHHmmss}");
            sb.Append($"/{testFinishTime:yyyyMMddHHmmss}");

            int idx = 1;
            foreach (var (measured, spec) in measData)
            {
                string code = $"A{idx:D3}";
                sb.Append($"/{code}-{measured}-{spec}-0");
                idx++;
            }
            sb.Append('/');

            return sb.ToString();
        }
    }
}
