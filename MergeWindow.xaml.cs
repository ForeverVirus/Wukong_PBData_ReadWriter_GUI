using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Wukong_PBData_ReadWriter_GUI.src;
using static Wukong_PBData_ReadWriter_GUI.MergeWindow;
using DataFormats = System.Windows.DataFormats;
using DragEventArgs = System.Windows.DragEventArgs;
using ListBox = System.Windows.Controls.ListBox;
using Path = System.IO.Path;

namespace Wukong_PBData_ReadWriter_GUI
{
    /// <summary>
    /// MergeWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MergeWindow : Window
    {
        public List<DataFile> _LeftFiles = new List<DataFile>();
        public List<DataFile> _RightFiles = new List<DataFile>();
        public enum Side
        {
            Left,
            Right
        }

        public MergeWindow()
        {
            InitializeComponent();
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
                string[] draggedFiles = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (draggedFiles.Length > 0 && draggedFiles[0].EndsWith(".pak"))
                {
                    string pakFilePath = draggedFiles[0];
                    string directoryPath = Path.Combine(Path.GetDirectoryName(pakFilePath), Path.GetFileNameWithoutExtension(pakFilePath));
                    RunBatFileWithFolder(@"ref\\make_pak_uncompressed.bat", pakFilePath);
                    // 更新UI
                    UpdateUIAfterDrop(directoryPath, (Border)sender, side);
                }
            }
        }

        private void RunBatFileWithFolder(string batPath, string folderPath)
        {
            ProcessStartInfo processInfo = new ProcessStartInfo("cmd.exe", $"/c {batPath} \"{folderPath}\"")
            {
                CreateNoWindow = true,
                UseShellExecute = false
            };
            Process process = Process.Start(processInfo);
            process.WaitForExit();
        }

        private void UpdateUIAfterDrop(string directoryPath, Border targetBorder, Side side)
        {
            targetBorder.Child = null;
            StackPanel newStack = new StackPanel();

            ScrollViewer listScrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            ListBox listBox = new ListBox();

           

            if (side == Side.Left)
            {
                RefreshFileList(directoryPath, _LeftFiles, listBox);
            }
            else
            {
                RefreshFileList(directoryPath, _RightFiles, listBox);
            }

            listScrollViewer.Content = listBox;
            newStack.Children.Add(listScrollViewer);

            targetBorder.Child = newStack;
        }

        private void RefreshFileList(string directoryPath, List<DataFile> fileList, ListBox listBox)
        {
            List<string> fileNames = new List<string>();
            List<string> filePaths = new List<string>();
            Exporter.Director(directoryPath + "\\", fileNames, filePaths);

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
                fileList.Add(file);
                ListBoxItem boxItem = new ListBoxItem();
                boxItem.Content = item;
                listBox.Items.Add(boxItem);

                index++;
            }
        }

        private void Merge_Click(object sender, RoutedEventArgs e)
        {
            List<DataFile> mergeFile = new List<DataFile>();

            mergeFile.AddRange(_LeftFiles);

            foreach(var file in _RightFiles)
            {
                var findFile = mergeFile.Find(x => x._FileName == file._FileName);
                if(findFile == null)
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
            string directoryPath = Path.Combine(Path.GetDirectoryName(_LeftFiles[0]._FilePath), "_Merge");
            if(!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            foreach (var file in mergeFile)
            {
                string targetPath = Path.Combine(directoryPath, file._FileName);
                if(File.Exists(targetPath))
                {
                    File.Delete(targetPath); 
                }
                File.Copy(file._FilePath, targetPath, true);
            }
        }
    }
}
