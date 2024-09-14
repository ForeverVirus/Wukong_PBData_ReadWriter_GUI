﻿using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Google.Protobuf;
using Wukong_PBData_ReadWriter_GUI.Models;
using Wukong_PBData_ReadWriter_GUI.ViewModels;
using Application = System.Windows.Application;
using Brushes = System.Windows.Media.Brushes;
using Button = System.Windows.Controls.Button;
using ComboBox = System.Windows.Controls.ComboBox;
using DataFormats = System.Windows.DataFormats;
using DragEventArgs = System.Windows.DragEventArgs;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using Image = System.Windows.Controls.Image;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using KeyEventHandler = System.Windows.Input.KeyEventHandler;
using Label = System.Windows.Controls.Label;
using MessageBox = System.Windows.MessageBox;
using Orientation = System.Windows.Controls.Orientation;
using Point = System.Windows.Point;
using TextBox = System.Windows.Controls.TextBox;
using ToolTip = System.Windows.Controls.ToolTip;

namespace Wukong_PBData_ReadWriter_GUI.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public List<DataFile> _DataFiles = new List<DataFile>();
        public DataFile _CurrentOpenFile = null;
        public List<(string, DataFile, DataItem)> _GlobalSearchCache = new List<(string, DataFile, DataItem)>();
        public DispatcherTimer _SearchTimer;
        public string _CurrentOpenFolder = "";
        public string version = "V1.3.0";
        public MergeWindow _MergeWindow;

        public MainWindow()
        {
            DataFileHelper.DescriptionConfig = DataFileHelper.ImportDescriptionConfig("DefaultDescConfig.json");
            _SearchTimer = new DispatcherTimer();
            _SearchTimer.Interval = TimeSpan.FromMilliseconds(500); // 设置延迟时间
            _SearchTimer.Tick += SearchTimer_Tick;
            this.Title = "黑猴配表编辑器" + version;
        }

        private void CloseAllOtherWindow(bool isClearDataGrid = true)
        {
            // WindowCollection windows = Application.Current.Windows;
            // foreach (var win in windows)
            // {
            //     if (win == null)
            //         continue;
            //
            //     if (win.GetType() != typeof(MainWindow))
            //     {
            //         (win as Window).Close();
            //     }
            // }
            //
            // if (isClearDataGrid)
            // {
            //     DataGrid.RowDefinitions.Clear();
            //     DataGrid.Children.Clear();
            // }
        }

        private void ImportDescription(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.AddExtension = true;
            dialog.Filter = "Json|*.json";
            dialog.Title = "导入备注配置";
            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
            var newDict = DataFileHelper.ImportDescriptionConfig(dialog.FileName);
            foreach (var kvp in newDict)
            {
                DataFileHelper.DescriptionConfig[kvp.Key] = kvp.Value;
            }
        }

        private void CreatePak(object sender, RoutedEventArgs e)
        {
            Window window = new Window();
            window.Title = "生成PAK";
            window.Width = 600;
            window.Height = 400;
            window.AllowDrop = true;
            window.Drop += Window_Drop;
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            window.Show();

            Grid grid = new Grid();
            window.Content = grid;

            TextBlock textBlock = new TextBlock();
            textBlock.Text = "拖拽要生成Pak的Data文件夹到此处";
            textBlock.HorizontalAlignment = HorizontalAlignment.Center;
            textBlock.VerticalAlignment = VerticalAlignment.Center;
            grid.Children.Add(textBlock);
        }

        private void UncompressPak(object sender, RoutedEventArgs e)
        {
            Window window = new Window();
            window.Title = "解包PAK";
            window.Width = 600;
            window.Height = 400;
            window.AllowDrop = true;
            window.Drop += Window_Drop_Uncompress;
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            window.Show();

            Grid grid = new Grid();
            window.Content = grid;

            TextBlock textBlock = new TextBlock();
            textBlock.Text = "拖拽要解包的Pak文件夹到此处";
            textBlock.HorizontalAlignment = HorizontalAlignment.Center;
            textBlock.VerticalAlignment = VerticalAlignment.Center;
            grid.Children.Add(textBlock);
        }

        private void Window_Drop_Uncompress(object sender, DragEventArgs e)
        {
            // 检查拖拽的数据是否是文件夹
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // 获取拖拽的文件路径
                string[] draggedItems = (string[])e.Data.GetData(DataFormats.FileDrop);


                RunBatFileWithFolder(@"ref\\make_pak_uncompressed.bat", draggedItems[0]);
            }
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            // 检查拖拽的数据是否是文件夹
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // 获取拖拽的文件路径
                string[] draggedItems = (string[])e.Data.GetData(DataFormats.FileDrop);

                // 确认用户拖拽的是文件夹（而非文件）
                if (Directory.Exists(draggedItems[0]))
                {
                    string folderPath = draggedItems[0];
                    RunBatFileWithFolder(@"ref\\make_pak_compressed.bat", folderPath);
                }
                else
                {
                    MessageBox.Show("请拖拽一个文件夹。");
                }
            }
        }

        private void RunBatFileWithFolder(string batPath, string folderPath)
        {
            // 设置 .bat 文件的路径
            //string batFilePath = @"ref\\make_pak_compressed.bat";

            // 创建一个新的 ProcessStartInfo 对象
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = batPath,
                Arguments = $"\"{folderPath}\"", // 将文件夹路径作为参数传递
                UseShellExecute = false, // 设置为 false 以便能够重定向输入/输出
                CreateNoWindow = true, // 如果你不想显示命令提示符窗口，设置为 true
                RedirectStandardOutput = true, // 如果你需要捕获输出
                RedirectStandardError = true // 捕获错误信息
            };

            // 启动进程
            using (Process process = new Process())
            {
                process.StartInfo = psi;
                process.Start();

                // 等待进程完成
                process.WaitForExit();

                // 获取标准输出
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                // 显示输出或错误信息
                MessageBox.Show($"Output: {output}\nError: {error}");
            }
        }

        private void CreateUnpackWindow(object sender, RoutedEventArgs e)
        {
            if (_MergeWindow == null || !_MergeWindow.IsVisible)
            {
                _MergeWindow = new MergeWindow();
                _MergeWindow.Show();
            }
            else
            {
                _MergeWindow.Activate();
            }
        }

        private void ExportDescription(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.AddExtension = true;
            dialog.Filter = "Json|*.json";
            dialog.Title = "导出备注配置";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                DataFileHelper.ExportDescriptionConfig(DataFileHelper.DescriptionConfig, dialog.FileName);
            }
        }

        private void OpenDataFolder(object sender, RoutedEventArgs e)
        {
            //选择文件夹,并返回选择的文件夹路径，FolderBrowserDialog是一个选择文件夹的对话框
            var dialog = new FolderBrowserDialog();
            dialog.Description = "请选择Data数据文件夹";
            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
            ((MainWindowViewModel)DataContext).ChangeOpenFolder(dialog.SelectedPath);
            // RefreshFolderFile(dialog.SelectedPath);

            // _GlobalSearchCache = Exporter.GlobalSearchCache(_DataFiles);
            //_DescriptionConfig = Exporter.GenerateFirstDescConfig(_DataFiles);
            // CloseAllOtherWindow();
            // _CurrentOpenFile = null;
        }

        // private void RefreshFolderFile(string dir)
        // {
        //     //将选择的文件夹路径显示在文本框中
        //     _CurrentOpenFolder = dir;
        //     List<string> fileNames = new List<string>();
        //     List<string> filePaths = new List<string>();
        //     DataFileHelper.Director(dir + "\\", fileNames, filePaths);
        //
        //     _DataFiles.Clear();
        //     int index = 0;
        //     foreach (var item in fileNames)
        //     {
        //         var isValid = DataFileHelper.GetIsValidFile(item, filePaths[index]);
        //         if (!isValid)
        //         {
        //             index++;
        //             continue;
        //         }
        //
        //         DataFile file = new DataFile();
        //         file._FileName = item;
        //         file._FilePath = filePaths[index];
        //         _DataFiles.Add(file);
        //         index++;
        //     }
        //
        //     //把_DataFiles绑定到FileList上并自动生成 ListBoxItem, 每个Item显示FileName 并且对应有一个打开按钮
        //     RefreshDataFile(_DataFiles);
        // }


        // private void RefreshDataFile(List<DataFile> files)
        // {
        //     if (FileList == null) return;
        //     FileList.Items.Clear();
        //     foreach (var item in files)
        //     {
        //         if (!item._IsShow) continue;
        //
        //         ListBoxItem listBoxItem = new ListBoxItem();
        //         listBoxItem.Content = item._FileName;
        //         listBoxItem.MouseDoubleClick += new MouseButtonEventHandler(OpenDataFile);
        //         listBoxItem.DataContext = item;
        //         item._ListBoxItem = listBoxItem;
        //
        //         listBoxItem.ToolTip = item._Desc;
        //         if (!string.IsNullOrEmpty(item._Desc))
        //         {
        //             listBoxItem.Foreground = new SolidColorBrush(Colors.Blue);
        //         }
        //
        //         listBoxItem.ContextMenu = new ContextMenu();
        //         MenuItem menuItem = new MenuItem();
        //         menuItem.Header = "备注";
        //         string descKey = item._FileName;
        //         Action descSuccessAction = () => { RefreshDataFile(files); };
        //         menuItem.DataContext = new Tuple<string, Action>(descKey, descSuccessAction);
        //         menuItem.Click += OpenDescriptionWindow;
        //         listBoxItem.ContextMenu.Items.Add(menuItem);
        //
        //         MenuItem topMenuItem = new MenuItem();
        //         topMenuItem.Header = "置顶";
        //         topMenuItem.DataContext = item;
        //         topMenuItem.Click += SetTopFile;
        //         listBoxItem.ContextMenu.Items.Add(topMenuItem);
        //
        //         MenuItem openFolderMenuItem = new MenuItem();
        //         openFolderMenuItem.Header = "打开所在文件夹";
        //         openFolderMenuItem.DataContext = item._FilePath;
        //         openFolderMenuItem.Click += OpenContainingFolder_Click;
        //         listBoxItem.ContextMenu.Items.Add(openFolderMenuItem);
        //
        //         FileList.Items.Add(listBoxItem);
        //     }
        //
        //     RefreshTopFileList();
        // }

        // private void SetTopFile(object sender, RoutedEventArgs e)
        // {
        //     var menuItem = sender as MenuItem;
        //     if (menuItem == null) return;
        //
        //     var file = menuItem.DataContext as DataFile;
        //
        //     if (file == null) return;
        //
        //     file._IsTop = true;
        //
        //     RefreshTopFileList();
        // }

        // private void CancelTopFile(object sender, RoutedEventArgs e)
        // {
        //     var menuItem = sender as MenuItem;
        //     if (menuItem == null) return;
        //
        //     var file = menuItem.DataContext as DataFile;
        //
        //     if (file == null) return;
        //
        //     file._IsTop = false;
        //
        //     RefreshTopFileList();
        // }

        // private void RefreshTopFileList()
        // {
        //     TopFileList.Items.Clear();
        //     bool hasTop = false;
        //     foreach (var file in _DataFiles)
        //     {
        //         if (file._IsTop)
        //         {
        //             ListBoxItem listBoxItem = new ListBoxItem();
        //             listBoxItem.Content = file._FileName;
        //             listBoxItem.MouseDoubleClick += new MouseButtonEventHandler(OpenDataFile);
        //             listBoxItem.DataContext = file;
        //
        //             listBoxItem.ToolTip = new ToolTip()
        //             {
        //                 Content = new TextBlock
        //                 {
        //                     Text = file._Desc,
        //                     TextWrapping = TextWrapping.Wrap
        //                 }
        //             };
        //
        //             if (!string.IsNullOrEmpty(file._Desc))
        //             {
        //                 listBoxItem.Foreground = new SolidColorBrush(Colors.Blue);
        //             }
        //
        //             listBoxItem.ContextMenu = new ContextMenu();
        //             MenuItem topMenuItem = new MenuItem();
        //             topMenuItem.Header = "取消置顶";
        //             topMenuItem.DataContext = file;
        //             topMenuItem.Click += CancelTopFile;
        //             listBoxItem.ContextMenu.Items.Add(topMenuItem);
        //
        //             MenuItem openFolderMenuItem = new MenuItem();
        //             openFolderMenuItem.Header = "打开所在文件夹";
        //             openFolderMenuItem.DataContext = file._FilePath;
        //             openFolderMenuItem.Click += OpenContainingFolder_Click;
        //             listBoxItem.ContextMenu.Items.Add(openFolderMenuItem);
        //
        //             TopFileList.Items.Add(listBoxItem);
        //             hasTop = true;
        //         }
        //     }
        //
        //     if (!hasTop)
        //     {
        //         TextBlock topFileText = new TextBlock();
        //         topFileText.HorizontalAlignment = HorizontalAlignment.Center;
        //         TopFileList.VerticalAlignment = VerticalAlignment.Center;
        //         topFileText.Text = "选中对应条目的右键置顶此区域";
        //         TopFileList.Items.Add(topFileText);
        //         //TopFileList.Visibility = Visibility.Hidden;
        //         //Grid.SetRow(TopFileList, 2);
        //         //Grid.SetRow(FileList, 1);
        //     }
        // }

        private void OpenContainingFolder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.DataContext is string filePath)
            {
                string folderPath = Path.GetDirectoryName(filePath);
                if (Directory.Exists(folderPath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"/select,\"{filePath}\"",
                        UseShellExecute = true
                    });
                }
                else
                {
                    MessageBox.Show("文件夹不存在", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveDataFile(object sender, RoutedEventArgs e)
        {
            // var pakPath = _CurrentOpenFile._FilePath;
            //
            // //rename pakPath file if exist
            // if (File.Exists(pakPath))
            // {
            //     var dir = Path.GetDirectoryName(pakPath);
            //     var fileName = Path.GetFileNameWithoutExtension(pakPath);
            //     var extension = Path.GetExtension(pakPath);
            //
            //     var newPath = dir + "\\" + fileName + ".bak" + extension;
            //     if (File.Exists(newPath))
            //         File.Delete(newPath);
            //
            //     File.Move(pakPath, newPath);
            // }
            //
            // Exporter.SaveDataFile(pakPath, _CurrentOpenFile);
            //
            // RefreshFolderFile(_CurrentOpenFolder);
            //
            // //_GlobalSearchCache = Exporter.GlobalSearchCache(_DataFiles);
            // DataItemList.Items.Clear();
            // DataGrid.Children.Clear();
            // CloseAllOtherWindow();
            // _CurrentOpenFile = null;
        }

        private void SaveAsNewDataFile(object sender, RoutedEventArgs e)
        {
            // FolderBrowserDialog dialog = new FolderBrowserDialog();
            // dialog.Description = "请选择要保存Data数据的文件夹";
            // if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            // {
            //     string dir = dialog.SelectedPath;
            //
            //     var pakPath = _CurrentOpenFile._FilePath;
            //
            //     var b1Index = _CurrentOpenFile._FilePath.IndexOf("b1");
            //     if (b1Index != -1)
            //         pakPath = _CurrentOpenFile._FilePath.Substring(b1Index,
            //             _CurrentOpenFile._FilePath.Length - b1Index);
            //
            //     var outPath = Path.Combine(dir, pakPath);
            //
            //     DataFileHelper.SaveDataFile(outPath, _CurrentOpenFile);
            // }
        }

        private void OpenDataFile(object sender, MouseButtonEventArgs e)
        {
            // ListBoxItem listBoxItem = sender as ListBoxItem;
            // if (listBoxItem != null)
            // {
            //     if (_CurrentOpenFile != null && _CurrentOpenFile._IsDirty)
            //     {
            //         MessageBoxResult result = MessageBox.Show("切换data文件，当前修改将被还原", "确认",
            //             MessageBoxButton.YesNo, MessageBoxImage.Question);
            //
            //         // 根据用户的选择执行相应的逻辑
            //         if (result == MessageBoxResult.Yes)
            //         {
            //             _CurrentOpenFile._IsDirty = false;
            //             DataFile file = listBoxItem.DataContext as DataFile;
            //             OpenFile(file);
            //         }
            //     }
            //     else
            //     {
            //         DataFile file = listBoxItem.DataContext as DataFile;
            //         OpenFile(file);
            //     }
            // }
        }

        // private void OpenFile(DataFile file)
        // {
        //     if (file != null)
        //     {
        //         file.LoadData();
        //
        //         if (file.DataItemList != null && file.DataItemList.Count > 0)
        //         {
        //             RefreshFileDataItemList(file.DataItemList);
        //         }
        //         else
        //         {
        //             if (DataItemList != null)
        //             {
        //                 DataItemList.Items.Clear();
        //             }
        //         }
        //
        //         _CurrentOpenFile = file;
        //         var b1Index = file._FilePath.IndexOf("b1");
        //         var pakPath = file._FilePath;
        //         if (b1Index != -1)
        //             pakPath = file._FilePath.Substring(b1Index, file._FilePath.Length - b1Index);
        //         DataFilePath.Text = $"配置数据({pakPath})";
        //         CloseAllOtherWindow();
        //     }
        // }

        private void RefreshFileDataItemList(List<DataItem> list)
        {
            if (DataItemList == null) return;
            DataItemList.Items.Clear();
            foreach (var item in list)
            {
                if (!item._IsShow) continue;

                ListBoxItem listItem = new ListBoxItem();
                listItem.Content = item.Id + "  " + item.Desc;
                listItem.DataContext = item;
                item._ListBoxItem = listItem;
                listItem.MouseDoubleClick += new MouseButtonEventHandler(OpenDataItem);
                listItem.ContextMenu = new ContextMenu();
                MenuItem menuItem = new MenuItem();
                menuItem.Header = "备注";
                string descKey = item._File.FileData.GetType().Name + "_" + item.Id;
                Action descSuccessAction = () => { RefreshFileDataItemList(list); };
                menuItem.DataContext = new Tuple<string, Action>(descKey, descSuccessAction);
                menuItem.Click += OpenDescriptionWindow;
                listItem.ContextMenu.Items.Add(menuItem);

                MenuItem cloneMenuItem = new MenuItem();
                cloneMenuItem.Header = "克隆";
                cloneMenuItem.DataContext = item;
                cloneMenuItem.Click += CloneMenuItem_Click;
                listItem.ContextMenu.Items.Add(cloneMenuItem);

                MenuItem delMenuItem = new MenuItem();
                delMenuItem.Header = "删除";
                delMenuItem.DataContext = item;
                delMenuItem.Click += DelMenuItem_Click;
                listItem.ContextMenu.Items.Add(delMenuItem);

                DataItemList.Items.Add(listItem);
            }
        }

        private void CloneMenuItem_Click(object sender, EventArgs e)
        {
            // var menuItem = sender as MenuItem;
            // if (menuItem == null) return;
            //
            // var dataItem = menuItem.DataContext as DataItem;
            //
            // if (dataItem == null) return;
            //
            // if (_CurrentOpenFile == null)
            //     return;
            //
            // var bytes = dataItem._Data.ToByteArray();
            //
            // var list = _CurrentOpenFile._ListPropertyInfo.GetValue(_CurrentOpenFile.FileData, null) as IList;
            //
            // if (_CurrentOpenFile.DataItemList != null)
            // {
            //     var newItemType = list.GetType().GetGenericArguments()[0];
            //     if (newItemType != null)
            //     {
            //         IMessage newItem = null;
            //
            //         var parser = newItemType.GetProperty("Parser", BindingFlags.Static | BindingFlags.Public);
            //         if (parser != null)
            //         {
            //             try
            //             {
            //                 MessageParser parserValue = parser.GetMethod.Invoke(null, null) as MessageParser;
            //                 var message = parserValue.ParseFrom(bytes);
            //                 if (message != null)
            //                 {
            //                     newItem = message;
            //                 }
            //             }
            //             catch (Exception ex)
            //             {
            //                 MessageBox.Show(ex.ToString());
            //             }
            //         }
            //
            //         if (newItem == null)
            //             return;
            //
            //
            //         var property = newItemType.GetProperty("Id");
            //         if (property == null)
            //         {
            //             property = newItemType.GetProperty("ID");
            //         }
            //
            //         if (property == null)
            //             return;

                    // DataItem newDataItem = new DataItem();
                    // newDataItem.Id = _CurrentOpenFile.GetNewID();
                    // property.SetValue(newItem, newDataItem.Id, null);
                    // _CurrentOpenFile._IDList.Add(newDataItem.Id);
                    // newDataItem._Data = newItem;
                    // newDataItem._File = _CurrentOpenFile;
                    // _CurrentOpenFile.DataItemList.Add(newDataItem);
                    //
                    // list.Add(newItem);

                    // DataFileHelper.DescriptionConfig.Add(
                    //     newDataItem._File.FileData.GetType().Name + "_" + newDataItem.Id,
                    //     dataItem.Desc);

                    // RefreshFileDataItemList(_CurrentOpenFile.DataItemList);
                // }
            // }
        }

        private void DelMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // var menuItem = sender as MenuItem;
            // if (menuItem == null) return;
            //
            // var dataItem = menuItem.DataContext as DataItem;
            //
            // if (dataItem == null) return;
            //
            // var list = _CurrentOpenFile._ListPropertyInfo.GetValue(_CurrentOpenFile.FileData, null) as IList;
            //
            // if (list == null || list.Count <= 0)
            //     return;
            //
            // list.Remove(dataItem._Data);
            //
            // dataItem._File.DataItemList.Remove(dataItem);
            //
            // RefreshFileDataItemList(_CurrentOpenFile.DataItemList);
        }

        private void OpenDescriptionWindow(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem == null) return;

            var data = menuItem.DataContext as Tuple<string, Action>;

            Window window = new Window();
            window.Title = "备注";
            window.Width = 600;
            window.Height = 150;
            // 获取鼠标相对于主窗口的位置
            Point mousePosition = Mouse.GetPosition(this);

            // 转换为屏幕坐标
            Point screenPosition = PointToScreen(mousePosition);
            window.Left = screenPosition.X;
            window.Top = screenPosition.Y;
            window.Show();

            Grid grid = new Grid();
            window.Content = grid;
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

            Label label = new Label();
            label.Content = "备注";
            label.HorizontalAlignment = HorizontalAlignment.Left;
            label.VerticalAlignment = VerticalAlignment.Top;
            label.Margin = new Thickness(10, 10, 0, 0);
            Grid.SetRow(label, 0);
            Grid.SetColumn(label, 1);
            grid.Children.Add(label);

            TextBox textBox = new TextBox();
            textBox.HorizontalAlignment = HorizontalAlignment.Left;
            textBox.VerticalAlignment = VerticalAlignment.Top;
            textBox.Margin = new Thickness(100, 10, 0, 0);
            textBox.AcceptsReturn = true;
            textBox.TextWrapping = TextWrapping.Wrap;
            string desc = "这里写备注";
            DataFileHelper.DescriptionConfig.TryGetValue(data.Item1, out desc);
            textBox.Text = desc;
            Grid.SetRow(textBox, 0);
            Grid.SetColumn(textBox, 1);
            grid.Children.Add(textBox);

            Button button = new Button();
            button.HorizontalAlignment = HorizontalAlignment.Left;
            button.VerticalAlignment = VerticalAlignment.Top;
            button.Margin = new Thickness(40, 40, 0, 0);
            button.Content = "确定";
            button.Click += (sender, e) =>
            {
                if (DataFileHelper.DescriptionConfig.ContainsKey(data.Item1))
                {
                    DataFileHelper.DescriptionConfig.Remove(data.Item1);
                }

                DataFileHelper.DescriptionConfig.TryAdd(data.Item1, textBox.Text);
                data.Item2?.Invoke();
                window.Close();
            };
            Grid.SetRow(button, 1);
            Grid.SetColumn(button, 1);
            grid.Children.Add(button);
        }

        private void OpenDataItem(object sender, MouseButtonEventArgs e)
        {
            // ListBoxItem listBoxItem = sender as ListBoxItem;
            // if (listBoxItem != null)
            // {
            //     var data = listBoxItem.DataContext as DataItem;
            //     if (data != null)
            //     {
            //         if (data._Data == null)
            //         {
            //             return;
            //         }
            //
            //         RefreshDataItemList(data._DataPropertyItems);
            //
            //         CloseAllOtherWindow(false);
            //     }
            // }
        }

        private void RefreshDataItemList(List<DataPropertyItem> propertyItemList)
        {
            // DataGrid.RowDefinitions.Clear();
            // DataGrid.Children.Clear();
            // DataGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            // int rowIndex = 0;
            // foreach (var item in propertyItemList)
            // {
            //     Label label = new Label();
            //     label.Content = $"{item._PropertyName}";
            //
            //     label.ToolTip = new ToolTip()
            //     {
            //         Content = new TextBlock
            //         {
            //             Text = item._PropertyDesc,
            //             TextWrapping = TextWrapping.Wrap
            //         }
            //     };
            //     Grid.SetRow(label, rowIndex);
            //     Grid.SetColumn(label, 0);
            //     label.HorizontalAlignment = HorizontalAlignment.Left;
            //     label.VerticalAlignment = VerticalAlignment.Top;
            //     label.Margin = new Thickness(0, 10 + rowIndex * 30, 0, 0);
            //
            //
            //     label.ContextMenu = new ContextMenu();
            //     MenuItem menuItem = new MenuItem();
            //     menuItem.Header = "备注";
            //
            //     string descKey = item._DataItem._File.FileData.GetType().Name + "_" + item._PropertyName;
            //
            //     if (DataFileHelper.DescriptionConfig.ContainsKey(descKey))
            //     {
            //         label.Foreground = new SolidColorBrush(Colors.Blue);
            //     }
            //
            //     Action descSuccessAction = () => { RefreshDataItemList(propertyItemList); };
            //     menuItem.DataContext = new Tuple<string, Action>(descKey, descSuccessAction);
            //     menuItem.Click += OpenDescriptionWindow;
            //     label.ContextMenu.Items.Add(menuItem);
            //
            //     DataGrid.Children.Add(label);
            //
            //     var valueType = item._PropertyInfo.PropertyType;
            //     ProcessPropertyType(valueType, item, rowIndex, DataGrid, 300);
            //     rowIndex++;
            // }
        }

        private void ProcessPropertyType(Type valueType, DataPropertyItem item, int rowIndex, Grid curGrid, int left)
        {
            if (valueType == typeof(int) || valueType == typeof(float) || valueType == typeof(long) ||
                valueType == typeof(double))
            {
                TextBox numberTextBox = new TextBox();
                numberTextBox.PreviewTextInput += new TextCompositionEventHandler(NumericTextBox_PreviewTextInput);
                numberTextBox.PreviewKeyDown += new KeyEventHandler(NumericTextBox_PreviewKeyDown);
                numberTextBox.LostFocus += new RoutedEventHandler(NumericTextBox_LostFocus);
                numberTextBox.Text = item._PropertyInfo.GetValue(item._BelongData).ToString();
                numberTextBox.HorizontalAlignment = HorizontalAlignment.Left;
                numberTextBox.VerticalAlignment = VerticalAlignment.Top;
                numberTextBox.Margin = new Thickness(0, 10 + rowIndex * 30, 0, 0);
                numberTextBox.DataContext = item;
                numberTextBox.TextChanged += NumberTextBox_TextChanged;
                Grid.SetRow(numberTextBox, rowIndex);
                Grid.SetColumn(numberTextBox, 1);
                curGrid.Children.Add(numberTextBox);
            }
            else if (valueType == typeof(string))
            {
                TextBox stringTextBox = new TextBox();
                stringTextBox.Text = item._PropertyInfo.GetValue(item._BelongData).ToString();
                stringTextBox.HorizontalAlignment = HorizontalAlignment.Left;
                stringTextBox.VerticalAlignment = VerticalAlignment.Top;
                stringTextBox.Margin = new Thickness(0, 10 + rowIndex * 30, 0, 0);
                stringTextBox.DataContext = item;
                stringTextBox.TextChanged += StringTextBox_TextChanged;
                Grid.SetRow(stringTextBox, rowIndex);
                Grid.SetColumn(stringTextBox, 1);

                curGrid.Children.Add(stringTextBox);
            }
            else if (valueType.IsEnum)
            {
                ComboBox comboBox = new ComboBox();
                comboBox.HorizontalAlignment = HorizontalAlignment.Left;
                comboBox.VerticalAlignment = VerticalAlignment.Top;
                comboBox.Margin = new Thickness(0, 10 + rowIndex * 30, 0, 0);
                comboBox.ItemsSource = Enum.GetValues(valueType);
                comboBox.SelectedItem = item._PropertyInfo.GetValue(item._BelongData);
                comboBox.DataContext = item;
                comboBox.SelectionChanged += ComboBox_SelectionChanged;
                Grid.SetRow(comboBox, rowIndex);
                Grid.SetColumn(comboBox, 1);
                curGrid.Children.Add(comboBox);
            }
            else if (typeof(IMessage).IsAssignableFrom(valueType))
            {
                var button = new Button();
                button.Content = "打开";
                button.HorizontalAlignment = HorizontalAlignment.Left;
                button.VerticalAlignment = VerticalAlignment.Top;
                button.Margin = new Thickness(0, 10 + rowIndex * 30, 0, 0);
                button.Click += new RoutedEventHandler(OpenNestedData);
                var dataCtx = item._PropertyInfo.GetValue(item._BelongData);
                if (dataCtx == null)
                    dataCtx = Activator.CreateInstance(item._PropertyInfo.PropertyType);
                button.DataContext = dataCtx;
                Grid.SetRow(button, rowIndex);
                Grid.SetColumn(button, 1);
                curGrid.Children.Add(button);
            }
            else if (typeof(IList).IsAssignableFrom(valueType))
            {
                var button = new Button();
                button.Content = "打开";
                button.HorizontalAlignment = HorizontalAlignment.Left;
                button.VerticalAlignment = VerticalAlignment.Top;
                button.Margin = new Thickness(0, 10 + rowIndex * 30, 0, 0);
                button.Click += new RoutedEventHandler(OpenListData);
                button.DataContext = item._PropertyInfo.GetValue(item._BelongData);
                Grid.SetRow(button, rowIndex);
                Grid.SetColumn(button, 1);
                curGrid.Children.Add(button);
            }
        }

        private void NumberTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                var item = textBox.DataContext as DataPropertyItem;


                if (item._PropertyInfo.PropertyType == typeof(int))
                {
                    if (int.TryParse(textBox.Text, out var value))
                        OnValueChanged(item, value);
                }
                else if (item._PropertyInfo.PropertyType == typeof(long))
                {
                    if (long.TryParse(textBox.Text, out var value))
                        OnValueChanged(item, value);
                }
                else if (item._PropertyInfo.PropertyType == typeof(float))
                {
                    if (float.TryParse(textBox.Text, out var value))
                        OnValueChanged(item, value);
                }
                else if (item._PropertyInfo.PropertyType == typeof(double))
                {
                    if (double.TryParse(textBox.Text, out var value))
                        OnValueChanged(item, value);
                }
            }
        }

        private void StringTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                var item = textBox.DataContext as DataPropertyItem;
                OnValueChanged(item, textBox.Text);
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox != null)
            {
                var item = comboBox.DataContext as DataPropertyItem;
                OnValueChanged(item, comboBox.SelectedValue);
            }
        }

        private void OnValueChanged(DataPropertyItem item, object value)
        {
            if (item != null)
            {
                item._PropertyInfo.SetValue(item._BelongData, value);
                _CurrentOpenFile._IsDirty = true;
            }
        }

        private void OpenListData(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                var data = button.DataContext as IList;
                if (data != null)
                {
                    Window window = new Window();
                    var ListType = data.GetType().GetGenericArguments()[0].Name;
                    window.Title = $"Repeated<{ListType}>";
                    window.Width = 800;
                    window.Height = 600;

                    window.WindowStartupLocation = WindowStartupLocation.CenterScreen;

                    window.Show();
                    //window 增加一个Grid,与parent Grid一样
                    Grid grid = new Grid();
                    window.Content = grid;
                    grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
                    grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
                    grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

                    //var list = data._PropertyInfo.GetValue(data._BelongData) as IList;

                    RefreshList(data, ListType, grid);
                }
            }
        }

        private void RefreshList(IList data, string ListType, Grid grid)
        {
            grid.Children.Clear();
            int rowIndex = 0;
            foreach (var item in data)
            {
                Label groupLabel = new Label();
                groupLabel.Content = ListType + "-" + rowIndex;
                groupLabel.HorizontalAlignment = HorizontalAlignment.Left;
                groupLabel.VerticalAlignment = VerticalAlignment.Top;
                groupLabel.Margin = new Thickness(10, 10 + rowIndex * 30, 0, 0);
                groupLabel.ContextMenu = new ContextMenu();
                //MenuItem descItem = new MenuItem();
                //descItem.Header = "备注";
                //descItem.Click += (sender, e) =>
                //{

                //};
                MenuItem delMenu = new MenuItem();
                delMenu.Header = "删除";
                delMenu.DataContext = new Tuple<int, IList, Grid>(rowIndex, data, grid);
                delMenu.Click += DelMenu_Click;
                groupLabel.ContextMenu.Items.Add(delMenu);

                grid.Children.Add(groupLabel);

                var valueType = item.GetType();
                if (valueType == typeof(int) || valueType == typeof(float) || valueType == typeof(long) ||
                    valueType == typeof(double))
                {
                    TextBox numberTextBox = new TextBox();
                    numberTextBox.PreviewTextInput += new TextCompositionEventHandler(NumericTextBox_PreviewTextInput);
                    numberTextBox.PreviewKeyDown +=
                        new KeyEventHandler(NumericTextBox_PreviewKeyDown);
                    numberTextBox.LostFocus += new RoutedEventHandler(NumericTextBox_LostFocus);
                    numberTextBox.Text = item.ToString();
                    numberTextBox.HorizontalAlignment = HorizontalAlignment.Left;
                    numberTextBox.VerticalAlignment = VerticalAlignment.Top;
                    numberTextBox.Margin = new Thickness(0, 10 + rowIndex * 30, 0, 0);
                    numberTextBox.DataContext = new Tuple<int, IList, Type>(rowIndex, data, valueType);
                    numberTextBox.TextChanged += NumberTextBox_TextChanged1;

                    Grid.SetRow(numberTextBox, rowIndex);
                    Grid.SetColumn(numberTextBox, 1);
                    grid.Children.Add(numberTextBox);
                }
                else if (valueType == typeof(string))
                {
                    TextBox stringTextBox = new TextBox();
                    stringTextBox.Text = item.ToString();
                    stringTextBox.HorizontalAlignment = HorizontalAlignment.Left;
                    stringTextBox.VerticalAlignment = VerticalAlignment.Top;
                    stringTextBox.Margin = new Thickness(0, 10 + rowIndex * 30, 0, 0);
                    stringTextBox.DataContext = new Tuple<int, IList, Type>(rowIndex, data, valueType);
                    stringTextBox.TextChanged += StringTextBox_TextChanged1;
                    Grid.SetRow(stringTextBox, rowIndex);
                    Grid.SetColumn(stringTextBox, 1);

                    grid.Children.Add(stringTextBox);
                }
                else if (valueType.IsEnum)
                {
                    ComboBox comboBox = new ComboBox();
                    comboBox.HorizontalAlignment = HorizontalAlignment.Left;
                    comboBox.VerticalAlignment = VerticalAlignment.Top;
                    comboBox.Margin = new Thickness(0, 10 + rowIndex * 30, 0, 0);
                    comboBox.ItemsSource = Enum.GetValues(valueType);
                    comboBox.SelectedItem = item;
                    comboBox.DataContext = new Tuple<int, IList, Type>(rowIndex, data, valueType);
                    comboBox.SelectionChanged += ComboBox_SelectionChanged1;
                    Grid.SetRow(comboBox, rowIndex);
                    Grid.SetColumn(comboBox, 1);
                    grid.Children.Add(comboBox);
                }
                else if (typeof(IMessage).IsAssignableFrom(valueType))
                {
                    var newButton = new Button();
                    newButton.Content = "打开";
                    newButton.HorizontalAlignment = HorizontalAlignment.Left;
                    newButton.VerticalAlignment = VerticalAlignment.Top;
                    newButton.Margin = new Thickness(0, 10 + rowIndex * 30, 0, 0);
                    newButton.Click += new RoutedEventHandler(OpenNestedData);

                    newButton.DataContext = item;
                    Grid.SetRow(newButton, rowIndex);
                    Grid.SetColumn(newButton, 1);
                    grid.Children.Add(newButton);
                }
                else if (typeof(IList).IsAssignableFrom(valueType))
                {
                    var newButton = new Button();
                    newButton.Content = "打开";
                    newButton.HorizontalAlignment = HorizontalAlignment.Left;
                    newButton.VerticalAlignment = VerticalAlignment.Top;
                    newButton.Margin = new Thickness(0, 10 + rowIndex * 30, 0, 0);
                    newButton.Click += new RoutedEventHandler(OpenListData);
                    newButton.DataContext = item;
                    Grid.SetRow(newButton, rowIndex);
                    Grid.SetColumn(newButton, 1);
                    grid.Children.Add(newButton);
                }

                rowIndex++;
            }

            var addItemButton = new Button();
            addItemButton.Content = "新增";
            addItemButton.HorizontalAlignment = HorizontalAlignment.Left;
            addItemButton.VerticalAlignment = VerticalAlignment.Top;
            addItemButton.Margin = new Thickness(0, 10 + rowIndex * 30, 0, 0);
            addItemButton.DataContext = new Tuple<IList, Grid>(data, grid);
            addItemButton.Click += AddItemButton_Click;
            grid.Children.Add(addItemButton);
        }

        private void DelMenu_Click(object sender, RoutedEventArgs e)
        {
            var item = (MenuItem)sender;
            if (item == null) return;

            var data = item.DataContext as Tuple<int, IList, Grid>;
            if (data == null) return;

            var listType = data.Item2.GetType().GetGenericArguments()[0];
            data.Item2.RemoveAt(data.Item1);

            RefreshList(data.Item2, listType.Name, data.Item3);
        }

        private void AddItemButton_Click(object sender, RoutedEventArgs e)
        {
            var item = (Button)sender;
            if (item == null) return;

            var data = item.DataContext as Tuple<IList, Grid>;

            if (data == null) return;

            var listType = data.Item1.GetType().GetGenericArguments()[0];
            if (listType == null) return;
            object newItem;
            if (listType == typeof(string))
            {
                newItem = "";
            }
            else
                newItem = Activator.CreateInstance(listType);

            data.Item1.Add(newItem);

            RefreshList(data.Item1, listType.Name, data.Item2);
            _CurrentOpenFile._IsDirty = true;

            //data.Item2
        }

        private void ComboBox_SelectionChanged1(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox != null)
            {
                var data = comboBox.DataContext as Tuple<int, IList, Type>;

                OnListItemChanged(data.Item1, data.Item2, comboBox.SelectedItem);
            }
        }

        private void StringTextBox_TextChanged1(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                var data = textBox.DataContext as Tuple<int, IList, Type>;

                OnListItemChanged(data.Item1, data.Item2, textBox.Text);
            }
        }

        private void OnListItemChanged(int index, IList list, object value)
        {
            if (list != null && index >= 0 && index < list.Count)
            {
                list[index] = value;

                _CurrentOpenFile._IsDirty = true;
            }
        }

        private void NumberTextBox_TextChanged1(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                var data = textBox.DataContext as Tuple<int, IList, Type>;

                if (data.Item3 == typeof(int))
                {
                    if (int.TryParse(textBox.Text, out var value))
                        OnListItemChanged(data.Item1, data.Item2, Convert.ToInt32(textBox.Text));
                }
                else if (data.Item3 == typeof(long))
                {
                    if (long.TryParse(textBox.Text, out var value))
                        OnListItemChanged(data.Item1, data.Item2, Convert.ToInt64(textBox.Text));
                }
                else if (data.Item3 == typeof(float))
                {
                    if (float.TryParse(textBox.Text, out var value))
                        OnListItemChanged(data.Item1, data.Item2, Convert.ToSingle(textBox.Text));
                }
                else if (data.Item3 == typeof(double))
                {
                    if (double.TryParse(textBox.Text, out var value))
                        OnListItemChanged(data.Item1, data.Item2, Convert.ToDouble(textBox.Text));
                }
            }
        }

        private void OpenNestedData(object sender, RoutedEventArgs e)
        {
            // var button = sender as Button;
            // if (button != null)
            // {
            //     var data = button.DataContext as IMessage;
            //
            //     if (data == null)
            //     {
            //         data = Activator.CreateInstance(data.GetType()) as IMessage;
            //     }
            //
            //     if (data != null)
            //     {
            //         //创建一个新的窗口
            //         Window window = new Window();
            //         window.Title = data.GetType().Name;
            //         window.Width = 800;
            //         window.Height = 600;
            //         window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            //         window.Show();
            //         //window 增加一个Grid,与parent Grid一样
            //         Grid grid = new Grid();
            //         window.Content = grid;
            //         grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            //         grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            //         grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            //
            //         //获取属性
            //         var properties = data.GetType().GetProperties();
            //         int rowIndex = 0;
            //         foreach (var property in properties)
            //         {
            //             Label label = new Label();
            //             label.Content = property.Name;
            //             label.HorizontalAlignment = HorizontalAlignment.Left;
            //             label.VerticalAlignment = VerticalAlignment.Top;
            //             label.Margin = new Thickness(10, 10 + rowIndex * 30, 0, 0);
            //             grid.Children.Add(label);
            //
            //             DataPropertyItem dataPropertyItem = new DataPropertyItem();
            //             dataPropertyItem.PropertyName = property.Name;
            //             dataPropertyItem.PropertyDesc = "";
            //             dataPropertyItem._PropertyInfo = property;
            //             dataPropertyItem._BelongData = data;
            //             ProcessPropertyType(property.PropertyType, dataPropertyItem, rowIndex, grid, 200);
            //             rowIndex++;
            //         }
            //     }
            // }
        }

        private void NumericTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                var value = textBox.Text;
                if (string.IsNullOrEmpty(value))
                {
                    textBox.Text = "0";
                    return;
                }

                if (int.TryParse(value, out int intValue))
                {
                    textBox.Text = intValue.ToString();
                }
                else if (float.TryParse(value, out float floatValue))
                {
                    textBox.Text = floatValue.ToString();
                }
                else if (long.TryParse(value, out long longValue))
                {
                    textBox.Text = longValue.ToString();
                }
                else if (double.TryParse(value, out double doubleValue))
                {
                    textBox.Text = doubleValue.ToString();
                }
                else
                    textBox.Text = "0";
            }
        }

        private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // 使用正则表达式检查输入是否为有效的浮点数字符
            Regex regex = new Regex(@"^[0-9.\-+eE]$");
            e.Handled = !regex.IsMatch(e.Text);
        }

        private void NumericTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // 允许使用退格键、删除键、Tab键、箭头键等
            if (e.Key == Key.Back || e.Key == Key.Delete || e.Key == Key.Tab ||
                e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Up || e.Key == Key.Down)
            {
                e.Handled = false;
            }
            else
            {
                // 其他键处理
                e.Handled = !IsNumericKey(e.Key);
            }
        }

        private bool IsNumericKey(Key key)
        {
            // 检查按键是否为数字键或小数点、正负号、指数符号
            return (key >= Key.D0 && key <= Key.D9) ||
                   (key >= Key.NumPad0 && key <= Key.NumPad9) ||
                   key == Key.OemPeriod || key == Key.Decimal ||
                   key == Key.OemMinus || key == Key.Subtract ||
                   key == Key.OemPlus || key == Key.Add ||
                   key == Key.E || key == Key.Oem5; // Oem5 is for 'e' in some keyboards
        }

        private void AuthorMenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://space.bilibili.com/8729996") { UseShellExecute = true });
        }

        private void HelpMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Window window = new Window();
            window.Title = "帮助";
            window.Width = 800;
            window.Height = 800;
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            window.Show();

            Grid grid = new Grid();
            window.Content = grid;


            // 创建Grid作为根布局容器
            Grid rootGrid = new Grid();

            // 创建ScrollViewer来支持上下滚动
            ScrollViewer scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            // 创建StackPanel并设置其水平对齐方式为居中
            StackPanel stackPanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(10)
            };

            // 创建一个水平StackPanel来并排显示两个二维码图片
            StackPanel qrCodePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 10)
            };


            // 创建二维码图片
            Image qrCodeImage1 = new Image
            {
                Source = new BitmapImage(new Uri("pack://application:,,,/zfb.jpg")),
                Width = 150,
                Height = 150,
                Margin = new Thickness(5)
            };

            Image qrCodeImage2 = new Image
            {
                Source = new BitmapImage(new Uri("pack://application:,,,/vx.jpg")),
                Width = 150,
                Height = 150,
                Margin = new Thickness(5)
            };

            // 创建二维码文字
            TextBlock qrCodeText = new TextBlock
            {
                Text = "这里是赛博乞讨\n如果你喜欢这个工具可以赞助一下，谢谢~",
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20),
                FontSize = 18
            };


            qrCodePanel.Children.Add(qrCodeImage1);
            qrCodePanel.Children.Add(qrCodeImage2);

            string helpText = "";

            if (File.Exists("README.md"))
            {
                helpText = File.ReadAllText("README.md");
            }

            // 创建帮助文本内容
            TextBlock helpTextBlock = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Text = helpText
            };

            // 将控件添加到StackPanel
            stackPanel.Children.Add(qrCodePanel);
            stackPanel.Children.Add(qrCodeText);
            stackPanel.Children.Add(helpTextBlock);

            // 将StackPanel添加到ScrollViewer
            scrollViewer.Content = stackPanel;

            // 将ScrollViewer添加到Grid
            rootGrid.Children.Add(scrollViewer);

            // 将Grid设置为窗口的内容
            window.Content = rootGrid;
        }

        private void AddNewDataItem_Click(object sender, RoutedEventArgs e)
        {
            // if (_CurrentOpenFile == null)
            //     return;
            //
            // var list = _CurrentOpenFile._ListPropertyInfo.GetValue(_CurrentOpenFile.FileData, null) as IList;
            //
            // if (_CurrentOpenFile.DataItemList != null)
            // {
            //     var newItemType = list.GetType().GetGenericArguments()[0];
            //     if (newItemType != null)
            //     {
            //         var newItem = Activator.CreateInstance(newItemType) as IMessage;
            //
            //         if (newItem == null)
            //             return;
            //
            //         var property = newItemType.GetProperty("Id");
            //         if (property == null)
            //         {
            //             property = newItemType.GetProperty("ID");
            //         }
            //
            //         if (property == null)
            //             return;
            //
            //         DataItem dataItem = new DataItem();
            //         dataItem.Id = _CurrentOpenFile.GetNewID();
            //         property.SetValue(newItem, dataItem.Id, null);
            //         _CurrentOpenFile._IDList.Add(dataItem.Id);
            //         dataItem._Data = newItem;
            //         dataItem._File = _CurrentOpenFile;
            //         _CurrentOpenFile.DataItemList.Add(dataItem);
            //
            //         list.Add(newItem);
            //
            //         //_CurrentOpenFile._ListPropertyInfo.SetValue(_CurrentOpenFile._FileData, list, null);
            //
            //         RefreshFileDataItemList(_CurrentOpenFile.DataItemList);
            //     }
            // }
        }

        private void GlobalSearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (GlobalSearchBox.Text == "全局搜索")
            {
                GlobalSearchBox.Text = "";
                GlobalSearchBox.Foreground = Brushes.Black;
            }
        }

        private void GlobalSearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(GlobalSearchBox.Text))
            {
                GlobalSearchBox.Text = "全局搜索";
                GlobalSearchBox.Foreground = Brushes.Gray;
            }
        }

        private void GlobalSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _SearchTimer.Stop();
            _SearchTimer.Start();
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

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            CloseAllOtherWindow();
            this.Close();
        }
    }
}