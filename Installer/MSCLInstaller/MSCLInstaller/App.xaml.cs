using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace MSCLInstaller;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
    private Mutex mutex;
    
    private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
#if DEBUG
            throw e.Exception;
#else
        MessageBox.Show($"Error:{Environment.NewLine}{Environment.NewLine}{e.Exception.Message}{Environment.NewLine}{Environment.NewLine}Please include 'Log.txt' file when reporting this issue.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        Dbg.Log("!!! Unhandled Exception !!!", true, true);
        Dbg.Log(e.Exception.ToString());
        e.Handled = true;
#endif       
    }

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        mutex = new Mutex(true, "MSCLInstaller", out var aIsNewInstance);
        if (aIsNewInstance) return;
        MessageBox.Show("MSCLInstaller is already running...", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        Environment.Exit(0);
    }
}