using System.IO;
using System.Windows;

namespace Wukong_PBData_ReadWriter_GUI.Views;

public partial class HelpWindowView
{
    public HelpWindowView()
    {
        InitializeComponent();
    }

    private void WindowLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            HelpText.Text = File.ReadAllText("README.md");
        }
        catch (Exception)
        {
            HelpText.Text = string.Empty;
        }
    }
}