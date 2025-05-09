using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using IniParser;
using Ionic.Zip;

namespace MSCLInstaller;

/// <summary>
/// Interaction logic for InstallProgress.xaml
/// </summary>
public partial class InstallProgress
{
    private readonly MainWindow main;
    private readonly Progress<(string text, string log)> progressLog = new();
    private readonly Progress<(int percent, int max, bool isIndeterminate)> progress = new();

    public InstallProgress(MainWindow m)
    {
        InitializeComponent();
        LogBox.Clear();
        progressLog.ProgressChanged += (_, e) =>
        {
            ProgressText.Text = e.text;
            AddLog(e.log);
        };
        progress.ProgressChanged += (_, e) =>
        {
            ProgressBar.IsIndeterminate = e.isIndeterminate;
            ProgressBar.Maximum = e.max;
            ProgressBar.Value = e.percent;
        };
        main = m;
    }

    internal void ChangeModsFolder(ModsFolder newfolder, bool deleteTarget)
    {
        AddLog("Changing Mods Folder...");
        var failed = ChangeModsFolderWork(newfolder, deleteTarget, progressLog, progress);
        if (failed) return;
        if (MessageBox.Show("Folder has been changed", "Change Mods Folder", MessageBoxButton.OK,
                MessageBoxImage.Information) != MessageBoxResult.OK) return;
        main.ReInitInstallerPage();
        main.FreeMscLoaderInstallerPage();
    }
    internal void ChangeModsFolderAndCopyAsync(string oldPath, ModsFolder newFolder)
    {
        AddLog("Changing Mods Folder...");
        var failed = ChangeModsFolderWork(newFolder, true, progressLog, progress);
        if (failed) return;
        AddLog($"Copying mods from {oldPath} to {Storage.modsPath}...");
        ProgressBar.Maximum = 100;
        var failed2 = ChangeModsFolderAndCopyWork(oldPath, progressLog, progress);
        if (failed2) return;
        if (MessageBox.Show("Folder has been changed", "Change Mods Folder", MessageBoxButton.OK,
                MessageBoxImage.Information) != MessageBoxResult.OK) return;
        main.ReInitInstallerPage();
        main.FreeMscLoaderInstallerPage();
    }
    internal void UninstallFreeMscLoader()
    {
        AddLog("Uninstalling FreeMSCLoader...");
        var failed = UninstallFreeMscLoaderWork(progressLog, progress);
        if (failed) return;
        if (MessageBox.Show("Uninstall Complete", "Uninstalling FreeMSCLoader", MessageBoxButton.OK,
                MessageBoxImage.Information) != MessageBoxResult.OK) return;
        main.ReInitInstallerPage();
        main.FreeMscLoaderInstallerPage();
    }

    internal void InstallFreeMscLoader(ModsFolder modsfolder)
    {
        AddLog("Installing FreeMSCLoader...");
        var failed = InstallFreeMscLoaderWork(modsfolder, progressLog, progress);
        if (failed) return;
        if (MessageBox.Show("Install Complete", "Install FreeMSCLoader", MessageBoxButton.OK,
                MessageBoxImage.Information) != MessageBoxResult.OK) return;
        main.ReInitInstallerPage();
        main.FreeMscLoaderInstallerPage();
    }

    internal void UpdateFreeMscLoader(bool updateCore, bool updateRefs, bool updateMscl)
    {
        AddLog("Updating FreeMSCLoader...");
        var failed = UpdateFreeMscLoaderWork(updateCore, updateRefs, updateMscl, progressLog, progress);
        if (failed) return;
        if (MessageBox.Show("Update Complete", "Update FreeMSCLoader", MessageBoxButton.OK,
                MessageBoxImage.Information) != MessageBoxResult.OK) return;
        main.ReInitInstallerPage();
        main.FreeMscLoaderInstallerPage();
    }

