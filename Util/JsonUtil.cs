using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wukong_PBData_ReadWriter_GUI.Util
{
    public class JsonUtil
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="content"></param>
        /// <returns></returns>
        public static T? Deserialize<T>(string content) where T : class
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(content);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string Serialize(object obj)
        {
            try
            {
                return JsonConvert.SerializeObject(obj, Formatting.Indented);
            }
            catch (Exception e)
            {
                return "";
            }
        }

        /// <summary>
        /// 拷贝
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static T Copy<T>(object obj) where T : class
        {
            try
            {
                return Deserialize<T>(Serialize(obj));
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }
}
