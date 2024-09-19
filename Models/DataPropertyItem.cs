using System.Reflection;
using Google.Protobuf;

namespace Wukong_PBData_ReadWriter_GUI.Models;

public class DataPropertyItem
{
    public required string PropertyName { get; init; }
    public required PropertyInfo PropertyInfo { get; init; }
    public required IMessage BelongData { get; init; }
    public required DataItem DataItem { get; init; }

    // public string PropertyDesc =>
    //     DataFileHelper.DescriptionConfig.GetValueOrDefault(
    //         DataItem.File.FileData?.GetType().Name + "_" + PropertyName, ""
    //     );
}