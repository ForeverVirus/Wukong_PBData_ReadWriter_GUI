using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Wukong_PBData_ReadWriter_GUI.Entity
{

    public class Config
    {
        /// <summary>
        /// 备注文件路径
        /// </summary>
        [ConfigParam(Desc = "备注文件路径")]
        public AttributeChangeNotification RemarkFilePath =
            new(data: ".\\Json\\DefaultRemark.json");

        /// <summary>
        /// 对照表文件路径
        /// </summary>
        [ConfigParam(Desc = "对照文件路径")]
        public AttributeChangeNotification ComparisonTableFilePath =
            new(data: ".\\Json\\ComparisonTable.json");

        /// <summary>
        /// 数据文件路径
        /// </summary>
        [ConfigParam(Desc = "数据文件路径")]
        public AttributeChangeNotification DataFilePath
            = new(data: "");

        /// <summary>
        /// 打包文件路径
        /// </summary>
        [ConfigParam(Desc = "打包文件路径")]
        public AttributeChangeNotification OutPakFilePath
            = new(data: "");

        /// <summary>
        /// 自动保存文件
        /// </summary>
        [ConfigParam(Desc = "自动保存文件", DataType = typeof(bool))]
        public AttributeChangeNotification AutoSaveFile =
            new(data: false);

        /// <summary>
        /// 显示源数据
        /// </summary>
        [ConfigParam(Desc = "显示源数据", DataType = typeof(bool))]
        public AttributeChangeNotification DisplaysSourceInformation =
            new(data: true);

        /// <summary>
        /// 临时文件路径
        /// </summary>
        [ConfigParam(Desc = "临时文件路径")]
        public AttributeChangeNotification TempFileDicPath =
            new(data: ".\\Temp");

        /// <summary>
        /// 搜索功能自动生效
        /// </summary>
        [ConfigParam(Desc = "搜索功能自动生效", DataType = typeof(bool))]
        public AttributeChangeNotification AutoSearchInEffect =
            new(data: false);

        /// <summary>
        /// 默认保存地址
        /// </summary>
        [ConfigParam(Desc = "默认保存地址")]
        public AttributeChangeNotification DefaultSavePath =
            new(data: "");

        /// <summary>
        /// 配置更新时间
        /// </summary>
        [ConfigParam(Desc = "配置更新时间", DataType = typeof(DateTime))]
        public DateTime SaveTime { set; get; }
    }

    public class ConfigParam : Attribute
    {
        /// <summary>
        /// 参数
        /// </summary>
        public string Desc { set; get; }

        /// <summary>
        /// 数据类型
        /// </summary>
        public Type DataType { set; get; } = typeof(string);

        /// <summary>
        /// 默认值
        /// </summary>
        public object DefaultValue { set; get; }
    }

    /// <summary>
    /// 配置数据
    /// </summary>
    public class ConfigData
    {
        /// <summary>
        /// 描述
        /// </summary>
        public string Desc { set; get; }

        /// <summary>
        /// 数据
        /// </summary>
        public object Data { set; get; }
    }

    /// <summary>
    /// 属性变更通知
    /// </summary>
    public class AttributeChangeNotification
    {
        /// <summary>
        /// 
        /// </summary>
        public Func<string, object, object, int> Change;

        /// <summary>
        /// 
        /// </summary>
        public string Key { set; get; }

        /// <summary>
        /// 
        /// </summary>
        private object _value;

        /// <summary>
        /// 
        /// </summary>
        public object Value
        {
            set
            {
                if (_value == null || !EqualityComparer<object>.Default.Equals(_value, value))
                    Change?.Invoke(Key, _value, value);
                _value = value;
            }
            get
            {
                if (_value == null)
                    _value = default(object);
                return _value;
            }
        }

        public AttributeChangeNotification()
        {

        }

        public AttributeChangeNotification(string key)
        {
        }

        public AttributeChangeNotification(object data)
        {
            _value = data;
        }

        public AttributeChangeNotification(Func<string, object, object, int> func)
        {
            Change += func;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="func"></param>
        public AttributeChangeNotification(object data, Func<string, object, object, int> func) : this(data)
        {
            Change += func;
        }
    }
}
