using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wukong_PBData_ReadWriter_GUI.Entity
{

    public class Config
    {
        /// <summary>
        /// 备注文件路径
        /// </summary>
        [ConfigParam(Desc = "备注文件路径", DefaultValue = ".\\Config\\DefaultRemark.json")]
        public string RemarkFilePath { set; get; }

        /// <summary>
        /// 对照表文件路径
        /// </summary>
        [ConfigParam(Desc = "对照文件路径", DefaultValue = ".\\Config\\ComparisonTable.json")]
        public string ComparisonTableFilePath { set; get; }

        /// <summary>
        /// 数据文件路径
        /// </summary>
        [ConfigParam(Desc = "数据文件路径")]
        public string DataFilePath { set; get; }

        /// <summary>
        /// 打包文件路径
        /// </summary>
        [ConfigParam(Desc = "打包文件路径")]
        public string OutPakFilePath { set; get; }

        /// <summary>
        /// 自动保存文件
        /// </summary>
        [ConfigParam(Desc = "自动保存文件", DataType = typeof(bool))]
        public bool AutoSaveFile { set; get; }

        /// <summary>
        /// 显示源数据
        /// </summary>
        [ConfigParam(Desc = "显示源数据", DataType = typeof(bool),DefaultValue = true)]
        public bool DisplaysSourceInformation { set; get; } = true;

        /// <summary>
        /// 临时文件路径
        /// </summary>
        [ConfigParam(Desc = "临时文件路径")]
        public string TempFileDicPath { set; get; } = ".\\Temp";

        /// <summary>
        /// 搜索功能自动生效
        /// </summary>
        [ConfigParam(Desc = "搜索功能自动生效", DataType = typeof(bool))]
        public bool AutoSearchInEffect { set; get; }

        /// <summary>
        /// 默认保存地址
        /// </summary>
        [ConfigParam(Desc = "默认保存地址")]
        public string DefaultSavePath { set; get; }
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
}
