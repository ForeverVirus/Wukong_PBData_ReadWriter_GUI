using System.Collections;
using System.IO;
using System.Reflection;
using Google.Protobuf;

namespace Wukong_PBData_ReadWriter_GUI.Models
{
    public class DataFile
    {
        private readonly Lazy<IMessage?> _fileData;
        private readonly Lazy<List<DataItem>> _dataItemList;

        public FileInfo FileInfo { get; }
        public IMessage? FileData => _fileData.Value;
        public string Desc => DataFileHelper.DescriptionConfig.GetValueOrDefault(FileInfo.Name, "");

        public bool _IsShow = true;
        public bool _IsTop = false;
        public bool _IsDirty = false;
        public List<DataItem> DataItemList => _dataItemList.Value;
        private PropertyInfo? _listPropertyInfo;

        public DataFile(FileInfo fileInfo)
        {
            FileInfo = fileInfo;

            _fileData = new Lazy<IMessage?>(() => DataFileHelper.GetDataByFile(FileInfo));
            _dataItemList = new Lazy<List<DataItem>>(() =>
            {
                var res = new List<DataItem>();
                if (FileData == null) return res;
                var type = FileData.GetType();
                if (!type.Name.StartsWith("TB")) return res;
                //获取名为List的public 属性，并且判定类型是否为 RepeatedField<T>
                _listPropertyInfo = type.GetProperty("List");
                if (_listPropertyInfo == null) return res;
                if (_listPropertyInfo.GetValue(FileData) is not IList list) return res;
                foreach (var item in list)
                {
                    var itemType = item.GetType();
                    var property = itemType.GetProperty("Id") ??
                                   itemType.GetProperty("ID");

                    if (property == null)
                        continue;

                    res.Add(new DataItem(
                        property.GetValue(item) as int? ?? 0,
                        (IMessage)item,
                        this));
                }

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