using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Wukong_PBData_ReadWriter_GUI.Util;

namespace Wukong_PBData_ReadWriter_GUI.DataControllers
{
    /// <summary>
    /// 对照表
    /// </summary>
    public class ComparisonTableController
    {
        /// <summary>
        /// 实例
        /// </summary>
        private static ComparisonTableController _instance;

        /// <summary>
        /// 实例
        /// </summary>
        public static ComparisonTableController Instance => _instance ?? (_instance = new ComparisonTableController());

        /// <summary>
        /// 对照信息
        /// </summary>
        private ConcurrentDictionary<string, string> _comparisonInformation;

        /// <summary>
        /// 上次文件路径
        /// </summary>
        private string _lastFilePath;

        /// <summary>
        /// 构造
        /// </summary>
        public ComparisonTableController()
        {
            _comparisonInformation = new();
        }

        /// <summary>
        /// 获取数据
        /// </summary>
        /// <param name="key"></param>
        /// <param name="content"></param>
        /// <param name="oldData"></param>
        /// <returns></returns>
        public bool GetData(string key, out string content, object oldData = null)
        {
            if (key.StartsWith("enum_") && _comparisonInformation.ContainsKey(key.ToLower().Replace("enum_", "")))
            {
                //删除旧数据
                _comparisonInformation.TryRemove(key.ToLower().Replace("enum_", ""), out _);
                //_comparisonEnumInformation.TryAdd(key);
            }
            return _comparisonInformation.TryGetValue(key.ToLower(), out content);
        }

        /// <summary>
        /// 更新数据
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void UpdateData(string key, string value)
        {
            if (_comparisonInformation.TryGetValue(key, out var oldValue))
                _comparisonInformation.TryUpdate(key, value, oldValue);
        }

        public void Test()
        {
            var keys = _comparisonInformation.Keys.ToList();
            foreach (var key in keys)
            {
                if (key.Contains("."))
                    continue;
                var tempKeys = keys.Where(t => t.EndsWith($".{key}")).ToList();
                tempKeys.ForEach(k =>
                {
                    if (_comparisonInformation.TryRemove(key, out var content))
                    {
                        _comparisonInformation[k] = content;
                    }
                });
            }
            SaveData();
        }

        /// <summary>
        /// 从本地加载数据
        /// </summary>
        /// <param name="filePath"></param>
        public void LoadData(string filePath)
        {
            _lastFilePath = filePath;
            if (!File.Exists(filePath))
            {
                CreateComparisonTable(filePath);
                return;
            }

            var content = File.ReadAllText(filePath);
            if (string.IsNullOrWhiteSpace(content))
            {
                CreateComparisonTable(filePath);
                return;
            }

            var dic = JsonUtil.Deserialize<ConcurrentDictionary<string, object>>(content);
            if (dic == null || dic.IsEmpty)
            {
                CreateComparisonTable(filePath);
                return;
            }
            _comparisonInformation.Clear();
            foreach (var keyValuePair in dic)
            {
                _comparisonInformation.TryAdd(keyValuePair.Key.ToLower(), keyValuePair.Value.ToString());
            }
        }

        private void CreateComparisonTable(string filePath)
        {
            _comparisonInformation ??= new();

            SaveData();
        }

        /// <summary>
        /// 
        /// </summary>
        public void SaveData(string filePath = null)
        {
            if (string.IsNullOrWhiteSpace(_lastFilePath) && string.IsNullOrWhiteSpace(filePath))
                filePath = Path.Combine(".", "ComparisonTable.json");
            File.WriteAllText(filePath ?? _lastFilePath, JsonUtil.Serialize(_comparisonInformation));
        }

        /// <summary>
        /// 增加数据
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void AddData(string key, string value)
        {
            key = key.ToLower();
            if (key.StartsWith("enum_"))
            {
                _comparisonInformation.TryRemove(key.ToLower().Replace("enum_", ""), out _);
            }

            _comparisonInformation.TryAdd(key, value);
        }

    }
}
