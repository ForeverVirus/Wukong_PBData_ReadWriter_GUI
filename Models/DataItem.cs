using System.Windows.Controls;
using Google.Protobuf;

namespace Wukong_PBData_ReadWriter_GUI.Models
{
    public class DataItem
    {
        public int Id { get; }

        public string Desc => DataFileHelper.DescriptionConfig.TryGetValue(
            _File.FileData?.GetType().Name + "_" + Id,
            out var desc
        )
            ? $"{Id,-10}{desc}"
            : Id.ToString();

        public IMessage _Data;
        public DataFile _File;
        public bool _IsShow = true;
        public ListBoxItem _ListBoxItem;

        public List<DataPropertyItem> DataPropertyItems { get; } = [];

        public DataItem(int id, IMessage data, DataFile file)
        {
            Id = id;
            _Data = data;
            _File = file;
            var type = _Data.GetType();
            var properties = type.GetProperties();

            foreach (var property in properties)
            {
                DataPropertyItems.Add(new DataPropertyItem
                {
                    PropertyName = property.Name,
                    PropertyDesc = "",
                    _PropertyInfo = property,
                    _BelongData = _Data,
                    _DataItem = this
                });
            }
        }
    }
}