using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Wukong_PBData_ReadWriter_GUI.Services;
using Wukong_PBData_ReadWriter_GUI.ViewModels;

namespace Wukong_PBData_ReadWriter_GUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            
            // 将 Console 输出重定向到标准输出流
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
            Console.WriteLine("Application started");
            
            Ioc.Default.ConfigureServices(
                new ServiceCollection()
                    .AddSingleton<ISharedDataService, SharedDataService>()  // 注册共享服务
                    .AddSingleton<PakDecompressViewModel>() // 注册 PakDecompressViewModel
                    .AddSingleton<PakCompressViewModel>()
                    .AddSingleton<MenuViewModel>()
                    .BuildServiceProvider());
            Console.WriteLine("Service builded");
            
            base.OnStartup(e);
        }
    }

}
