using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Navigation;

namespace MSCLInstaller;

/// <summary>
/// Interaction logic for About.xaml
/// </summary>
public partial class About
{
    public About()
    {
        InitializeComponent();
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(e.Uri.ToString());
    }

    private void Hyperlink_Click(object sender, RoutedEventArgs e)
    {
        //Open License.txt file
        Process.Start(Path.Combine(Storage.currentPath, "License.txt"));
    }
}