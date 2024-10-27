using System.Windows;
using System.Windows.Controls;
using Wukong_PBData_ReadWriter_GUI.src;
using DataFormats = System.Windows.DataFormats;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using ListBox = System.Windows.Controls.ListBox;
using Path = System.IO.Path;

namespace Wukong_PBData_ReadWriter_GUI;

/// <summary>
///     MergeWindow.xaml 的交互逻辑
/// </summary>
public partial class MergeWindow : Window
{
    public enum Side
    {
        Left,
        Right
    }

    public List<DataFile> _LeftFiles = new();
    public List<DataFile> _RightFiles = new();

    public MergeWindow()
    {
        InitializeComponent();

        Title = "合并PAK";
    }

    private void Border_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
            e.Effects = DragDropEffects.Copy;
        else
            e.Effects = DragDropEffects.None;
    }

    private void LeftBorder_Drop(object sender, DragEventArgs e)
    {
        HandleFileDrop(sender, e, Side.Left);
    }

    private void RightBorder_Drop(object sender, DragEventArgs e)
    {
        HandleFileDrop(sender, e, Side.Right);
    }

    private void HandleFileDrop(object sender, DragEventArgs e, Side side)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var draggedFiles = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (draggedFiles.Length > 0 && draggedFiles[0].EndsWith(".pak"))
            {
                var pakFilePath = draggedFiles[0];
                var directoryPath = Path.Combine(Path.GetDirectoryName(pakFilePath),
                    Path.GetFileNameWithoutExtension(pakFilePath));
                RunBatFileWithFolder(@"ref\\make_pak_uncompressed.bat", pakFilePath);
                // 更新UI
                UpdateUIAfterDrop(directoryPath, (Border)sender, side);
            }
        }
    }

    private void RunBatFileWithFolder(string batPath, string folderPath)
    {
        var processInfo = new ProcessStartInfo("cmd.exe", $"/c {batPath} \"{folderPath}\"")
        {
            CreateNoWindow = true,
            UseShellExecute = false
        };
        var process = Process.Start(processInfo);
        process.WaitForExit();
    }

    private void UpdateUIAfterDrop(string directoryPath, Border targetBorder, Side side)
    {
        targetBorder.Child = null;
        var newStack = new StackPanel();

        var listScrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        var listBox = new ListBox();


        if (side == Side.Left)
            RefreshFileList(directoryPath, _LeftFiles, listBox);
        else
            RefreshFileList(directoryPath, _RightFiles, listBox);

        listScrollViewer.Content = listBox;
        newStack.Children.Add(listScrollViewer);

        targetBorder.Child = newStack;
    }

    private void RefreshFileList(string directoryPath, List<DataFile> fileList, ListBox listBox)
    {
        var fileNames = new List<string>();
        var filePaths = new List<string>();
        Exporter.Director(directoryPath + "\\", fileNames, filePaths);

        var index = 0;
        foreach (var item in fileNames)
        {
            var isValid = Exporter.GetIsValidFile(item, filePaths[index]);
            if (!isValid)
            {
                index++;
                continue;
            }

            var file = new DataFile();
            file._FileName = item;
            file._FilePath = filePaths[index];
            fileList.Add(file);
            var boxItem = new ListBoxItem();
            boxItem.Content = item;
            listBox.Items.Add(boxItem);

            index++;
        }
    }

    private void Merge_Click(object sender, RoutedEventArgs e)
    {
        var mergeFile = new List<DataFile>();

        mergeFile.AddRange(_LeftFiles);

        foreach (var file in _RightFiles)
        {
            var findFile = mergeFile.Find(x => x._FileName == file._FileName);
            if (findFile == null)
            {
                mergeFile.Add(file);
            }
            else
            {
                //用md5比较两个byte数组 是否相同
                file.LoadData();
                findFile.LoadData();
            }
        }


        //Save mergeFile to Directory
        var directoryPath = Path.Combine(Path.GetDirectoryName(_LeftFiles[0]._FilePath), "_Merge");
        if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);

        foreach (var file in mergeFile)
        {
            var targetPath = Path.Combine(directoryPath, file._FileName);
            if (File.Exists(targetPath)) File.Delete(targetPath);
            File.Copy(file._FilePath, targetPath, true);
        }
    }
}