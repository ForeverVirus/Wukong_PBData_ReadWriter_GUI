using System.Collections;
using System.IO;
using System.Reflection;
using Google.Protobuf;

namespace Wukong_PBData_ReadWriter_GUI.Models
{
    public class DataFile
    {
        public FileInfo FileInfo { get; }
        public IMessage? FileData { get; }

        public string Desc => DataFileHelper.DescriptionConfig.GetValueOrDefault(FileInfo.Name, "");

        public bool _IsShow = true;
        public bool _IsTop = false;
        public bool _IsDirty = false;
        public List<DataItem> DataItemList { get; set; } = [];
        private PropertyInfo? _listPropertyInfo;

        public DataFile(FileInfo fileInfo)
        {
            FileInfo = fileInfo;

            var data = DataFileHelper.GetDataByFile(FileInfo);
            if (data == null) return;
            FileData = data;

            var type = data.GetType();
            if (!type.Name.StartsWith("TB")) return;
            //获取名为List的public 属性，并且判定类型是否为 RepeatedField<T>
            _listPropertyInfo = type.GetProperty("List");
            if (_listPropertyInfo == null) return;
            if (_listPropertyInfo.GetValue(data) is not IList list) return;
            foreach (var item in list)
            {
                var itemType = item.GetType();
                var property = itemType.GetProperty("Id") ??
                               itemType.GetProperty("ID");

                if (property == null)
                    continue;

                DataItemList.Add(new DataItem(
                    property.GetValue(item) as int? ?? 0,
                    (IMessage)item,
                    this
                ));
            }
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