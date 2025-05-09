namespace MSCLInstaller;

internal enum SelectedAction
{
    Nothing,
    ChangeModsFolder,
    InstallMscLoader,
    UpdateMscLoader,
    ReinstallMscLoader,
    UninstallMscLoader,
    AdvancedOptions
}
internal enum ModsFolder
{
    GameFolder,
    MyDocuments,
    Appdata
}
internal static class Storage
{
    public static string currentPath;
    public static string mscPath;
    public static string modsPath;
    public static Game selectedGame = Game.Msc;
    public static bool is64 = true;
    public static bool dbgPack = false;
    public static string[] packFiles = null;
    public static SelectedAction selectedAction = SelectedAction.Nothing;
    public static bool skipIntroCfg = false;
    public static bool skipConfigScreenCfg = false;
    public static ModsFolder modsFolderCfg = ModsFolder.GameFolder;
}