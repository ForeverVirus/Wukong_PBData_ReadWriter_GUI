using Google.Protobuf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Wukong_PBData_ReadWriter_GUI.src
{
    public class DataFile
    {
        public string _FileName;
        public string _FilePath;
        public IMessage _FileData;
        public bool _IsShow = true;
        public List<DataItem> _FileDataItemList;

        public void LoadData()
        {
            var data = Exporter.GetDataByFile(_FileName, _FilePath);
            if (data != null)
            {
                _FileData = data;

                _FileDataItemList = new List<DataItem>();

                var type = data.GetType();
                if (type.Name.StartsWith("TB"))
                {
                    //获取名为List的public 属性，并且判定类型是否为 RepeatedField<T>
                    var listProperty = type.GetProperty("List");
                    if (listProperty != null)
                    {
                        var list = listProperty.GetValue(data) as IList;
                        if (list != null)
                        {
                            foreach (var item in list)
                            {
                                var itemType = item.GetType();
                                var property = itemType.GetProperty("Id");
                                if(property == null)
                                {
                                    property = itemType.GetProperty("ID");
                                }

                                if(property == null)
                                    continue;

                                DataItem dataItem = new DataItem();
                                dataItem._ID = property.GetValue(item) as int? ?? 0;
                                dataItem._Data = item as IMessage;
                                dataItem._File = this;
                                _FileDataItemList.Add(dataItem);
                            }
                        }
                    }
                    //type.GetFields()
                }
            }
        }

    }

}
