using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DragEventArgs = System.Windows.DragEventArgs;

namespace Wukong_PBData_ReadWriter_GUI.ViewModels;

public partial class MenuViewModel : ObservableObject
{
    [RelayCommand]
    private void OpenDataFolder()
    {
        Console.WriteLine("OpenDataFolder method called.");
        // 打开Data目录的逻辑
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
