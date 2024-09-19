using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wukong_PBData_ReadWriter_GUI.Models;

namespace Wukong_PBData_ReadWriter_GUI.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private List<DataFile> _dataFiles = [];
    public string CurrentOpenFolder { get; set; } = string.Empty;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(FilteredDataFiles))]
    private string _fileSearchText = string.Empty;

    public List<DataFile> FilteredDataFiles => _dataFiles
        .Where(f =>
            f.DisplayName.Contains(FileSearchText, StringComparison.OrdinalIgnoreCase)
        ).ToList();

    [RelayCommand]
    private void OpenFolder()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "请选择Data数据文件夹"
        };
        if (dialog.ShowDialog() != true) return;
        CurrentOpenFolder = dialog.FolderName;
        _dataFiles = Directory
            .GetFiles(dialog.FolderName, "*.data", SearchOption.AllDirectories)
            .Select(file => new DataFile(file))
            .ToList();
        OnPropertyChanged(nameof(FilteredDataFiles));
    }

    [RelayCommand]
    private void Save()
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
    private void SaveAs()
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

    [RelayCommand]
    private void ImportDescription()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
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
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            AddExtension = true,
            Filter = "Json|*.json",
            Title = "导出备注配置"
        };
        if (dialog.ShowDialog() != true) return;
        DataFileHelper.ExportDescriptionConfig(dialog.FileName);
    }
}