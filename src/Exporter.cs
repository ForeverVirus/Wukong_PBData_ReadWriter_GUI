using BtlB1;
using BtlShare;
using Google.Protobuf;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ResB1;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using Wukong_PBData_ReadWriter_GUI.DataControllers;

namespace Wukong_PBData_ReadWriter_GUI.src
{
    public class Exporter
    {
        public static Assembly s_protobufDB = Assembly.Load("GSE.ProtobufDB");
        public static Assembly s_runtime = Assembly.Load("Protobuf.Runtime");

        /// <summary>
        /// 
        /// </summary>
        private static MD5 _md5 = MD5.Create();


        public static Dictionary<string, (Type, Assembly)> s_speFileTypeMapping = new Dictionary<string, (Type, Assembly)>()
        {
            {"FUStBeAttackedInfoOldDesc.data", (typeof(FUStBeAttackedInfoDesc), s_protobufDB)},
            {"FUStBeAttackedInfoOldDesc1.data", (typeof(FUStBeAttackedInfoDesc), s_protobufDB)},
            {"EndingCredits_EndA.data", (typeof(EndingCreditsData), s_runtime) },
            {"EndingCredits_EndA_Other.data", (typeof(EndingCreditsData), s_runtime) },
            {"EndingCredits_EndB.data", (typeof(EndingCreditsData), s_runtime) },
            {"EndingCredits_EndB_Other.data", (typeof(EndingCreditsData), s_runtime) },
            {"FUStUnitCommOverrideDesc.data", (typeof(FUStUnitCommDesc), s_protobufDB) },
        };

