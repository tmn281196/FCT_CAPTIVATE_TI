using System;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Touch_Panel.Model;

namespace Touch_Panel.View
{
    /// <summary>
    /// Interaction logic for LogPageView.xaml. 2 vùng log riêng: T1 (Log1) / T2 (Log2).
    /// </summary>
    public partial class LogPageView : UserControl
    {
        public LogPageView()
        {
            InitializeComponent();
            this.DataContext = Logger.Instance;

            Logger.Instance.Log1.CollectionChanged += Log1_CollectionChanged;
            Logger.Instance.Log2.CollectionChanged += Log2_CollectionChanged;
        }

        private void ScrollToEnd(ListBox list)
        {
            // Hoãn scroll để chạy SAU khi ListBox cập nhật container, tránh "Generator_Inconsistent".
            Dispatcher.BeginInvoke(new Action(() =>
            {
                int n = list.Items.Count;
                if (n > 0) list.ScrollIntoView(list.Items[n - 1]);
            }), DispatcherPriority.Background);
        }

        private void Log1_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (autoScroll1 != null && autoScroll1.IsChecked == true) ScrollToEnd(list1);
        }

        private void Log2_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (autoScroll2 != null && autoScroll2.IsChecked == true) ScrollToEnd(list2);
        }

        private void ClearLog1_Click(object sender, RoutedEventArgs e) => Logger.Instance.Log1.Clear();
        private void ClearLog2_Click(object sender, RoutedEventArgs e) => Logger.Instance.Log2.Clear();
    }
}
