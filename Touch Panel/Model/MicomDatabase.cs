using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Touch_Panel.Model
{
    public partial class MicomDatabases : ObservableObject
    {
        [ObservableProperty]
        private string revision = string.Empty;

        [ObservableProperty]
        private ObservableCollection<MicomDatabase> micomDatabaseList = new ObservableCollection<MicomDatabase>();

    }
    public partial class MicomDatabase : ObservableObject
    {
        [ObservableProperty]
        private string firmwareMicom = string.Empty;

        [ObservableProperty]
        private MICOMData micomData1 = new MICOMData();

        [ObservableProperty]
        private MICOMData micomData2 = new MICOMData();

    }
}