        public static bool InputJson2Data(string filePath, string outputPath)
        {
            var json = File.ReadAllText(filePath);

            if (json == null) return false;

            string fileName = Path.GetFileName(filePath);

            var protobufDBTypes = s_protobufDB.GetTypes();
            var runtimeTypes = s_runtime.GetTypes();

            var tuple = GetTypeAssemblyTupleByFileName(fileName, protobufDBTypes, runtimeTypes);
            var realType = tuple.Item1;

            Type tbType = null;

            try
            {
                tbType = tuple.Item2.GetTypes().First(a => a.Name == "TB" + realType.Name);
            }
            catch (Exception ex)
            {

            }

            if (tbType != null)
            {
                realType = tbType;
            }


            IMessage data = JsonConvert.DeserializeObject(json, realType) as IMessage;

            if (data == null) return false;

            try
            {
                if (!Directory.Exists(outputPath))
                {
                    var outDir = outputPath.Substring(0, outputPath.LastIndexOf("\\"));
                    Directory.CreateDirectory(outDir);
                }
                // 使用 FileStream 将序列化的数据写入文件
                using (FileStream output = File.Create(outputPath))
                {
                    data.WriteTo(output);
                }
                Console.WriteLine($"数据已成功写入到: {outputPath}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"写入文件时发生错误: {ex.Message}");
                return false;
            }
        }

        static (Type, Assembly) GetTypeAssemblyTupleByFileName(string fileName, Type[] protobufDBTypes, Type[] runtimeTypes)
        {
            bool find = false;
            foreach (var type in protobufDBTypes)
            {
                if (fileName.Contains(type.Name) && type.IsClass)
                {
                    return (type, s_protobufDB);
                }
            }

            foreach (var type in runtimeTypes)
            {
                if (fileName.Contains(type.Name))
                {
                    return (type, s_runtime);
                }
            }
            if (s_speFileTypeMapping.TryGetValue(fileName, out var typeAssemblyTuple))
            {
                return (typeAssemblyTuple.Item1, typeAssemblyTuple.Item2);
            }

            return (null, null);
        }

        public static bool ExportAll2Json(string outputDir)
        {
            List<string> fileNameList = new List<string>();
            List<string> filePathList = new List<string>();
            List<Type> types = new List<Type>();
            Director("ProtoData", fileNameList, filePathList);

            if (fileNameList.Count > 0)
            {
                int index = 0;

                var protobufDBTypes = s_protobufDB.GetTypes();
                var runtimeTypes = s_runtime.GetTypes();
                foreach (string fileName in fileNameList)
                {
                    var tuple = GetTypeAssemblyTupleByFileName(fileName, protobufDBTypes, runtimeTypes);

                    if (tuple.Item1 != null && tuple.Item2 != null)
                    {
                        ExportToJson(tuple.Item2, tuple.Item1, filePathList[index], outputDir);
                    }

                    index++;
                }
            }

            return true;
        }

        public static bool GetIsValidFile(string fileName, string filePath)
        {
            var protobufDBTypes = s_protobufDB.GetTypes();
            var runtimeTypes = s_runtime.GetTypes();
            var tuple = GetTypeAssemblyTupleByFileName(fileName, protobufDBTypes, runtimeTypes);

            if (tuple.Item1 == null || tuple.Item2 == null)
                return false;

            return true;
        }

        public static IMessage GetDataByFile(string fileName, string filePath)
        {
            var protobufDBTypes = s_protobufDB.GetTypes();
            var runtimeTypes = s_runtime.GetTypes();
            var tuple = GetTypeAssemblyTupleByFileName(fileName, protobufDBTypes, runtimeTypes);

            if (tuple.Item1 == null || tuple.Item2 == null)
                return null;

            var bytes = File.ReadAllBytes(filePath);

            var realType = tuple.Item1;
            Type tbType = null;

            try
            {
                tbType = tuple.Item2.GetTypes().First(a => a.Name == "TB" + realType.Name);
            }
            catch (Exception ex)
            {

            }
            if (tbType != null)
            {
                realType = tbType;
            }

            var parser = realType.GetProperty("Parser", BindingFlags.Static | BindingFlags.Public);
            if (parser != null)
            {
                try
                {
                    MessageParser parserValue = parser.GetMethod.Invoke(null, null) as MessageParser;
                    var message = parserValue.ParseFrom(bytes);
                    if (message != null)
                    {
                        return message;
                    }
                }
                catch
                {
                    Console.WriteLine("Data Failed File : " + filePath);
                }
            }
            return null;
        }

        static void ExportToJson(Assembly assmebly, Type type, string filePath, string outputDir)
        {
            if (assmebly == null || type == null || string.IsNullOrEmpty(filePath))
            { return; }

            var bytes = File.ReadAllBytes(filePath);

            var realType = type;
            Type tbType = null;

            try
            {
                tbType = assmebly.GetTypes().First(a => a.Name == "TB" + realType.Name);
            }
            catch (Exception ex)
            {

            }
            if (tbType != null)
            {
                realType = tbType;
            }


            var parser = realType.GetProperty("Parser", BindingFlags.Static | BindingFlags.Public);
            if (parser != null)
            {
                try
                {
                    MessageParser parserValue = parser.GetMethod.Invoke(null, null) as MessageParser;
                    var message = parserValue.ParseFrom(bytes);
                    if (message != null)
                    {
                        string outPath = outputDir + "\\" + filePath + ".json";

                        if (!Directory.Exists(outPath))
                        {
                            var outDir = outPath.Substring(0, outPath.LastIndexOf("\\"));
                            Directory.CreateDirectory(outDir);
                        }

                        if (File.Exists(outPath))
                        {
                            File.Delete(outPath);
                        }

                        var outputJson = JsonConvert.SerializeObject(message, Formatting.Indented);

                        Console.WriteLine("Success : " + outPath);
                        File.WriteAllText(outPath, outputJson);
                    }
                }
                catch
                {
                    Console.WriteLine("Data Failed File : " + filePath);
                }
            }

        }

        public static void Director(string dir, List<string> list, List<string> filePathList)
        {
            DirectoryInfo d = new DirectoryInfo(dir);
            if (!d.Exists)
                return;
            FileInfo[] files = d.GetFiles();//文件
            DirectoryInfo[] directs = d.GetDirectories();//文件夹
            foreach (FileInfo f in files)
            {
                if (f.Extension == ".data")
                {
                    list.Add(f.Name);//添加文件名到列表中  
                    var indexOf = f.FullName.LastIndexOf("\\");
                    filePathList.Add(f.FullName);
                }
            }
            //获取子文件夹内的文件列表，递归遍历  
            foreach (DirectoryInfo dd in directs)
            {
                Director(dd.FullName, list, filePathList);
            }
        }

        public static void ExportDescriptionConfig(Dictionary<string, string> config, string path)
        {
            var json = JsonConvert.SerializeObject(config);

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            File.WriteAllText(path, json);
        }

        public static void ExportItemDataBytes(Dictionary<string, byte[]> itemData, string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            //write itemData to file with BinaryWriter
            using (BinaryWriter bw = new BinaryWriter(new FileStream(path, FileMode.CreateNew)))
            {
                bw.Write(itemData.Count);
                foreach (var item in itemData)
                {
                    bw.Write(item.Key);
                    bw.Write(item.Value.Length);
                    bw.Write(item.Value);
                }
            }
        }

        public static Dictionary<string, byte[]> ImportItemDataBytes(string path)
        {
            Dictionary<string, byte[]> itemData = new Dictionary<string, byte[]>();

            //read itemData from file
            using (BinaryReader br = new BinaryReader(new FileStream(path, FileMode.Open)))
            {
                var count = br.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    var key = br.ReadString();
                    var byteLength = br.ReadInt32();
                    var bytes = br.ReadBytes(byteLength);

                    itemData.Add(key, bytes);
                }
            }
            return itemData;
        }

