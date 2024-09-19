using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Google.Protobuf;
using HandyControl.Controls;
using Wukong_PBData_ReadWriter_GUI.Models;
using ComboBox = HandyControl.Controls.ComboBox;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using NumericUpDown = HandyControl.Controls.NumericUpDown;
using Orientation = System.Windows.Controls.Orientation;
using TextBox = HandyControl.Controls.TextBox;

namespace Wukong_PBData_ReadWriter_GUI.Views;

public partial class DataItemView
{
    public static readonly DependencyProperty ItemProperty = DependencyProperty.Register(
        nameof(Item), typeof(DataItem), typeof(DataItemView), new PropertyMetadata(null, ShowProperties)
    );

    public DataItem? Item
    {
        get => (DataItem?)GetValue(ItemProperty);
        set => SetValue(ItemProperty, value);
    }

    public DataItemView()
    {
        InitializeComponent();
    }

    private static void ShowProperties(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DataItemView { Item: { } item } dataItemView) return;
        dataItemView.PropertyViewer.Content = GetControl(item.Data, () => item.Changed());
    }

    private static SimpleStackPanel GetControl(object obj, Action changeCallBack)
    {
        var stackPanel = new SimpleStackPanel { HorizontalAlignment = HorizontalAlignment.Left };

        foreach (var propertyInfo in obj.GetType().GetProperties())
        {
            if (propertyInfo.PropertyType == typeof(int))
            {
                var line = new SimpleStackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(5) };
                line.Children.Add(new SimpleText
                {
                    Text = propertyInfo.Name,
                    Width = 200,
                    VerticalAlignment = VerticalAlignment.Center
                });
                var control = new NumericUpDown
                {
                    Minimum = int.MinValue,
                    Maximum = int.MaxValue,
                    Value = Convert.ToDouble(propertyInfo.GetValue(obj)),
                    IsEnabled = propertyInfo.CanWrite
                };
                control.ValueChanged += (_, _) =>
                {
                    propertyInfo.SetValue(obj, Convert.ToInt32(control.Value));
                    changeCallBack();
                };
                line.Children.Add(control);
                stackPanel.Children.Add(line);
            }
            else if (propertyInfo.PropertyType == typeof(float))
            {
                var line = new SimpleStackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(5) };
                line.Children.Add(new SimpleText
                {
                    Text = propertyInfo.Name,
                    Width = 200,
                    VerticalAlignment = VerticalAlignment.Center
                });
                var control = new NumericUpDown
                {
                    Minimum = float.MinValue,
                    Maximum = float.MaxValue,
                    Value = Convert.ToDouble(propertyInfo.GetValue(obj)),
                    IsEnabled = propertyInfo.CanWrite
                };
                control.ValueChanged += (_, _) =>
                {
                    propertyInfo.SetValue(obj, Convert.ToSingle(control.Value));
                    changeCallBack();
                };
                line.Children.Add(control);
                stackPanel.Children.Add(line);
            }
            else if (propertyInfo.PropertyType == typeof(string))
            {
                var line = new SimpleStackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(5) };
                line.Children.Add(new SimpleText
                {
                    Text = propertyInfo.Name,
                    Width = 200,
                    VerticalAlignment = VerticalAlignment.Center
                });
                var control = new TextBox
                {
                    Text = Convert.ToString(propertyInfo.GetValue(obj)) ?? "",
                    IsEnabled = propertyInfo.CanWrite
                };
                control.TextChanged += (_, _) =>
                {
                    propertyInfo.SetValue(obj, control.Text);
                    changeCallBack();
                };
                line.Children.Add(control);
                stackPanel.Children.Add(line);
            }
            else if (propertyInfo.PropertyType.IsEnum)
            {
                var line = new SimpleStackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(5) };
                line.Children.Add(new SimpleText
                {
                    Text = propertyInfo.Name,
                    Width = 200,
                    VerticalAlignment = VerticalAlignment.Center
                });
                var control = new ComboBox
                {
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    ItemsSource = Enum.GetValues(propertyInfo.PropertyType),
                    SelectedItem = propertyInfo.GetValue(obj),
                    IsEnabled = propertyInfo.CanWrite
                };
                control.SelectionChanged += (_, _) =>
                {
                    propertyInfo.SetValue(obj, control.SelectedValue);
                    changeCallBack();
                };
                line.Children.Add(control);
                stackPanel.Children.Add(line);
            }
            else if (typeof(IMessage).IsAssignableFrom(propertyInfo.PropertyType))
            {
                var line = new Expander { Header = propertyInfo.Name };
                if (propertyInfo.GetValue(obj) is not IMessage subObject) break;
                var sub = GetControl(subObject, changeCallBack);
                sub.Margin = new Thickness(20, 0, 0, 0);
                line.Content = sub;
                stackPanel.Children.Add(line);
            }
        }

        return stackPanel;
    }
}