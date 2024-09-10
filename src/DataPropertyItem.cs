using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Wukong_PBData_ReadWriter_GUI.src
{
    public class DataPropertyItem
    {
        public string _PropertyName;
        public string _PropertyDesc
        {
            get
            {
                if(_DataItem != null && _DataItem._File != null)
                {
                    var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
                    if(mainWindow._DescriptionConfig.TryGetValue(_DataItem._File._FileData.GetType().Name + "_" + _PropertyName, out var desc))
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
        public PropertyInfo _PropertyInfo;
        public object _BelongData;

        private string _desc;

        public DataItem _DataItem;
    }
}
