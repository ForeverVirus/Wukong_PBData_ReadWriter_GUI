using BtlB1;
using BtlShare;
using Google.Protobuf;
using Newtonsoft.Json;
using ResB1;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Wukong_PBData_ReadWriter_GUI.src
{
    public class Exporter
    {
        public static Assembly s_protobufDB = Assembly.Load("GSE.ProtobufDB");
        public static Assembly s_runtime = Assembly.Load("Protobuf.Runtime");

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
                else
                    File.Delete(f.FullName);
            }
            //获取子文件夹内的文件列表，递归遍历  
            foreach (DirectoryInfo dd in directs)
            {
                Director(dd.FullName, list, filePathList);
            }
        }
    }
}
