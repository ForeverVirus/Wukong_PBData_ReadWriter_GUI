using System.Collections;
using System.IO;
using Google.Protobuf;

namespace Wukong_PBData_ReadWriter_GUI.Models
{
    public class DataFile
    {
        private readonly Lazy<IMessage?> _fileData;
        private readonly Lazy<List<DataItem>> _dataItemList;
        private readonly FileInfo _fileInfo;

        public string DisplayName => Path.GetFileNameWithoutExtension(_fileInfo.Name);
        public IMessage? FileData => _fileData.Value;
        public string Desc => DataFileHelper.DescriptionConfig.GetValueOrDefault(DisplayName, "");

        public bool _IsShow = true;
        public bool _IsTop = false;
        public bool _IsDirty = false;
        public List<DataItem> DataItemList => _dataItemList.Value;

        public DataFile(FileInfo fileInfo)
        {
            _fileInfo = fileInfo;

            _fileData = new Lazy<IMessage?>(() => DataFileHelper.GetDataByFile(_fileInfo));
            _dataItemList = new Lazy<List<DataItem>>(() =>
            {
                var res = new List<DataItem>();
                if (FileData == null) return res;
                var type = FileData.GetType();
                if (!type.Name.StartsWith("TB")) return res;
                //获取名为List的public 属性，并且判定类型是否为 RepeatedField<T>
                var listPropertyInfo = type.GetProperty("List");
                if (listPropertyInfo == null ||
                    listPropertyInfo.GetValue(FileData) is not IList list)
                {
                    return res;
                }

                // foreach (var item in list)
                // {
                //     var itemType = item.GetType();
                //     var property = itemType.GetProperty("Id") ??
                //                    itemType.GetProperty("ID");
                //
                //     if (property == null)
                //         continue;
                //
                //     res.Add(new DataItem(
                //         property.GetValue(item) as int? ?? 0,
                //         (IMessage)item,
                //         this));
                // }

                res.AddRange(
                    from object? item in list
                    let itemType = item.GetType()
                    let property = itemType.GetProperty("Id") ?? itemType.GetProperty("ID")
                    where property != null
                    select new DataItem(property.GetValue(item) as int? ?? 0, (IMessage)item, this)
                );

                return res;
            });
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
}