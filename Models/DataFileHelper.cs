using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using BtlB1;
using BtlShare;
using Google.Protobuf;
using Newtonsoft.Json;
using ResB1;

namespace Wukong_PBData_ReadWriter_GUI.Models
{
    public static class DataFileHelper
    {
        public static Dictionary<string, string> DescriptionConfig { get; set; } = new();
        private static Assembly s_protobufDB = Assembly.Load("GSE.ProtobufDB");
        private static Assembly s_runtime = Assembly.Load("Protobuf.Runtime");

        private static Dictionary<string, (Type, Assembly)> s_speFileTypeMapping =
            new()
            {
                { "FUStBeAttackedInfoOldDesc.data", (typeof(FUStBeAttackedInfoDesc), s_protobufDB) },
                { "FUStBeAttackedInfoOldDesc1.data", (typeof(FUStBeAttackedInfoDesc), s_protobufDB) },
                { "EndingCredits_EndA.data", (typeof(EndingCreditsData), s_runtime) },
                { "EndingCredits_EndA_Other.data", (typeof(EndingCreditsData), s_runtime) },
                { "EndingCredits_EndB.data", (typeof(EndingCreditsData), s_runtime) },
                { "EndingCredits_EndB_Other.data", (typeof(EndingCreditsData), s_runtime) },
                { "FUStUnitCommOverrideDesc.data", (typeof(FUStUnitCommDesc), s_protobufDB) },
            };

        public static List<DataFile> GetAllDataFiles(DirectoryInfo dirInfo)
        {
            var res = dirInfo.GetFiles().Where(file => file.Extension == ".data")
                .Select(fi => new DataFile(fi)).ToList();

            foreach (var subDirInfo in dirInfo.GetDirectories())
            {
                res.AddRange(GetAllDataFiles(subDirInfo));
            }

            return res;
        }
        
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

