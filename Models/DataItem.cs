using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using Google.Protobuf;

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

    [ObservableProperty]
    private bool _isDirty;

    public List<DataPropertyItem> DataPropertyItems => _dataPropertyItems.Value;

    public DataItem(int id, IMessage data, DataFile file)
    {
        Id = id;
        Data = data;
        File = file;
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