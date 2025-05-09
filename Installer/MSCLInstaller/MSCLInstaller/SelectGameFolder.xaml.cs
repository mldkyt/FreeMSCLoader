using System;
using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.Win32;

namespace MSCLInstaller;

/// <summary>
/// Interaction logic for SelectGameFolder.xaml
/// </summary>
public partial class SelectGameFolder
{
    private MainWindow main;
    public SelectGameFolder()
    {
        InitializeComponent();
    }
    public void Init(MainWindow m)
    {
        main = m;
        main.Title += Storage.selectedGame switch
        {
            Game.Msc => " - My Summer Car",
            Game.MscIma => " - My Summer Car (Community)",
            Game.Mwc => " - My Winter Car",
            _ => throw new ArgumentOutOfRangeException()
        };
        FindMSCOnSteam();
        if (!string.IsNullOrEmpty(Storage.mscPath)) return;
        if (!File.Exists("MSCFolder.txt")) return;
        var mscPath = File.ReadAllText("MSCFolder.txt");
        if (Directory.Exists(mscPath))
        {
            Storage.mscPath = mscPath;
            MscFolder.Text = Storage.mscPath;
            Dbg.Log($"Loaded saved MSC Folder: {mscPath}");
            GameInfo();
        }
        else
        {
            Dbg.Log($"Saved MSC Folder, doesn't exists: {mscPath}");
            try
            {
                File.Delete("MSCFolder.txt");
            }
            catch (Exception e)
            {
                Dbg.Log("Error deleting MSCFolder.txt", true, true);
                Dbg.Log(e.ToString());
            }
        }

    }

    private void FindMSCOnSteam()
    {
        Dbg.Log("Trying get MSC path from steam...");
        var steam = Registry.LocalMachine.OpenSubKey("SOFTWARE\\WOW6432Node\\Valve\\Steam");
        if (steam == null)
        {
            Dbg.Log("Error: Steam not found");
            return;
        }
        var steamPath = steam.GetValue("InstallPath").ToString();
        if (string.IsNullOrEmpty(steamPath))
        {
            Dbg.Log("Error: Steam InstallPath is null! Trying SteamService installpath_default");
            var steam2 = Registry.LocalMachine.OpenSubKey("SOFTWARE\\WOW6432Node\\Valve\\SteamService");
            if (steam2 == null)
            {
                Dbg.Log("Error: Steam is most likely not correctly installed");
                return;
            }

            steamPath = steam2.GetValue("installpath_default").ToString();
            if (string.IsNullOrEmpty(steamPath))
            {
                Dbg.Log("Error: SteamService installpath_default is null! Failed to find MSC path from steam");
                return;
            }

        }

        var msc = Path.Combine(steamPath, "steamapps", "common", "My Summer Car");
        if (File.Exists(Path.Combine(msc, "mysummercar.exe")))
        {
            Storage.mscPath = msc;
        }
        else
        {
            Dbg.Log("Trying get MSC from additional library folders...");
            var steamLib = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
            if (File.Exists(Path.Combine(steamLib)))
            {
                var lib = File.ReadAllLines(steamLib);
                foreach (var s in lib)
                {
                    if (!s.Trim().StartsWith("\"path\"")) continue;
                    var p = Path.GetFullPath(s.Trim().Split('"')[3]);
                    if (string.IsNullOrEmpty(p))
                    {
                        Dbg.Log($"Warn: Got invalid path in libraryfolders.vdf: {s}");
                        continue;
                    }
                    var msclib = Path.Combine(p, "steamapps", "common", "My Summer Car");
                    if (File.Exists(Path.Combine(msclib, "mysummercar.exe")))
                    {
                        Storage.mscPath = msclib;
                    }
                }
            }
        }

        if (string.IsNullOrEmpty(Storage.mscPath)) return;
        MscFolder.Text = Storage.mscPath;
        Dbg.Log($"Got MSC path from steam: {Storage.mscPath}");
        GameInfo();
    }
    private void GameInfo()
    {
        var game = Path.Combine(Storage.mscPath, "mysummercar.exe");
        if (!File.Exists(game)) return;
        DetectedGameText.Text = string.Empty;
        switch (Md5FileHashes.Md5HashFile(game))
        {
            case Md5FileHashes.Msc64:
            case Md5FileHashes.Msc64d9:
                DetectedGameText.Inlines.Add(new Run("Detected: ") { FontWeight = FontWeights.Bold });
                DetectedGameText.Inlines.Add(new Bold(new Run("My Summer Car (64-bit)") { Foreground = Brushes.LightGreen }));
                break;
            case Md5FileHashes.Msc32:
                Storage.is64 = false;
                DetectedGameText.Inlines.Add(new Run("Detected: ") { FontWeight = FontWeights.Bold });
                DetectedGameText.Inlines.Add(new Bold(new Run("My Summer Car (32-bit)") { Foreground = Brushes.LightGreen }));
                break;
            default:
                DetectedGameText.Inlines.Add(new Run("Detected: ") { FontWeight = FontWeights.Bold });
                DetectedGameText.Inlines.Add(new Bold(new Run("Unknown") { Foreground = Brushes.Red }));
                break;
        }
        GoNext.IsEnabled = true;

    }
    private void MSCFBrowse_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "mysummercar.exe|mysummercar.exe"
        };
        if (openFileDialog.ShowDialog() != true) return;
        Storage.mscPath = Path.GetFullPath(Path.GetDirectoryName(openFileDialog.FileName) ?? throw new InvalidOperationException());
        MscFolder.Text = Storage.mscPath;
        Dbg.Log($"MSC path set manually: {Storage.mscPath}");
        try
        {
            File.WriteAllText("MSCFolder.txt", Storage.mscPath);
        }
        catch (Exception ex)
        {
            Dbg.Log("Error creating MSCFolder.txt", true, true);
            Dbg.Log(ex.ToString());
        }
        GameInfo();
    }

    private void GoNext_Click(object sender, RoutedEventArgs e)
    {
        main.FreeMscLoaderInstallerPage();
    }
}