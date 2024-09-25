using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Wukong_PBData_ReadWriter_GUI.ViewModels;

public class ViewModelLocator
{
    public PakDecompressViewModel PakDecompressViewModel => Ioc.Default.GetService<PakDecompressViewModel>();

    public ViewModelLocator()
    {
        // 注册 ViewModel
        Ioc.Default.ConfigureServices(
            new ServiceCollection()
                .AddSingleton<PakDecompressViewModel>()
                .BuildServiceProvider());
    }
}
