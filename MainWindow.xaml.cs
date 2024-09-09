using BtlShare;
using Google.Protobuf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
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
        public Dictionary<string, string> _DescriptionConfig = new Dictionary<string, string>();
        public DataFile _CurrentOpenFile = null;

        public MainWindow()
        {
            
        }

        private void CloseAllOtherWindow(bool isClearDataGrid = true)
        {
            WindowCollection windows = System.Windows.Application.Current.Windows;
            foreach (var win in windows)
            {
                if (win == null)
                    continue;

                if (win.GetType() != typeof(MainWindow))
                {
                    (win as Window).Close();
                }
            }

            if (isClearDataGrid)
            {
                DataGrid.RowDefinitions.Clear();
                DataGrid.Children.Clear();
            }

        }

        private void ImportDescription(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog();
            dialog.AddExtension = true;
            dialog.Filter = "Json|*.json";
            dialog.Title = "导入备注配置";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _DescriptionConfig = Exporter.ImportDescriptionConfig(dialog.FileName);
            }
        }

        private void ExportDescription(object sender, RoutedEventArgs e) 
        {
            System.Windows.Forms.SaveFileDialog dialog = new System.Windows.Forms.SaveFileDialog();
            dialog.AddExtension = true;
            dialog.Filter = "Json|*.json";
            dialog.Title = "导出备注配置";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Exporter.ExportDescriptionConfig(_DescriptionConfig, dialog.FileName);
            }
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

                _DataFiles.Clear();
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
                RefreshDataFile(_DataFiles);
                CloseAllOtherWindow();
                _CurrentOpenFile = null;
            }
        }

        private void RefreshDataFile(List<DataFile> files)
        {
            if (FileList == null) return;
            FileList.Items.Clear();
            foreach (var item in files)
            {
                if(!item._IsShow) continue;

                ListBoxItem listBoxItem = new ListBoxItem();
                listBoxItem.Content = item._FileName;
                listBoxItem.MouseDoubleClick += new MouseButtonEventHandler(OpenDataFile);
                listBoxItem.DataContext = item;
                FileList.Items.Add(listBoxItem);
            }
        }

        private void SaveAsNewDataFile(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.SaveFileDialog dialog = new System.Windows.Forms.SaveFileDialog();
            dialog.AddExtension = true;
            dialog.Filter = "Data|*.data";
            dialog.Title = "保存当前Data";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Exporter.SaveDataFile(dialog.FileName, _CurrentOpenFile);
            }
        }

        private void OpenDataFile(object sender, MouseButtonEventArgs e)
        {
            ListBoxItem listBoxItem = sender as ListBoxItem;
            if(listBoxItem != null)
            {
                DataFile file = listBoxItem.DataContext as DataFile;
                OpenFile(file);
            }
        }

        private void OpenFile(DataFile file)
        {
            if (file != null)
            {
                file.LoadData();

                if (file._FileDataItemList != null && file._FileDataItemList.Count > 0)
                {
                    RefreshFileDataItemList(file._FileDataItemList);
                }

                _CurrentOpenFile = file;
                CloseAllOtherWindow();
            }
        }

        private void RefreshFileDataItemList(List<DataItem> list)
        {
            if (DataItemList == null) return;
            DataItemList.Items.Clear();
            foreach (var item in list)
            {
                if (!item._IsShow) continue;

                ListBoxItem listItem = new ListBoxItem();
                listItem.Content = item._ID + "  " + item._Desc;
                listItem.DataContext = item;
                listItem.MouseDoubleClick += new MouseButtonEventHandler(OpenDataItem);
                listItem.ContextMenu = new ContextMenu();
                MenuItem menuItem = new MenuItem();
                menuItem.Header = "备注";
                string descKey = item._File._FileData.GetType().Name + "_" + item._ID;
                Action descSuccessAction = () =>
                {
                    RefreshFileDataItemList(list);
                };
                menuItem.DataContext = new Tuple<string, Action>(descKey, descSuccessAction);
                menuItem.Click += OpenDescriptionWindow;
                listItem.ContextMenu.Items.Add(menuItem);

                DataItemList.Items.Add(listItem);
            }
        }

        private void OpenDescriptionWindow(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem == null) return;

            var data = menuItem.DataContext as Tuple<string, Action>;

            Window window = new Window();
            window.Title = "备注";
            window.Width = 300;
            window.Height = 150;
            // 获取鼠标相对于主窗口的位置
            System.Windows.Point mousePosition = Mouse.GetPosition(this);

            // 转换为屏幕坐标
            System.Windows.Point screenPosition = PointToScreen(mousePosition);
            window.Left = screenPosition.X;
            window.Top = screenPosition.Y;
            window.Show();

            Grid grid = new Grid();
            window.Content = grid;
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

            System.Windows.Controls.Label label = new System.Windows.Controls.Label();
            label.Content = "备注";
            label.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            label.VerticalAlignment = VerticalAlignment.Top;
            label.Margin = new Thickness(10, 10, 0, 0);
            Grid.SetRow(label, 0);
            Grid.SetColumn(label, 1);
            grid.Children.Add(label);

            System.Windows.Controls.TextBox textBox = new System.Windows.Controls.TextBox();
            textBox.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            textBox.VerticalAlignment = VerticalAlignment.Top;
            textBox.Margin = new Thickness(100, 10, 0, 0);
            textBox.Text = "这里写备注";
            Grid.SetRow(textBox, 0);
            Grid.SetColumn(textBox, 1);
            grid.Children.Add(textBox);

            System.Windows.Controls.Button button = new System.Windows.Controls.Button();
            button.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            button.VerticalAlignment = VerticalAlignment.Top;
            button.Margin = new Thickness(40, 40, 0, 0);
            button.Content = "确定";
            button.Click += (sender, e) =>
            {
                _DescriptionConfig.TryAdd(data.Item1, textBox.Text);
                data.Item2?.Invoke();
                window.Close();
            };
            Grid.SetRow(button, 1);
            Grid.SetColumn(button, 0);
            grid.Children.Add(button);
        }

        private void OpenDataItem(object sender, MouseButtonEventArgs e)
        {
            ListBoxItem listBoxItem = sender as ListBoxItem;
            if (listBoxItem != null)
            {
                var data = listBoxItem.DataContext as DataItem;
                if (data != null)
                {

                    if (data._Data == null)
                    {
                        return;
                    }

                    data.LoadData();

                    RefreshDataItemList(data._DataPropertyItems);

                    CloseAllOtherWindow(false);
                }
            }
        }

        private void RefreshDataItemList(List<DataPropertyItem> propertyItemList)
        {
            DataGrid.RowDefinitions.Clear();
            DataGrid.Children.Clear();
            DataGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            int rowIndex = 0;
            foreach (var item in propertyItemList)
            {
                System.Windows.Controls.Label label = new System.Windows.Controls.Label();
                label.Content = $"{item._PropertyName}({item._PropertyDesc})";
                label.ToolTip = item._PropertyDesc;
                Grid.SetRow(label, rowIndex);
                Grid.SetColumn(label, 0);
                label.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                label.VerticalAlignment = VerticalAlignment.Top;
                label.Margin = new Thickness(0, 10 + rowIndex * 30, 0, 0);

                label.ContextMenu = new ContextMenu();
                MenuItem menuItem = new MenuItem();
                menuItem.Header = "备注";

                string descKey = item._DataItem._File._FileData.GetType().Name + "_" + item._DataItem._ID + "_" + item._PropertyName;
                Action descSuccessAction = () =>
                {
                    RefreshDataItemList(propertyItemList);
                };
                menuItem.DataContext = new Tuple<string, Action>(descKey, descSuccessAction);
                menuItem.Click += OpenDescriptionWindow;
                label.ContextMenu.Items.Add(menuItem);

                DataGrid.Children.Add(label);

                var valueType = item._PropertyInfo.PropertyType;
                ProcessPropertyType(valueType, item, rowIndex, DataGrid, 300);
                rowIndex++;
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
                numberTextBox.DataContext = item;
                numberTextBox.TextChanged += NumberTextBox_TextChanged;
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
                stringTextBox.DataContext = item;
                stringTextBox.TextChanged += StringTextBox_TextChanged;
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
                comboBox.DataContext = item;
                comboBox.SelectionChanged += ComboBox_SelectionChanged;
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

        private void NumberTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as System.Windows.Controls.TextBox;
            if (textBox != null)
            {
                var item = textBox.DataContext as DataPropertyItem;

                if(item._PropertyInfo.PropertyType == typeof(int))
                {
                    OnValueChanged(item, System.Convert.ToInt32(textBox.Text));
                }
                else if(item._PropertyInfo.PropertyType == typeof(long))
                {
                    OnValueChanged(item, System.Convert.ToInt64(textBox.Text));
                }
                else if (item._PropertyInfo.PropertyType == typeof(float))
                {
                    OnValueChanged(item, System.Convert.ToSingle(textBox.Text));
                }
                else if (item._PropertyInfo.PropertyType == typeof(double))
                {
                    OnValueChanged(item, System.Convert.ToDouble(textBox.Text));
                }
            }
        }

        private void StringTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as System.Windows.Controls.TextBox;
            if (textBox != null)
            {
                var item = textBox.DataContext as DataPropertyItem;
                OnValueChanged(item, textBox.Text);
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as System.Windows.Controls.ComboBox;
            if (comboBox != null)
            {
                var item = comboBox.DataContext as DataPropertyItem;
                OnValueChanged(item, comboBox.SelectedValue);
            }
        }

        private void OnValueChanged(DataPropertyItem item, object value)
        {
            if (item != null)
            {
                item._PropertyInfo.SetValue(item._BelongData, value);
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

                    window.WindowStartupLocation = WindowStartupLocation.CenterScreen;

                    window.Show();
                    //window 增加一个Grid,与parent Grid一样
                    Grid grid = new Grid();
                    window.Content = grid;
                    grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
                    grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
                    grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

                    //var list = data._PropertyInfo.GetValue(data._BelongData) as IList;

                    RefreshList(data, ListType, grid);

                    
                }
            }
        }

        private void RefreshList(IList data, string ListType, Grid grid)
        {
            grid.Children.Clear();
            int rowIndex = 0;
            foreach (var item in data)
            {
                System.Windows.Controls.Label groupLabel = new System.Windows.Controls.Label();
                groupLabel.Content = ListType + "-" + rowIndex;
                groupLabel.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                groupLabel.VerticalAlignment = VerticalAlignment.Top;
                groupLabel.Margin = new Thickness(10, 10 + rowIndex * 30, 0, 0);
                groupLabel.ContextMenu = new ContextMenu();
                //MenuItem descItem = new MenuItem();
                //descItem.Header = "备注";
                //descItem.Click += (sender, e) =>
                //{

                //};
                MenuItem delMenu = new MenuItem();
                delMenu.Header = "删除";
                delMenu.DataContext = new Tuple<int, IList, Grid>(rowIndex, data, grid);
                delMenu.Click += DelMenu_Click;
                groupLabel.ContextMenu.Items.Add(delMenu);

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
                    numberTextBox.DataContext = new Tuple<int, IList, Type>(rowIndex, data, valueType);
                    numberTextBox.TextChanged += NumberTextBox_TextChanged1;

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
                    stringTextBox.DataContext = new Tuple<int, IList, Type>(rowIndex, data, valueType);
                    stringTextBox.TextChanged += StringTextBox_TextChanged1;
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
                    comboBox.DataContext = new Tuple<int, IList, Type>(rowIndex, data, valueType);
                    comboBox.SelectionChanged += ComboBox_SelectionChanged1;
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

            var addItemButton = new System.Windows.Controls.Button();
            addItemButton.Content = "新增";
            addItemButton.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            addItemButton.VerticalAlignment = VerticalAlignment.Top;
            addItemButton.Margin = new Thickness(0, 10 + rowIndex * 30, 0, 0);
            addItemButton.DataContext = new Tuple<IList, Grid>(data, grid);
            addItemButton.Click += AddItemButton_Click;
            grid.Children.Add(addItemButton);
        }

        private void DelMenu_Click(object sender, RoutedEventArgs e)
        {
            var item = (MenuItem)sender;
            if (item == null) return;

            var data = item.DataContext as Tuple<int, IList, Grid>;
            if (data == null) return;

            var listType = data.Item2.GetType().GetGenericArguments()[0];
            data.Item2.RemoveAt(data.Item1);

            RefreshList(data.Item2, listType.Name, data.Item3);
        }

        private void AddItemButton_Click(object sender, RoutedEventArgs e)
        {
            var item = (System.Windows.Controls.Button)sender;
            if (item == null) return;

            var data = item.DataContext as Tuple<IList, Grid>;

            if (data == null) return;

            var listType = data.Item1.GetType().GetGenericArguments()[0];
            var newItem = Activator.CreateInstance(listType);

            data.Item1.Add(newItem);

            RefreshList(data.Item1, listType.Name, data.Item2);
            //data.Item2


        }

        private void ComboBox_SelectionChanged1(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as System.Windows.Controls.ComboBox;
            if (comboBox != null)
            {
                var data = comboBox.DataContext as Tuple<int, IList, Type>;

                OnListItemChanged(data.Item1, data.Item2, comboBox.SelectedItem);
            }
        }

        private void StringTextBox_TextChanged1(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as System.Windows.Controls.TextBox;
            if (textBox != null)
            {
                var data = textBox.DataContext as Tuple<int, IList, Type>;

                OnListItemChanged(data.Item1, data.Item2, textBox.Text);
            }
        }

        private void OnListItemChanged(int index, IList list, object value)
        {
            if (list != null && index >= 0 && index < list.Count) 
            {
                list[index] = value;
            }
        }

        private void NumberTextBox_TextChanged1(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as System.Windows.Controls.TextBox;
            if (textBox != null)
            {
                var data = textBox.DataContext as Tuple<int, IList, Type>;

                if (data.Item3 == typeof(int))
                {
                    OnListItemChanged(data.Item1, data.Item2, System.Convert.ToInt32(textBox.Text));
                }
                else if (data.Item3 == typeof(long))
                {
                    OnListItemChanged(data.Item1, data.Item2, System.Convert.ToInt64(textBox.Text));
                }
                else if (data.Item3 == typeof(float))
                {
                    OnListItemChanged(data.Item1, data.Item2, System.Convert.ToSingle(textBox.Text));
                }
                else if (data.Item3 == typeof(double))
                {
                    OnListItemChanged(data.Item1, data.Item2, System.Convert.ToDouble(textBox.Text));
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
                    window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
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

        private void FileSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as System.Windows.Controls.TextBox;
            if (textBox != null) 
            {
                if (!string.IsNullOrEmpty(textBox.Text) && textBox.Text != "搜索配表文件")
                {
                    foreach (var file in _DataFiles)
                    {
                        if(file._FileName.Contains(textBox.Text, StringComparison.OrdinalIgnoreCase))
                        {
                            file._IsShow = true;
                        }
                        else
                        {
                            file._IsShow = false;
                        }
                    }

                }
                else
                {
                    foreach(var file in _DataFiles)
                    {
                        file._IsShow = true;
                    }
                }

                RefreshDataFile(_DataFiles);
            }
        }

        private void ItemSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            //将提示文本清空
            if (ItemSearch.Text == "搜索ID或备注")
            {
                ItemSearch.Text = "";
            }
        }

        private void ItemSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            //如果搜索框为空，则显示提示文本
            if (ItemSearch.Text == "")
            {
                ItemSearch.Text = "搜索ID或备注";
            }
        }

        private void ItemSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_CurrentOpenFile == null) return;

            var textBox = sender as System.Windows.Controls.TextBox;
            if (textBox != null)
            {
                var itemList = _CurrentOpenFile._FileDataItemList;
                if (!string.IsNullOrEmpty(textBox.Text) && textBox.Text != "搜索ID或备注")
                {
                    foreach (var item in itemList)
                    {
                        if(item._ID.ToString().Contains(textBox.Text, StringComparison.OrdinalIgnoreCase) 
                            || (!string.IsNullOrEmpty(item._Desc) && item._Desc.Contains(textBox.Text, StringComparison.OrdinalIgnoreCase)))
                        {
                            item._IsShow = true;
                        }
                        else
                            item._IsShow = false;
                    }
                }
                else
                {
                    foreach (var item in itemList)
                    {
                        item._IsShow = true;
                    }
                }
                
                RefreshFileDataItemList(_CurrentOpenFile._FileDataItemList);

                //RefreshDataFile(_DataFiles);
            }
        }
    }
}