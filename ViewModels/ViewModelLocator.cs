using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Wukong_PBData_ReadWriter_GUI.ViewModels
{
    public class ViewModelLocator
    {
        public ViewModelLocator()
        {
            // 注册 ViewModel
            Console.WriteLine("ViewModelLocator initialized.");

            Ioc.Default.ConfigureServices(
                new ServiceCollection()
                    .AddSingleton<PakDecompressViewModel>() // 注册 PakDecompressViewModel
                    .BuildServiceProvider());

            // 检查 ViewModel 是否被正确注册
            var viewModel = Ioc.Default.GetService<PakDecompressViewModel>();
            if (viewModel == null)
            {
                Console.WriteLine("PakDecompressViewModel is null in Locator.");
            }
            else
            {
                Console.WriteLine("PakDecompressViewModel retrieved successfully.");
            }
        }

        public PakDecompressViewModel PakDecompressViewModel 
        {
            get
            {
                var viewModel = Ioc.Default.GetService<PakDecompressViewModel>();
                if (viewModel == null)
                {
                    Console.WriteLine("PakDecompressViewModel is null when accessed.");
                }
                else
                {
                    Console.WriteLine("PakDecompressViewModel retrieved successfully when accessed.");
                }
                return viewModel;
            }
        }
    }
}