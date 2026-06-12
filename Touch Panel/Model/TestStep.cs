using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Touch_Panel.Model
{
    public partial class TestStep : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<Step> steps = new ObservableCollection<Step>();

        public void AddStep(string test, string objectId, string specvalue, int idx, int timeDelay, int timeTest)
        {
            if (idx == -1)
            {
                Step step = new Step() { No = (Steps.Count + 1).ToString(), Test = test, Objectid = objectId, Specvalue = specvalue, Timedelay = timeDelay, Timetest = timeTest };
                Steps.Add(step);
            }
            else
            {
                Step step = new Step() { No = (idx + 1).ToString(), Test = test, Objectid = objectId, Specvalue = specvalue , Timedelay = timeDelay, Timetest =timeTest};
                foreach (var stepp in Steps)
                {
                    int stepNo = int.Parse(stepp.No);
                    if (stepNo > idx)
                    {
                        stepNo += 1;
                        stepp.No = stepNo.ToString();
                    }
                }
                Steps.Insert(idx, step);
            }
        }

        public void DeleteStep(Step step)
        {
            if (Steps.Count == 0)
            {
                return;
            }
            if (step == null)
            {
                int idx = Steps.Count() - 1;
                Steps.RemoveAt(idx);
            }
            else
            {
                int idx = Steps.IndexOf(step) + 1;

                foreach (var stepp in Steps)
                {
                    int stepNo = int.Parse(stepp.No);
                    if (stepNo > idx)
                    {
                        stepNo -= 1;
                        stepp.No = stepNo.ToString();
                    }
                }
                Steps.Remove(step);
            }
        }

        // Nhân đôi step đang chọn, chèn ngay phía dưới nó.
        public void DuplicateStep(Step step)
        {
            if (step == null)
            {
                return;
            }

            int idx = Steps.IndexOf(step);
            if (idx < 0)
            {
                return;
            }

            Step clone = (Step)step.Clone();
            Steps.Insert(idx + 1, clone);
            Renumber();
        }

        // Di chuyển step đang chọn lên trên một vị trí.
        public void MoveStepUp(Step step)
        {
            if (step == null)
            {
                return;
            }

            int idx = Steps.IndexOf(step);
            if (idx <= 0)
            {
                return;
            }

            Steps.Move(idx, idx - 1);
            Renumber();
        }

        // Di chuyển step đang chọn xuống dưới một vị trí.
        public void MoveStepDown(Step step)
        {
            if (step == null)
            {
                return;
            }

            int idx = Steps.IndexOf(step);
            if (idx < 0 || idx >= Steps.Count - 1)
            {
                return;
            }

            Steps.Move(idx, idx + 1);
            Renumber();
        }

        // Đánh lại số thứ tự (No) theo vị trí hiện tại trong danh sách.
        private void Renumber()
        {
            for (int i = 0; i < Steps.Count; i++)
            {
                Steps[i].No = (i + 1).ToString();
            }
        }
    }

    public partial class Step : ObservableObject, ICloneable
    {
        [ObservableProperty]
        private string no;

        [ObservableProperty]
        private string test;

        [ObservableProperty]
        private string objectid;

        [ObservableProperty]
        private string specvalue;

        [ObservableProperty]
        private int timedelay;


        [ObservableProperty]
        private int timetest;

        [property: JsonIgnore]
        [ObservableProperty]
        private string taktTime;


        [property:JsonIgnore]
        [ObservableProperty]
        private string value;

        [property: JsonIgnore]
        [ObservableProperty]
        private string result;

        [ObservableProperty]
        private bool noSkip = true;

        public object Clone()
        {
            return new Step
            {
                No = this.No,
                Test = this.Test,
                Objectid = this.Objectid,
                Specvalue = this.Specvalue,
                Timedelay = this.Timedelay,
                Timetest = this.Timetest,
                Value = this.Value,
                Result = this.Result
            };
        }
    }
}
