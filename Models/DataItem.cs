using CommunityToolkit.Mvvm.ComponentModel;
using Google.Protobuf;

namespace Wukong_PBData_ReadWriter_GUI.Models;

public class DataItem(int id, dynamic data, DataFile file) : ObservableObject
{
    public IMessage Data { get; } = data.Clone();

    public string Desc => DataFileHelper.DescriptionConfig.TryGetValue(
        (string)file.FileData!.GetType().Name + "_" + id,
        out var desc
    )
        ? $"{id,-10}{desc}"
        : id.ToString();

    public bool IsDirty => data.Equals(Data);

    public void Changed()
    {
        OnPropertyChanged(nameof(IsDirty));
        file.Changed();
    }
}