using System;
using System.IO;
using System.Windows;

namespace MSCLInstaller;

/// <summary>
/// Interaction logic for SelectModsFolder.xaml
/// </summary>
public partial class SelectModsFolder
{
    private MainWindow main;
    private string gfPath;
    private string mdPath;
    private string adPath;
    private ModsFolder currentFolder = ModsFolder.GameFolder;
    private ModsFolder selectedFolder = ModsFolder.GameFolder;
    private bool changeFolder;
    public SelectModsFolder()
    {
        InitializeComponent();
    }

    public void Init(MainWindow m, bool change)
    {
        main = m;
        changeFolder = change;
        PopulatePaths();
    }

    private void PopulatePaths()
    {
        Dbg.Log("Getting paths");
        gfPath = Path.GetFullPath(Path.Combine(Storage.mscPath, "Mods"));
        GameFolderPathText.Text = gfPath;
        Dbg.Log($"Game folder path: {gfPath}");
        if (changeFolder && gfPath == Storage.modsPath)
        {
            GameFolderRb.IsChecked = true;
            currentFolder = ModsFolder.GameFolder;
            GfCurrentText.Visibility = Visibility.Visible;
            Dbg.Log("[Game folder is current path]");
        }
        mdPath = Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "MySummerCar", "Mods"));
        MyDocumentsPathText.Text = mdPath;
        Dbg.Log($"My documents path: {mdPath}");
        if (mdPath.Contains("OneDrive"))
        {
            MyDocumentsRb.IsEnabled = false;
            OneDriveWarning.Visibility = Visibility.Visible;
            Dbg.Log("[OneDrive path detected]");
        }
        else
        {
            if (changeFolder && mdPath == Storage.modsPath)
            {
                MyDocumentsRb.IsChecked = true;
                currentFolder = ModsFolder.MyDocuments;
                MdCurrentText.Visibility = Visibility.Visible;
                Dbg.Log("[My documents is current path]");
            }
        }
        adPath = Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "..", "LocalLow", "Amistech", "My Summer Car", "Mods"));
        AppdataPathText.Text = adPath;
        Dbg.Log($"Appdata path: {adPath}");
        switch (changeFolder)
        {
            case true when adPath == Storage.modsPath:
                AppdataRb.IsChecked = true;
                currentFolder = ModsFolder.Appdata;
                AdCurrentText.Visibility = Visibility.Visible;
                Dbg.Log("[Appdata is current path]");
                break;
            case false:
                currentFolder = ModsFolder.GameFolder;
                GameFolderRb.IsChecked = true;
                break;
        }
    }
    private void SelectModsFolderBtn_Click(object sender, RoutedEventArgs e)
    {
        var oldPath = Storage.modsPath;
        var newPath = selectedFolder switch
        {
            ModsFolder.GameFolder => gfPath,
            ModsFolder.MyDocuments => mdPath,
            _ => adPath
        };
        //If Change Folder is selected
        if (changeFolder)
        {
            if (selectedFolder == currentFolder)
            {
                Dbg.Log("Selected folder is same as current, no changes will be made");
                main.MSCLoaderInstallerPage(); //No change, go back.
                return;
            }

            if (MessageBox.Show(
                    $"Your new Mods folder will be changed to {Environment.NewLine}{newPath}{Environment.NewLine}{Environment.NewLine}Continue?",
                    "Mods Folder", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            Dbg.Log($"Changing mods folder from {oldPath} to {newPath}");
            if (MessageBox.Show($"Do you want to copy all mods from old location to new one?{Environment.NewLine}{Environment.NewLine}Selecting No will create blank Mods folder in new location", "Copy Old Mods?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                Dbg.Log($"Selected Yes, copying old mods from {oldPath} to {newPath}");
                ChangeModsFolderStart(true, oldPath, newPath, selectedFolder);
            }
            else
            {
                Dbg.Log($"Creating new blank mods folder in {newPath}");
                ChangeModsFolderStart(false, oldPath, newPath, selectedFolder);
            }
            return;
        }
        Storage.modsPath = newPath;
        main.InstallProgressPage().InstallMSCLoader(selectedFolder);
    }
    private void ChangeModsFolderStart(bool copyOldMods, string oldPath, string newPath, ModsFolder newFolder)
    {
        var deleteTargetFolder = false;

        if (Directory.Exists(newPath) && copyOldMods)
        {
            Dbg.Log($"New path for Mods folder {newPath} already exists");
            if (MessageBox.Show($"Warning!{Environment.NewLine}Selected mods path already exists, all files will be deleted in new path!{Environment.NewLine}{Environment.NewLine}Continue?", "Mods Folder", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
            {
                Dbg.Log("Cancelling folder change");
                return;
            }
        }
        if (Directory.Exists(newPath) && !copyOldMods)
        {
            Dbg.Log($"New path for Mods folder {newPath} already exists");
            var messageBoxResult = MessageBox.Show($"Warning!{Environment.NewLine}Selected mods path already exists, do you want to delete it and make it new Mods folder?{Environment.NewLine}{Environment.NewLine}Yes - Delete and make it as new blank Mods folder{Environment.NewLine}No - Keep all files in target folder{Environment.NewLine}Cancel - Cancel folder change", "Mods Folder", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
            switch (messageBoxResult)
            {
                case MessageBoxResult.Cancel:
                    Dbg.Log("Cancelling folder change");
                    return;
                case MessageBoxResult.Yes:
                    deleteTargetFolder = true;
                    break;
                case MessageBoxResult.None:
                case MessageBoxResult.OK:
                case MessageBoxResult.No:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        Storage.modsPath = newPath;
        if (copyOldMods)
        {
            main.InstallProgressPage().ChangeModsFolderAndCopyAsync(oldPath, newFolder);
        }
        else
        {
            main.InstallProgressPage().ChangeModsFolder(newFolder, deleteTargetFolder);
        }
    }
    private void GameFolderRB_Checked(object sender, RoutedEventArgs e)
    {
        selectedFolder = ModsFolder.GameFolder;
    }
    private void MyDocumentsRB_Checked(object sender, RoutedEventArgs e)
    {
        selectedFolder = ModsFolder.MyDocuments;
    }

    private void AppdataRB_Checked(object sender, RoutedEventArgs e)
    {
        selectedFolder = ModsFolder.Appdata;
    }
}