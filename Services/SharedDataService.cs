using Wukong_PBData_ReadWriter_GUI.Models;

namespace Wukong_PBData_ReadWriter_GUI.Services;

public interface ISharedDataService
{
    ConfigDataModel ConfigData { get; }

    GlobalDataModel GlobalData { get; }
}

public class SharedDataService : ISharedDataService
{
    public ConfigDataModel ConfigData { get; } = new(); // 初始化共享数据模型
    public GlobalDataModel GlobalData { get; } = new(); // 初始化共享数据模型
}