using System.ComponentModel;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using BtlShare;
using CommunityToolkit.Mvvm.ComponentModel;
using Google.Protobuf;
using HandyControl.Controls;
using Color = System.Windows.Media.Color;

namespace Wukong_PBData_ReadWriter_GUI.Models;

public partial class DataItem : ObservableObject
{
    private readonly Lazy<List<DataPropertyItem>> _dataPropertyItems;
    private int Id { get; }

    public IMessage Data { get; }
    public DataFile File { get; }

    public string Desc => DataFileHelper.DescriptionConfig.TryGetValue(
        File.FileData?.GetType().Name + "_" + Id,
        out var desc
    )
        ? $"{Id,-10}{desc}"
        : Id.ToString();

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(Foreground))]
    private bool _isDirty;

    public SolidColorBrush Foreground =>
        IsDirty ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Black);

    public List<DataPropertyItem> DataPropertyItems => _dataPropertyItems.Value;

    public DataItem(int id, IMessage data, DataFile file)
    {
        Id = id;
        Data = data;
        File = file;
        // var properties = Data.GetType().GetProperties();
        // foreach (var propertyInfo in properties)
        // {
        //     if (typeof(IMessage).IsAssignableFrom(propertyInfo.PropertyType))
        //     {
        //         var s = typeof(MemberDescriptor).GetFields();
        //         typeof(MemberDescriptor).GetField("displayName").SetValue(
        //             TypeDescriptor.GetProperties(Data)[propertyInfo.Name],""
        //             );
        //         TypeDescriptor.AddAttributes(propertyInfo, new EditorAttribute(
        //             typeof(MessageEditor), typeof(MessageEditor)
        //         ));
        //         NumberPropertyEditor
        //     }
        // }

        _dataPropertyItems = new Lazy<List<DataPropertyItem>>(
            () =>
            {
                var properties = Data.GetType().GetProperties();

                return properties.Select(
                    property => new DataPropertyItem
                    {
                        PropertyName = property.Name,
                        PropertyInfo = property,
                        BelongData = Data,
                        DataItem = this
                    }
                ).ToList();
            }
        );
    }
}

public class MessageEditor : PropertyEditorBase
{
    public override FrameworkElement CreateElement(PropertyItem propertyItem)
    {
        return new HandyControl.Controls.PropertyGrid();
    }

    // 设置对应实体属性与控件关联的依赖属性
    public override DependencyProperty GetDependencyProperty()
    {
        return HandyControl.Controls.PropertyGrid.SelectedObjectProperty;
    }
}