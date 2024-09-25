using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Windows;

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
            base.OnStartup(e);
        }
    }

}
