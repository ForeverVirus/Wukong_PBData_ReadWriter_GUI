using BtlShare;
using Google.Protobuf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Wukong_PBData_ReadWriter_GUI.src;

namespace Wukong_PBData_ReadWriter_GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public List<DataFile> _DataFiles = new List<DataFile>();

        public MainWindow()
        {

        }

        private void OpenDataFolder(object sender, RoutedEventArgs e)
        {
            //选择文件夹,并返回选择的文件夹路径，FolderBrowserDialog是一个选择文件夹的对话框
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = "请选择Data数据文件夹";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                //将选择的文件夹路径显示在文本框中
                string dir = dialog.SelectedPath;
                List<string> fileNames = new List<string>();
                List<string> filePaths = new List<string>();
                Exporter.Director(dir + "\\", fileNames, filePaths);

                int index = 0;
                foreach(var item in fileNames)
                {
                    var isValid = Exporter.GetIsValidFile(item, filePaths[index]);
                    if (!isValid)
                        continue;
                    DataFile file = new DataFile();
                    file._FileName = item;
                    file._FilePath = filePaths[index];
                    _DataFiles.Add(file);
                    index++;
                }

                //把_DataFiles绑定到FileList上并自动生成 ListBoxItem, 每个Item显示FileName 并且对应有一个打开按钮
                foreach(var item in _DataFiles)
                {
                    ListBoxItem listBoxItem = new ListBoxItem();
                    listBoxItem.Content = item._FileName;
                    listBoxItem.MouseDoubleClick += new MouseButtonEventHandler(OpenDataFile);
                    listBoxItem.DataContext = item;
                    FileList.Items.Add(listBoxItem);
                }
            }
        }

        private void OpenDataFile(object sender, MouseButtonEventArgs e)
        {
            ListBoxItem listBoxItem = sender as ListBoxItem;
            if(listBoxItem != null)
            {
                DataFile file = listBoxItem.DataContext as DataFile;
                if (file != null)
                {
                    DataItemList.Items.Clear();
                    file.LoadData();

                    if(file._FileDataItemList != null && file._FileDataItemList.Count > 0)
                    {
                        foreach(var item in file._FileDataItemList)
                        {
                            ListBoxItem listItem = new ListBoxItem();
                            listItem.Content = item._ID + "  " + item._Desc;
                            listItem.DataContext = item;
                            listItem.MouseDoubleClick += new MouseButtonEventHandler(OpenDataItem);
                            listItem.MouseRightButtonUp += new MouseButtonEventHandler(DescripeDataItem);
                            DataItemList.Items.Add(listItem);
                        }
                    }
                }
            }
        }

        private void DescripeDataItem(object sender, MouseButtonEventArgs e)
        {
            
        }

        private void OpenDataItem(object sender, MouseButtonEventArgs e)
        {
            ListBoxItem listBoxItem = sender as ListBoxItem;
            if (listBoxItem != null)
            {
                var data = listBoxItem.DataContext as DataItem;
                if (data != null)
                {
                    DataGrid.RowDefinitions.Clear();
                    DataGrid.Children.Clear();

                    if (data._Data == null)
                    {
                        return;
                    }

                    DataGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    data.LoadData();

                    int rowIndex = 0;
                    foreach (var item in data._DataPropertyItems)
                    {
                        System.Windows.Controls.Label label = new System.Windows.Controls.Label();
                        label.Content = item._PropertyName;
                        Grid.SetRow(label, rowIndex);
                        Grid.SetColumn(label, 0);
                        label.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                        label.VerticalAlignment = VerticalAlignment.Top;
                        label.Margin = new Thickness(0, 10 + rowIndex * 30, 0, 0);
                        DataGrid.Children.Add(label);

                        //var value = property.GetValue(data._Data);

                        var valueType = item._PropertyInfo.PropertyType;
                        ProcessPropertyType(valueType, item, rowIndex, DataGrid, 300);
                        rowIndex++;
                    }
                }
            }
        }

        private void ProcessPropertyType(Type valueType, DataPropertyItem item, int rowIndex, Grid curGrid, int left)
        {
            if (valueType == typeof(int) || valueType == typeof(float) || valueType == typeof(long) || valueType == typeof(double))
            {
                System.Windows.Controls.TextBox numberTextBox = new System.Windows.Controls.TextBox();
                numberTextBox.PreviewTextInput += new TextCompositionEventHandler(NumericTextBox_PreviewTextInput);
                numberTextBox.PreviewKeyDown += new System.Windows.Input.KeyEventHandler(NumericTextBox_PreviewKeyDown);
                numberTextBox.LostFocus += new RoutedEventHandler(NumericTextBox_LostFocus);
                numberTextBox.Text = item._PropertyInfo.GetValue(item._BelongData).ToString();
                numberTextBox.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                numberTextBox.VerticalAlignment = VerticalAlignment.Top;
                numberTextBox.Margin = new Thickness(0, 10 + rowIndex * 30, 0, 0);

                Grid.SetRow(numberTextBox, rowIndex);
                Grid.SetColumn(numberTextBox, 1);
                curGrid.Children.Add(numberTextBox);
            }
            else if (valueType == typeof(string))
            {
                System.Windows.Controls.TextBox stringTextBox = new System.Windows.Controls.TextBox();
                stringTextBox.Text = item._PropertyInfo.GetValue(item._BelongData).ToString();
                stringTextBox.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                stringTextBox.VerticalAlignment = VerticalAlignment.Top;
                stringTextBox.Margin = new Thickness(0, 10 + rowIndex * 30, 0, 0);
                Grid.SetRow(stringTextBox, rowIndex);
                Grid.SetColumn(stringTextBox, 1);

                curGrid.Children.Add(stringTextBox);
            }
            else if (valueType.IsEnum)
            {
                System.Windows.Controls.ComboBox comboBox = new System.Windows.Controls.ComboBox();
                comboBox.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                comboBox.VerticalAlignment = VerticalAlignment.Top;
                comboBox.Margin = new Thickness(0, 10 + rowIndex * 30, 0, 0);
                comboBox.ItemsSource = Enum.GetValues(valueType);
                comboBox.SelectedItem = item._PropertyInfo.GetValue(item._BelongData);
                Grid.SetRow(comboBox, rowIndex);
                Grid.SetColumn(comboBox, 1);
                curGrid.Children.Add(comboBox);
            }
            else if (typeof(IMessage).IsAssignableFrom(valueType))
            {
                var button = new System.Windows.Controls.Button();
                button.Content = "打开";
                button.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                button.VerticalAlignment = VerticalAlignment.Top;
                button.Margin = new Thickness(0, 10 + rowIndex * 30, 0, 0);
                button.Click += new RoutedEventHandler(OpenNestedData);
                button.DataContext = item._PropertyInfo.GetValue(item._BelongData);
                Grid.SetRow(button, rowIndex);
                Grid.SetColumn(button, 1);
                curGrid.Children.Add(button);
            }
            else if (typeof(IList).IsAssignableFrom(valueType))
            {
                var button = new System.Windows.Controls.Button();
                button.Content = "打开";
                button.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                button.VerticalAlignment = VerticalAlignment.Top;
                button.Margin = new Thickness(0, 10 + rowIndex * 30, 0, 0);
                button.Click += new RoutedEventHandler(OpenListData);
                button.DataContext = item._PropertyInfo.GetValue(item._BelongData);
                Grid.SetRow(button, rowIndex);
                Grid.SetColumn(button, 1);
                curGrid.Children.Add(button);
            } 
        }

        private void OpenListData(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            if (button != null)
            {
                var data = button.DataContext as IList;
                if (data != null)
                {
                    Window window = new Window();
                    var ListType = data.GetType().GetGenericArguments()[0].Name;
                    window.Title = $"Repeated<{ListType}>";
                    window.Width = 800;
                    window.Height = 600;
                    window.Show();
                    //window 增加一个Grid,与parent Grid一样
                    Grid grid = new Grid();
                    window.Content = grid;
                    grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
                    grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
                    grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

                    //var list = data._PropertyInfo.GetValue(data._BelongData) as IList;
                    int rowIndex = 0;
                    foreach (var item in data)
                    {
                        System.Windows.Controls.Label groupLabel = new System.Windows.Controls.Label();
                        groupLabel.Content = ListType + "-" + rowIndex;
                        groupLabel.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                        groupLabel.VerticalAlignment = VerticalAlignment.Top;
                        groupLabel.Margin = new Thickness(10, 10 + rowIndex * 30, 0, 0);
                        grid.Children.Add(groupLabel);

                        var valueType = item.GetType();
                        if (valueType == typeof(int) || valueType == typeof(float) || valueType == typeof(long) || valueType == typeof(double))
                        {
                            System.Windows.Controls.TextBox numberTextBox = new System.Windows.Controls.TextBox();
                            numberTextBox.PreviewTextInput += new TextCompositionEventHandler(NumericTextBox_PreviewTextInput);
                            numberTextBox.PreviewKeyDown += new System.Windows.Input.KeyEventHandler(NumericTextBox_PreviewKeyDown);
                            numberTextBox.LostFocus += new RoutedEventHandler(NumericTextBox_LostFocus);
                            numberTextBox.Text = item.ToString();
                            numberTextBox.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                            numberTextBox.VerticalAlignment = VerticalAlignment.Top;
                            numberTextBox.Margin = new Thickness(0, 10 + rowIndex * 30, 0, 0);

                            Grid.SetRow(numberTextBox, rowIndex);
                            Grid.SetColumn(numberTextBox, 1);
                            grid.Children.Add(numberTextBox);
                        }
                        else if (valueType == typeof(string))
                        {
                            System.Windows.Controls.TextBox stringTextBox = new System.Windows.Controls.TextBox();
                            stringTextBox.Text = item.ToString();
                            stringTextBox.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                            stringTextBox.VerticalAlignment = VerticalAlignment.Top;
                            stringTextBox.Margin = new Thickness(0, 10 + rowIndex * 30, 0, 0);
                            Grid.SetRow(stringTextBox, rowIndex);
                            Grid.SetColumn(stringTextBox, 1);

                            grid.Children.Add(stringTextBox);
                        }
                        else if (valueType.IsEnum)
                        {
                            System.Windows.Controls.ComboBox comboBox = new System.Windows.Controls.ComboBox();
                            comboBox.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                            comboBox.VerticalAlignment = VerticalAlignment.Top;
                            comboBox.Margin = new Thickness(0, 10 + rowIndex * 30, 0, 0);
                            comboBox.ItemsSource = Enum.GetValues(valueType);
                            comboBox.SelectedItem = item;
                            Grid.SetRow(comboBox, rowIndex);
                            Grid.SetColumn(comboBox, 1);
                            grid.Children.Add(comboBox);
                        }
                        else if (typeof(IMessage).IsAssignableFrom(valueType))
                        {
                            var newButton = new System.Windows.Controls.Button();
                            newButton.Content = "打开";
                            newButton.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                            newButton.VerticalAlignment = VerticalAlignment.Top;
                            newButton.Margin = new Thickness(0, 10 + rowIndex * 30, 0, 0);
                            newButton.Click += new RoutedEventHandler(OpenNestedData);

                            newButton.DataContext = item;
                            Grid.SetRow(newButton, rowIndex);
                            Grid.SetColumn(newButton, 1);
                            grid.Children.Add(newButton);
                        }
                        else if (typeof(IList).IsAssignableFrom(valueType))
                        {
                            var newButton = new System.Windows.Controls.Button();
                            newButton.Content = "打开";
                            newButton.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                            newButton.VerticalAlignment = VerticalAlignment.Top;
                            newButton.Margin = new Thickness(0, 10 + rowIndex * 30, 0, 0);
                            newButton.Click += new RoutedEventHandler(OpenListData);
                            newButton.DataContext = item;
                            Grid.SetRow(newButton, rowIndex);
                            Grid.SetColumn(newButton, 1);
                            grid.Children.Add(newButton);
                        }
                        rowIndex++;
                    }
                }
            }
        }

        private void OpenNestedData(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            if (button != null)
            {
                var data = button.DataContext as IMessage;

                if (data != null)
                {
                    //创建一个新的窗口
                    Window window = new Window();
                    window.Title = data.GetType().Name;
                    window.Width = 800;
                    window.Height = 600;
                    window.Show();
                    //window 增加一个Grid,与parent Grid一样
                    Grid grid = new Grid();
                    window.Content = grid;
                    grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
                    grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
                    grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

                    //获取属性
                    var properties = data.GetType().GetProperties();
                    int rowIndex = 0;
                    foreach (var property in properties)
                    {
                        System.Windows.Controls.Label label = new System.Windows.Controls.Label();
                        label.Content = property.Name;
                        label.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                        label.VerticalAlignment = VerticalAlignment.Top;
                        label.Margin = new Thickness(10, 10 + rowIndex * 30, 0, 0);
                        grid.Children.Add(label);

                        DataPropertyItem dataPropertyItem = new DataPropertyItem();
                        dataPropertyItem._PropertyName = property.Name;
                        dataPropertyItem._PropertyDesc = "";
                        dataPropertyItem._PropertyInfo = property;
                        dataPropertyItem._BelongData = data;
                        ProcessPropertyType(property.PropertyType, dataPropertyItem, rowIndex, grid, 200);
                        rowIndex++;
                    }
                }
            }
        }

        private void NumericTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as System.Windows.Controls.TextBox;
            if (textBox != null)
            {
                var value = textBox.Text;
                if (string.IsNullOrEmpty(value))
                {
                    textBox.Text = "0";
                    return;
                }

                if (int.TryParse(value, out int intValue))
                {
                    textBox.Text = intValue.ToString();
                }
                else if (float.TryParse(value, out float floatValue))
                {
                    textBox.Text = floatValue.ToString();
                }
                else if (long.TryParse(value, out long longValue))
                {
                    textBox.Text = longValue.ToString();
                }
                else if (double.TryParse(value, out double doubleValue))
                {
                    textBox.Text = doubleValue.ToString();
                }
                else
                    textBox.Text = "0";
            }
        }

        private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // 使用正则表达式检查输入是否为有效的浮点数字符
            Regex regex = new Regex(@"^[0-9.\-+eE]$");
            e.Handled = !regex.IsMatch(e.Text);
        }

        private void NumericTextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // 允许使用退格键、删除键、Tab键、箭头键等
            if (e.Key == Key.Back || e.Key == Key.Delete || e.Key == Key.Tab ||
                e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Up || e.Key == Key.Down)
            {
                e.Handled = false;
            }
            else
            {
                // 其他键处理
                e.Handled = !IsNumericKey(e.Key);
            }
        }

        private bool IsNumericKey(Key key)
        {
            // 检查按键是否为数字键或小数点、正负号、指数符号
            return (key >= Key.D0 && key <= Key.D9) ||
                   (key >= Key.NumPad0 && key <= Key.NumPad9) ||
                   key == Key.OemPeriod || key == Key.Decimal ||
                   key == Key.OemMinus || key == Key.Subtract ||
                   key == Key.OemPlus || key == Key.Add ||
                   key == Key.E || key == Key.Oem5; // Oem5 is for 'e' in some keyboards
        }

        private void FileSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            //将提示文本清空
            if (FileSearch.Text == "搜索配表文件")
            {
                FileSearch.Text = "";
            }
        }

        private void FileSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            //如果搜索框为空，则显示提示文本
            if (FileSearch.Text == "")
            {
                FileSearch.Text = "搜索配表文件";
            }
        }
    }
}