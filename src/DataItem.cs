using Google.Protobuf;
using System.Windows;
using System.Windows.Controls;
using Wukong_PBData_ReadWriter_GUI.DataControllers;

namespace Wukong_PBData_ReadWriter_GUI.src
{
    public class DataItem
    {
        public int _ID;
        public string _Desc
        {
            get
            {
                if (_File != null)
                {
                    //if(MainWindow.s_DescriptionConfig.TryGetValue(_File._FileData.GetType().Name + "_" + _ID, out var desc))
                    if (MainWindow.s_DescriptionConfig.TryGetValue(DescKey, out var desc))
                    {
                        return desc;
                    }
                    //var windows = Application.Current.Windows;
                }
                return _desc;
            }
            set
            {
                _desc = value;
            }
        }
        public IMessage _Data;
        public DataFile _File;
        public bool _IsShow = true;
        public ListBoxItem _ListBoxItem;
        public bool _IsModified = false;
        public bool _IsDirty = false;

        private string _desc;

        public List<DataPropertyItem> _DataPropertyItems;

        /// <summary>
        /// 描述键
        /// </summary>
        private string _descKey;

        /// <summary>
        /// 描述键
        /// </summary>
        public string DescKey
        {
            get
            {
                if (_File != null && string.IsNullOrWhiteSpace(_descKey))
                {
                    _descKey = _File._FileData.GetType().Name + "_" + _ID;
                }
                return _descKey;
            }
            set => _descKey = value;
        }

        /// <summary>
        /// 
        /// </summary>
        private Type _dataType;

        /// <summary>
        /// 
        /// </summary>
        public Type DataType
        {
            get
            {
                if (_dataType == null)
                    _dataType = _Data.GetType();
                return _dataType;
            }
        }

        /// <summary>
        /// 显示名称
        /// </summary>
        public string DisplayName => _ID + "  " + _Desc;

        public void LoadData()
        {
            _DataPropertyItems?.Clear();
            _DataPropertyItems = new List<DataPropertyItem>();

            var type = _Data.GetType();
            //var properties = type.GetProperties();
            if (!PropertiesDataController.Instance.Get(type, out var properties))
            {
                properties = PropertiesDataController.Instance.Add(type);
            }

            foreach (var property in properties)
            {
                DataPropertyItem dataPropertyItem = new DataPropertyItem();
                dataPropertyItem._PropertyName = property.Name;
                dataPropertyItem._PropertyDesc = "";
                dataPropertyItem._PropertyInfo = property;
                dataPropertyItem._BelongData = _Data;
                dataPropertyItem._DataItem = this;
                _DataPropertyItems.Add(dataPropertyItem);
            }
        }
    }
}
