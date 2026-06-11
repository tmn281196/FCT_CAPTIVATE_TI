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
        private TestState test;
    }
    public partial class AutoPageViewModel : ObservableObject
    {
        public bool runWhileLoop;

        [ObservableProperty]
        private Model.Model model;

        [ObservableProperty]
        private string elapsedTime = "0.00";


        [ObservableProperty]
        private AutoState state;

        [RelayCommand]
        private async Task ForceStart()
        {


            Model.Devices.ConnectorAllDown();
            await Task.Delay(1000);

            State.Test = TestState.Testing;
            Model.Devices.SystemData.MainUpFlag = false;

        }

        [RelayCommand]
        private async Task ForceStop()
        {

            testLogic.Tester1.stopTest = true;
            testLogic.Tester2.stopTest = true;
        }

        [ObservableProperty]
        private int pass;

        partial void OnPassChanged(int oldValue, int newValue)
        {
            Total += 1;
        }

        [ObservableProperty]
        private int fail;

        partial void OnFailChanged(int oldValue, int newValue)
        {
            Total += 1;
        }

        [ObservableProperty]
        private int total;

        partial void OnTotalChanged(int oldValue, int newValue)
        {
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
            string today = $"{DateTime.Now:yyMMdd}";
            if (Model.LastPrintDate != today)
            {
                NextSerialNumber = 1;
                return;
            }
            int next = Model.Settings.SerialNumber + 1;
            NextSerialNumber = next > 9999 ? 1 : next;
        }

        private async Task PrintNextLabelAsync()
        {
            string today = $"{DateTime.Now:yyMMdd}";
            if (Model.LastPrintDate != today)
            {
                Model.Settings.SerialNumber = 0;
            }

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
                MessageBox.Show(
                    "QR label was NOT printed. Check the printer connection and retry.",
                    "Print Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            else
            {
                Model.LastPrintDate = today;
            }
        }

        private void TaktTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ElapsedTime = Math.Round(stopwatch.Elapsed.TotalSeconds, 0).ToString() + " s";
            });
        }

        public void Dispose()
        {
            taktTimer.Elapsed -= TaktTimer_Elapsed;

        }

        public async void START()
        {
            await Task.Run(TestStart);
        }


        private async void TestStart()
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


            while (runWhileLoop)
            {
                switch (State.Test)
                {
                    case TestState.Wait:
                        Status = "Wait";

                        if(!(Model.Devices.DeviceManager.MicomPort1 == null && Model.Devices.DeviceManager.MicomPort2 == null))
                        {
                            bool micom1 = TestLogic.Tester1.Steps.Count > 0 ? Model.Devices.DeviceManager.MicomPort1.IsOpen : true;
                            bool micom2 = TestLogic.Tester2.Steps.Count > 0 ? Model.Devices.DeviceManager.MicomPort2.IsOpen : true;
                            bool soleinod1 = TestLogic.Tester1.Steps.Count > 0 ? Model.Devices.DeviceManager.Solenoid1Port.IsOpen : true;
                            bool soleinod2 = TestLogic.Tester2.Steps.Count > 0 ? Model.Devices.DeviceManager.Solenoid2Port.IsOpen : true;
                            bool soleinod3 = Model.Devices.DeviceManager.Solenoid3Port.IsOpen;
                            bool system = Model.Devices.DeviceManager.SystemPort.IsOpen;


                            bool allDevicesConnected = micom1 && soleinod1 && micom2 && soleinod2 && soleinod3 && system;


                            if (allDevicesConnected && Model.Settings.LogDir != "")
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
                        if (Model.Devices.SystemData.MainUpFlag == true && Model.Devices.SystemData.MainBottom)
                        {

                            Model.Devices.ConnectorAllDown();

                            StringTestResult = "BEGIN";

                            State.Test = TestState.Testing;
                            Model.Devices.SystemData.MainUpFlag = false;
                            await Task.Delay(1000);

                        }
                        break;

                    case TestState.Testing:

                   

                        await Task.WhenAll(
                          Model.Devices.ResumeMICOM(1),
                          Model.Devices.ResumeMICOM(2)
                        );


                        await Task.WhenAll(

                             Model.Devices.SetAllTunningValuesToMicom(TestLogic.Tester1),
                             Model.Devices.SetAllTunningValuesToMicom(TestLogic.Tester2)
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



                        //await Task.WhenAll(
                        //  Model.Devices.GetTunningValueIfPass(TestLogic.Tester1),
                        //  Model.Devices.GetTunningValueIfPass(TestLogic.Tester2)
                        //);


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

                        await Model.Devices.TurnOnBuzzer();


                        StringTestResult = "Fail";
                        await Task.Delay(500);

                        await PrintNextLabelAsync();


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


        private void SaveLog(bool pass)
        {
            if (!Model.Settings.ShouldSaveLog) return;
            string dir = Model.Settings.LogDir;
            if (dir == null) return;
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            string today = DateTime.Now.ToString("ddMMyyyy");
            string now = DateTime.Now.ToString("HHmmss");
            string dateFolderPath = Path.Combine(dir, today);
            if (!Directory.Exists(dateFolderPath))
            {
                Directory.CreateDirectory(dateFolderPath);
            }

            string filePath = "";
            if (pass)
            {
                filePath = Path.Combine(dateFolderPath, "PASS_" + now + ".csv");
            }
            else
            {
                filePath = Path.Combine(dateFolderPath, "FAIL_" + now + ".csv");
            }

            using (StreamWriter sw = new StreamWriter(filePath, true, Encoding.UTF8))
            {
                sw.WriteLine("Group,No,Test,Content,Spec,Value,Result");
                var data1 = Model.Micom1TestStep.Steps as IEnumerable<Step>;
                var data2 = Model.Micom2TestStep.Steps as IEnumerable<Step>;

                foreach (var row in data1)
                {
                    string line = $"L,{row.No},{row.Test},{row.Objectid},{row.Specvalue},{row.Value},{row.Result}";
                    sw.WriteLine(line);
                }

                foreach (var row in data2)
                {
                    string line = $"R,{row.No},{row.Test},{row.Objectid},{row.Specvalue},{row.Value},{row.Result}";
                    sw.WriteLine(line);
                }
            }
        }

        private static readonly Dictionary<int, char> YearCodes = new()
        {
            [2021] = 'R', [2022] = 'T', [2023] = 'W', [2024] = 'X', [2025] = 'Y',
            [2026] = 'L', [2027] = 'P', [2028] = 'Q', [2029] = 'S', [2030] = 'Z',
            [2031] = 'B', [2032] = 'C', [2033] = 'D', [2034] = 'E', [2035] = 'F',
            [2036] = 'G', [2037] = 'H', [2038] = 'J', [2039] = 'K', [2040] = 'M',
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

            sb.Append($"{measSteps.Count:D2}");
            sb.Append($"/{testStartTime:yyyyMMddHHmmss}");
            sb.Append($"/{testFinishTime:yyyyMMddHHmmss}");

            int idx = 1;
            foreach (var step in measSteps)
            {
                string code = $"A{idx:D3}";
                string measured = string.IsNullOrEmpty(step.Value) ? "0" : step.Value;
                string spec = string.IsNullOrEmpty(step.Specvalue) ? "0" : step.Specvalue;
                sb.Append($"/{code}-{measured}-{spec}-0");
                idx++;
            }
            sb.Append('/');

            return sb.ToString();
        }
    }
}
