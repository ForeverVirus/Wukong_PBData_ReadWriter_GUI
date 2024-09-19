using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using Google.Protobuf;

namespace Wukong_PBData_ReadWriter_GUI.Models;

public class DataFile : ObservableObject
{
    private readonly Lazy<IMessage?> _fileData;
    private readonly Lazy<List<DataItem>> _dataItemList;
    private readonly string _filePath;

    public string DisplayName => Path.GetFileNameWithoutExtension(_filePath);
    public dynamic? FileData => _fileData.Value;
    public string Desc => DataFileHelper.DescriptionConfig.GetValueOrDefault(DisplayName, "");
    public List<DataItem> DataItemList => _dataItemList.Value;
    public bool IsDirty => _dataItemList.IsValueCreated && DataItemList.Any(item => item.IsDirty);

    public DataFile(string filePath)
    {
        _filePath = filePath;
        _fileData = new Lazy<IMessage?>(() => DataFileHelper.GetDataByFile(_filePath));
        _dataItemList = new Lazy<List<DataItem>>(() =>
        {
            if (FileData == null) return [];
            // // if (!type.Name.StartsWith("TB")) return res;
            // //获取名为List的public 属性，并且判定类型是否为 RepeatedField<T>
            // var listPropertyInfo = FileData.GetType().GetProperty("List");
            // if (listPropertyInfo == null || listPropertyInfo.GetValue(FileData) is not IList list)
            // {
            //     return [];
            // }

            var res = new List<DataItem>();
            foreach (IMessage item in FileData.List)
            {
                var itemType = item.GetType();
                var idProperty = itemType.GetProperty("Id") ?? itemType.GetProperty("ID");
                if (idProperty == null || idProperty.GetValue(item) is not int id) continue;
                res.Add(new DataItem(id, item, this));
            }

            return res;
        });
    }

    public void Changed()
    {
        OnPropertyChanged(nameof(IsDirty));
    }

    public int GetNewID()
    {
        // if (_IDList != null && _IDList.Count > 0)
        // {
        //     return _IDList.Max() + 1;
        // }

        return 1000000;
    }
}