    private bool ChangeModsFolderWork(ModsFolder newfolder, bool deleteTarget, IProgress<(string text, string log)> progressLog, IProgress<(int percent, int max, bool isIndeterminate)> progress)
    {
        progress.Report((0, 10, true));

        if (!UpdateConfigFile(newfolder, progressLog))
            return true;

        if (deleteTarget)
        {
            try
            {
                if (Directory.Exists(Storage.modsPath))
                {
                    progressLog.Report(("Deleting existing folder", $"Deleting.....{Storage.modsPath}"));
                    Directory.Delete(Storage.modsPath, true);

                }
                progressLog.Report(("Creating new folder structure", "Creating new folder structure"));
                progress.Report((0, 5, false));


                Directory.CreateDirectory(Storage.modsPath);
                progressLog.Report(($"Creating: {Storage.modsPath}", $"Creating.....{Storage.modsPath}"));
                progress.Report((1, 5, false));

                Directory.CreateDirectory(Path.Combine(Storage.modsPath, "Assets"));
                progressLog.Report(("Creating Assets folder", $"Creating.....{Path.Combine(Storage.modsPath, "Assets")}"));
                progress.Report((2, 5, false));

                Directory.CreateDirectory(Path.Combine(Storage.modsPath, "References"));
                progressLog.Report(("Creating References folder", $"Creating.....{Path.Combine(Storage.modsPath, "References")}"));
                progress.Report((3, 5, false));
            }
            catch (Exception e)
            {
                progressLog.Report(("Failed", e.ToString()));
                MessageBox.Show($"Failed to create folder structure.{Environment.NewLine}{Environment.NewLine} {e.Message}", "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return true;
            }
        }

        if (!ExtractFiles(Path.Combine(Storage.currentPath, "main_msc.pack"), Path.Combine(Storage.currentPath, "temp"), progressLog, progress, true) || !ExtractFiles(Path.Combine(Storage.currentPath, "temp", "Mods.zip"), Storage.modsPath, progressLog, progress, false))
        {
            MessageBox.Show("Failed to update files, check Log for more info.", "Fatal Error.", MessageBoxButton.OK, MessageBoxImage.Error);
            progressLog.Report(("Something went wrong", "Some steps failed to complete. Please try again by running installer as Administrator"));
            return true;
        }

        Directory.Delete(Path.Combine(Storage.currentPath, "temp"), true);
        progress.Report((5, 5, false));
        progressLog.Report(("Done", "Done!"));
        return false;
    }

    private static bool ChangeModsFolderAndCopyWork(string oldPath, IProgress<(string text, string log)> progressLog, IProgress<(int percent, int max, bool isIndeterminate)> progress)
    {
        progress.Report((0, 10, true));
        var oldMods = Directory.GetFiles(oldPath, "*.dll");
        var oldAssets = Directory.GetDirectories(Path.Combine(oldPath, "Assets"));
        var oldRefs = Directory.GetFiles(Path.Combine(oldPath, "References"), "*.dll");

        progressLog.Report(("Copying mods", "- Start Copying Mods -"));
        try
        {
            for (var i = 0; i < oldMods.Length; i++)
            {
                progressLog.Report(($"Copying mod {Path.GetFileName(oldMods[i])}", $"Copying mod {Path.GetFileName(oldMods[i])}....."));
                progress.Report((i + 1, oldMods.Length, false));
                File.Copy(oldMods[i], Path.Combine(Storage.modsPath, Path.GetFileName(oldMods[i])));

            }
            progressLog.Report(("Copying mods", "- End Copying Mods -"));

            progressLog.Report(("Copying Assets", "- Start Copying Assets -"));
            for (var i = 0; i < oldAssets.Length; i++)
            {
                var dir = new DirectoryInfo(oldAssets[i]);
                if (dir.Name.StartsWith("MSCLoader_")) continue;
                progressLog.Report(($"Copying Asset {dir.Name}", $"Copying Asset {dir.Name}....."));
                progress.Report((i + 1, oldAssets.Length, false));
                CopyDirectory(oldAssets[i], Path.Combine(Path.Combine(Storage.modsPath, "Assets"), dir.Name), true);

            }
            progressLog.Report(("Copying Assets", "- End Copying Assets -"));

            progressLog.Report(("Copying Config", "- Start Copying Config -"));
            CopyDirectory(Path.Combine(oldPath, "Config"), Path.Combine(Storage.modsPath, "Config"), true);
            progressLog.Report(("Copying Config", "- End Copying Config -"));

            progressLog.Report(("Copying Refereces", "- Start Copying Refereces -"));
            for (var i = 0; i < oldRefs.Length; i++)
            {
                progressLog.Report(($"Copying reference {Path.GetFileName(oldRefs[i])}", $"Copying reference {Path.GetFileName(oldMods[i])}....."));
                progress.Report((i + 1, oldRefs.Length, false));
                File.Copy(oldRefs[i], Path.Combine(Path.Combine(Storage.modsPath, "References"), Path.GetFileName(oldRefs[i])));

            }
        }
        catch (Exception e)
        {
            MessageBox.Show($"Error: {e.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            progressLog.Report((e.Message, e.ToString()));
            return true;
        }
        progressLog.Report(("Copying Refereces", "- End Copying Refereces -"));
        progress.Report((5, 5, false));
        progressLog.Report(("Done", "Done!"));
        return false;
    }
    private static bool UninstallFreeMscLoaderWork(IProgress<(string text, string log)> progressLog, IProgress<(int percent, int max, bool isIndeterminate)> progress)
    {
        progress.Report((0, 10, true));
        int max;

        try
        {
            var zip1 = ZipFile.Read(Path.Combine(Storage.currentPath, "main_ref.pack"));
            max = zip1.Entries.Count + 3;
            for (var i = 0; i < zip1.Entries.Count; i++)
            {
                var zz = zip1[i];
                progressLog.Report(($"Deleting {zz.FileName}", $"Deleting.....{zz.FileName}"));
                DeleteIfExists(Path.Combine(Storage.mscPath, "mysummercar_Data", "Managed" + zz.FileName));
                progress.Report((i, max, false));

            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            progressLog.Report((ex.Message, ex.ToString()));
            return true;
        }

        DeleteIfExists(Path.Combine(Storage.mscPath, "mysummercar_Data", "Managed", "MSCLoader.dll"));
        DeleteIfExists(Path.Combine(Storage.mscPath, "mysummercar_Data", "Managed", "MSCLoader.dll.mdb"));
        DeleteIfExists(Path.Combine(Storage.mscPath, "mysummercar_Data", "Managed", "MSCLoader.pdb"));
        progressLog.Report(("Deleting FreeMSCLoader.dll", "Deleting.....FreeMSCLoader.dll"));
        progress.Report((max - 2, max, false));

        DeleteIfExists(Path.Combine(Storage.mscPath, "winhttp.dll"));

        progressLog.Report(("Deleting winhttp.dll", "Deleting.....winhttp.dll"));
        progress.Report((max - 1, max, false));

        DeleteIfExists(Path.Combine(Storage.mscPath, "doorstop_config.ini"));
        progressLog.Report(("Deleting doorstop_config.ini", "Deleting.....doorstop_config.ini"));
        progress.Report((max, max, false));

        progressLog.Report(("Done", "Done!"));
        return false;
    }
    private bool InstallFreeMscLoaderWork(ModsFolder newfolder, IProgress<(string text, string log)> progressLog, IProgress<(int percent, int max, bool isIndeterminate)> progress)
    {
        var failed = false;
        progress.Report((0, 10, true));
        progressLog.Report(("Installing Core Files (1 / 4)", "Installing Core Files"));

        if (Storage.is64)
        {
            if (!ExtractFiles(Path.Combine(Storage.currentPath, "core64.pack"), Storage.mscPath, progressLog, progress, false)) failed = true;
        }
        else
        {
            if (!ExtractFiles(Path.Combine(Storage.currentPath, "core32.pack"), Storage.mscPath, progressLog, progress, false)) failed = true;
        }
        if (failed)
        {
            MessageBox.Show("Failed to install core files, check Log for more info.", "Fatal Error.", MessageBoxButton.OK, MessageBoxImage.Error);
            progressLog.Report(("Something went wrong", "Some steps failed to complete. Please try again by running installer as Administrator"));
            return true;
        }
        progress.Report((0, 10, true));
        progressLog.Report(("Installing References (2 / 4)", "Installing References"));

        if (!ExtractFiles(Path.Combine(Storage.currentPath, "main_ref.pack"), Path.Combine(Storage.mscPath, "mysummercar_Data", "Managed"), progressLog, progress, false))
        {
            MessageBox.Show("Failed to install files, check Log for more info.", "Fatal Error.", MessageBoxButton.OK, MessageBoxImage.Error);
            progressLog.Report(("Something went wrong", "Some steps failed to complete. Please try again by running installer as Administrator"));
            return true;
        }
        progress.Report((0, 10, true));
        progressLog.Report(("Installing FreeMSCLoader (3 / 4)", "Installing FreeMSCLoader"));

        if (!ExtractFiles(Path.Combine(Storage.currentPath, "main_msc.pack"), Path.Combine(Storage.currentPath, "temp"), progressLog, progress, true)) failed = true;

        if (!ExtractFiles(Path.Combine(Storage.currentPath, "temp", "Managed.zip"), Path.Combine(Storage.mscPath, "mysummercar_Data", "Managed"), progressLog, progress, false)) failed = true;
        if (failed)
        {
            MessageBox.Show("Failed to install files, check Log for more info.", "Fatal Error.", MessageBoxButton.OK, MessageBoxImage.Error);
            progressLog.Report(("Something went wrong", "Some steps failed to complete. Please try again by running installer as Administrator"));
            return true;
        }
        progress.Report((0, 10, true));
        progressLog.Report(("Installing FreeMSCLoader Assets (4 / 4)", "Installing FreeMSCLoader Assets"));

        if (!ExtractFiles(Path.Combine(Storage.currentPath, "temp", "Mods.zip"), Storage.modsPath, progressLog, progress, false))
        {
            MessageBox.Show("Failed to install files, check Log for more info.", "Fatal Error.", MessageBoxButton.OK, MessageBoxImage.Error);
            progressLog.Report(("Something went wrong", "Some steps failed to complete. Please try again by running installer as Administrator"));
            return true;
        }
        progress.Report((0, 10, true));
        progressLog.Report(("Cleaning temp files", "Cleaning temp files"));

        Directory.Delete(Path.Combine(Storage.currentPath, "temp"), true);
        progress.Report((0, 10, true));
        progressLog.Report(("Updating config file", "Updating config file"));

        if (!UpdateConfigFile(newfolder, progressLog))
            failed = true;
        progress.Report((5, 5, false));

        progressLog.Report(failed
            ? ("Something went wrong",
                "Some steps failed to complete. Please try again by running installer as Administrator")
            : ("Done", "Done!"));
        return failed;
    }
    private bool UpdateFreeMscLoaderWork(bool updateCore, bool updateRefs, bool updateMscl, IProgress<(string text, string log)> progressLog, IProgress<(int percent, int max, bool isIndeterminate)> progress)
    {
        var failed = false;
        progress.Report((0, 10, true));
        progressLog.Report(("Updating MSCLoader Files", "Updating MSCLoader Files"));

        if (updateCore)
        {
            if (Storage.is64)
                if (!ExtractFiles(Path.Combine(Storage.currentPath, "core64.pack"), Storage.mscPath, progressLog, progress, false) || !ExtractFiles(Path.Combine(Storage.currentPath, "core32.pack"), Storage.mscPath, progressLog, progress, false)) failed = true;
            progressLog.Report(("Updating config file", "Updating config file"));

            if (!UpdateConfigFile(Storage.modsFolderCfg, progressLog)) failed = true;
        }
        if (failed)
        {
            MessageBox.Show("Failed to update files, check Log for more info.", "Fatal Error.", MessageBoxButton.OK, MessageBoxImage.Error);
            progressLog.Report(("Something went wrong", "Some steps failed to complete. Please try again by running installer as Administrator"));
            return true;
        }

        if (updateRefs)
            if (!ExtractFiles(Path.Combine(Storage.currentPath, "main_ref.pack"), Path.Combine(Storage.mscPath, "mysummercar_Data", "Managed"), progressLog, progress, false)) failed = true;
        if (failed)
        {
            MessageBox.Show("Failed to update files, check Log for more info.", "Fatal Error.", MessageBoxButton.OK, MessageBoxImage.Error);
            progressLog.Report(("Something went wrong", "Some steps failed to complete. Please try again by running installer as Administrator"));
            return true;
        }

        if (updateMscl)
        {
            if (!ExtractFiles(Path.Combine(Storage.currentPath, "main_msc.pack"), Path.Combine(Storage.currentPath, "temp"), progressLog, progress, true)) failed = true;

            if (!ExtractFiles(Path.Combine(Storage.currentPath, "temp", "Managed.zip"), Path.Combine(Storage.mscPath, "mysummercar_Data", "Managed"), progressLog, progress, false)) failed = true;

            if (!ExtractFiles(Path.Combine(Storage.currentPath, "temp", "Mods.zip"), Storage.modsPath, progressLog, progress, false)) failed = true;
            progress.Report((0, 10, true));
            progressLog.Report(("Cleaning temp files", "Cleaning temp files"));

            Directory.Delete(Path.Combine(Storage.currentPath, "temp"), true);
        }
        if (failed)
        {
            MessageBox.Show("Failed to update files, check Log for more info.", "Fatal Error.", MessageBoxButton.OK, MessageBoxImage.Error);
            progressLog.Report(("Something went wrong", "Some steps failed to complete. Please try again by running installer as Administrator"));
            return true;
        }
        progress.Report((5, 5, false));
        progressLog.Report(("Done", "Done!"));
        return false;
    }
    private void AddLog(string log)
    {
        LogBox.AppendText($"{log}{Environment.NewLine}");
        LogBox.ScrollToEnd();
        Dbg.Log(log);
    }
    private static bool ExtractFiles(string fn, string target, IProgress<(string text, string log)> progressLog, IProgress<(int percent, int max, bool isIndeterminate)> progress, bool isTemp)
    {
        try
        {
            if (!ZipFile.IsZipFile(fn))
            {
                progressLog.Report(($"ERROR: Failed read file: {Path.GetFileName(fn)}", $"ERROR: Failed read file.....{Path.GetFileName(fn)}"));
                return false;
            }

            var zip1 = ZipFile.Read(fn);
            progressLog.Report(($"Reading file: {Path.GetFileName(fn)}", $"Reading file.....{Path.GetFileName(fn)}"));
            if (!isTemp)
                progress.Report((0, zip1.Entries.Count, false));


            for (var i = 0; i < zip1.Entries.Count; i++)
            {
                var zz = zip1[i];
                if (!isTemp)
                {
                    progressLog.Report(($"Copying file: {zz.FileName}", $"Copying file.....{zz.FileName}"));
                    progress.Report((i, zip1.Entries.Count, false));
                }
                zz.Extract(target, ExtractExistingFileAction.OverwriteSilently);


            }
            zip1.Dispose();

            return true;
        }
        catch (Exception e)
        {
            progressLog.Report(($"Failed to extract files {e.Message}", $"Failed to extract files{Environment.NewLine}{e}"));
            return false;
        }
    }
    private static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
    {
        // Get information about the source directory
        var dir = new DirectoryInfo(sourceDir);

        // Check if the source directory exists
        if (!dir.Exists)
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

        // Cache directories before we start copying
        var dirs = dir.GetDirectories();

        // Create the destination directory
        Directory.CreateDirectory(destinationDir);

        // Get the files in the source directory and copy to the destination directory
        foreach (var file in dir.GetFiles())
        {
            var targetFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(targetFilePath);
        }

        // If recursive and copying subdirectories, recursively call this method
        if (!recursive) return;
        foreach (var subDir in dirs)
        {
            var newDestinationDir = Path.Combine(destinationDir, subDir.Name);
            CopyDirectory(subDir.FullName, newDestinationDir, true);
        }
    }
    private bool UpdateConfigFile(ModsFolder modsFolder, IProgress<(string text, string log)> progressLog)
    {
        var newCfg = modsFolder switch
        {
            ModsFolder.GameFolder => "GF",
            ModsFolder.MyDocuments => "MD",
            ModsFolder.Appdata => "AD",
            _ => "GF"
        };

        if (File.Exists(Path.Combine(Storage.mscPath, "doorstop_config.ini")))
        {
            try
            {
                progressLog.Report(("Reading: doorstop_config.ini", "Reading.....doorstop_config.ini"));
                var ini = new FileIniDataParser().ReadFile(Path.Combine(Storage.mscPath, "doorstop_config.ini"));

                ini["MSCLoader"]["mods"] = newCfg;
                ini["MSCLoader"]["skipIntro"] = Storage.skipIntroCfg.ToString().ToLower();
                ini["MSCLoader"]["skipConfigScreen"] = Storage.skipConfigScreenCfg.ToString().ToLower();
                progressLog.Report(("Writing: doorstop_config.ini", "Writing.....doorstop_config.ini"));
                new FileIniDataParser().WriteFile(Path.Combine(Storage.mscPath, "doorstop_config.ini"), ini, Encoding.ASCII);

            }
            catch (Exception e)
            {
                progressLog.Report(("ERROR: Failed to update doorstop_config.ini", e.ToString()));
                MessageBox.Show($"ERROR: Failed to update doorstop_config.ini{Environment.NewLine}{Environment.NewLine}{e.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            return true;
        }

        //This is probably super outdated MSCLoader.
        progressLog.Report(("ERROR: doorstop_config.ini does not exist", "ERROR: doorstop_config.ini does not exist"));
        progressLog.Report(("ERROR: doorstop_config.ini does not exist", "Please re-install MSCLoader first."));
        return false;
    }
    private static void DeleteIfExists(string filename)
    {
        if (File.Exists(filename))
        {
            File.Delete(filename);
        }
    }
}