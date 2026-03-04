using CommunityToolkit.Mvvm.ComponentModel;
using RJCP.IO.Ports;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO.Ports;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Touch_Panel.Model
{
    public enum TestResult
    {
        Pass,
        Fail,
        Unknown
    }

    public partial class Tester : ObservableObject
    {
        [ObservableProperty]
        private int currStep;

        public bool stopTest;

        public int ID;


        public ObservableCollection<Step> Steps;


        public TestResult TestResult;

        public Tester(int id)
        {
            this.ID = id;
        }



        public void ClearSteps()
        {
            foreach (var step in Steps)
            {
                step.Value = "";
                step.Result = "";
            }
        }


    }

    public partial class TestLogic : ObservableObject
    {
        [ObservableProperty]
        private Model model;

        [ObservableProperty]
        private Tester tester1 = new Tester(0);

        [ObservableProperty]
        private Tester tester2 = new Tester(1);

        partial void OnModelChanged(Model? oldValue, Model newValue)
        {
            Tester1 = new Tester(0);
            Tester2 = new Tester(1);

            Tester1.Steps = Model.Step1.Steps;
            Tester2.Steps = Model.Step2.Steps;
        }

        public TestResult TestResult = TestResult.Unknown;


        public async void manualSingleTest(Step selectedItem, Tester tester)
        {
            await RunTestStep(selectedItem, tester);
        }

        public async Task RunTester(Tester tester)
        {
            tester.stopTest = false;
            tester.ClearSteps();
            tester.CurrStep = 0;

            foreach (var step in tester.Steps)
            {
                if (tester.stopTest)
                {
                    break;
                }

                await RunTestStep(step, tester);
                if (Model.Settings.ShouldStopAllWhenAnyFailedStep && step.Result == "Fail")
                {
                    break;
                }
                tester.CurrStep++;

            }
            if (!tester.stopTest)
            {
                tester.TestResult = TestResult.Pass;
                foreach (var step in tester.Steps)
                {
                    if (step.Result == "Fail")
                    {
                        tester.TestResult = TestResult.Fail;
                    }
                }
            }
            else
            {
                tester.TestResult = TestResult.Unknown;
            }
        }

        public async void manualFullTest(Tester tester)
        {
            await RunTester(tester);
        }
        public void manualResetTest()
        {
            Tester1.ClearSteps();
            Tester2.ClearSteps();
        }

        public async Task RunAllTestSteps()
        {

            Task t1 = RunTester(Tester1);
            Task t2 = RunTester(Tester2);

            await Task.WhenAll(t1, t2);


            if (Tester1.TestResult == TestResult.Unknown || Tester2.TestResult == TestResult.Unknown)
            {
                TestResult = TestResult.Unknown;
            }
            else if (Tester1.TestResult == TestResult.Pass && Tester2.TestResult == TestResult.Pass)
            {
                TestResult = TestResult.Pass;
            }
            else if (Tester1.TestResult == TestResult.Fail || Tester2.TestResult == TestResult.Fail)
            {
                TestResult = TestResult.Fail;
            }

        }





        public async Task RunTestStep(Step step, Tester tester)
        {
            string stepName = step.Test;
            bool stepSkip = step.Skip;
            if (stepSkip)
            {
                step.Result = "Skip";
                await Task.Delay(50);
                return;
            }
            switch (stepName)
            {
                case "MEAS":
                    await MEAS(step, tester.ID);
                    break;

                case "NOP":
                    await NOP(step, tester.ID);
                    break;
                case "HALT":
                    await HALT(step, tester.ID);
                    break;

                case "RESUME":
                    await RESUME(step, tester.ID);
                    break;

                case "KEY":
                    await KEY(step, tester.ID);
                    break;
                case "CALIB":
                    await CALIB(step, tester.ID);
                    break;
                default:
                    break;
            }
        }

        private async Task KEY(Step step, int testerID)
        {
            await PendingStep(step);
            string[] val = step.Objectid.Split(',');
            List<int> intArray = val.Select(int.Parse).ToList();

            ConvertChannelsToBytes(intArray, out byte data2, out byte data3, out byte data4);
            MakeAndSendTx(step, (testerID + 1).ToString(), data2, data3, data4);
        }

        private async Task CALIB(Step step, int testerID)
        {
            await PendingStep(step);
            await Model.Devices.RecalibMICOM(testerID + 1);
            int TIMEOUT_MILLISECONDS = step.Timetest;  // Timeout tối đa

            var stopwatch = Stopwatch.StartNew();  // Bắt đầu đếm thời gian

            bool micomResponse = false;

            while (!micomResponse)
            {
                // Kiểm tra timeout bằng Stopwatch
                var now = stopwatch.Elapsed.TotalMilliseconds;

                step.Value = $"{now:0} ms";

                if (now > TIMEOUT_MILLISECONDS)
                {
                    break;
                }

                micomResponse = testerID == 0 ? Model.Devices.Micom1CalibResponse : Model.Devices.Micom2CalibResponse;

                if (micomResponse) break;
                await Task.Delay(10); 
            }

            if (micomResponse)
            {
                step.Result = "Pass";
            }
            else
            {
                step.Result = "Fail";
            }
        }

        private async void MakeAndSendTx(Step step, string solNum, byte data2, byte data3, byte data4)
        {
            SerialPortStream sol = null;
            if (solNum == "1")
            {
                sol = Model.Devices.DeviceManager.Solenoid1Port;
            }
            if (solNum == "2")
            {
                sol = Model.Devices.DeviceManager.Solenoid2Port;
            }
            if (!sol.IsOpen)
            {
                step.Value = "CE";
                step.Result = "Fail";
                return;
            }
            ;

            bool rx1Pass = false;
            bool rx2Pass = false;
            byte[] tx = { 0x44, 0x45, 0x06, 0x53, data2, data3, data4, 0x00 };
            List<byte> txFinal = tx.ToList();
            byte chkSumByte = CalculateChecksum(tx);

            txFinal.Add(chkSumByte);
            txFinal.Add(0x56);

            byte[] txFFinal = txFinal.ToArray();

            foreach (var item in txFinal)
            {
                Debug.Write(item.ToString("X2") + " ");
            }

            if (solNum == "1")
            {
                var device = Model.Devices.DevicesStatus.FirstOrDefault(d => d.Name == "Solenoid 1");
                device.RxCount = 0;
                device.TxSent = true;
                Model.Devices.DeviceManager.Solenoid1Port.Write(txFFinal, 0, txFFinal.Length);
                device.TxSent = false;

                var start = DateTime.Now;
                while (DateTime.Now.Subtract(start).TotalMilliseconds < 5000)
                {
                    if (device.RxCount > 0)
                    {
                        rx1Pass = true;
                        break;
                    }
                    await Task.Delay(1);
                }
                if (rx1Pass)
                {
                    step.Value = step.Specvalue;
                    step.Result = "Pass";
                }
                else
                {
                    step.Value = "CE";
                    step.Result = "Fail";
                }
            }
            if (solNum == "2")
            {
                var device = Model.Devices.DevicesStatus.FirstOrDefault(d => d.Name == "Solenoid 2");
                device.RxCount = 0;
                device.TxSent = true;
                Model.Devices.DeviceManager.Solenoid2Port.Write(txFFinal, 0, txFFinal.Length);
                device.TxSent = false;

                var start = DateTime.Now;
                while (DateTime.Now.Subtract(start).TotalMilliseconds < 5000)
                {
                    if (device.RxCount > 0)
                    {
                        rx2Pass = true;
                        break;
                    }
                    await Task.Delay(1);
                }
                if (rx2Pass)
                {
                    step.Value = step.Specvalue;
                    step.Result = "Pass";
                }
                else
                {
                    step.Value = "CE";
                    step.Result = "Fail";
                }
            }
        }





        private async Task PendingStep(Step step)
        {
            int delayMs = step.Timedelay;

            var sw = Stopwatch.StartNew();

            while (sw.ElapsedMilliseconds < delayMs)
            {
                await Task.Delay(10);

                step.Value = $"pending {sw.ElapsedMilliseconds:0} ms";
            }

            sw.Stop();
            step.Value = string.Empty;

        }
        private async Task MEAS(Step step, int testerID)
        {
            ObservableCollection<CAPSensor> listCAPSensor = new();
            if (testerID == 0)
            {
                listCAPSensor = Model.Devices.ListCAPSensor1;
            }
            if (testerID == 1)
            {
                listCAPSensor = Model.Devices.ListCAPSensor2;
            }
            await PendingStep(step);

            var objectID = step.Objectid.Split(".");

            if (objectID.Length != 3)
            {

                step.Value = "CE";
                step.Result = "Fail";
                return;
            }


            try
            {
                var groupID = int.Parse(objectID[0]);
                var cycleID = int.Parse(objectID[1]);
                var elementID = int.Parse(objectID[2]);

                int timeCheck = step.Timetest;

                var minValue = int.Parse(step.Specvalue);

                var sw = Stopwatch.StartNew();

                while (true)
                {
                    var elementSensor = listCAPSensor[groupID].ListCAPCycle[cycleID].ListCAPElement[elementID];

                    var value = elementSensor.Delta;
                    step.Value = value.ToString();

                    var condition1 = value >= minValue;
                    var condition2 = elementSensor.IsMax;

                    if (condition2 && condition1)
                    {
                        step.Value = elementSensor.Delta.ToString();
                        step.Result = "Pass";
                        break;
                    }

                    if (sw.ElapsedMilliseconds > timeCheck)
                    {
                        step.Value = elementSensor.Delta.ToString();
                        step.Result = "Fail";
                        break;
                    }

                    await Task.Delay(1);
                }
            }
            catch
            {
                step.Value = "CE";
                step.Result = "Fail";
            }


        }

        private async Task NOP(Step step, int testerID)
        {
            await PendingStep(step);


            step.Value = string.Empty;
            step.Result = step.Specvalue == "OK" ? "Pass" : (step.Specvalue == "NG" ? "Fail" : "Pass");

        }

        private async Task HALT(Step step, int testerID)
        {
            await PendingStep(step);
            await Model.Devices.HaltMICOM(testerID + 1);
            step.Result = "Pass";

        }

        private async Task RESUME(Step step, int testerID)
        {
            await PendingStep(step);
            await Model.Devices.ResumeMICOM(testerID + 1);
            step.Result = "Pass";


        }


        byte CalculateChecksum(byte[] tx)
        {
            Byte chkSumByte = 0x00;
            for (int i = 0; i < tx.Length; i++)
                chkSumByte ^= tx[i];
            return chkSumByte;
        }

        void ConvertChannelsToBytes(List<int> channels, out byte data1, out byte data2, out byte data3)
        {
            data1 = 0x00; // S01–S08
            data2 = 0x00; // S09–S16
            data3 = 0x00; // S17–S24

            foreach (int ch in channels)
            {
                byte channel = (byte)((byte)(ch % 24));

                if (channel >= 1 && channel <= 8)
                    data1 |= (byte)(1 << (channel - 1));       // S1 = Bit0 ... S8 = Bit7
                else if (channel >= 9 && channel <= 16)
                    data2 |= (byte)(1 << (channel - 9));       // S9 = Bit0 ... S16 = Bit7
                else if (channel >= 17 && channel <= 24)
                    data3 |= (byte)(1 << (channel - 17));      // S17 = Bit0 ... S24 = Bit7
            }
        }
    }
}
