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
            await Task.Delay(1000);


            Model.Devices.ConnectorAllDown();

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

        public event EventHandler EscapTimeChange;

        private Stopwatch stopwatch = new Stopwatch();

        private Timer taktTimer = new Timer()
        {
            Interval = 100,
            Enabled = true
        };

        public AutoPageViewModel(Model.Model sharedModel)
        {
            this.Model = sharedModel;
            taktTimer.Stop();
            stopwatch.Stop();
            taktTimer.Elapsed += TaktTimer_Elapsed;
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
            //Model.Devices.SystemData.MainUpFlag = true;
            State.Test = TestState.Ready;
            StringTestResult = "BEGIN";

            await Task.WhenAll(
                Model.Devices.HaltMICOM(1),
                Model.Devices.HaltMICOM(2)
            );

            await Task.WhenAll(
                Model.Devices.ResetSolenoid(testLogic.Tester1),
                Model.Devices.ResetSolenoid(testLogic.Tester2)
            );

            TestLogic.Tester1.ClearSteps();
            TestLogic.Tester2.ClearSteps();


            while (runWhileLoop)
            {
                switch (State.Test)
                {
                    case TestState.Wait:
                        Status = "Wait";

                        bool allDevicesConnected = true;
                        foreach (var device in Model.Devices.DevicesStatus)
                        {
                            if (!device.Connected)
                            {
                                allDevicesConnected = false;
                            }
                        }
                        if (allDevicesConnected && Model.Settings.LogDir != "")
                        {

                            if (Model.Devices.SystemData.MainDirection == Direction.Up)
                            {

                                await Task.WhenAll(
                                      Model.Devices.ResetSolenoid(testLogic.Tester1),
                                      Model.Devices.ResetSolenoid(testLogic.Tester2)
                                );

                                Model.Devices.ConnectorAllUp();
                                State.Test = TestState.Ready;
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
                        }
                        break;

                    case TestState.Testing:

                        await Task.Delay(2000);

                        await Task.WhenAll(
                          Model.Devices.ResumeMICOM(1),
                          Model.Devices.ResumeMICOM(2)
                        );



                        Status = "Testing";

                        stopwatch.Start();
                        taktTimer.Start();

                        await TestLogic.RunAllTestSteps();

                        stopwatch.Stop();
                        taktTimer.Stop();


                        await Task.WhenAll(
                            Model.Devices.ResetSolenoid(testLogic.Tester1),
                            Model.Devices.ResetSolenoid(testLogic.Tester2)
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
                        StringTestResult = "Fail";
                        await Task.Delay(500);
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
    }
}
