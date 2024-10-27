using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wukong_PBData_ReadWriter_GUI.Services;
using Wukong_PBData_ReadWriter_GUI.src;
using DragEventArgs = System.Windows.DragEventArgs;

namespace Wukong_PBData_ReadWriter_GUI.ViewModels;

public partial class MenuViewModel(ISharedDataService sharedDataService) : ObservableObject
{
    private readonly ISharedDataService _sharedDataService = sharedDataService;

    [ObservableProperty] private ObservableCollection<DataFile> _dataFiles = new ObservableCollection<DataFile>();


    [RelayCommand]
    private void OpenDataFolder()
    {
        Console.WriteLine("OpenDataFolder method called.");
        var _config = sharedDataService.GlobalData.config;


        //选择文件夹,并返回选择的文件夹路径，FolderBrowserDialog是一个选择文件夹的对话框
        System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
        dialog.Description = "请选择Data数据文件夹";
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            _config.DataFilePath.Value = dialog.SelectedPath;
            // ClearTempFiles();
            // _updateFiles.Clear();
            RefreshFolderFile(dialog.SelectedPath);
            // CloseAllOtherWindow();
            // _CurrentOpenFile = null;
            // _selectedSaveFolder = string.Empty;
            //
            //
            // if (_GlobalSearchTask != null && !_GlobalSearchTask.IsCompleted)
            // {
            //     _GlobalSearchTask = null;
            //     _GlobalSearchCache.Clear();
            //     s_TraditionGlobalSearchCache.Clear();
            // }
            //
            // _GlobalSearchTask = CacheGlobalSearchAsync(_DataFiles.Values.ToList());
            // await _GlobalSearchTask;


            // var files = _DataFiles.Values.ToList();
            // files.Sort((a, b) => a._FileName.CompareTo(b._FileName));
            // s_DescriptionConfig = Exporter.GenerateFirstDescConfig(files);
            // _MD5Config = Exporter.CollectItemMD5(files);
            // _OrigItemData = Exporter.CollectItemBytes(files);
        }
    }

    private void RefreshFolderFile(string dir)
    {
        ConcurrentDictionary<string, DataFile> dataFiles = new();

        //将选择的文件夹路径显示在文本框中
        // _CurrentOpenFolder = dir;
        List<string> fileNames = new List<string>();
        List<string> filePaths = new List<string>();
        Exporter.Director(dir + "\\", fileNames, filePaths);

        dataFiles.Clear();
        DataFiles.Clear();
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
            // if (_updateFiles.TryGetValue(item, out var oldFile))
            // {
            //     file.Tag = oldFile.Tag;
            //     file.CanOpen = true;
            // }

            dataFiles.TryAdd(item, file);
            DataFiles.Add(file);
            index++;
        }

        //把_DataFiles绑定到FileList上并自动生成 ListBoxItem, 每个Item显示FileName 并且对应有一个打开按钮
        //把 _DataFiles.Values to List 并按FileName的首字母排序 从小到大排序
        var files = dataFiles.Values.ToList();
        files.Sort((a, b) => a._FileName.CompareTo(b._FileName));
        Console.WriteLine("ssss: " + DataFiles.Count);
        
        foreach (var dataFile in files)
        {
            Console.WriteLine($"loadFile: {dataFile._FileName} = {dataFile._FilePath}");
        }

        RefreshDataFile(files);
    }

    private void RefreshDataFile(List<DataFile> files)
    {
        foreach (var item in files)
        {
            if (!item._IsShow) continue;

            ListBoxItem listBoxItem = new ListBoxItem();
            var hasTag = item.Tag != null;
            listBoxItem.Content = item._FileName + (hasTag ? "*" : "");
            // listBoxItem.MouseDoubleClick += new MouseButtonEventHandler(OpenDataFile);
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
            var descSuccessAction = () => { RefreshDataFile(files); };
            menuItem.DataContext = new Tuple<string, Action>(descKey, descSuccessAction);
            // menuItem.Click += OpenDescriptionWindow;
            listBoxItem.ContextMenu.Items.Add(menuItem);

            MenuItem topMenuItem = new MenuItem();
            topMenuItem.Header = "置顶";
            topMenuItem.DataContext = item;
            // topMenuItem.Click += SetTopFile;
            listBoxItem.ContextMenu.Items.Add(topMenuItem);

            MenuItem openFolderMenuItem = new MenuItem();
            openFolderMenuItem.Header = "打开所在文件夹";
            openFolderMenuItem.DataContext = item._FilePath;
            // openFolderMenuItem.Click += OpenContainingFolder_Click;
            listBoxItem.ContextMenu.Items.Add(openFolderMenuItem);

            // _dataFiles.Add(listBoxItem.Content as ListBoxItem);
        }

        // RefreshTopFileList();
    }
    
    private void FileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        throw new NotImplementedException();
    }

    [RelayCommand]
    private void SaveDataFile()
    {
        Console.WriteLine("SaveDataFile method called.");
        // 保存Data文件的逻辑
    }

    [RelayCommand]
    private void SaveAsNewDataFile()
    {
        Console.WriteLine("SaveAsNewDataFile method called.");
        // 另存为Data文件的逻辑
    }

    [RelayCommand]
    private void ImportDescription()
    {
        Console.WriteLine("ImportDescription method called.");
        // 导入备注配置的逻辑
    }

    [RelayCommand]
    private void ExportDescription()
    {
        Console.WriteLine("ExportDescription method called.");
        // 导出备注配置的逻辑
    }

    [RelayCommand]
    private void CreatePak()
    {
        Console.WriteLine("CreatePak method called.");
        // 生成PAK的逻辑
    }

    [RelayCommand]
    private void DecompressPak()
    {
        Console.WriteLine("DecompressPak method called.");
        // 解包PAK的逻辑
    }

    [RelayCommand]
    private void LoadComparisonInformation()
    {
        Console.WriteLine("LoadComparisonInformation method called.");
        // 加载翻译项的逻辑
    }

    [RelayCommand]
    private void LoadComparisonInformationTest()
    {
        Console.WriteLine("LoadComparisonInformationTest method called.");
        // 保存翻译项的逻辑
    }

    [RelayCommand]
    private void Close()
    {
        Console.WriteLine("Close method called.");
        // 退出的逻辑
    }

    [RelayCommand]
    private void ToggleAutoSave()
    {
        Console.WriteLine("ToggleAutoSave method called.");
        // 自动保存的逻辑
    }

    [RelayCommand]
    private void ToggleDisplaySourceInformation()
    {
        Console.WriteLine("ToggleDisplaySourceInformation method called.");
        // 显示旧数据的逻辑
    }

    [RelayCommand]
    private void ToggleAutoSearchInEffect()
    {
        Console.WriteLine("ToggleAutoSearchInEffect method called.");
        // 搜索功能自动生效的逻辑
    }

    [RelayCommand]
    private void ClearLastUpdateLog()
    {
        Console.WriteLine("ClearLastUpdateLog method called.");
        // 清理修改记录的逻辑
    }

    private void Help()
    {
        Console.WriteLine("Help method called.");
        // 帮助的逻辑
    }
}