using System.Windows;

namespace Wukong_PBData_ReadWriter_GUI.Views;

public partial class HelpMenuWindow : Window
{
    public HelpMenuWindow()
    {
        InitializeComponent();
        if (File.Exists("README.md"))
        {
            HelpTextBlock.Text = File.ReadAllText("README.md");
        }
    }

    private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
    {
        System.Diagnostics.Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }
}