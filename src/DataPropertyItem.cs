using System.Reflection;
using Wukong_PBData_ReadWriter_GUI.DataControllers;

namespace Wukong_PBData_ReadWriter_GUI.src
{
    public class DataPropertyItem
    {

        /// <summary>
        /// 
        /// </summary>
        private string _propertyName;

        /// <summary>
        /// 
        /// </summary>
        public string _PropertyName
        {
            set
            {
                _propertyName = value;
                //this.DisplayName = ComparisonTableController.Instance.GetData(_propertyName, out var content)
                //    ? content
                //    : "";
            }
            get => _propertyName;
        }


        public string _PropertyDesc
        {
            get
            {
                if (_DataItem != null && _DataItem._File != null)
                {
                    if (MainWindow.s_CustomDescriptionConfig.TryGetValue(_DataItem._File._FileData.GetType().Name + "_" + _PropertyName, out var customDesc))
                    {
                        return customDesc;
                    }
                    if (MainWindow.s_DefaultDescriptionConfig.TryGetValue(_DataItem._File._FileData.GetType().Name + "_" + _PropertyName, out var desc))
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

        public PropertyInfo _PropertyInfo
        {
            set
            {
                _propertyInfo = value;
                this.DisplayName = ComparisonTableController.Instance.GetData($"{_propertyInfo.DeclaringType.Name}.{_propertyName}", out var content)
                    ? content
                    : "";
            }
            get => _propertyInfo;
        }

        private PropertyInfo _propertyInfo;

        public object _BelongData;

        private string _desc;

        public DataItem _DataItem;

        /// <summary>
        /// 展示名称
        /// </summary>
        public string DisplayName { set; get; }
    }
}
