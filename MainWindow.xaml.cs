using System.Reflection;
using System.Security.Cryptography;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.TextFormatting;
using System.Xml.Serialization;
using System.Windows.Controls.Primitives;
using System.Windows.Navigation;
using System.Windows.Threading;
using UnrealEngine.Runtime;
using ArchiveB1;
using BtlShare;
using Google.Protobuf;

using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using CheckBox = System.Windows.Controls.CheckBox;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

using MessageBox = System.Windows.MessageBox;
using TextBox = Wpf.Ui.Controls.TextBox;

using Wukong_PBData_ReadWriter_GUI.Entity;
using Wukong_PBData_ReadWriter_GUI.Views;
using Wukong_PBData_ReadWriter_GUI.DataControllers;
using Wukong_PBData_ReadWriter_GUI.Extensions;
using Wukong_PBData_ReadWriter_GUI.src;
using Wukong_PBData_ReadWriter_GUI.Util;
using System;


namespace Wukong_PBData_ReadWriter_GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ConcurrentDictionary<string, DataFile> _DataFiles = new();
        public static Dictionary<string, string> s_DescriptionConfig = new Dictionary<string, string>();
        public static List<(string, DataFile, DataItem)> s_TraditionGlobalSearchCache = new List<(string, DataFile, DataItem)>();

        //public List<DataFile> _DataFiles = new List<DataFile>();
        public Dictionary<string, string> _MD5Config = new Dictionary<string, string>();
        public Dictionary<string, byte[]> _OrigItemData = new Dictionary<string, byte[]>();
        public DataFile _CurrentOpenFile = null;
        public List<(string, DataFile, DataItem)> _GlobalSearchCache = new List<(string, DataFile, DataItem)>();
        public DispatcherTimer _SearchTimer;
        public string _CurrentOpenFolder = "";
        public string version = "V1.6.0";
        public MergeWindow _MergeWindow;
        public Task _GlobalSearchTask = null;
        private string _selectedSaveFolder = string.Empty;
        private DispatcherTimer _autoSaveTimer;

        /*
             试试 System.Timers.Timer
         */

        /// <summary>
        /// 日志工具
        /// </summary>
        private readonly LogUtil _logUtil;

        /// <summary>
        /// 配置
        /// </summary>
        private readonly Config _config;

        /// <summary>
        /// 
        /// </summary>
        private readonly ConcurrentDictionary<string, DataFile> _updateFiles = new();

        /// <summary>
        /// 上次操作组件
        /// </summary>
        private object _lastActionComponent;

        /// <summary>
        /// 加载
        /// </summary>
        private bool _init;

        #region 委托

        private Action<ListBoxItem?, System.Windows.Media.Color?, string?> _listBoxItemAction;

        #endregion

        /// <summary>
        /// 构造
        /// </summary>
        public MainWindow()
        {
            _config = new();
            _logUtil = new("main", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log"), 0.5, null, true, 3, 3);
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            _listBoxItemAction += SetListBox;

            this.Closed += OnClosed;
            this.Loaded += OnLoaded;
            //this.Title = "黑猴配表编辑器" + version;

            #region 异常捕获

            //处理非UI线程异常  
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                _logUtil.Error("Error", e.ExceptionObject as Exception);
            };
            //处理UI线程异常  
            System.Windows.Forms.Application.ThreadException += (sender, e) =>
            {
                _logUtil.Error("Error", e.Exception);
            };
            //Task线程内未捕获异常处理事件
            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                _logUtil.Error($"Error", args.Exception);
            };

            //UI线程未捕获异常处理事件（UI主线程）
            this.Dispatcher.UnhandledException += (sender, args) =>
            {
                _logUtil.Error($"Error", args.Exception);
            };

            #endregion
            _logUtil.Info($"编辑器启动");
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Init();
            }
            catch (Exception exception)
            {
                _logUtil.Error($"Error", exception);
            }
            this.Title = "黑猴配表编辑器" + version;
            _init = true;
        }

        private void GlobalSearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _SearchTimer.Stop();
                _SearchTimer.Start();
                // 重新执行搜索
                //SearchTimer_Tick(null, null);
            }
        }

        /// <summary>
        /// 初始化
        /// </summary>
        private void Init()
        {
            var configPath = ".\\config.cfg";
            if (!File.Exists(configPath))
            {
                //生成一份
                SaveConfig();
            }
            s_DescriptionConfig = Exporter.ImportDescriptionConfig(Path.Combine(GlobalConfig.JsonDirPath, "DefaultDescConfig.json"));
            _MD5Config = Exporter.ImportDescriptionConfig(Path.Combine(GlobalConfig.JsonDirPath, "DefaultMD5Config.json"));
            _OrigItemData = Exporter.ImportItemDataBytes(Path.Combine(GlobalConfig.JsonDirPath, "DefaultOriData.oridata"));

            _SearchTimer = new DispatcherTimer();
            _SearchTimer.Interval = TimeSpan.FromMilliseconds(500); // 设置延迟时间
            _SearchTimer.Tick += SearchTimer_Tick;


            var content = File.ReadAllText(configPath);
            var configData = JsonUtil.Deserialize<ConcurrentDictionary<string, ConfigData>>(content);
            if (configData == null)
            {
                var bakFilePath = configPath + $"_{DateTime.Now:yyyyMMddHHmmss}.bak";
                File.Copy(configPath, bakFilePath);
                _logUtil.Debug($"配置文件读取失败,请查看配置内容。已做备份操作。文件路径:{bakFilePath}");
                SaveConfig();
                return;
            }

            LoadConfig(configData);

            if (!string.IsNullOrWhiteSpace(_config.ComparisonTableFilePath.Value as string))
                ComparisonTableController.Instance.LoadData(_config.ComparisonTableFilePath.Value.ToString());
            if (!string.IsNullOrWhiteSpace(_config.RemarkFilePath.Value as string))
                LoadDescription(_config.RemarkFilePath.Value.ToString());
            if (!string.IsNullOrWhiteSpace(_config.DataFilePath.Value as string))
            {
                RefreshFolderFile(_config.DataFilePath.Value.ToString());
                //CloseAllOtherWindow();
                _CurrentOpenFile = null;
            }

            if (!string.IsNullOrWhiteSpace(_config.TempFileDicPath.Value as string) && Directory.Exists(_config.TempFileDicPath.Value.ToString()))
            {
                var files = Directory.GetFiles(_config.TempFileDicPath.Value.ToString(), "", SearchOption.AllDirectories);
                if (files != null && files.Length > 0)
                {
                    var result = MessageBox.Show("检测到上次修改的文件，是否恢复？", "提示", MessageBoxButton.YesNo, MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        foreach (var file in files)
                        {
                            if (_DataFiles.TryGetValue(Path.GetFileNameWithoutExtension(file) + ".data",
                                    out var dataFile))
                            {
                                dataFile.Tag = file;
                                _updateFiles.TryAdd(dataFile._FileName, dataFile);
                            }
                        }
                        var dataFiles = _DataFiles.Values.ToList();
                        //首字母排序 dataFiles 但是Tag不为空的排在前面
                        dataFiles.Sort((a, b) =>
                        {
                            if (a.Tag != null && b.Tag == null)
                                return -1;
                            if (a.Tag == null && b.Tag != null)
                                return 1;
                            return a._FileName.CompareTo(b._FileName);
                        });
                        RefreshDataFile(dataFiles);
                    }
                    else
                    {
                        //加个是否删除上次修改
                        //否则建个Bak文件去覆盖会不会好点？
                        foreach (var file in files)
                        {
                            File.Delete(file);
                        }
                    }
                }
            }

            if (_DataFiles != null && _DataFiles.Values != null)
                CacheGlobalSearchAsync(_DataFiles.Values.ToList());

            ControlInitialization();

        }

        /// <summary>
        /// 控件初始化
        /// </summary>
        private void ControlInitialization()
        {
            AutoSaveFileCheck.IsChecked = _config.AutoSaveFile.Value.ToBool();
            DisplaysSourceInformationCheck.IsChecked = _config.DisplaysSourceInformation.Value.ToBool();
            AutoSearchInEffectCheck.IsChecked = _config.AutoSearchInEffect.Value.ToBool();
        }

        private void StartAutoSaveTick()
        {
            StopAutoSaveTimer();
            _autoSaveTimer = new DispatcherTimer();
            _autoSaveTimer.Interval = TimeSpan.FromMinutes(5); // 设置自动保存间隔时间为5分钟
            _autoSaveTimer.Tick += AutoSaveTimer_Tick;
            _autoSaveTimer.Start();
        }

        private void AutoSaveTimer_Tick(object sender, EventArgs e)
        {
            if (_config.AutoSaveFile.Value.ToBool() && !_updateFiles.IsEmpty && !string.IsNullOrEmpty(_selectedSaveFolder))
            {
                // 调用自动保存方法
                foreach (var file in _updateFiles.Values)
                {
                    SaveDataFile(file, _selectedSaveFolder);
                }
                _updateFiles.Clear();
            }
        }

        private void StopAutoSaveTimer()
        {
            if (_autoSaveTimer != null)
            {
                _autoSaveTimer.Stop();
                _autoSaveTimer.Tick -= AutoSaveTimer_Tick;
                _autoSaveTimer = null;
            }
        }

        public void ClearTempFiles()
        {
            if (!string.IsNullOrWhiteSpace(_config.TempFileDicPath.Value.ToString()) && Directory.Exists(_config.TempFileDicPath.Value.ToString()))
            {
                var files = Directory.GetFiles(_config.TempFileDicPath.Value.ToString(), "", SearchOption.AllDirectories);
                if (files != null && files.Length > 0)
                {
                    foreach (var file in files)
                    {
                        File.Delete(file);
                    }
                }
            }
        }

        /// <summary>
        /// 加载配置
        /// </summary>
        /// <param name="configData"></param>
        private void LoadConfig(ConcurrentDictionary<string, ConfigData> configData)
        {
            foreach (var propertyInfo in typeof(Config).GetProperties())
            {
                try
                {
                    if (!propertyInfo.CanWrite)
                        continue;
                    if (!configData.TryGetValue(propertyInfo.Name, out var data) || data.Data == null)
                        continue;
                    var configParma = propertyInfo.GetCustomAttribute(typeof(ConfigParam));
                    object writeData;
                    object oldData = propertyInfo.GetValue(_config);

                    if (propertyInfo.PropertyType == typeof(AttributeChangeNotification))
                    {
                        if (oldData != null && oldData is AttributeChangeNotification attributeChangeNotification)
                        {
                            attributeChangeNotification.Value = data.Data;
                            if (attributeChangeNotification.Change == null)
                                attributeChangeNotification.Change += ChangeDataFunc;
                            continue;
                        }
                    }

                    if (configParma != null && configParma is ConfigParam param && param.DataType != typeof(string))
                    {
                        writeData = Convert.ChangeType(data.Data, param.DataType);
                    }
                    else
                    {
                        writeData = data.Data;
                    }

                    propertyInfo.SetValue(_config, writeData);
                }
                catch (Exception e)
                {
                    _logUtil.Error($"Error", e);
                }
            }

            foreach (var fieldInfo in typeof(Config).GetFields())
            {
                try
                {
                    if (!configData.TryGetValue(fieldInfo.Name, out var data) || data.Data == null)
                        continue;
                    var configParma = (ConfigParam?)fieldInfo.GetCustomAttribute(typeof(ConfigParam));
                    var oldData = fieldInfo.GetValue(_config);
                    if (oldData is AttributeChangeNotification attributeChangeNotification)
                    {
                        attributeChangeNotification.Value = data.Data;
                        if (attributeChangeNotification.Change == null)
                            attributeChangeNotification.Change += ChangeDataFunc;
                        continue;
                    }

                    fieldInfo.SetValue(_config, data.Data);
                }
                catch (Exception e)
                {
                    _logUtil.Error($"Error", e);
                }
            }
        }

        /// <summary>
        /// 保存配置
        /// </summary>
        private void SaveConfig()
        {
            var configPath = ".\\config.cfg";
            var fileInfo = new FileInfo(configPath);
            if (fileInfo.Exists && fileInfo.LastWriteTime >= _config.SaveTime)
                return;
            File.WriteAllText(configPath, GetConfig(_config));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        private string GetConfig(Config config)
        {
            var dic = new ConcurrentDictionary<string, ConfigData>();

            foreach (var fieldInfo in typeof(Config).GetFields())
            {
                var configParma = (ConfigParam?)fieldInfo.GetCustomAttribute(typeof(ConfigParam));
                var data = fieldInfo.GetValue(config);
                object writeData;

                if (data is AttributeChangeNotification attributeChangeNotification)
                {
                    writeData = attributeChangeNotification.Value;
                }
                else
                {
                    writeData = data;
                }


                dic.TryAdd(fieldInfo.Name, new ConfigData
                {
                    Data = writeData,
                    Desc = configParma?.Desc ?? ""
                });
            }

            foreach (var propertyInfo in typeof(Config).GetProperties())
            {
                if (!propertyInfo.CanRead)
                    continue;
                var configParma = (ConfigParam?)propertyInfo.GetCustomAttribute(typeof(ConfigParam));
                var data = propertyInfo.GetValue(config);
                object writeData;

                if (data is AttributeChangeNotification attributeChangeNotification)
                {
                    writeData = attributeChangeNotification.Value;
                }
                else
                {
                    writeData = data;
                }

                if (writeData is DateTime time)
                {
                    writeData = time.ToString("yyyy-MM-dd HH:mm:ss");
                }

                dic.TryAdd(propertyInfo.Name, new ConfigData
                {
                    Data = writeData,
                    Desc = configParma?.Desc ?? ""
                });
            }

            return JsonUtil.Serialize(dic);
        }

        /// <summary>
        /// 关闭事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnClosed(object? sender, EventArgs e)
        {
            SaveConfig();

            CloseAllOtherWindow();
            this.Close();
        }

        private void CloseAllOtherWindow(bool isClearDataGrid = true)
        {
            WindowCollection windows = System.Windows.Application.Current.Windows;
            foreach (var win in windows)
            {
                if (win == null)
                    continue;

                if (win.GetType() != typeof(MainWindow))
                {
                    (win as Window).Close();
                }
            }

            if (isClearDataGrid)
            {
                DataGrid.RowDefinitions.Clear();
                DataGrid.Children.Clear();
            }

        }

        private void ImportDescription(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog();
            dialog.AddExtension = true;
            dialog.Filter = "Json|*.json";
            dialog.Title = "导入备注配置";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                LoadDescription(dialog.FileName);
                _config.RemarkFilePath.Value = dialog.FileName;
            }
        }

        /// <summary>
        /// 加载备注
        /// </summary>
        /// <param name="dialogFileName"></param>
        private void LoadDescription(string dialogFileName)
        {
            var newDict = Exporter.ImportDescriptionConfig(dialogFileName);
            foreach (var kvp in newDict)
            {
                if (s_DescriptionConfig.ContainsKey(kvp.Key))
                {
                    s_DescriptionConfig[kvp.Key] = kvp.Value;
                }
                else
                {
                    s_DescriptionConfig.Add(kvp.Key, kvp.Value);
                }
            }
        }

        private void CreatePak(object sender, RoutedEventArgs e)
        {
            try
            {
                PakCompressWindow pakDecompress = new PakCompressWindow();
                pakDecompress.Show();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"xxxxxxxxxxxxxxxxxxError in PakCompressWindow#OnDrop: {ex.Message}");
            }
        }

        private void DecompressPak(object sender, RoutedEventArgs e)
        {
            try
            {
                PakDecompressWindow pakDecompress = new PakDecompressWindow();
                pakDecompress.Show();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"xxxxxxxxxxxxxxxxxxError in PakDecompressWindow#OnDrop: {ex.Message}");
            }
        }

        private void Window_Drop_Uncompress(object sender, System.Windows.DragEventArgs e)
        {
            // 检查拖拽的数据是否是文件夹
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                // 获取拖拽的文件路径
                string[] draggedItems = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);


                RunBatFileWithFolder(@"ref\\make_pak_uncompressed.bat", draggedItems[0]);
            }
        }

        private void Window_Drop(object sender, System.Windows.DragEventArgs e)
        {
            // 检查拖拽的数据是否是文件夹
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                // 获取拖拽的文件路径
                string[] draggedItems = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);

                // 确认用户拖拽的是文件夹（而非文件）
                if (Directory.Exists(draggedItems[0]))
                {
                    string folderPath = draggedItems[0];
                    RunBatFileWithFolder(@"ref\\make_pak_compressed.bat", folderPath);
                }
                else
                {
                    System.Windows.MessageBox.Show("请拖拽一个文件夹。");
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
                UseShellExecute = false,  // 设置为 false 以便能够重定向输入/输出
                CreateNoWindow = true,    // 如果你不想显示命令提示符窗口，设置为 true
                RedirectStandardOutput = true,  // 如果你需要捕获输出
                RedirectStandardError = true    // 捕获错误信息
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
                System.Windows.MessageBox.Show($"Output: {output}\nError: {error}");
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
            System.Windows.Forms.SaveFileDialog dialog = new System.Windows.Forms.SaveFileDialog();
            dialog.AddExtension = true;
            // dialog.Filter = "Data|*.oridata";
            dialog.Filter = "Json|*.json";
            dialog.Title = "导出备注配置";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Exporter.ExportDescriptionConfig(s_DescriptionConfig, dialog.FileName);
                // Exporter.ExportDescriptionConfig(_MD5Config, dialog.FileName);
                // Exporter.ExportItemDataBytes(_OrigItemData, dialog.FileName);
            }
        }

        private async Task CacheGlobalSearchAsync(List<DataFile> fileList)
        {
            _GlobalSearchCache = await Exporter.GlobalSearchCacheAsync(fileList);
        }

        private async void OpenDataFolder(object sender, RoutedEventArgs e)
        {
            //选择文件夹,并返回选择的文件夹路径，FolderBrowserDialog是一个选择文件夹的对话框
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = "请选择Data数据文件夹";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _config.DataFilePath.Value = dialog.SelectedPath;
                ClearTempFiles();
                _updateFiles.Clear();
                RefreshFolderFile(dialog.SelectedPath);
                CloseAllOtherWindow();
                _CurrentOpenFile = null;
                _selectedSaveFolder = string.Empty;


                if (_GlobalSearchTask != null && !_GlobalSearchTask.IsCompleted)
                {
                    _GlobalSearchTask = null;
                    _GlobalSearchCache.Clear();
                    s_TraditionGlobalSearchCache.Clear();
                }

                _GlobalSearchTask = CacheGlobalSearchAsync(_DataFiles.Values.ToList());
                await _GlobalSearchTask;


                // var files = _DataFiles.Values.ToList();
                // files.Sort((a, b) => a._FileName.CompareTo(b._FileName));
                // s_DescriptionConfig = Exporter.GenerateFirstDescConfig(files);
                // _MD5Config = Exporter.CollectItemMD5(files);
                // _OrigItemData = Exporter.CollectItemBytes(files);


            }
        }

        private void RefreshFolderFile(string dir)
        {
            //将选择的文件夹路径显示在文本框中
            _CurrentOpenFolder = dir;
            List<string> fileNames = new List<string>();
            List<string> filePaths = new List<string>();
            Exporter.Director(dir + "\\", fileNames, filePaths);

            _DataFiles.Clear();
            int index = 0;
            foreach (var item in fileNames)
            {
                var isValid = Exporter.GetIsValidFile(item, filePaths[index]);
                if (!isValid)
                {
                    index++;
                    continue;
                }
                DataFile file = new DataFile();
                file._FileName = item;
                file._FilePath = filePaths[index];
                //_DataFiles.Add(file);
                if (_updateFiles.TryGetValue(item, out var oldFile))
                {
                    file.Tag = oldFile.Tag;
                    file.CanOpen = true;
                }

                _DataFiles.TryAdd(item, file);
                index++;
            }

            //把_DataFiles绑定到FileList上并自动生成 ListBoxItem, 每个Item显示FileName 并且对应有一个打开按钮
            //把 _DataFiles.Values to List 并按FileName的首字母排序 从小到大排序
            var files = _DataFiles.Values.ToList();
            files.Sort((a, b) => a._FileName.CompareTo(b._FileName));

            RefreshDataFile(files);
        }


        private void RefreshDataFile(List<DataFile> files)
        {
            if (FileList == null)
                return;
            FileList.Items.Clear();

            foreach (var item in files)
            {
                if (!item._IsShow) continue;

                ListBoxItem listBoxItem = new ListBoxItem();
                var hasTag = item.Tag != null;
                listBoxItem.Content = item._FileName + (hasTag ? "*" : "");
                listBoxItem.MouseDoubleClick += new MouseButtonEventHandler(OpenDataFile);
                listBoxItem.DataContext = item;
                item._ListBoxItem = listBoxItem;

                listBoxItem.ToolTip = item._Desc;
                if (!string.IsNullOrEmpty(item._Desc))
                {
                    listBoxItem.Foreground = new SolidColorBrush(Colors.Blue);
                }

                if (hasTag)
                {
                    listBoxItem.Foreground = new SolidColorBrush(Colors.Red);
                }

                listBoxItem.ContextMenu = new ContextMenu();
                MenuItem menuItem = new MenuItem();
                menuItem.Header = "备注";
                string descKey = item._FileName;
                var descSuccessAction = () =>
                {
                    RefreshDataFile(files);
                };
                menuItem.DataContext = new Tuple<string, Action>(descKey, descSuccessAction);
                menuItem.Click += OpenDescriptionWindow;
                listBoxItem.ContextMenu.Items.Add(menuItem);

                MenuItem topMenuItem = new MenuItem();
                topMenuItem.Header = "置顶";
                topMenuItem.DataContext = item;
                topMenuItem.Click += SetTopFile;
                listBoxItem.ContextMenu.Items.Add(topMenuItem);

                MenuItem openFolderMenuItem = new MenuItem();
                openFolderMenuItem.Header = "打开所在文件夹";
                openFolderMenuItem.DataContext = item._FilePath;
                openFolderMenuItem.Click += OpenContainingFolder_Click;
                listBoxItem.ContextMenu.Items.Add(openFolderMenuItem);

                FileList.Items.Add(listBoxItem);
            }

            RefreshTopFileList();
        }

        // private void RefreshDataFile(ICollection<DataFile> files)
        // {
        //     if (FileList == null)
        //         return;
        //     FileList.Items.Clear();
        //     foreach (var item in files)
        //     {
        //         if (!item._IsShow) continue;
        //
        //         ListBoxItem listBoxItem = new ListBoxItem();
        //         var hasTag = !string.IsNullOrEmpty((item.Tag as string));
        //         listBoxItem.Content = item._FileName + (hasTag ? "*" : "");
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
        //         if (hasTag)
        //         {
        //             listBoxItem.Foreground = new SolidColorBrush(Colors.Red);
        //         }
        //
        //         listBoxItem.ContextMenu = new ContextMenu();
        //         MenuItem menuItem = new MenuItem();
        //         menuItem.Header = "备注";
        //         string descKey = item._FileName;
        //         Action descSuccessAction = () =>
        //         {
        //             RefreshDataFile(files);
        //         };
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

        private void SetTopFile(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem == null) return;

            var file = menuItem.DataContext as DataFile;

            if (file == null) return;

            file._IsTop = true;

            RefreshTopFileList();
        }
        private void CancelTopFile(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem == null) return;

            var file = menuItem.DataContext as DataFile;

            if (file == null) return;

            file._IsTop = false;

            RefreshTopFileList();
        }

        private void RefreshTopFileList()
        {
            TopFileList.Items.Clear();
            bool hasTop = false;
            foreach (var file in _DataFiles.Values)
            {
                if (file._IsTop)
                {
                    ListBoxItem listBoxItem = new ListBoxItem();
                    listBoxItem.Content = file._FileName;
                    listBoxItem.MouseDoubleClick += new MouseButtonEventHandler(OpenDataFile);
                    listBoxItem.DataContext = file;

                    listBoxItem.ToolTip = new System.Windows.Controls.ToolTip()
                    {
                        Content = new TextBlock
                        {
                            Text = file._Desc,
                            TextWrapping = TextWrapping.Wrap
                        }
                    };

                    if (!string.IsNullOrEmpty(file._Desc))
                    {
                        listBoxItem.Foreground = new SolidColorBrush(Colors.Blue);
                    }

                    listBoxItem.ContextMenu = new ContextMenu();
                    MenuItem topMenuItem = new MenuItem();
                    topMenuItem.Header = "取消置顶";
                    topMenuItem.DataContext = file;
                    topMenuItem.Click += CancelTopFile;
                    listBoxItem.ContextMenu.Items.Add(topMenuItem);

                    MenuItem openFolderMenuItem = new MenuItem();
                    openFolderMenuItem.Header = "打开所在文件夹";
                    openFolderMenuItem.DataContext = file._FilePath;
                    openFolderMenuItem.Click += OpenContainingFolder_Click;
                    listBoxItem.ContextMenu.Items.Add(openFolderMenuItem);

                    TopFileList.Items.Add(listBoxItem);
                    hasTop = true;
                }
            }

            if (!hasTop)
            {
                System.Windows.Controls.TextBlock topFileText = new System.Windows.Controls.TextBlock();
                topFileText.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                TopFileList.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                topFileText.Text = "选中对应条目的右键置顶此区域";
                TopFileList.Items.Add(topFileText);
                //TopFileList.Visibility = Visibility.Hidden;
                //Grid.SetRow(TopFileList, 2);
                //Grid.SetRow(FileList, 1);
            }
        }

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
                    System.Windows.MessageBox.Show("文件夹不存在", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        void SaveDataFile(DataFile file, string dir, bool useDir = false)
        {
            try
            {
                var pakPath = file._FilePath;

                var b1Index = file._FilePath.IndexOf("b1");
                if (b1Index != -1)
                    pakPath = file._FilePath.Substring(b1Index,
                        file._FilePath.Length - b1Index);

                var outPath = Path.Combine(dir, pakPath);

                Exporter.SaveDataFile(useDir ? dir : outPath, file);
            }
            catch (Exception e)
            {
                _logUtil.Error($"Error", e);
            }

        }

        private void SaveDataFile(object sender, RoutedEventArgs e)
        {
            // if (_config.AutoSaveFile && !_updateFiles.IsEmpty && !string.IsNullOrEmpty(_selectedSaveFolder))
            // {
            //     foreach (var file in _updateFiles.Values)
            //     {
            //         SaveDataFile(file, _selectedSaveFolder);
            //     }
            //     _updateFiles.Clear();
            //     return;
            // }
            /*
                    建议将文件先写入Temp文件夹
                    然后再将Temp文件夹覆盖到输出路径
                    然后重新载入的时候这些被标记的文件需要从temp路径去读

                    这个时候引出了一个问题，保存的时候要不要删掉Temp文件的数据,如果删掉就要去读输出目录的文件,目前先不删Temp文件夹的内容先用着        
            */
            if (string.IsNullOrEmpty(_selectedSaveFolder))
            {
                var result = MessageBox.Show("第一次保存，请选择一个Mod文件夹用于保存修改后的数据", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                if (result == MessageBoxResult.OK)
                {
                    System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
                    dialog.Description = "请选择要保存Data数据的文件夹";
                    if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        _selectedSaveFolder = dialog.SelectedPath;

                        foreach (var file in _updateFiles.Values)
                        {
                            SaveDataFile(file, _selectedSaveFolder);
                            if (file.Tag is string tempPath)
                                SaveDataFile(file, tempPath, true);
                        }

                        RefreshFolderFile(_config.DataFilePath.Value.ToString());
                        DataItemList.Items.Clear();
                        DataGrid.Children.Clear();
                        CloseAllOtherWindow();
                        _CurrentOpenFile = null;
                        if (MessageBox.Show("是否保存该路径为默认保存位置？", "提示", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                            _config.DefaultSavePath.Value = _selectedSaveFolder;
                    }
                }
            }
            else
            {


                foreach (var file in _updateFiles.Values)
                {
                    SaveDataFile(file, _selectedSaveFolder);
                    if (file.Tag is string tempPath)
                        SaveDataFile(file, tempPath, true);
                }
                //_updateFiles.Clear();

                RefreshFolderFile(_config.DataFilePath.Value.ToString());
                DataItemList.Items.Clear();
                DataGrid.Children.Clear();
                CloseAllOtherWindow();
                _CurrentOpenFile = null;
            }



            //_GlobalSearchCache = Exporter.GlobalSearchCache(_DataFiles);


        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pakPath"></param>
        private void SavePakFile(string pakPath)
        {
            //rename pakPath file if exist
            if (File.Exists(pakPath))
            {
                File.Delete(pakPath);
            }

            Exporter.SaveDataFile(pakPath, _CurrentOpenFile);
        }

        private void SaveAsNewDataFile(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.InitialDirectory = _config.OutPakFilePath.Value.ToString();
            dialog.Description = "请选择要保存Data数据的文件夹";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    string dir = dialog.SelectedPath;
                    _config.OutPakFilePath.Value = dir;
                    var pakPath = "";
                    var b1Index = -1;
                    var outPath = "";
                    // if (_config.AutoSaveFile && !_updateFiles.IsEmpty)
                    // {
                    //     foreach (var key in _updateFiles.Keys)
                    //     {
                    //         pakPath = _updateFiles[key]._FilePath;
                    //
                    //         b1Index = _updateFiles[key]._FilePath.IndexOf("b1");
                    //         if (b1Index != -1)
                    //             pakPath = _updateFiles[key]._FilePath.Substring(b1Index,
                    //                 _updateFiles[key]._FilePath.Length - b1Index);
                    //
                    //         outPath = Path.Combine(dir, pakPath);
                    //
                    //         Exporter.SaveDataFile(outPath, _updateFiles[key]);
                    //     }
                    //     //_updateFiles.Clear();
                    //     return;
                    // }

                    pakPath = _CurrentOpenFile._FilePath;

                    b1Index = _CurrentOpenFile._FilePath.IndexOf("b1");
                    if (b1Index != -1)
                        pakPath = _CurrentOpenFile._FilePath.Substring(b1Index,
                            _CurrentOpenFile._FilePath.Length - b1Index);

                    outPath = Path.Combine(dir, pakPath);

                    Exporter.SaveDataFile(outPath, _CurrentOpenFile);
                }
                catch (Exception exception)
                {
                    _logUtil.Error($"SaveAsNewDataFile.Error", exception);
                }
            }
        }

        private void OpenDataFile(object sender, MouseButtonEventArgs e)
        {

            ListBoxItem listBoxItem = sender as ListBoxItem;
            if (listBoxItem != null)
            {
                var file = listBoxItem.DataContext as DataFile;
                if (_lastActionComponent != listBoxItem)
                    _lastActionComponent = listBoxItem;

                CurrentOpenFileChange(file);

                // if (_CurrentOpenFile != null && _config.AutoSaveFile)
                // {
                //     //if (!_updateFiles.TryGetValue(_CurrentOpenFile._FileName,out var data))
                //
                //     //_CurrentOpenFile._IsDirty = false;
                //
                //     //if (_updateFiles.TryGetValue(file._FileName, out var useData))
                //     //{
                //     //    OpenFile(useData);
                //     //    return;
                //     //}
                //     CurrentOpenFileChange(file);
                //     return;
                // }
                // if (_CurrentOpenFile != null && _CurrentOpenFile._IsDirty)
                // {
                //
                //     MessageBoxResult result = System.Windows.MessageBox.Show("切换data文件，当前修改将被还原", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
                //
                //     // 根据用户的选择执行相应的逻辑
                //     if (result == MessageBoxResult.Yes)
                //     {
                //         _CurrentOpenFile._IsDirty = false;
                //         
                //     }
                // }
                // else
                // {
                //     //DataFile file = listBoxItem.DataContext as DataFile;
                //     //OpenFile(file);
                //     CurrentOpenFileChange(file);
                // }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="listBoxItem"></param>
        private void CurrentOpenFileChange(DataFile file)
        {
            //if (_lastActionComponent != listBoxItem)
            //    _lastActionComponent = listBoxItem;
            //DataFile file = listBoxItem.DataContext as DataFile;

            //释放掉全局搜索
            // _GlobalSearchCache.Clear();
            OpenFile(file);

            if (_config.AutoSearchInEffect.Value.ToBool()
                && !string.IsNullOrWhiteSpace(ItemSearch.Text)
                && !ItemSearch.Text.Equals("搜索ID或备注"))
                ItemSearch.Dispatcher.BeginInvoke(new Action(() =>
                {
                    ItemSearch_TextChanged(ItemSearch, null);
                }));

            //ItemSearch_TextChanged(ItemSearch, null);
        }

        private void DataItemList_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            AddNewDataItem.Width = DataItemList.ActualWidth;
        }

        private void OpenFile(DataFile file)
        {
            if (file != null)
            {
                file.LoadData();

                if (file._FileDataItemList != null && file._FileDataItemList.Count > 0)
                {
                    RefreshFileDataItemList(file._FileDataItemList);
                }
                else
                {
                    if (DataItemList != null)
                    {
                        DataItemList.Items.Clear();
                    }
                }

                _CurrentOpenFile = file;
                var b1Index = file._FilePath.IndexOf("b1");
                var pakPath = file._FilePath;
                if (b1Index != -1)
                    pakPath = file._FilePath.Substring(b1Index, file._FilePath.Length - b1Index);
                DataFilePath.Text = $"正在修改：{pakPath}";
                CloseAllOtherWindow();
            }
        }

        private void RefreshFileDataItemList(List<DataItem> list)
        {
            if (DataItemList == null) return;
            DataItemList.Items.Clear();
            var descSuccessAction = () =>
            {
                RefreshFileDataItemList(list);
            };
            foreach (var item in list)
            {
                if (!item._IsShow) continue;

                ListBoxItem listItem = new ListBoxItem();
                listItem.Content = item._ID + "  " + item._Desc;
                if (!Exporter.IsSameAsMd5(item, _MD5Config))
                {
                    listItem.Foreground = new SolidColorBrush(Colors.Red);
                    item._IsModified = true;
                }

                listItem.DataContext = item;
                item._ListBoxItem = listItem;
                // listItem.MouseLeftButtonDown += new MouseButtonEventHandler(OpenDataItem);
                listItem.ContextMenu = new ContextMenu();
                MenuItem menuItem = new MenuItem();
                menuItem.Header = "备注";
                //string descKey = item._File._FileData.GetType().Name + "_" + item._ID;

                menuItem.DataContext = new Tuple<string, Action>(item.DescKey, descSuccessAction);
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


                //TODO 等待更改Tag
                DataItemList.Items.Add(listItem);
            }
        }

        private void CloneMenuItem_Click(object sender, EventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem == null) return;

            var dataItem = menuItem.DataContext as DataItem;

            if (dataItem == null) return;

            if (_CurrentOpenFile == null)
                return;

            var bytes = dataItem._Data.ToByteArray();

            var list = _CurrentOpenFile._ListPropertyInfo.GetValue(_CurrentOpenFile._FileData, null) as IList;

            try
            {
                if (_CurrentOpenFile._FileDataItemList != null)
                {
                    var newItemType = list.GetType().GetGenericArguments()[0];
                    if (newItemType != null)
                    {
                        IMessage newItem = null;

                        var parser = newItemType.GetProperty("Parser", BindingFlags.Static | BindingFlags.Public);
                        if (parser != null)
                        {
                            try
                            {
                                MessageParser parserValue = parser.GetMethod.Invoke(null, null) as MessageParser;
                                var message = parserValue.ParseFrom(bytes);
                                if (message != null)
                                {
                                    newItem = message;
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.ToString());
                            }
                        }

                        if (newItem == null)
                            return;


                        var property = newItemType.GetProperty("Id");
                        if (property == null)
                        {
                            property = newItemType.GetProperty("ID");
                        }

                        if (property == null)
                            return;

                        DataItem newDataItem = new DataItem();
                        newDataItem._ID = _CurrentOpenFile.GetNewID();
                        while (_CurrentOpenFile._IDList.Contains(newDataItem._ID))
                        {
                            newDataItem._ID++;
                        }

                        property.SetValue(newItem, newDataItem._ID, null);
                        _CurrentOpenFile._IDList.Add(newDataItem._ID);
                        property.SetValue(newItem, newDataItem._ID, null);
                        newDataItem._Data = newItem;
                        newDataItem._File = _CurrentOpenFile;
                        _CurrentOpenFile._FileDataItemList.Add(newDataItem);

                        list.Add(newItem);
                        var key = newDataItem._File._FileData.GetType().Name + "_" + newDataItem._ID;
                        //解决克隆新增ID重复 
                        if (s_DescriptionConfig.ContainsKey(key))
                        {
                            _CurrentOpenFile._IDList.Remove(newDataItem._ID);
                            while (s_DescriptionConfig.ContainsKey(key))
                            {
                                newDataItem._ID++;
                                key = newDataItem._File._FileData.GetType().Name + "_" + newDataItem._ID;
                            }

                            newDataItem.DescKey = key;
                        }

                        //s_DescriptionConfig.Add(newDataItem._File._FileData.GetType().Name + "_" + newDataItem._ID, dataItem._Desc);
                        s_DescriptionConfig.Add(newDataItem.DescKey, dataItem._Desc);
                        RefreshFileDataItemList(_CurrentOpenFile._FileDataItemList);
                    }
                }
            }
            catch
            {
                //
            }
            finally
            {
                bytes = null;
            }
        }

        private void DelMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem == null) return;

            var dataItem = menuItem.DataContext as DataItem;

            if (dataItem == null) return;

            var list = _CurrentOpenFile._ListPropertyInfo.GetValue(_CurrentOpenFile._FileData, null) as IList;

            if (list == null || list.Count <= 0)
                return;

            list.Remove(dataItem._Data);

            dataItem._File._FileDataItemList.Remove(dataItem);

            RefreshFileDataItemList(_CurrentOpenFile._FileDataItemList);
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
            System.Windows.Point mousePosition = Mouse.GetPosition(this);

            // 转换为屏幕坐标
            System.Windows.Point screenPosition = PointToScreen(mousePosition);
            window.Left = screenPosition.X;
            window.Top = screenPosition.Y;
            window.Show();

            Grid grid = new Grid();
            window.Content = grid;
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

            System.Windows.Controls.Label label = new System.Windows.Controls.Label();
            label.Content = "备注";
            label.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            label.VerticalAlignment = VerticalAlignment.Top;
            label.Margin = new Thickness(10, 10, 0, 0);
            Grid.SetRow(label, 0);
            Grid.SetColumn(label, 1);
            grid.Children.Add(label);

            System.Windows.Controls.TextBox textBox = new System.Windows.Controls.TextBox();
            textBox.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            textBox.VerticalAlignment = VerticalAlignment.Top;
            textBox.Margin = new Thickness(100, 10, 0, 0);
            textBox.AcceptsReturn = true;
            textBox.TextWrapping = TextWrapping.Wrap;
            string desc = "这里写备注";
            s_DescriptionConfig.TryGetValue(data.Item1, out desc);
            textBox.Text = desc;
            Grid.SetRow(textBox, 0);
            Grid.SetColumn(textBox, 1);
            grid.Children.Add(textBox);

            System.Windows.Controls.Button button = new System.Windows.Controls.Button();
            button.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            button.VerticalAlignment = VerticalAlignment.Top;
            button.Margin = new Thickness(40, 40, 0, 0);
            button.Content = "确定";
            button.Click += (sender, e) =>
            {
                if (s_DescriptionConfig.ContainsKey(data.Item1))
                {
                    s_DescriptionConfig.Remove(data.Item1);
                }

                s_DescriptionConfig.TryAdd(data.Item1, textBox.Text);
                data.Item2?.Invoke();
                window.Close();
            };
            Grid.SetRow(button, 1);
            Grid.SetColumn(button, 1);
            grid.Children.Add(button);
        }

        private ListBoxItem FindListBoxItem(DependencyObject child)
        {
            // 检查child是否是ListBoxItem  
            if (child is ListBoxItem listBoxItem)
            {
                return listBoxItem;
            }

            // 如果child不是ListBoxItem，则检查其父元素  
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            // 如果找到了根元素或Popup，则返回null（通常不应该发生，但这里作为检查）  
            if (parentObject == null || parentObject is Popup)
            {
                return null;
            }

            // 递归查找ListBoxItem  
            return FindListBoxItem(parentObject);
        }

        private void OpenDataItem(object sender, MouseButtonEventArgs e)
        {
            ListBoxItem listBoxItem = FindListBoxItem(e.OriginalSource as DependencyObject);
            if (listBoxItem != null)
            {
                var data = listBoxItem.DataContext as DataItem;
                if (data != null)
                {

                    if (data._Data == null)
                    {
                        return;
                    }

                    data.LoadData();

                    RefreshDataItemList(data._DataPropertyItems);

                    CloseAllOtherWindow(false);

                    if (data._IsModified)
                    {
                        var key = data._File._FileData.GetType().Name + "_" + data._ID;
                        if (_OrigItemData.TryGetValue(key, out var itemDataBytes)
                            && _config.DisplaysSourceInformation.Value.ToBool()
                            && System.Windows.MessageBox.Show("发现文件有更改,是否展示源信息", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                        {
                            OpenOriDataWindow(itemDataBytes, data);
                        }
                    }
                }
            }
        }

        private void OpenOriDataWindow(byte[] itemData, DataItem curData)
        {
            if (itemData == null || itemData.Length <= 0)
                return;

            //IMessage
            var dataPropertyItems = new List<DataPropertyItem>();

            //var dataType = curData._Data.GetType();
            var dataType = curData.DataType;
            var parser = dataType.GetProperty("Parser", BindingFlags.Static | BindingFlags.Public);
            if (parser != null)
            {
                try
                {
                    MessageParser parserValue = parser.GetMethod.Invoke(null, null) as MessageParser;
                    var message = parserValue.ParseFrom(itemData);
                    if (message != null)
                    {
                        //PropertyInfo[] properties = dataType.GetProperties();
                        if (!PropertiesDataController.Instance.Get(dataType, out var properties))
                        {
                            properties = PropertiesDataController.Instance.Add(dataType);
                        }

                        foreach (var property in properties)
                        {
                            DataPropertyItem dataPropertyItem = new DataPropertyItem();
                            dataPropertyItem._PropertyName = property.Name;
                            dataPropertyItem._PropertyDesc = "";
                            dataPropertyItem._PropertyInfo = property;
                            dataPropertyItem._BelongData = message;
                            dataPropertyItem._DataItem = curData;
                            dataPropertyItems.Add(dataPropertyItem);
                        }
                    }
                }
                catch
                {
                    Console.WriteLine("Data Failed ");
                }
            }

            if (dataPropertyItems == null || dataPropertyItems.Count <= 0)
            {
                return;
            }

            Window window = new Window();
            window.Title = "原始数据";
            window.Width = 640;
            window.Height = 720;

            StackPanel stackPanel = new StackPanel();
            window.Content = stackPanel;
            stackPanel.Margin = new Thickness(5);
            TextBlock textBlock = new TextBlock();
            textBlock.Text = "原始配置数据";
            textBlock.FontWeight = FontWeights.Bold;
            textBlock.Margin = new Thickness(0, 0, 0, 10);
            stackPanel.Children.Add(textBlock);

            ScrollViewer scrollViewer = new ScrollViewer();
            scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            scrollViewer.Height = 600;
            stackPanel.Children.Add(scrollViewer);

            Grid grid = new Grid();
            grid.Width = 640;
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(320) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(320) });
            scrollViewer.Content = grid;

            RefreshDataItemList(dataPropertyItems, grid);

            var mainWindowRect = new Rect(this.Left, this.Top, this.ActualWidth, this.ActualHeight);

            // 设置子窗口的位置，使其吸附在主窗口的右边
            window.Left = mainWindowRect.Right;
            window.Top = mainWindowRect.Top;

            window.Show();
        }

        private void RefreshDataItemList(List<DataPropertyItem> propertyItemList, Grid grid = null)
        {
            if (grid == null)
                grid = DataGrid;
            grid.RowDefinitions.Clear();
            grid.Children.Clear();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            int rowIndex = 0;
            var descSuccessAction = () =>
            {
                RefreshDataItemList(propertyItemList);
            };
            foreach (var item in propertyItemList)
            {
                TextBlock label = new TextBlock();
                if (string.IsNullOrWhiteSpace(item.DisplayName))
                {
                    label.Text = $"{item._PropertyName}";
                    ComparisonTableController.Instance.AddData($"{item._PropertyInfo.DeclaringType.Name}.{item._PropertyName}", "");
                }
                else
                {
                    label.Text = $"{item.DisplayName}";
                }

                label.ToolTip = new System.Windows.Controls.ToolTip()
                {
                    Content = new TextBlock
                    {
                        Text = item._PropertyDesc,
                        TextWrapping = TextWrapping.Wrap
                    }
                };
                Grid.SetRow(label, rowIndex);
                Grid.SetColumn(label, 0);
                label.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                label.VerticalAlignment = VerticalAlignment.Top;
                label.Padding = new Thickness(0, 8, 0, 0);
                label.Margin = new Thickness(0, 10 + rowIndex * 40, 0, 0);


                label.ContextMenu = new ContextMenu();
                MenuItem menuItem = new MenuItem();
                menuItem.Header = "备注";

                string descKey = item._DataItem._File._FileData.GetType().Name + "_" + item._PropertyName;

                if (s_DescriptionConfig.ContainsKey(descKey))
                {
                    label.Foreground = new SolidColorBrush(Colors.Blue);
                }

                menuItem.DataContext = new Tuple<string, Action>(descKey, descSuccessAction);
                menuItem.Click += OpenDescriptionWindow;
                label.ContextMenu.Items.Add(menuItem);

                grid.Children.Add(label);

                var valueType = item._PropertyInfo.PropertyType;
                ProcessPropertyType(valueType, item, rowIndex, grid, 300);
                rowIndex++;
            }
            ComparisonTableController.Instance.SaveData();
        }

        private void ProcessPropertyType(Type valueType, DataPropertyItem item, int rowIndex, Grid curGrid, int left)
        {
            if (valueType == typeof(int) || valueType == typeof(float) || valueType == typeof(long) || valueType == typeof(double))
            {
                TextBox numberTextBox = new TextBox();
                numberTextBox.PreviewTextInput += new TextCompositionEventHandler(NumericTextBox_PreviewTextInput);
                numberTextBox.PreviewKeyDown += new System.Windows.Input.KeyEventHandler(NumericTextBox_PreviewKeyDown);
                numberTextBox.LostFocus += new RoutedEventHandler(NumericTextBox_LostFocus);
                numberTextBox.Text = item._PropertyInfo.GetValue(item._BelongData).ToString();
                numberTextBox.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                numberTextBox.VerticalAlignment = VerticalAlignment.Top;
                numberTextBox.Margin = new Thickness(0, 10 + rowIndex * 40, 0, 0);
                numberTextBox.Width = 250;
                numberTextBox.DataContext = item;
                //numberTextBox.IsReadOnly = curGrid != DataGrid;
                numberTextBox.IsReadOnly = false;
                numberTextBox.Tag = "numberTextBox";
                //numberTextBox.TextChanged += NumberTextBox_TextChanged;
                numberTextBox.TextChanged += ChangeEvent;
                Grid.SetRow(numberTextBox, rowIndex);
                Grid.SetColumn(numberTextBox, 1);
                curGrid.Children.Add(numberTextBox);
            }
            else if (valueType == typeof(string))
            {
                TextBox stringTextBox = new TextBox();
                stringTextBox.Text = item._PropertyInfo.GetValue(item._BelongData).ToString();
                stringTextBox.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                stringTextBox.VerticalAlignment = VerticalAlignment.Top;
                stringTextBox.Margin = new Thickness(0, 10 + rowIndex * 40, 0, 0);
                stringTextBox.DataContext = item;
                stringTextBox.Width = 400;
                //stringTextBox.IsReadOnly = curGrid != DataGrid;
                stringTextBox.IsReadOnly = false;
                //stringTextBox.TextChanged += StringTextBox_TextChanged;
                stringTextBox.TextChanged += ChangeEvent;
                Grid.SetRow(stringTextBox, rowIndex);
                Grid.SetColumn(stringTextBox, 1);

                curGrid.Children.Add(stringTextBox);
            }
            else if (valueType.IsEnum)
            {
                System.Windows.Controls.ComboBox comboBox = new System.Windows.Controls.ComboBox();
                comboBox.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                comboBox.VerticalAlignment = VerticalAlignment.Top;
                comboBox.Width = 250;
                comboBox.Margin = new Thickness(0, 10 + rowIndex * 40, 0, 0);
                var items = Enum.GetValues(valueType);
                var itemSource = new List<object>();
                var needSave = false;
                for (var i = 0; i < items.Length; i++)
                {
                    //var key = $"enum_{valueType.Name}.{i}";
                    var itemContent = items.GetValue(i);
                    var key = $"enum_{valueType.Name}.{itemContent}";
                    if (!ComparisonTableController.Instance.GetData(key, out var objItemContent) || string.IsNullOrWhiteSpace(objItemContent))
                    {
                        ComparisonTableController.Instance.AddData(key, "");
                        itemSource.Add(itemContent);
                        needSave = true;
                    }
                    else
                    {
                        itemSource.Add(objItemContent);
                    }
                }

                if (needSave)
                    ComparisonTableController.Instance.SaveData();
                comboBox.ItemsSource = itemSource;
                var selectContentIndex = Array.IndexOf(items, item._PropertyInfo.GetValue(item._BelongData));
                if (selectContentIndex == -1)
                {
                    selectContentIndex = 0;
                }
                comboBox.SelectedItem = itemSource[selectContentIndex];
                comboBox.DataContext = item;
                comboBox.IsReadOnly = curGrid != DataGrid;
                //comboBox.SelectionChanged += ComboBox_SelectionChanged;
                comboBox.SelectionChanged += ChangeEvent;
                comboBox.Tag = items;
                Grid.SetRow(comboBox, rowIndex);
                Grid.SetColumn(comboBox, 1);
                curGrid.Children.Add(comboBox);
            }
            else if (typeof(IMessage).IsAssignableFrom(valueType))
            {
                var button = new System.Windows.Controls.Button();
                button.Content = "打开";
                button.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                button.VerticalAlignment = VerticalAlignment.Top;
                button.Width = 250;
                button.Margin = new Thickness(0, 10 + rowIndex * 40, 0, 0);
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
                var button = new System.Windows.Controls.Button();
                button.Content = "打开";
                button.Width = 250;
                button.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                button.VerticalAlignment = VerticalAlignment.Top;
                button.Margin = new Thickness(0, 10 + rowIndex * 40, 0, 0);
                button.Click += new RoutedEventHandler(OpenListData);
                button.DataContext = item._PropertyInfo.GetValue(item._BelongData);
                Grid.SetRow(button, rowIndex);
                Grid.SetColumn(button, 1);
                curGrid.Children.Add(button);
            }
        }

        private void NumberTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as System.Windows.Controls.TextBox;
            if (textBox != null)
            {
                string text = textBox.Text;
                string numericText = new string(text.Where(c => char.IsDigit(c) || c == '.' || c == '-').ToArray());

                // 确保负号只能出现在第一个字符位置
                if (numericText.IndexOf('-') > 0)
                {
                    numericText = numericText.Replace("-", "");
                    numericText = "-" + numericText;
                }

                // 确保小数点只能出现一次
                int dotIndex = numericText.IndexOf('.');
                if (dotIndex != -1)
                {
                    numericText = numericText.Substring(0, dotIndex + 1) + numericText.Substring(dotIndex + 1).Replace(".", "");
                }

                if (text != numericText)
                {
                    textBox.Text = numericText;
                    textBox.CaretIndex = numericText.Length; // 保持光标在文本末尾
                }

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
            var textBox = sender as System.Windows.Controls.TextBox;
            if (textBox != null)
            {
                var item = textBox.DataContext as DataPropertyItem;
                OnValueChanged(item, textBox.Text);
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as System.Windows.Controls.ComboBox;
            if (comboBox != null)
            {
                var item = comboBox.DataContext as DataPropertyItem;
                if (comboBox.Tag != null)
                {
                    OnValueChanged(item, ((Array)comboBox.Tag).GetValue(comboBox.SelectedIndex));
                    return;
                }

                OnValueChanged(item, comboBox.SelectedValue);
            }
        }

        private void OnValueChanged(DataPropertyItem item, object value)
        {
            if (item != null)
            {
                item._PropertyInfo.SetValue(item._BelongData, value);
                _CurrentOpenFile._IsDirty = true;
                AddCurrentFileInUpdateFiles();
                //item._DataItem._ListBoxItem
            }
        }

        private void OpenListData(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
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

                    ScrollViewer scrollViewer = new ScrollViewer();
                    scrollViewer.Height = 550;
                    window.Content = scrollViewer;
                    //window 增加一个Grid,与parent Grid一样
                    Grid grid = new Grid();
                    scrollViewer.Content = grid;
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
                System.Windows.Controls.Label groupLabel = new System.Windows.Controls.Label();
                groupLabel.Content = ListType + "-" + rowIndex;
                groupLabel.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                groupLabel.VerticalAlignment = VerticalAlignment.Top;
                groupLabel.Margin = new Thickness(10, 10 + rowIndex * 40, 0, 0);
                groupLabel.ContextMenu = new ContextMenu();
                //MenuItem descItem = new MenuItem();
                //descItem.Header = "备注";
                //descItem.Click += (sender, e) =>
                //{

                //};
                MenuItem delMenu = new MenuItem();
                delMenu.Header = "删除";
                delMenu.DataContext = new Tuple<int, IList, Grid>(rowIndex, data, grid);
                //delMenu.Click += DelMenu_Click;
                delMenu.Click += ChangeEvent;
                groupLabel.ContextMenu.Items.Add(delMenu);

                grid.Children.Add(groupLabel);

                var valueType = item.GetType();
                if (valueType == typeof(int) || valueType == typeof(float) || valueType == typeof(long) || valueType == typeof(double))
                {
                    System.Windows.Controls.TextBox numberTextBox = new System.Windows.Controls.TextBox();
                    numberTextBox.PreviewTextInput += new TextCompositionEventHandler(NumericTextBox_PreviewTextInput);
                    numberTextBox.PreviewKeyDown += new System.Windows.Input.KeyEventHandler(NumericTextBox_PreviewKeyDown);
                    numberTextBox.LostFocus += new RoutedEventHandler(NumericTextBox_LostFocus);
                    numberTextBox.Text = item.ToString();
                    numberTextBox.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                    numberTextBox.VerticalAlignment = VerticalAlignment.Top;
                    numberTextBox.Margin = new Thickness(0, 10 + rowIndex * 40, 0, 0);
                    numberTextBox.DataContext = new Tuple<int, IList, Type>(rowIndex, data, valueType);
                    numberTextBox.Tag = "numberTextBox";
                    //numberTextBox.TextChanged += NumberTextBox_TextChanged1;
                    numberTextBox.TextChanged += ChangeEvent;

                    Grid.SetRow(numberTextBox, rowIndex);
                    Grid.SetColumn(numberTextBox, 1);
                    grid.Children.Add(numberTextBox);
                }
                else if (valueType == typeof(string))
                {
                    System.Windows.Controls.TextBox stringTextBox = new System.Windows.Controls.TextBox();
                    stringTextBox.Text = item.ToString();
                    stringTextBox.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                    stringTextBox.VerticalAlignment = VerticalAlignment.Top;
                    stringTextBox.Margin = new Thickness(0, 10 + rowIndex * 40, 0, 0);
                    stringTextBox.DataContext = new Tuple<int, IList, Type>(rowIndex, data, valueType);
                    //stringTextBox.TextChanged += StringTextBox_TextChanged1;
                    stringTextBox.TextChanged += ChangeEvent;
                    Grid.SetRow(stringTextBox, rowIndex);
                    Grid.SetColumn(stringTextBox, 1);

                    grid.Children.Add(stringTextBox);
                }
                else if (valueType.IsEnum)
                {
                    System.Windows.Controls.ComboBox comboBox = new System.Windows.Controls.ComboBox();
                    comboBox.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                    comboBox.VerticalAlignment = VerticalAlignment.Top;
                    comboBox.Margin = new Thickness(0, 10 + rowIndex * 40, 0, 0);
                    comboBox.ItemsSource = Enum.GetValues(valueType);
                    comboBox.SelectedItem = item;
                    comboBox.DataContext = new Tuple<int, IList, Type>(rowIndex, data, valueType);
                    //comboBox.SelectionChanged += ComboBox_SelectionChanged1;
                    comboBox.SelectionChanged += ChangeEvent;
                    Grid.SetRow(comboBox, rowIndex);
                    Grid.SetColumn(comboBox, 1);
                    grid.Children.Add(comboBox);
                }
                else if (typeof(IMessage).IsAssignableFrom(valueType))
                {
                    var newButton = new System.Windows.Controls.Button();
                    newButton.Content = "打开";
                    newButton.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                    newButton.VerticalAlignment = VerticalAlignment.Top;
                    newButton.Margin = new Thickness(0, 10 + rowIndex * 40, 0, 0);
                    newButton.Click += new RoutedEventHandler(OpenNestedData);

                    newButton.DataContext = item;
                    Grid.SetRow(newButton, rowIndex);
                    Grid.SetColumn(newButton, 1);
                    grid.Children.Add(newButton);
                }
                else if (typeof(IList).IsAssignableFrom(valueType))
                {
                    var newButton = new System.Windows.Controls.Button();
                    newButton.Content = "打开";
                    newButton.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                    newButton.VerticalAlignment = VerticalAlignment.Top;
                    newButton.Margin = new Thickness(0, 10 + rowIndex * 40, 0, 0);
                    newButton.Click += new RoutedEventHandler(OpenListData);
                    newButton.DataContext = item;
                    Grid.SetRow(newButton, rowIndex);
                    Grid.SetColumn(newButton, 1);
                    grid.Children.Add(newButton);
                }
                rowIndex++;
            }

            var addItemButton = new System.Windows.Controls.Button();
            addItemButton.Content = "新增";
            addItemButton.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            addItemButton.VerticalAlignment = VerticalAlignment.Top;
            addItemButton.Margin = new Thickness(0, 10 + rowIndex * 40, 0, 0);
            addItemButton.DataContext = new Tuple<IList, Grid>(data, grid);
            //addItemButton.Click += AddItemButton_Click;
            addItemButton.Click += ChangeEvent;
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

            _CurrentOpenFile._IsDirty = true;
            AddCurrentFileInUpdateFiles();
            RefreshList(data.Item2, listType.Name, data.Item3);
        }

        private void AddItemButton_Click(object sender, RoutedEventArgs e)
        {
            var item = (System.Windows.Controls.Button)sender;
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
            AddCurrentFileInUpdateFiles();
            //data.Item2


        }

        private void ComboBox_SelectionChanged1(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as System.Windows.Controls.ComboBox;
            if (comboBox != null)
            {
                var data = comboBox.DataContext as Tuple<int, IList, Type>;

                OnListItemChanged(data.Item1, data.Item2, comboBox.SelectedItem);
            }
        }

        private void StringTextBox_TextChanged1(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as System.Windows.Controls.TextBox;
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
                AddCurrentFileInUpdateFiles();

            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void AddCurrentFileInUpdateFiles()
        {
            if (!_updateFiles.ContainsKey(_CurrentOpenFile._FileName) || _CurrentOpenFile.Tag == null)
            {
                var pakPath = _CurrentOpenFile._FilePath;

                var b1Index = _CurrentOpenFile._FilePath.IndexOf("b1");
                if (b1Index != -1)
                    pakPath = _CurrentOpenFile._FilePath.Substring(b1Index,
                        _CurrentOpenFile._FilePath.Length - b1Index);

                var outPath = Path.Combine(_config.TempFileDicPath.Value.ToString(), pakPath);
                _CurrentOpenFile.Tag = outPath;

                _updateFiles.TryAdd(_CurrentOpenFile._FileName, _CurrentOpenFile);
                Exporter.SaveDataFile(outPath, _CurrentOpenFile);

            }
            //标红
        }

        private void NumberTextBox_TextChanged1(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as System.Windows.Controls.TextBox;
            if (textBox != null)
            {
                string text = textBox.Text;
                string numericText = new string(text.Where(c => char.IsDigit(c) || c == '.' || c == '-').ToArray());

                // 确保负号只能出现在第一个字符位置
                if (numericText.IndexOf('-') > 0)
                {
                    numericText = numericText.Replace("-", "");
                    numericText = "-" + numericText;
                }

                // 确保小数点只能出现一次
                int dotIndex = numericText.IndexOf('.');
                if (dotIndex != -1)
                {
                    numericText = numericText.Substring(0, dotIndex + 1) + numericText.Substring(dotIndex + 1).Replace(".", "");
                }

                if (text != numericText)
                {
                    textBox.Text = numericText;
                    textBox.CaretIndex = numericText.Length; // 保持光标在文本末尾
                }
                var data = textBox.DataContext as Tuple<int, IList, Type>;

                if (data.Item3 == typeof(int))
                {
                    if (int.TryParse(textBox.Text, out var value))
                        OnListItemChanged(data.Item1, data.Item2, System.Convert.ToInt32(textBox.Text));
                }
                else if (data.Item3 == typeof(long))
                {
                    if (long.TryParse(textBox.Text, out var value))
                        OnListItemChanged(data.Item1, data.Item2, System.Convert.ToInt64(textBox.Text));
                }
                else if (data.Item3 == typeof(float))
                {
                    if (float.TryParse(textBox.Text, out var value))
                        OnListItemChanged(data.Item1, data.Item2, System.Convert.ToSingle(textBox.Text));
                }
                else if (data.Item3 == typeof(double))
                {
                    if (double.TryParse(textBox.Text, out var value))
                        OnListItemChanged(data.Item1, data.Item2, System.Convert.ToDouble(textBox.Text));
                }
            }
        }

        private void OpenNestedData(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            if (button != null)
            {
                var data = button.DataContext as IMessage;

                if (data == null)
                {
                    data = Activator.CreateInstance(data.GetType()) as IMessage;
                }

                if (data != null)
                {
                    var dataType = data.GetType();

                    //创建一个新的窗口
                    Window window = new Window();
                    window.Title = dataType.Name;
                    window.Width = 800;
                    window.Height = 600;
                    window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    window.Show();

                    ScrollViewer scrollViewer = new ScrollViewer();
                    scrollViewer.Height = 550;
                    window.Content = scrollViewer;
                    //window 增加一个Grid,与parent Grid一样
                    Grid grid = new Grid();
                    scrollViewer.Content = grid;
                    grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
                    grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
                    grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

                    //获取属性
                    //var properties = data.GetType().GetProperties();
                    if (!PropertiesDataController.Instance.Get(dataType, out var properties))
                    {
                        properties = PropertiesDataController.Instance.Add(dataType);
                    }

                    int rowIndex = 0;
                    foreach (var property in properties)
                    {
                        System.Windows.Controls.Label label = new System.Windows.Controls.Label();
                        var key = $"{property.DeclaringType.Name}.{property.Name}";
                        if (!ComparisonTableController.Instance.GetData(key, out var labelContent) || string.IsNullOrWhiteSpace(labelContent))
                        {
                            labelContent = $"{property.Name}";
                            ComparisonTableController.Instance.AddData(key, "");
                        }

                        label.Content = labelContent;
                        label.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                        label.VerticalAlignment = VerticalAlignment.Top;
                        label.Margin = new Thickness(10, 10 + rowIndex * 40, 0, 0);
                        grid.Children.Add(label);

                        DataPropertyItem dataPropertyItem = new DataPropertyItem();
                        dataPropertyItem._PropertyName = property.Name;
                        dataPropertyItem._PropertyDesc = "";
                        dataPropertyItem._PropertyInfo = property;
                        dataPropertyItem._BelongData = data;
                        ProcessPropertyType(property.PropertyType, dataPropertyItem, rowIndex, grid, 200);
                        rowIndex++;
                    }
                    ComparisonTableController.Instance.SaveData();
                }
            }
        }

        private void NumericTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as System.Windows.Controls.TextBox;
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

        private void NumericTextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // 允许使用退格键、删除键、Tab键、箭头键等
            if (e.Key == Key.Back || e.Key == Key.Delete || e.Key == Key.Tab ||
                e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Up || e.Key == Key.Down || e.Key == Key.LeftCtrl || e.Key == Key.C || e.Key == Key.V)
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

        private void FileSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            //将提示文本清空
            if (FileSearch.Text == "搜索配表文件")
            {
                FileSearch.Text = "";
            }
        }

        private void FileSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            //如果搜索框为空，则显示提示文本
            if (FileSearch.Text == "")
            {
                FileSearch.Text = "搜索配表文件";
            }
        }

        private void FileSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as System.Windows.Controls.TextBox;
            if (textBox != null)
            {
                if (!string.IsNullOrEmpty(textBox.Text) && textBox.Text != "搜索配表文件")
                {
                    var searchTerms = textBox.Text.Split(new[] { '|', '&' }, StringSplitOptions.RemoveEmptyEntries);
                    bool isOrSearch = textBox.Text.Contains('|');
                    bool isAndSearch = textBox.Text.Contains('&');

                    foreach (var file in _DataFiles.Values)
                    {
                        bool isMatch = isOrSearch ? false : true;
                        foreach (var term in searchTerms)
                        {
                            bool containsTerm = file._FileName.Contains(term, StringComparison.OrdinalIgnoreCase)
                                                || (!string.IsNullOrEmpty(file._Desc) && file._Desc.Contains(term, StringComparison.OrdinalIgnoreCase));

                            if (isOrSearch)
                            {
                                isMatch |= containsTerm;
                                if (isMatch) break;
                            }
                            else if (isAndSearch)
                            {
                                isMatch &= containsTerm;
                                if (!isMatch) break;
                            }
                            else
                            {
                                isMatch = containsTerm;
                            }
                        }
                        file._IsShow = isMatch;
                    }

                }
                else
                {
                    foreach (var file in _DataFiles.Values)
                    {
                        file._IsShow = true;
                    }
                }

                var files = _DataFiles.Values.ToList();
                files.Sort((a, b) => a._FileName.CompareTo(b._FileName));
                RefreshDataFile(files);
            }
        }

        private void ItemSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            //将提示文本清空
            if (ItemSearch.Text == "搜索ID或备注")
            {
                ItemSearch.Text = "";
            }
        }

        private void ItemSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            //如果搜索框为空，则显示提示文本
            if (ItemSearch.Text == "")
            {
                ItemSearch.Text = "搜索ID或备注";
            }
        }

        private void ItemSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_CurrentOpenFile == null) return;

            var textBox = sender as System.Windows.Controls.TextBox;
            if (textBox != null)
            {
                var itemList = _CurrentOpenFile._FileDataItemList;
                if (!string.IsNullOrEmpty(textBox.Text) && textBox.Text != "搜索ID或备注")
                {

                    var searchTerms = textBox.Text.Split(new[] { '|', '&' }, StringSplitOptions.RemoveEmptyEntries);
                    bool isOrSearch = textBox.Text.Contains('|');
                    bool isAndSearch = textBox.Text.Contains('&');

                    foreach (var item in itemList)
                    {
                        bool isMatch = isOrSearch ? false : true;
                        foreach (var term in searchTerms)
                        {
                            bool containsTerm = item._ID.ToString().Contains(term, StringComparison.OrdinalIgnoreCase)
                                                || (!string.IsNullOrEmpty(item._Desc) && item._Desc.Contains(term, StringComparison.OrdinalIgnoreCase));

                            if (isOrSearch)
                            {
                                isMatch |= containsTerm;
                                if (isMatch) break;
                            }
                            else if (isAndSearch)
                            {
                                isMatch &= containsTerm;
                                if (!isMatch) break;
                            }
                            else
                            {
                                isMatch = containsTerm;
                            }
                        }
                        item._IsShow = isMatch;
                    }
                }
                else
                {
                    foreach (var item in itemList)
                    {
                        item._IsShow = true;
                    }
                }

                RefreshFileDataItemList(_CurrentOpenFile._FileDataItemList);

                //RefreshDataFile(_DataFiles);
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private void HelpMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Window window = new Window();
            window.Title = "帮助";
            window.Width = 800;
            window.Height = 800;
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;

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
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                Margin = new Thickness(10)
            };

            // 创建一个水平StackPanel来并排显示两个二维码图片
            StackPanel qrCodePanel = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 10)
            };


            // 创建二维码图片
            System.Windows.Controls.Image qrCodeImage1 = new System.Windows.Controls.Image
            {
                Source = new BitmapImage(new Uri("pack://application:,,,/zfb.jpg")),
                Width = 150,
                Height = 150,
                Margin = new Thickness(5)
            };

            System.Windows.Controls.Image qrCodeImage2 = new System.Windows.Controls.Image
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
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20),
                FontSize = 18
            };


            qrCodePanel.Children.Add(qrCodeImage1);
            qrCodePanel.Children.Add(qrCodeImage2);

            string helpText = "";

            TextBlock tb1 = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Text = "视频教程",
                FontSize = 16
            };

            Hyperlink hl = new Hyperlink();
            hl.NavigateUri = new Uri("https://www.bilibili.com/video/BV15usSe7ETf");
            hl.RequestNavigate += Hyperlink_RequestNavigate;
            //设置HyplerLink的文字
            hl.Inlines.Add("禽兽-云轩 简易教程");
            //将HyperLink添加到TextBlock
            tb1.Inlines.Add(hl);

            TextBlock tb2 = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Text = "视频教程",
                FontSize = 16
            };

            Hyperlink yh_hl1 = new Hyperlink();
            yh_hl1.NavigateUri = new Uri("https://www.bilibili.com/video/BV1br4he8Eg1");
            yh_hl1.RequestNavigate += Hyperlink_RequestNavigate;
            //设置HyplerLink的文字
            yh_hl1.Inlines.Add("银河Buster 详细制作教程一");
            //将HyperLink添加到TextBlock
            tb2.Inlines.Add(yh_hl1);

            Hyperlink yh_hl2 = new Hyperlink();
            yh_hl2.NavigateUri = new Uri("https://www.bilibili.com/video/BV1TZ4UekEKq");
            yh_hl2.RequestNavigate += Hyperlink_RequestNavigate;
            //设置HyplerLink的文字
            yh_hl2.Inlines.Add(" 详细制作教程二");
            //将HyperLink添加到TextBlock
            tb2.Inlines.Add(yh_hl2);

            Hyperlink yh_hl3 = new Hyperlink();
            yh_hl3.NavigateUri = new Uri("https://www.bilibili.com/video/BV195t7eeEUc");
            yh_hl3.RequestNavigate += Hyperlink_RequestNavigate;
            //设置HyplerLink的文字
            yh_hl3.Inlines.Add(" 详细制作教程三\n");
            //将HyperLink添加到TextBlock
            tb2.Inlines.Add(yh_hl3);


            if (File.Exists("README.md"))
            {
                helpText = System.IO.File.ReadAllText("README.md");
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
            stackPanel.Children.Add(tb1);
            stackPanel.Children.Add(tb2);
            stackPanel.Children.Add(helpTextBlock);

            // 将StackPanel添加到ScrollViewer
            scrollViewer.Content = stackPanel;

            // 将ScrollViewer添加到Grid
            rootGrid.Children.Add(scrollViewer);

            // 将Grid设置为窗口的内容
            window.Content = rootGrid;
            window.Show();
        }

        private void AddNewDataItem_Click(object sender, RoutedEventArgs e)
        {
            if (_CurrentOpenFile == null)
                return;

            var list = _CurrentOpenFile._ListPropertyInfo.GetValue(_CurrentOpenFile._FileData, null) as IList;

            if (_CurrentOpenFile._FileDataItemList != null)
            {
                var newItemType = list.GetType().GetGenericArguments()[0];
                if (newItemType != null)
                {
                    var newItem = Activator.CreateInstance(newItemType) as IMessage;

                    if (newItem == null)
                        return;

                    var property = newItemType.GetProperty("Id");
                    if (property == null)
                    {
                        property = newItemType.GetProperty("ID");
                    }

                    if (property == null)
                        return;

                    DataItem dataItem = new DataItem();
                    dataItem._ID = _CurrentOpenFile.GetNewID();
                    property.SetValue(newItem, dataItem._ID, null);
                    _CurrentOpenFile._IDList.Add(dataItem._ID);
                    dataItem._Data = newItem;
                    dataItem._File = _CurrentOpenFile;
                    _CurrentOpenFile._FileDataItemList.Add(dataItem);

                    list.Add(newItem);

                    //_CurrentOpenFile._ListPropertyInfo.SetValue(_CurrentOpenFile._FileData, list, null);

                    RefreshFileDataItemList(_CurrentOpenFile._FileDataItemList);
                }
            }

        }

        private void GlobalSearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (GlobalSearchBox.Text == "全局搜索")
            {
                GlobalSearchBox.Text = "";
                GlobalSearchBox.Foreground = System.Windows.Media.Brushes.Black;
            }
        }

        private void GlobalSearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(GlobalSearchBox.Text))
            {
                GlobalSearchBox.Text = "全局搜索";
                GlobalSearchBox.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        private void GlobalSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {



        }
        private void SearchTimer_Tick(object sender, EventArgs e)
        {
            if (!_init)
                return;
            _SearchTimer.Stop();

            string searchText = GlobalSearchBox.Text;

            if (SearchResultsList == null) return;

            // 清空之前的搜索结果
            SearchResultsList.Items.Clear();

            try
            {
                if (!string.IsNullOrWhiteSpace(searchText) && searchText != "全局搜索")
                {
                    bool isRegex = RegexRadioButton.IsChecked == true;
                    bool isWildcard = WildcardRadioButton.IsChecked == true;
                    bool isExactMatch = ExactMatchRadioButton.IsChecked == true;
                    bool isTradition = TraditionContainsRadioButton.IsChecked == true;

                    // 将通配符转换为正则表达式
                    if (isWildcard)
                    {
                        searchText = "^" + Regex.Escape(searchText).Replace("\\*", ".*").Replace("\\?", ".") + "$";
                    }

                    var cache = _GlobalSearchCache;
                    if (isTradition)
                        cache = s_TraditionGlobalSearchCache;

                    foreach (var item in cache)
                    {
                        bool isMatch = false;

                        if (isRegex || isWildcard)
                        {
                            isMatch = Regex.IsMatch(item.Item1, searchText, RegexOptions.IgnoreCase);
                        }
                        else if (isExactMatch)
                        {
                            isMatch = string.Equals(item.Item1, searchText, StringComparison.OrdinalIgnoreCase);
                        }
                        else
                        {
                            isMatch = item.Item1.Contains(searchText, StringComparison.OrdinalIgnoreCase);
                        }

                        if (isMatch)
                        {
                            ListBoxItem listItem = new ListBoxItem();
                            listItem.Content = item.Item1;
                            listItem.DataContext = new Tuple<DataFile, DataItem>(item.Item2, item.Item3);
                            // listItem.MouseDoubleClick += OpenGlobalSearchItem;
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
            catch (Exception exception)
            {
                _logUtil.Error($"Error", exception);
            }
        }

        private void SearchModeRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            // 重新执行搜索
            SearchTimer_Tick(null, null);
        }

        private void OpenGlobalSearchItem(object sender, MouseButtonEventArgs e)
        {
            ListBoxItem listBoxItem = FindListBoxItem(e.OriginalSource as DependencyObject);
            if (listBoxItem != null)
            {
                var data = listBoxItem.DataContext as Tuple<DataFile, DataItem>;
                if (data != null)
                {
                    if (_lastActionComponent != listBoxItem)
                        _lastActionComponent = listBoxItem;
                    if (data.Item1 != null)
                    {
                        #region 注释代码块

                        // if (_CurrentOpenFile != null && _CurrentOpenFile._IsDirty)
                        // {
                        //     if (_config.AutoSaveFile)
                        //     {
                        //         //todo 等待操作
                        //     }
                        //
                        //     MessageBoxResult result = System.Windows.MessageBox.Show("切换data文件，当前修改将被还原", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        //
                        //     // 根据用户的选择执行相应的逻辑
                        //     if (result == MessageBoxResult.Yes)
                        //     {
                        //         _CurrentOpenFile._IsDirty = false;
                        //         OpenFile(data.Item1);
                        //
                        //         if (data.Item2 != null)
                        //         {
                        //             data.Item2.LoadData();
                        //             RefreshDataItemList(data.Item2._DataPropertyItems);
                        //
                        //             foreach (var item2 in data.Item1._FileDataItemList)
                        //             {
                        //                 if (item2._ID == data.Item2._ID)
                        //                 {
                        //
                        //                     DataItemList.ScrollIntoView(item2._ListBoxItem);
                        //                     DataItemList.SelectedItem = item2._ListBoxItem;
                        //                     break;
                        //                 }
                        //             }
                        //         }
                        //     }
                        // }
                        // else
                        // {

                        #endregion
                        DataFile file = listBoxItem.DataContext as DataFile;
                        OpenFile(data.Item1);

                        if (data.Item2 != null)
                        {
                            foreach (var item2 in data.Item1._FileDataItemList)
                            {
                                if (item2._ID == data.Item2._ID)
                                {
                                    item2.LoadData();
                                    RefreshDataItemList(item2._DataPropertyItems);
                                    DataItemList.ScrollIntoView(item2._ListBoxItem);
                                    DataItemList.SelectedItem = item2._ListBoxItem;
                                    break;
                                }
                            }
                        }
                        // }

                        FileList.ScrollIntoView(data.Item1._ListBoxItem);
                        FileList.SelectedItem = data.Item1._ListBoxItem;
                    }
                }
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            CloseAllOtherWindow();
            this.Close();
        }

        private void LoadComparisonInformation(object sender, EventArgs e)
        {
            var temp = new OpenFileDialog();
            temp.Title = "选择翻译文件路径";
            temp.InitialDirectory = System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            temp.CheckFileExists = false;
            if (temp.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var filePath = temp.FileName;
                ComparisonTableController.Instance.LoadData(filePath);
                _config.ComparisonTableFilePath.Value = filePath;
            }
        }

        private void LoadComparisonInformationTest(object sender, EventArgs e)
        {
            ComparisonTableController.Instance.NeedSave = true;
            ComparisonTableController.Instance.SaveData();
            return;
            var temp = new OpenFileDialog();
            temp.Title = "选择翻译文件路径";
            temp.InitialDirectory = System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            temp.CheckFileExists = false;
            if (temp.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var filePath = temp.FileName;
                ReadCsv(filePath, out var datas);
                if (datas != null && datas.Any())
                {
                    datas.ForEach(d =>
                    {
                        var splitStr = d.Split(',');
                        if (splitStr.Length < 2)
                            return;
                        var key = splitStr[0];
                        key = $"enum_ebguattrfloat.{key.ToLower()}";
                        var value = splitStr[1];
                        if (splitStr.Length >= 3)
                            value += $"[{splitStr[2].TrimEnd('。').TrimEnd(',')}]";
                        value = splitStr[0] + $"({value})";
                        ComparisonTableController.Instance.UpdateData(key, value);
                    });
                }
                ComparisonTableController.Instance.SaveData();
            }
        }

        /// <summary>
        /// 读CSV
        /// </summary>
        /// <param name="path"></param>
        /// <param name="data"></param>
        public static void ReadCsv(string path, out List<string> data)
        {
            StreamReader sr;
            data = new List<string>();
            try
            {
                using (sr = new StreamReader(path, Encoding.GetEncoding("GB2312")))
                {
                    string str = "";
                    while ((str = sr.ReadLine()) != null)
                    {
                        data.Add(str);
                    }
                }
            }
            catch (Exception ex)
            {
                foreach (Process process in Process.GetProcesses())
                {
                    if (process.ProcessName.ToUpper().Equals("EXCEL"))
                        process.Kill();
                }
                GC.Collect();
                Thread.Sleep(10);
                Console.WriteLine(ex.StackTrace);
                using (sr = new StreamReader(path, Encoding.GetEncoding("GB2312")))
                {
                    string str = "";
                    while ((str = sr.ReadLine()) != null)
                    {
                        data.Add(str);
                    }
                }
            }

        }

        #region 一些Checkbox的事件

        /// <summary>
        /// 自动保存
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AutoSaveFileCheck_OnCheckedOrUnChecked(object sender, RoutedEventArgs e)
        {
            _config.AutoSaveFile.Value = ((CheckBox)sender).IsChecked!.Value;
            if (_config.AutoSaveFile.Value.ToBool())
            {
                if (string.IsNullOrEmpty(_selectedSaveFolder))
                {
                    var result = MessageBox.Show("因开启了自动保存，请选择一个Mod文件夹用于保存修改后的数据", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    if (result == MessageBoxResult.OK)
                    {
                        System.Windows.Forms.FolderBrowserDialog
                            dialog = new System.Windows.Forms.FolderBrowserDialog();
                        dialog.Description = "请选择要保存Data数据的文件夹";
                        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            _selectedSaveFolder = dialog.SelectedPath;
                            StartAutoSaveTick();
                        }
                        else
                        {
                            _config.AutoSaveFile.Value = false;
                            AutoSaveFileCheck.IsChecked = _config.AutoSaveFile.Value.ToBool();
                        }
                    }
                }
                else
                {
                    StartAutoSaveTick();
                }
            }
            else
            {
                StopAutoSaveTimer();
            }
        }

        /// <summary>
        /// 是否显示源数据对比
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DisplaysSourceInformationCheck_OnCheckedOrUnChecked(object sender, RoutedEventArgs e)
        {
            _config.DisplaysSourceInformation.Value = ((CheckBox)sender).IsChecked!.Value;
        }

        private void AutoSearchInEffectCheck_OnCheckedOrUnChecked(object sender, RoutedEventArgs e)
        {
            _config.AutoSearchInEffect.Value = ((CheckBox)sender).IsChecked!.Value;
        }

        #endregion

        /// <summary>
        /// 清理记录
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClearLastUpdateLog(object sender, RoutedEventArgs e)
        {
            if (_updateFiles.IsEmpty)
                return;
            var keys = _updateFiles.Keys;
            foreach (var key in keys)
            {
                if (_updateFiles.TryRemove(key, out var dataFile) && dataFile.Tag is string path)
                {
                    dataFile.Tag = null;
                    dataFile.CanOpen = true;
                    File.Delete(path);
                }
            }

            if (_lastActionComponent != null)
            {
                CurrentOpenFileChange((_lastActionComponent as ListBoxItem).DataContext as DataFile);
            }

        }

        #region 设置

        private void SetListBox(ListBoxItem? listBoxItem, System.Windows.Media.Color? color, string? content)
        {
            if (listBoxItem == null)
                return;
            if (content == null && color == null)
                return;
            if (content != null)
                listBoxItem.Content = content;
            if (color != null)
                listBoxItem.Foreground = new SolidColorBrush(color.Value);
        }

        #endregion

        #region 事件

        /// <summary>
        /// 先这样，暂时先用着，后面再换
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChangeEvent(object sender, EventArgs e)
        {
            //(sender as  System.Windows.Controls.Control).DataContext

            if (sender is System.Windows.Controls.Control control)
            {
                switch (control.DataContext)
                {
                    case DataPropertyItem:
                        switch (sender)
                        {
                            case System.Windows.Controls.ComboBox:
                                ComboBox_SelectionChanged(sender, e as SelectionChangedEventArgs);
                                break;
                            case System.Windows.Controls.TextBox:
                                if (control.Tag != null)
                                    NumberTextBox_TextChanged(sender, e as TextChangedEventArgs);
                                else
                                    StringTextBox_TextChanged(sender, e as TextChangedEventArgs);
                                break;
                        }
                        break;
                    case Tuple<int, IList, Type>:
                        switch (sender)
                        {
                            case System.Windows.Controls.ComboBox:
                                ComboBox_SelectionChanged1(sender, e as SelectionChangedEventArgs);
                                break;
                            case System.Windows.Controls.TextBox:
                                if (control.Tag != null)
                                    NumberTextBox_TextChanged1(sender, e as TextChangedEventArgs);
                                else
                                    StringTextBox_TextChanged1(sender, e as TextChangedEventArgs);
                                break;
                        }
                        break;
                    case Tuple<int, IList, Grid>:
                        switch (sender)
                        {
                            case MenuItem:
                                DelMenu_Click(sender, e as RoutedEventArgs);
                                break;
                        }
                        break;
                    case Tuple<IList, Grid>:
                        switch (sender)
                        {
                            case System.Windows.Controls.Button:
                                AddItemButton_Click(sender, e as RoutedEventArgs);
                                break;
                        }
                        break;
                }
                //if (item._DataItem == null)
                //    return;
                //var changeData = new
                //{
                //    color = !item._DataItem._IsModified ? Colors.Blue : Colors.Black,
                //    content = $"{item._DataItem.DisplayName}{(item._DataItem._IsModified ? "*" : "")}"
                //};
                //_listBoxItemAction?.Invoke(item._DataItem._ListBoxItem, changeData.color, changeData.content);
            }

        }


        private int ChangeDataFunc(string key, object oldValue, object newValue)
        {
            _config.SaveTime = DateTime.Now;
            return 1;
        }
        #endregion
    }
}