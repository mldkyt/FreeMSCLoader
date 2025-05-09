using System;
using System.IO;
using System.Reflection;
using System.Windows;

namespace MSCLInstaller;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    private SelectGameFolder selectGameFolder;
    private FreeMscLoaderInstaller mscloaderInstaller;
    private SelectModsFolder selectModsFolder;
    private InstallProgress installProgress;
    private Version msclInstallerVer;
    public MainWindow()
    {
        InitializeComponent();
        try
        {
            Initialize();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}{Environment.NewLine}{Environment.NewLine}Failed to initialize installer.{Environment.NewLine}Make sure to:{Environment.NewLine}- Unpack ALL files before launching{Environment.NewLine}- Files aren't unpacked in protected folders (like Program Files or root of C drive). {Environment.NewLine}{Environment.NewLine}Installer will now close. {Environment.NewLine}You can try running it as admin.", "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Dbg.Log(ex.Message, true, true);
            Environment.Exit(0);
        }

    }
    private void Initialize()
    {
        msclInstallerVer = Assembly.GetExecutingAssembly().GetName().Version;
        Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
        Storage.currentPath = Path.GetFullPath(".");
        if (File.Exists("Log.txt")) File.Delete("Log.txt");
        if (Directory.Exists(Path.Combine(Storage.currentPath, "temp"))) Directory.Delete(Path.Combine(Storage.currentPath, "temp"), true);
        Dbg.Init();
        Dbg.Log($"Current folder: {Storage.currentPath}");
        selectGameFolder = new SelectGameFolder();
        mscloaderInstaller = new FreeMscLoaderInstaller();
        selectModsFolder = new SelectModsFolder();
        Storage.packFiles = Directory.GetFiles(Storage.currentPath, "*.pack");
        if (Storage.packFiles.Length == 0)
        {
            Dbg.MissingFilesError();
        }
        if (!File.Exists("Ionic.Zip.Reduced.dll") || !File.Exists("INIFileParser.dll"))
        {
            Dbg.MissingFilesError();
        }

        SelectGameFolderPage(Game.Msc);
        StatusBarText.Text = $"MSCLInstaller (ver. {msclInstallerVer.Major}.{msclInstallerVer.Minor}.{msclInstallerVer.Build})";
    }
    internal void SetFreeMscLoaderVer(string ver)
    {
        StatusBarText.Text = $"MSCLInstaller (ver. {msclInstallerVer.Major}.{msclInstallerVer.Minor}.{msclInstallerVer.Build}) | FreeMSCLoader (ver. {ver})";
    }
    internal void ReInitInstallerPage()
    {
        mscloaderInstaller = new FreeMscLoaderInstaller();
    }

    internal InstallProgress InstallProgressPage()
    {
        installProgress = new InstallProgress(this);
        MainFrame.Content = installProgress;
        return installProgress;
    }

    private void SelectGameFolderPage(Game game)
    {
        Dbg.Log("Select Game Folder", true, true);
        MsciPageTitle.Text = "Select Game Folder";
        Storage.selectedGame = game;
        MainFrame.Content = selectGameFolder;
        selectGameFolder.Init(this);
    }

    public void FreeMscLoaderInstallerPage()
    {
        Dbg.Log("FreeMSCLoader Installer", true, true);
        MainFrame.Content = mscloaderInstaller;
        MsciPageTitle.Text = "Select Option Below";
        mscloaderInstaller.Init(this);
    }

    public void SelectModsFolderPage(bool change)
    {
        Dbg.Log("Select Mods Folder", true, true);
        MsciPageTitle.Text = "Select Mods Folder";
        MainFrame.Content = selectModsFolder;
        selectModsFolder.Init(this, change);
    }

    private void Hyperlink_Click(object sender, RoutedEventArgs e)
    {
        var a = new About
        {
            Owner = this
        };
        a.ShowDialog();
    }
}