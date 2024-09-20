using CommunityToolkit.Mvvm.ComponentModel;
using Google.Protobuf;
using System.Security.Cryptography;

namespace Wukong_PBData_ReadWriter_GUI.Models;

public class DataItem : ObservableObject
{
    private readonly int _id;
    private readonly IMessage _originData;
    private readonly IMessage _data;
    private readonly DataFile _file;

    public string Desc => DataFileHelper.DescriptionConfig.TryGetValue(
        ((IMessage)_file.FileData).GetType().Name + "_" + _id,
        out var desc
    )
        ? $"{_id,-10}{desc}"
        : _id.ToString();

    public DataProperty[] PropertyInfos { get; }

    public bool IsDirty => !_data.Equals(_originData);

    public DataItem(int id, dynamic data, DataFile file)
    {
        _id = id;
        _data = data;
        _originData = (IMessage)data.Clone();
        _file = file;
        PropertyInfos = ((Type)data.GetType())
            .GetProperties().Where(p => p.Name != "Parser")
            .Select(p => new DataProperty(p, data, (Action)Changed))
            .ToArray();
    }

    public void Changed()
    {
        OnPropertyChanged(nameof(IsDirty));
        _file.Changed();
    }
}