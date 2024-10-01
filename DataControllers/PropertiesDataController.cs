using System.Reflection;


namespace Wukong_PBData_ReadWriter_GUI.DataControllers
{
    /// <summary>
    /// 
    /// </summary>
    public class PropertiesDataController
    {
        /// <summary>
        /// 实例
        /// </summary>
        private static PropertiesDataController _instance;

        /// <summary>
        /// 实例
        /// </summary>
        public static PropertiesDataController Instance => _instance ?? (_instance = new PropertiesDataController());

        /// <summary>
        /// 
        /// </summary>
        private readonly ConcurrentDictionary<Type, PropertyInfo[]> _cache = new();

        /// <summary>
        /// 增加
        /// </summary>
        /// <param name="type"></param>
        public PropertyInfo[] Add(Type type)
        {
            if (!_cache.TryGetValue(type, out var propertyInfos))
            {
                propertyInfos = type.GetProperties();
                _cache.TryAdd(type, propertyInfos);
            }
            return propertyInfos;
        }

        /// <summary>
        /// 获取值
        /// </summary>
        /// <param name="type"></param>
        /// <param name="propertyInfos"></param>
        /// <returns></returns>
        public bool Get(Type type, out PropertyInfo[] propertyInfos)
        {
            return _cache.TryGetValue(type, out propertyInfos);
        }
    }
}
