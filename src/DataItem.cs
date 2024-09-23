using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Wukong_PBData_ReadWriter_GUI.src
{
    public class DataItem
    {
        public int _ID;
        public string _Desc
        {
            get
            {
                if(_File != null)
                {
                    if(MainWindow.s_DescriptionConfig.TryGetValue(_File._FileData.GetType().Name + "_" + _ID, out var desc))
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

        private string _desc;

        public List<DataPropertyItem> _DataPropertyItems;

        public void LoadData()
        {
            _DataPropertyItems = new List<DataPropertyItem>();

            var type = _Data.GetType();
            var properties = type.GetProperties();

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
