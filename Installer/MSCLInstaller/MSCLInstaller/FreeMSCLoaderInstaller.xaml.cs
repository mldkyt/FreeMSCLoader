using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using IniParser;
using Ionic.Zip;

namespace MSCLInstaller;

/// <summary>
/// Interaction logic for MSCLoaderInstaller.xaml
/// </summary>
public partial class FreeMscLoaderInstaller
{
    private bool initialized;
    private MainWindow main;

    //MSCloader install status
    private bool isInstalled; //Is MSCLoader installed
    private bool resetConfig; //Config is out of date (or invalid), reset it.
    private bool fullinstall; //Do a full install
    private bool updateCore; //Only update core files
    private bool updateRefs; //Only update references
    private bool updateMscl; //Only update MSCLoader files

    public FreeMscLoaderInstaller()
    {
        InitializeComponent();
    }
    public void Init(MainWindow m)
    {
        if (initialized)
        {
            UpdatePathText();
            return;
        }

        main = m;
        if (File.Exists(Path.Combine(Storage.mscPath, "mysummercar_Data", "Managed", "MSCLoader.Preloader.dll")) && File.Exists(Path.Combine(Storage.mscPath, "winhttp.dll")) && File.Exists(Path.Combine(Storage.mscPath, "doorstop_config.ini")))
        {
            isInstalled = true;
        }

        ToggleAdvancedRadioVisibility(File.Exists(Path.Combine(Storage.currentPath, "dbg.pack"))
            ? Visibility.Visible
            : Visibility.Collapsed);
        if (isInstalled)
        {
            try
            {
                resetConfig = CheckConfig();
            }
            catch (Exception e)
            {
                Dbg.Log("Failed to read doorstop_config.ini");
                Dbg.Log(e.ToString());
                resetConfig = true;
            }
            Storage.modsPath ??= Path.GetFullPath(Path.Combine(Storage.mscPath, "Mods"));
            if (resetConfig)
            {
                Dbg.Log("Outdated config - update Required");
                fullinstall = true;
            }
            else
            {
                Dbg.Log("Comparing Versions:");
                try
                {
                    VersionCompares();
                }
                catch (Exception e)
                {
                    Dbg.Log("Fatal Error when checking versions");
                    Dbg.Log(e.ToString());
                    MessageBox.Show($"Fatal Error when checking versions.{Environment.NewLine}{Environment.NewLine}{e.Message}", "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    fullinstall = true;
                }
            }
        }
        UpdateInstallationStatus();
        UpdatePathText();
        initialized = true;
    }

    private void UpdatePathText()
    {
        MscFolderText.Text = "MSC Folder: ";
        MscFolderText.Inlines.Add(new Run(Storage.mscPath) { FontWeight = FontWeights.Bold, Foreground = Brushes.Wheat });
        ModsFolderText.Text = "Mods Folder: ";
        ModsFolderText.Inlines.Add(new Run(Storage.modsPath) { FontWeight = FontWeights.Bold, Foreground = Brushes.Wheat });
    }

    private void UpdateInstallationStatus()
    {
        ExecuteSelectedBtn.IsEnabled = false;
        InstallStatusText.Text = "Status: ";
        if (!isInstalled)
        {
            InstallStatusText.Inlines.Add(new Run("Not installed") { FontWeight = FontWeights.Bold, Foreground = Brushes.Red });
            ToggleRadioButtonsVisibility(Visibility.Collapsed, Visibility.Visible, Visibility.Collapsed, Visibility.Collapsed, Visibility.Collapsed);
            return;
        }
        if (fullinstall || updateCore || updateRefs || updateMscl)
        {
            InstallStatusText.Inlines.Add(new Run("Updates available") { FontWeight = FontWeights.Bold, Foreground = Brushes.LightBlue });
            ToggleRadioButtonsVisibility(resetConfig ? Visibility.Collapsed : Visibility.Visible, Visibility.Collapsed, Visibility.Visible, Visibility.Collapsed, Visibility.Visible);
            return;
        }
        InstallStatusText.Inlines.Add(new Run("Installed and up to date") { FontWeight = FontWeights.Bold, Foreground = Brushes.LightGreen });
        ToggleRadioButtonsVisibility(Visibility.Visible, Visibility.Collapsed, Visibility.Collapsed, Visibility.Visible, Visibility.Visible);
    }

    private void ToggleRadioButtonsVisibility(Visibility changeModsFolder, Visibility installMscLoader, Visibility updateMscLoader, Visibility reinstallMscLoader, Visibility uninstallMscLoader)
    {
        ModsFolderRadio.Visibility = changeModsFolder;
        InstallRadio.Visibility = installMscLoader;
        UpdateRadio.Visibility = updateMscLoader;
        ReinstallRadio.Visibility = reinstallMscLoader;
        UninstallRadio.Visibility = uninstallMscLoader;
    }

    private void ToggleAdvancedRadioVisibility(Visibility advancedOptions)
    {
        AdvancedRadio.Visibility = advancedOptions;
    }

    private void VersionCompares()
    {
        string corepack;
        corepack = Path.Combine(Storage.currentPath, Storage.is64 ? "core64.pack" : "core32.pack");
        if (!File.Exists(corepack))
        {
            Dbg.MissingFilesError();
        }
        if (!ZipFile.IsZipFile(corepack))
        {
            Dbg.Log($"Failed to read file: {corepack}");
        }
        var zip1 = ZipFile.Read(corepack);
        var coreVer = new Version("0.0.0.0");
        if (zip1.Comment != null)
        {
            coreVer = new Version(zip1.Comment);
        }
        zip1.Dispose();
        Dbg.Log("Comparing core file version...", true);
        if (InstallerHelpers.VersionCompare(coreVer, Path.Combine(Storage.mscPath, "winhttp.dll")))
        {
            updateCore = true;
        }
        Directory.CreateDirectory(Path.Combine(Storage.currentPath, "temp"));
        var refpack = Path.Combine(Storage.currentPath, "main_ref.pack");
        if (!File.Exists(refpack))
        {
            Dbg.MissingFilesError();
        }
        if (!ZipFile.IsZipFile(refpack))
        {
            Dbg.Log($"Failed to read file: {refpack}");
        }
        var zip2 = ZipFile.Read(refpack);
        zip2.ExtractAll(Path.Combine(Storage.currentPath, "temp"));
        zip2.Dispose();
        Dbg.Log("Comparing references versions...", true);
        foreach (var f in Directory.GetFiles(Path.Combine(Storage.currentPath, "temp")))
        {
            if (InstallerHelpers.VersionCompare(f, Path.Combine(Storage.mscPath, "mysummercar_Data", "Managed", Path.GetFileName(f))))
            {
                updateRefs = true;
            }
        }
        Directory.Delete(Path.Combine(Storage.currentPath, "temp"), true);
        var msc = Path.Combine(Storage.currentPath, "main_msc.pack");
        if (!File.Exists(msc))
        {
            Dbg.MissingFilesError();
        }
        if (!ZipFile.IsZipFile(msc))
        {
            Dbg.Log($"Failed to read file: {msc}");
        }
        var zip3 = ZipFile.Read(msc);
        var mscVer = new Version("0.0.0.0");
        if (zip3.Comment != null)
        {

            mscVer = new Version(zip3.Comment);
        }
        zip3.Dispose();
        Dbg.Log("Comparing FreeMSCLoader version...", true);
        if (InstallerHelpers.VersionCompare(mscVer, Path.Combine(Storage.mscPath, "mysummercar_Data", "Managed", "MSCLoader.dll")))
        {
            updateMscl = true;
        }
        main.SetFreeMscLoaderVer(mscVer.ToString());

    }

    private bool CheckConfig()
    {
        if (!File.Exists(Path.Combine(Storage.mscPath, "doorstop_config.ini"))) return true;
        Dbg.Log("Reading.....doorstop_config.ini");
        var ini = new FileIniDataParser().ReadFile(Path.Combine(Storage.mscPath, "doorstop_config.ini"));
        ini.Configuration.AssigmentSpacer = "";
        var cfg = ini["MSCLoader"]["mods"];
        var skipIntro = ini["MSCLoader"]["skipIntro"];
        var skipConfigScreen = ini["MSCLoader"]["skipConfigScreen"];
        if (cfg != null && skipIntro != null)
        {
            Storage.skipIntroCfg = bool.Parse(skipIntro);
            if (skipConfigScreen != null)
                Storage.skipConfigScreenCfg = bool.Parse(skipConfigScreen);
            switch (cfg)
            {
                case "GF":
                    Storage.modsPath = Path.GetFullPath(Path.Combine(Storage.mscPath, "Mods"));
                    Storage.modsFolderCfg = ModsFolder.GameFolder;
                    break;
                case "MD":
                    Storage.modsPath = Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "MySummerCar", "Mods"));
                    Storage.modsFolderCfg = ModsFolder.MyDocuments;
                    break;
                case "AD":
                    Storage.modsPath = Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "..", "LocalLow", "Amistech", "My Summer Car", "Mods"));
                    Storage.modsFolderCfg = ModsFolder.Appdata;
                    break;
                default:
                    Storage.modsPath = Path.GetFullPath(Path.Combine(Storage.mscPath, "Mods"));
                    Storage.modsFolderCfg = ModsFolder.GameFolder;
                    break;
            }
            Dbg.Log($"Found mods folder {Storage.modsPath}");
            if (ini["General"]["target_assembly"] != null) return false;
            Dbg.Log("doorstop_config.ini is outdated (pre-4.x)");
            return true;
        }

        Dbg.Log("doorstop_config.ini is outdated (really old)");

        return true;
    }

    private void ModsFolderRadio_Checked(object sender, RoutedEventArgs e)
    {
        Storage.selectedAction = SelectedAction.ChangeModsFolder;
        ExecuteSelectedBtn.IsEnabled = true;
    }

    private void InstallRadio_Checked(object sender, RoutedEventArgs e)
    {
        Storage.selectedAction = SelectedAction.InstallMscLoader;
        ExecuteSelectedBtn.IsEnabled = true;
    }

    private void UpdateRadio_Checked(object sender, RoutedEventArgs e)
    {
        Storage.selectedAction = SelectedAction.UpdateMscLoader;
        ExecuteSelectedBtn.IsEnabled = true;
    }

    private void ReinstallRadio_Checked(object sender, RoutedEventArgs e)
    {
        Storage.selectedAction = SelectedAction.ReinstallMscLoader;
        ExecuteSelectedBtn.IsEnabled = true;
    }

    private void UninstallRadio_Checked(object sender, RoutedEventArgs e)
    {
        Storage.selectedAction = SelectedAction.UninstallMscLoader;
        ExecuteSelectedBtn.IsEnabled = true;
    }

    private void AdvancedRadio_Checked(object sender, RoutedEventArgs e)
    {
        Storage.selectedAction = SelectedAction.AdvancedOptions;
        ExecuteSelectedBtn.IsEnabled = true;
    }

    private void ExecuteSelectedBtn_Click(object sender, RoutedEventArgs e)
    {
        switch (Storage.selectedAction)
        {
            case SelectedAction.ChangeModsFolder:
                main.SelectModsFolderPage(true);
                break;
            case SelectedAction.InstallMscLoader:
                main.SelectModsFolderPage(false);
                break;
            case SelectedAction.UpdateMscLoader:
                if (fullinstall)
                {
                    main.InstallProgressPage().InstallFreeMscLoader(Storage.modsFolderCfg);
                }
                else
                {
                    main.InstallProgressPage().UpdateFreeMscLoader(updateCore, updateRefs, updateMscl);
                }
                break;
            case SelectedAction.ReinstallMscLoader:
                if (MessageBox.Show("Do you want to reinstall FreeMSCLoader?", "Reinstall FreeMSCLoader", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    main.InstallProgressPage().InstallFreeMscLoader(Storage.modsFolderCfg);
                }
                break;
            case SelectedAction.UninstallMscLoader:
                if (MessageBox.Show("Do you want to uninstall MSCLoader?", "Uninstall MSCLoader", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    main.InstallProgressPage().UninstallFreeMscLoader();
                }
                break;
            case SelectedAction.AdvancedOptions:
                var a = new Advanced
                {
                    Owner = main
                };
                a.ShowDialog();
                break;
            case SelectedAction.Nothing:
            default:
                Dbg.Log("No action selected");
                MessageBox.Show("No action selected", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                break;
        }
    }
}