using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Wukong_PBData_ReadWriter_GUI.Models;

namespace Wukong_PBData_ReadWriter_GUI.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public List<(string, DataFile, DataItem)> _GlobalSearchCache = new();
        public DispatcherTimer _SearchTimer;
        public string version = "V1.3.0";

        public MainWindow()
        {
            InitializeComponent();
            _SearchTimer = new DispatcherTimer();
            _SearchTimer.Interval = TimeSpan.FromMilliseconds(500); // 设置延迟时间
            _SearchTimer.Tick += SearchTimer_Tick;
            Title = "黑猴配表编辑器" + version;
        }

        private void MainWindowClosed(object? sender, EventArgs e)
        {
            // 关闭子窗口
            foreach (var window in Application.Current.Windows)
            {
                if (window is Window childWindow)
                {
                    childWindow.Close();
                }
            }
        }

        private void ShowPakWindow(object sender, RoutedEventArgs e)
        {
            var window = new PakWindow();
            window.Show();
        }

        private void ShowHelpWindow(object sender, RoutedEventArgs e)
        {
            var window = new HelpWindowView();
            window.Show();
        }

        private void OpenAuthorHomeLink(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://space.bilibili.com/8729996") { UseShellExecute = true });
        }

        private void SearchTimer_Tick(object sender, EventArgs e)
        {
            _SearchTimer.Stop();

            string searchText = GlobalSearchBox.Text;

            if (SearchResultsList == null) return;

            // 清空之前的搜索结果
            SearchResultsList.Items.Clear();

            if (!string.IsNullOrWhiteSpace(searchText) && searchText != "全局搜索")
            {
                foreach (var item in _GlobalSearchCache)
                {
                    if (item.Item1.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                    {
                        ListBoxItem listItem = new ListBoxItem();
                        listItem.Content = item.Item1;
                        listItem.DataContext = new Tuple<DataFile, DataItem>(item.Item2, item.Item3);
                        listItem.MouseDoubleClick += OpenGlobalSearchItem;
                        SearchResultsList.Items.Add(listItem);
                    }
                }

                // 展开搜索结果
                SearchResultsExpander.Visibility = Visibility.Visible;
                SearchResultsExpander.IsExpanded = true;
                this.Height = 920;
            }
            else
            {
                // 折叠搜索结果
                SearchResultsExpander.Visibility = Visibility.Collapsed;
                this.Height = 720;
            }
        }

        private void OpenGlobalSearchItem(object sender, MouseButtonEventArgs e)
        {
            // var listBoxItem = sender as ListBoxItem;
            // if (listBoxItem != null)
            // {
            //     var data = listBoxItem.DataContext as Tuple<DataFile, DataItem>;
            //     if (data != null)
            //     {
            //         if (data.Item1 != null)
            //         {
            //             if (_CurrentOpenFile != null && _CurrentOpenFile._IsDirty)
            //             {
            //                 MessageBoxResult result = MessageBox.Show("切换data文件，当前修改将被还原", "确认",
            //                     MessageBoxButton.YesNo, MessageBoxImage.Question);
            //
            //                 // 根据用户的选择执行相应的逻辑
            //                 if (result == MessageBoxResult.Yes)
            //                 {
            //                     _CurrentOpenFile._IsDirty = false;
            //                     OpenFile(data.Item1);
            //
            //                     if (data.Item2 != null)
            //                     {
            //                         data.Item2.LoadData();
            //                         RefreshDataItemList(data.Item2._DataPropertyItems);
            //
            //                         foreach (var item2 in data.Item1.DataItemList)
            //                         {
            //                             if (item2._ID == data.Item2._ID)
            //                             {
            //                                 DataItemList.ScrollIntoView(item2._ListBoxItem);
            //                                 DataItemList.SelectedItem = item2._ListBoxItem;
            //                                 break;
            //                             }
            //                         }
            //                     }
            //                 }
            //             }
            //             else
            //             {
            //                 DataFile file = listBoxItem.DataContext as DataFile;
            //                 OpenFile(data.Item1);
            //
            //                 if (data.Item2 != null)
            //                 {
            //                     foreach (var item2 in data.Item1.DataItemList)
            //                     {
            //                         if (item2._ID == data.Item2._ID)
            //                         {
            //                             item2.LoadData();
            //                             RefreshDataItemList(item2._DataPropertyItems);
            //                             DataItemList.ScrollIntoView(item2._ListBoxItem);
            //                             DataItemList.SelectedItem = item2._ListBoxItem;
            //                             break;
            //                         }
            //                     }
            //                 }
            //             }
            //
            //             FileList.ScrollIntoView(data.Item1._ListBoxItem);
            //             FileList.SelectedItem = data.Item1._ListBoxItem;
            //         }
            //     }
            // }
        }

        private void CloseButtonClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}