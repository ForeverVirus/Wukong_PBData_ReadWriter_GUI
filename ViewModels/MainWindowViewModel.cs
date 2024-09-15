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
            f.DisplayName.Contains(FileSearchText, StringComparison.OrdinalIgnoreCase)
        ).ToList();

    public void ChangeOpenFolder(string dir)
    {
        CurrentOpenFolder = dir;
        _dataFiles = DataFileHelper.GetAllDataFiles(new DirectoryInfo(dir));
        OnPropertyChanged(nameof(FilteredDataFiles));
    }
}