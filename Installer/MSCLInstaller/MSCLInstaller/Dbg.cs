using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;

namespace MSCLInstaller;

internal static class Dbg
{
    private static readonly TraceSource _ts = new("MSCLInstaller");
    private static TextWriterTraceListener _tw;

    public static void Init()
    {
        Stream logFile = File.Create("Log.txt");
        _tw = new TextWriterTraceListener(logFile);
        _ts.Switch.Level = SourceLevels.All;
        _ts.Listeners.Add(_tw);
        Log($"MSCLoader Installer Log {DateTime.Now:u}");
        Log($"Installer Version {Assembly.GetExecutingAssembly().GetName().Version}");
    }

    public static void Log(string message, bool newline = false, bool separator = false)
    {
        if (_tw == null) return;
        if (newline) _tw.WriteLine("");
        _tw.WriteLine(message);
        if (separator) _tw.WriteLine("=================");
        _tw.Flush();
    }

    public static void MissingFilesError()
    {
        MessageBox.Show(
            $"Couldn't find any required core files.{Environment.NewLine}Please unpack all files before launching this installer!",
            "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
        Log($"!!! ERROR !!!", true, true);
        Log($"Core files not found, exiting.");
        Environment.Exit(0);
    }
}