        private static (Type?, Assembly?) GetTypeAssemblyTupleByFileName(
            string fileName,
            Type[] protobufDBTypes,
            Type[] runtimeTypes)
        {
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

            return s_speFileTypeMapping.TryGetValue(fileName, out var typeAssemblyTuple)
                ? (typeAssemblyTuple.Item1, typeAssemblyTuple.Item2)
                : (null, null);
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

        public static IMessage? GetDataByFile(FileInfo fileInfo)
        {
            var protobufDBTypes = s_protobufDB.GetTypes();
            var runtimeTypes = s_runtime.GetTypes();
            var tuple = GetTypeAssemblyTupleByFileName(fileInfo.Name, protobufDBTypes, runtimeTypes);

            if (tuple.Item1 == null || tuple.Item2 == null)
            {
                return null;
            }

            var bytes = File.ReadAllBytes(fileInfo.FullName);

            var realType = tuple.Item1;
            Type? tbType;
            try
            {
                tbType = tuple.Item2.GetTypes().First(a => a.Name == "TB" + realType.Name);
            }
            catch (Exception)
            {
                tbType = null;
            }

            if (tbType != null)
            {
                realType = tbType;
            }

            var parser = realType.GetProperty("Parser", BindingFlags.Static | BindingFlags.Public);
            if (parser == null) return null;
            try
            {
                var parserValue = parser.GetMethod?.Invoke(null, null) as MessageParser;
                var message = parserValue?.ParseFrom(bytes);
                if (message != null)
                {
                    return message;
                }
            }
            catch
            {
                Console.WriteLine("Data Failed File : " + fileInfo.FullName);
            }

            return null;
        }

        static void ExportToJson(Assembly? assembly, Type? type, string filePath, string outputDir)
        {
            if (assembly == null || type == null || string.IsNullOrEmpty(filePath))
            {
                return;
            }

            var bytes = File.ReadAllBytes(filePath);

            var realType = type;
            Type? tbType;
            try
            {
                tbType = assembly.GetTypes().First(a => a.Name == "TB" + realType.Name);
            }
            catch (Exception)
            {
                tbType = null;
            }

            if (tbType != null)
            {
                realType = tbType;
            }


            var parser = realType.GetProperty("Parser", BindingFlags.Static | BindingFlags.Public);
            if (parser == null) return;
            try
            {
                var parserValue = parser.GetMethod.Invoke(null, null) as MessageParser;
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

        public static void Director(string dir, List<string> list, List<string> filePathList)
        {
            DirectoryInfo d = new DirectoryInfo(dir);
            FileInfo[] files = d.GetFiles(); //文件
            DirectoryInfo[] directs = d.GetDirectories(); //文件夹
            foreach (FileInfo f in files)
            {
                if (f.Extension == ".data")
                {
                    list.Add(f.Name); //添加文件名到列表中  
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

        public static Dictionary<string, string> ImportDescriptionConfig(string path)
        {
            if (!File.Exists(path))
            {
                return new Dictionary<string, string>();
            }

            var json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(json)!;
        }

        public static void SaveDataFile(string path, DataFile dataFile)
        {
            // if (dataFile == null) return;
            //
            // string dir = Path.GetDirectoryName(path);
            //
            // if (!Directory.Exists(dir))
            // {
            //     Directory.CreateDirectory(dir);
            // }
            //
            // if (File.Exists(path))
            // {
            //     File.Delete(path);
            // }
            //
            // using var output = File.Create(path);
            // dataFile.FileData.WriteTo(output);
        }

        // public static List<(string, DataFile, DataItem)> GlobalSearchCache(List<DataFile> fileList)
        // {
        //     List<(string, DataFile, DataItem)> cache = new List<(string, DataFile, DataItem)>();
        //
        //     foreach (DataFile file in fileList)
        //     {
        //         if (file.DataItemList != null && file.DataItemList.Count > 0)
        //         {
        //             foreach (var data in file.DataItemList)
        //             {
        //                 string cacheKey = $"{file._FileName}({file._Desc})-{data._ID}({data.Desc})";
        //                 cache.Add((cacheKey, file, data));
        //
        //                 //var properties = data._Data.GetType().GetProperties();
        //                 //foreach (var property in properties)
        //                 //{
        //                 //    if (property.PropertyType == typeof(string))
        //                 //    {
        //                 //        var value = property.GetValue(data._Data, null) as string;
        //                 //        if (ContainsChineseUsingRegex(value))
        //                 //        {
        //                 //            cache.Add(file._FileData.GetType().Name + "_" + data._ID);
        //                 //            break;
        //                 //        }
        //                 //        if (IsPathFormat(value))
        //                 //        {
        //                 //            cache.Add(file._FileData.GetType().Name + "_" + data._ID);
        //                 //        }
        //                 //    }
        //                 //}
        //             }
        //         }
        //     }
        //
        //     return cache;
        // }

        // public static Dictionary<string, string> GenerateFirstDescConfig(List<DataFile> fileList)
        // {
        //     Dictionary<string, string> descConfig = new Dictionary<string, string>();
        //
        //     foreach (var file in fileList)
        //     {
        //         file.LoadData();
        //         if (file.DataItemList != null && file.DataItemList.Count > 0)
        //         {
        //             foreach (var data in file.DataItemList)
        //             {
        //                 var properties = data._Data.GetType().GetProperties();
        //                 foreach (var property in properties)
        //                 {
        //                     if (property.PropertyType == typeof(string))
        //                     {
        //                         var value = property.GetValue(data._Data, null) as string;
        //                         if (ContainsChineseUsingRegex(value))
        //                         {
        //                             descConfig.TryAdd(file.FileData.GetType().Name + "_" + data._ID, value);
        //                             break;
        //                         }
        //
        //                         if (IsPathFormat(value))
        //                         {
        //                             descConfig.TryAdd(file.FileData.GetType().Name + "_" + data._ID,
        //                                 GetFileName(value));
        //                         }
        //                     }
        //
        //                     if (property.PropertyType == typeof(int))
        //                     {
        //                         var value = (int)property.GetValue(data._Data, null);
        //                         if (property.Name.Contains("ID", StringComparison.OrdinalIgnoreCase) &&
        //                             !property.Name.Equals("ID", StringComparison.OrdinalIgnoreCase))
        //                         {
        //                             descConfig.TryAdd(file.FileData.GetType().Name + "_" + data._ID, value.ToString());
        //                         }
        //                     }
        //                 }
        //             }
        //         }
        //     }
        //
        //     return descConfig;
        // }

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
    }
}