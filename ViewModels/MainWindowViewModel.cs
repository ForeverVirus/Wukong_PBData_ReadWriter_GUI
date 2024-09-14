using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using Wukong_PBData_ReadWriter_GUI.Models;

namespace Wukong_PBData_ReadWriter_GUI.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private List<DataFile> _dataFiles = [];
    public string CurrentOpenFolder { get; set; } = "";

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(FilteredDataFiles))]
    private string _fileSearchText = string.Empty;

    public List<DataFile> FilteredDataFiles => _dataFiles
        .Where(f =>
            f.FileInfo.Name.Contains(FileSearchText, StringComparison.OrdinalIgnoreCase)
        ).ToList();

    public void ChangeOpenFolder(string dir)
    {
        CurrentOpenFolder = dir;
        _dataFiles = GetAllDataFiles(new DirectoryInfo(dir));
        OnPropertyChanged(nameof(FilteredDataFiles));
    }

    private List<DataFile> GetAllDataFiles(DirectoryInfo dirInfo)
    {
        var res = dirInfo.GetFiles().Where(file => file.Extension == ".data")
            .Select(fi => new DataFile(fi)).ToList();

        foreach (var subDirInfo in dirInfo.GetDirectories())
        {
            res.AddRange(GetAllDataFiles(subDirInfo));
        }

        return res;
    }

    [ObservableProperty] private DataFile? _selectedFile;

    [ObservableProperty] private DataItem? _selectedItem;
}