using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wukong_PBData_ReadWriter_GUI.src
{
    public class DataItem
    {
        public int _ID;
        public string _Desc;
        public IMessage _Data;

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
                _DataPropertyItems.Add(dataPropertyItem);
            }
        }
    }
}
