using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Touch_Panel.View_Model;

namespace Touch_Panel.View
{
    /// <summary>
    /// Interaction logic for SettingPageView.xaml
    /// </summary>
    public partial class SettingPageView : UserControl
    {
        public SettingPageView(SettingPageViewModel vm)
        {
            InitializeComponent();
            this.DataContext = vm;
        }

        /// <summary>Toggle: checked = nối tất cả, unchecked = ngắt tất cả (kiểu ZeroC).</summary>
        private void ConnectAllToggle_Click(object sender, RoutedEventArgs e)
        {
            var tb = sender as System.Windows.Controls.Primitives.ToggleButton;
            var vm = DataContext as SettingPageViewModel;
            if (tb == null || vm == null) return;

            if (tb.IsChecked == true)
            {
                if (vm.ConnectAllDevicesCommand.CanExecute(null))
                    vm.ConnectAllDevicesCommand.Execute(null);
            }
            else
            {
                if (vm.DisconnectAllDevicesCommand.CanExecute(null))
                    vm.DisconnectAllDevicesCommand.Execute(null);
            }
        }
    }
}
