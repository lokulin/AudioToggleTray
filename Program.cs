using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace AudioToggleTray;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => LogCrash(e.ExceptionObject as Exception);
        Application.ThreadException += (_, e) => LogCrash(e.Exception);

        // Prevent two copies from running at once (duplicate tray icons, hotkey conflicts).
        using var mutex = new Mutex(true, "Global\\AudioToggleTray_SingleInstance_9F3E1C2B", out var createdNew);

        if (!createdNew)
        {
            MessageBox.Show(
                "AudioToggleTray is already running. Look for its icon in the system tray.",
                "AudioToggleTray",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        try
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new TrayAppContext());
        }
        catch (Exception ex)
        {
            LogCrash(ex);
        }
    }

    internal static void LogCrash(Exception? ex)
    {
        try
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AudioToggleTray");
            Directory.CreateDirectory(folder);
            File.AppendAllText(Path.Combine(folder, "crash.log"), $"{DateTime.Now:u}{Environment.NewLine}{ex}{Environment.NewLine}{Environment.NewLine}");
        }
        catch
        {
            // If logging itself fails, there's nothing more we can do.
        }
    }
}

public class HotkeyWindow : NativeWindow, IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    public const int WM_DEVICES_CHANGED = 0x8001; // WM_APP + 1

    public event EventHandler? HotkeyPressed;
    public event EventHandler? DevicesChanged;

    public HotkeyWindow()
    {
        CreateHandle(new CreateParams());
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_HOTKEY)
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
        else if (m.Msg == WM_DEVICES_CHANGED)
            DevicesChanged?.Invoke(this, EventArgs.Empty);

        base.WndProc(ref m);
    }

    public void Dispose()
    {
        DestroyHandle();
    }
}
