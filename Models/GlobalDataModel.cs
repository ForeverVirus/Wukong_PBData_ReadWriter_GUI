using Wukong_PBData_ReadWriter_GUI.Entity;

namespace Wukong_PBData_ReadWriter_GUI.Models;

// 全局数据太多太多了，先移出来放一起再拆
public class GlobalDataModel
{
    public GlobalDataModel()
    {
        this.config = new();
    }

    /// <summary>
    /// 配置
    /// </summary>
    public readonly Config config;
    
}