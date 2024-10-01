using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DragEventArgs = System.Windows.DragEventArgs;

namespace Wukong_PBData_ReadWriter_GUI.ViewModels;

public class PakDecompressViewModel : ObservableObject
{
    public PakDecompressViewModel()
    {
        // 初始化 DropCommand
        DropCommand = new RelayCommand<DragEventArgs>(OnDrop, CanDrop);
        Console.WriteLine("DropCommand initialized.");
    }


    public RelayCommand<DragEventArgs> DropCommand { get; }

    private void OnDrop(DragEventArgs? e)
    {
        Console.WriteLine("OnDrop executed");
        // 检查拖拽的数据是否是文件夹
        if (e is not { Data: not null } || !e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            return;

        // 获取拖拽的文件路径
        var draggedItems = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop)!;

        // 遍历拖拽的文件夹路径并执行.bat 文件
        foreach (var item in draggedItems)
        {
            Console.WriteLine("draggedItems: " + item);
            RunBatFileWithFolder(@".\\ref\\make_pak_uncompressed.bat", item);
        }
    }

    private bool CanDrop(DragEventArgs? e)
    {
        // 根据需要定义 CanExecute 逻辑
        bool canDrop = e is { Data: not null } && e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop);
        Console.WriteLine("canDrop: " + canDrop);
        return canDrop;
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
            System.Windows.MessageBox.Show($"Output: {output}\nError: {error}");
        }
    }
}