using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Wukong_PBData_ReadWriter_GUI.Models;

namespace Wukong_PBData_ReadWriter_GUI.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private DataFile[] _dataFiles = [];
    public string CurrentOpenFolder { get; set; } = string.Empty;

    // [ObservableProperty] [NotifyPropertyChangedFor(nameof(FilteredDataFiles))]
    // private string _fileSearchText = string.Empty;
    [ObservableProperty] private DataFile[] _filteredDataFiles = [];
    [ObservableProperty] private DataItem[] _filteredDataItems = [];
    [ObservableProperty] private DataFile? _selectedFile;

    public string FileSearchText
    {
        set
        {
            FilteredDataFiles = _dataFiles
                .Where(f => f.DisplayName.Contains(value, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }
    }

    public string ItemSearchText
    {
        set
        {
            FilteredDataItems = SelectedFile == null
                ? []
                : SelectedFile.DataItemList
                    .Where(i => i.Desc.Contains(value, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
        }
    }

    partial void OnSelectedFileChanged(DataFile? value)
    {
        FilteredDataItems = value == null ? [] : value.DataItemList.ToArray();
    }

    [RelayCommand]
    private void OpenFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "请选择Data数据文件夹"
        };
        if (dialog.ShowDialog() != true) return;
        CurrentOpenFolder = dialog.FolderName;
        _dataFiles = Directory
            .GetFiles(dialog.FolderName, "*.data", SearchOption.AllDirectories)
            .Select(file => (data: DataFileHelper.GetDataByFile(file), file))
            .Where(tuple => tuple.data != null)
            .Select(tuple => new DataFile(tuple.file, tuple.data!))
            .ToArray();
        FileSearchText = "";
    }

    [RelayCommand]
    private void SaveAll()
    {
        foreach (var file in _dataFiles)
        {
            if (!file.IsDirty) continue;
            file.SaveDataFile();
        }
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

    [RelayCommand]
    private void SaveAllAs()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "请选择要保存Data数据的文件夹"
        };
        if (dialog.ShowDialog() == true)
        {
            var dir = dialog.FolderName;
            foreach (var file in _dataFiles)
            {
                var newPath = file.FilePath.Replace(CurrentOpenFolder, dir);
                file.SaveDataFile(newPath);
            }
            //
            //     var pakPath = _CurrentOpenFile._FilePath;
            //
            //     var b1Index = _CurrentOpenFile._FilePath.IndexOf("b1");
            //     if (b1Index != -1)
            //         pakPath = _CurrentOpenFile._FilePath.Substring(b1Index,
            //             _CurrentOpenFile._FilePath.Length - b1Index);
            //
            // var outPath = Path.Combine(dir, pakPath);

            // DataFileHelper.SaveDataFile(outPath, );
        }
    }

    [RelayCommand]
    private void ImportDescription()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Json|*.json",
            Title = "导入备注配置"
        };
        if (dialog.ShowDialog() != true) return;
        var newDict = DataFileHelper.ImportDescriptionConfig(dialog.FileName);
        foreach (var kvp in newDict)
        {
            DataFileHelper.DescriptionConfig[kvp.Key] = kvp.Value;
        }
    }

    [RelayCommand]
    private void ExportDescription()
    {
        var dialog = new SaveFileDialog
        {
            AddExtension = true,
            Filter = "Json|*.json",
            Title = "导出备注配置"
        };
        if (dialog.ShowDialog() != true) return;
        DataFileHelper.ExportDescriptionConfig(dialog.FileName);
    }

    [RelayCommand]
    private void SaveFile(DataFile file)
    {
        if (!file.IsDirty) return;
        file.SaveDataFile();
    }

    [RelayCommand]
    private void SaveFileAs(DataFile file)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "请选择要保存Data数据的文件夹"
        };
        if (dialog.ShowDialog() is not true) return;

        var dir = dialog.FolderName;
        var newPath = file.FilePath.Replace(CurrentOpenFolder, dir);
        file.SaveDataFile(newPath);
    }
    
    [RelayCommand]
    private void AddNewItem()
    {
        if (SelectedFile == null) return;
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
    }
    
    [RelayCommand]
    private void CloneItem()
    {
        
    }

    [RelayCommand]
    private void DeleteItem()
    {
        
    }
}