        public static Dictionary<string, string> ImportDescriptionConfig(string path)
        {
            var dict = new Dictionary<string, string>();
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);

                dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            }

            return dict;
        }

        public static void SaveDataFile(string path, DataFile dataFile)
        {
            if (dataFile == null) return;

            string dir = Path.GetDirectoryName(path);

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            using (FileStream output = File.Create(path))
            {
                if (dataFile._FileData == null && dataFile.Tag is string)
                {
                    dataFile.LoadData();
                }

                dataFile._FileData.WriteTo(output);
            }
        }

        public static async Task<List<(string, DataFile, DataItem)>> GlobalSearchCacheAsync(List<DataFile> fileList)
        {
            return await Task.Run(() =>
            {
                List<(string, DataFile, DataItem)> cache = new List<(string, DataFile, DataItem)>();

                foreach (DataFile file in fileList)
                {
                    file.LoadData();
                    if (file._FileDataItemList != null && file._FileDataItemList.Count > 0)
                    {
                        foreach (var data in file._FileDataItemList)
                        {
                            string cacheKey = $"{file._FileName}({file._Desc})-{data._ID}({data._Desc})";
                            cache.Add((cacheKey, file, data));
                            MainWindow.s_TraditionGlobalSearchCache.Add((cacheKey, file, data));

                            data.LoadData();

                            var propertyItems = data._DataPropertyItems;
                            foreach (var property in propertyItems)
                            {
                                if (property != null)
                                {
                                    var value = property._PropertyInfo.GetValue(property._BelongData, null);

                                    ProcessGlobalSearch(value, file, data, cache, property._PropertyName, property._PropertyDesc);
                                }
                            }
                        }
                    }
                }

                return cache;
            });
        }

        static void ProcessGlobalSearch(object value, DataFile file, DataItem data, List<(string, DataFile, DataItem)> cache, string propertyName, string propertyDesc = "")
        {
            if (value is string)
            {
                string key = $"{file._FileName}({file._Desc})-{data._ID}({data._Desc})-{propertyName}({propertyDesc})-{value}";

                cache.Add((key, file, data));
            }
            else if (IsNumber(value))
            {
                string key = $"{file._FileName}({file._Desc})-{data._ID}({data._Desc})-{propertyName}({propertyDesc})-{value}";

                cache.Add((key, file, data));
            }
            else if (value is Enum)
            {
                string key = $"{file._FileName}({file._Desc})-{data._ID}({data._Desc})-{propertyName}({propertyDesc})-{value.ToString()}";

                cache.Add((key, file, data));
            }
            else if (value is IMessage)
            {
                var type = value.GetType();

                //var ps = type.GetProperties();
                if (!PropertiesDataController.Instance.Get(type, out var ps))
                {
                    ps = PropertiesDataController.Instance.Add(type);
                }

                foreach (var p in ps)
                {
                    ProcessGlobalSearch(p.GetValue(value), file, data, cache, p.Name, "");
                }
            }
            else if (typeof(IList).IsAssignableFrom(value.GetType()))
            {
                var listValue = value as IList;
                int index = 0;
                foreach (var item in listValue)
                {
                    ProcessGlobalSearch(item, file, data, cache, $"{propertyName}{index}", "");
                    index++;
                }
            }
        }
        public static bool IsNumber(object value)
        {
            if (value == null) return false;

            TypeCode typeCode = Type.GetTypeCode(value.GetType());
            return typeCode == TypeCode.Int32 || typeCode == TypeCode.Int64 || typeCode == TypeCode.Single || typeCode == TypeCode.Double;
        }

        public static Dictionary<string, string> GenerateFirstDescConfig(List<DataFile> fileList)
        {
            Dictionary<string, string> descConfig = new Dictionary<string, string>();

            foreach (var file in fileList)
            {
                file.LoadData();
                if (file._FileDataItemList != null && file._FileDataItemList.Count > 0)
                {
                    foreach (var data in file._FileDataItemList)
                    {
                        var properties = data._Data.GetType().GetProperties();
                        foreach (var property in properties)
                        {
                            if (property.PropertyType == typeof(string))
                            {
                                var value = property.GetValue(data._Data, null) as string;
                                if (ContainsChineseUsingRegex(value))
                                {
                                    descConfig.TryAdd(file._FileData.GetType().Name + "_" + data._ID, value);
                                    break;
                                }
                                if (IsPathFormat(value))
                                {
                                    descConfig.TryAdd(file._FileData.GetType().Name + "_" + data._ID, GetFileName(value));
                                }
                            }
                            if (property.PropertyType == typeof(int))
                            {
                                var value = (int)property.GetValue(data._Data, null);
                                if (property.Name.Contains("ID", StringComparison.OrdinalIgnoreCase) && !property.Name.Equals("ID", StringComparison.OrdinalIgnoreCase))
                                {
                                    descConfig.TryAdd(file._FileData.GetType().Name + "_" + data._ID, value.ToString());
                                }
                            }
                        }
                    }
                }
            }
            return descConfig;
        }

        static bool ContainsChineseUsingRegex(string input)
        {
            // 使用正则表达式判断中文字符
            return Regex.IsMatch(input, @"[\u4e00-\u9fff]");
        }

        static bool IsPathFormat(string path)
        {
            // 正则表达式匹配路径格式
            string pattern = @"^[^/]*(/[^/ ]+)+/?$";
            return Regex.IsMatch(path, pattern);
        }

        static string GetFileName(string path)
        {
            // 提取最后一个斜杠后的部分作为文件名
            return path.Substring(path.LastIndexOf('.') + 1);
        }

        public static Dictionary<string, string> CollectItemMD5(List<DataFile> fileList)
        {
            Dictionary<string, string> md5Config = new Dictionary<string, string>();

            foreach (var file in fileList)
            {
                file.LoadData();
                if (file._FileDataItemList != null && file._FileDataItemList.Count > 0)
                {
                    foreach (var data in file._FileDataItemList)
                    {
                        var key = file._FileData.GetType().Name + "_" + data._ID;
                        var bytes = data._Data.ToByteArray();
                        var md5 = MD5.Create().ComputeHash(bytes);
                        var md5Str = BitConverter.ToString(md5).Replace("-", "").ToLower();
                        md5Config.TryAdd(key, md5Str);
                    }
                }
            }

            return md5Config;
        }

        public static Dictionary<string, byte[]> CollectItemBytes(List<DataFile> fileList)
        {
            Dictionary<string, byte[]> itemData = new Dictionary<string, byte[]>();

            foreach (var file in fileList)
            {
                file.LoadData();
                if (file._FileDataItemList != null && file._FileDataItemList.Count > 0)
                {
                    foreach (var data in file._FileDataItemList)
                    {
                        var key = file._FileData.GetType().Name + "_" + data._ID;
                        var bytes = data._Data.ToByteArray();
                        itemData.TryAdd(key, bytes);
                    }
                }
            }

            return itemData;
        }

        public static bool IsSameAsMd5(DataItem item, Dictionary<string, string> md5Config)
        {
            if (item == null || item._Data == null)
                return false;

            string key = item._File._FileData.GetType().Name + "_" + item._ID;

            if (md5Config.TryGetValue(key, out var md5))
            {
                var bytes = item._Data.ToByteArray();
                var itemMd5 = _md5.ComputeHash(bytes);
                var md5Str = BitConverter.ToString(itemMd5).Replace("-", "").ToLower();
                bytes = null;
                return md5Str.Equals(md5);
            }

            return false;

        }
    }
}
