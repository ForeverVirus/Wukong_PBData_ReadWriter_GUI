using Google.Protobuf;
using System.Reflection;
using System.Security.Cryptography;
using System.Windows.Controls;

namespace Wukong_PBData_ReadWriter_GUI.src
{
    public class DataFile
    {
        public string _FileName;
        public string _FilePath;
        public IMessage _FileData;
        public string _Desc
        {
            get
            {
                //var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
                if (MainWindow.s_DescriptionConfig.TryGetValue(_FileName, out var desc))
                {
                    return desc;
                }
                //var windows = Application.Current.Windows;
                return _desc;
            }
            set
            {
                _desc = value;
            }
        }
        private string _desc;
        public bool _IsShow = true;
        public bool _IsTop = false;
        public bool _IsDirty = false;
        public List<DataItem> _FileDataItemList;
        public PropertyInfo _ListPropertyInfo;
        public List<int> _IDList;
        public ListBoxItem _ListBoxItem;

        /// <summary>
        /// 
        /// </summary>
        public object Tag;

        /// <summary>
        /// 
        /// </summary>
        public bool CanOpen { set; get; } = true;

        public void LoadData()
        {
            var filePath = _FilePath;
            if (Tag is string path && File.Exists(path))
            {
                filePath = path;
            }
            if (!CanOpen)
                return;
            var data = Exporter.GetDataByFile(_FileName, filePath);
            if (data != null)
            {
                _FileData = data;
                CanOpen = false;
                _FileDataItemList = new List<DataItem>();
                _IDList = new List<int>();

                var type = data.GetType();
                if (type.Name.StartsWith("TB"))
                {
                    //获取名为List的public 属性，并且判定类型是否为 RepeatedField<T>
                    var listProperty = type.GetProperty("List");
                    if (listProperty != null)
                    {
                        _ListPropertyInfo = listProperty;
                        var list = listProperty.GetValue(data) as IList;
                        if (list != null)
                        {
                            foreach (var item in list)
                            {
                                var itemType = item.GetType();
                                var property = itemType.GetProperty("Id");
                                if (property == null)
                                {
                                    property = itemType.GetProperty("ID");
                                }

                                if (property == null)
                                    continue;

                                DataItem dataItem = new DataItem();
                                var idValue = property.GetValue(item);
                                if (idValue is int id)
                                    dataItem._ID = id;
                                else
                                {
                                    dataItem._ID = System.Convert.ToInt32(idValue);
                                }

                                // dataItem._ID = property.GetValue(item) as int? ?? 0;
                                _IDList.Add(dataItem._ID);
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

        public int GetNewID()
        {
            if (_IDList != null && _IDList.Count > 0)
            {
                return _IDList.Max() + 1;
            }

            return 1000000;
        }

    }

}
