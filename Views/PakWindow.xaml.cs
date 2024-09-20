using System.Diagnostics;
using System.IO;
using System.Windows;

namespace Wukong_PBData_ReadWriter_GUI.Views;

public partial class PakWindow
{
    public PakWindow()
    {
        InitializeComponent();
    }
    //private void RunBatFileWithFolder(string batPath, string folderPath)
    //{
    //    ProcessStartInfo processInfo = new ProcessStartInfo("cmd.exe", $"/c {batPath} \"{folderPath}\"")
    //    {
    //        CreateNoWindow = true,
    //        UseShellExecute = false
    //    };
    //    Process process = Process.Start(processInfo);
    //    process.WaitForExit();
    //}

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

    private void PackPakFile(object sender, DragEventArgs e)
    {
        //if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
        //if (e.Data.GetData(DataFormats.FileDrop) is not string[] files) return;
        //if (files.Length > 0 && Directory.Exists(files[0]))
        //{
        //    string folderPath = files[0];
        //    RunBatFileWithFolder(@"ref\\make_pak_compressed.bat", folderPath);
        //}
        //else
        //{
        //    MessageBox.Show("请拖拽一个文件夹");
        //}
    }

    private void UnpackPakFile(object sender, DragEventArgs e)
    {
        // 检查拖拽的数据是否是文件夹
        //if (e.Data.GetDataPresent(DataFormats.FileDrop))
        //{
        //    // 获取拖拽的文件路径
        //    string[] draggedItems = (string[])e.Data.GetData(DataFormats.FileDrop);


        //    RunBatFileWithFolder(@"ref\\make_pak_uncompressed.bat", draggedItems[0]);
        //}
    }

    private void Border_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = System.Windows.DragDropEffects.Copy;
        }
        else
        {
            e.Effects = System.Windows.DragDropEffects.None;
        }
    }

    private void LeftBorder_Drop(object sender, DragEventArgs e)
    {
        // HandleFileDrop(sender, e, Side.Left);
    }

    private void RightBorder_Drop(object sender, DragEventArgs e)
    {
        // HandleFileDrop(sender, e, Side.Right);
    }

    // private void HandleFileDrop(object sender, DragEventArgs e, Side side)
    // {
    //     if (e.Data.GetDataPresent(DataFormats.FileDrop))
    //     {
    //         string[] draggedFiles = (string[])e.Data.GetData(DataFormats.FileDrop);
    //         if (draggedFiles.Length > 0 && draggedFiles[0].EndsWith(".pak"))
    //         {
    //             string pakFilePath = draggedFiles[0];
    //             string directoryPath = Path.Combine(Path.GetDirectoryName(pakFilePath), Path.GetFileNameWithoutExtension(pakFilePath));
    //             RunBatFileWithFolder(@"ref\\make_pak_uncompressed.bat", pakFilePath);
    //             // 更新UI
    //             UpdateUIAfterDrop(directoryPath, (Border)sender, side);
    //         }
    //     }
    // }

    // private void UpdateUIAfterDrop(string directoryPath, Border targetBorder, Side side)
    // {
    //     targetBorder.Child = null;
    //     StackPanel newStack = new StackPanel();
    //
    //     ScrollViewer listScrollViewer = new ScrollViewer
    //     {
    //         VerticalScrollBarVisibility = ScrollBarVisibility.Auto
    //     };
    //
    //     ListBox listBox = new ListBox();
    //
    //    
    //
    //     if (side == Side.Left)
    //     {
    //         RefreshFileList(directoryPath, _LeftFiles, listBox);
    //     }
    //     else
    //     {
    //         RefreshFileList(directoryPath, _RightFiles, listBox);
    //     }
    //
    //     listScrollViewer.Content = listBox;
    //     newStack.Children.Add(listScrollViewer);
    //
    //     targetBorder.Child = newStack;
    // }

    // private void RefreshFileList(string directoryPath, List<DataFile> fileList, ListBox listBox)
    // {
    //     List<string> fileNames = new List<string>();
    //     List<string> filePaths = new List<string>();
    //     DataFileHelper.Director(directoryPath + "\\", fileNames, filePaths);
    //
    //     int index = 0;
    //     foreach (var item in fileNames)
    //     {
    //         var isValid = DataFileHelper.GetIsValidFile(item, filePaths[index]);
    //         if (!isValid)
    //         {
    //             index++;
    //             continue;
    //         }
    //         DataFile file = new DataFile();
    //         file._FileName = item;
    //         file._FilePath = filePaths[index];
    //         fileList.Add(file);
    //         ListBoxItem boxItem = new ListBoxItem();
    //         boxItem.Content = item;
    //         listBox.Items.Add(boxItem);
    //
    //         index++;
    //     }
    // }

    private void Merge_Click(object sender, RoutedEventArgs e)
    {
        // List<DataFile> mergeFile = new List<DataFile>();
        //
        // mergeFile.AddRange(_LeftFiles);
        //
        // foreach(var file in _RightFiles)
        // {
        //     var findFile = mergeFile.Find(x => x._FileName == file._FileName);
        //     if(findFile == null)
        //     {
        //         mergeFile.Add(file);
        //     }
        //     else
        //     {
        //         //用md5比较两个byte数组 是否相同
        //         file.LoadData();
        //         findFile.LoadData();
        //
        //
        //     }
        // }
        //
        //
        //
        // //Save mergeFile to Directory
        // string directoryPath = Path.Combine(Path.GetDirectoryName(_LeftFiles[0]._FilePath), "_Merge");
        // if(!Directory.Exists(directoryPath))
        // {
        //     Directory.CreateDirectory(directoryPath);
        // }
        //
        // foreach (var file in mergeFile)
        // {
        //     string targetPath = Path.Combine(directoryPath, file._FileName);
        //     if(File.Exists(targetPath))
        //     {
        //         File.Delete(targetPath); 
        //     }
        //     File.Copy(file._FilePath, targetPath, true);
        // }
    }
}