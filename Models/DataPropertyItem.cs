using System.Reflection;

namespace Wukong_PBData_ReadWriter_GUI.Models
{
    public class DataPropertyItem
    {
        public string PropertyName { get; set; }

        public string PropertyDesc
        {
            get
            {
                if (_DataItem?._File == null) return _desc;
                return DataFileHelper.DescriptionConfig.TryGetValue(
                    _DataItem._File.FileData.GetType().Name + "_" + PropertyName, out var desc)
                    ? desc
                    : _desc;
            }
            set { _desc = value; }
        }

        public PropertyInfo _PropertyInfo;
        public object _BelongData;

        private string _desc;

        public DataItem _DataItem;
    }
}