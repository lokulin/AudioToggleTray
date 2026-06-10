using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using NAudio.CoreAudioApi;

namespace AudioToggleTray;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new TrayAppContext());
    }
}

public class HotkeyWindow : NativeWindow, IDisposable
{
    private const int WM_HOTKEY = 0x0312;

    public event EventHandler? HotkeyPressed;

    public HotkeyWindow()
    {
        CreateHandle(new CreateParams());
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_HOTKEY)
        {
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
        }

        base.WndProc(ref m);
    }

    public void Dispose()
    {
        DestroyHandle();
    }
}

public class TrayAppContext : ApplicationContext
{
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(
        IntPtr hWnd,
        int id,
        uint fsModifiers,
        uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(
        IntPtr hWnd,
        int id);

    private const uint MOD_CONTROL = 0x0002;

    private const int HOTKEY_ID = 1;

    private HotkeyWindow? _hotkeyWindow;

    private readonly NotifyIcon _trayIcon;

    private readonly MMDeviceEnumerator _enumerator = new();

    private MMDevice? _speakers;
    private MMDevice? _headphones;

    private readonly Icon _speakerIcon;
    private readonly Icon _headphoneIcon;

    public TrayAppContext()
    {
        _speakerIcon = LoadIcon("speaker.ico");
        _headphoneIcon = LoadIcon("headphones.ico");

        RefreshDevices();

        _trayIcon = new NotifyIcon
        {
            Visible = true,
            ContextMenuStrip = BuildMenu(),
            Icon = _speakerIcon
        };

        _trayIcon.MouseClick += TrayIcon_MouseClick;

        _hotkeyWindow = new HotkeyWindow();

        _hotkeyWindow.HotkeyPressed += async (_, _) =>
        {
            ToggleDevice();
        };

        RegisterHotKey(
            _hotkeyWindow.Handle,
            HOTKEY_ID,
            MOD_CONTROL,
            (uint)Keys.F12);

        UpdateUI();
    }

    private Icon LoadIcon(string file)
    {
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", file);
        return new Icon(path);
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();

        menu.Items.Add("Toggle Audio", null, (_, _) => ToggleDevice());
        menu.Items.Add("Refresh Devices", null, (_, _) => RefreshDevices());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitThread());

        return menu;
    }

    private void TrayIcon_MouseClick(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
            ToggleDevice();
    }

    private void RefreshDevices()
    {
        var devices = _enumerator
            .EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
            .ToList();

        _speakers = devices.FirstOrDefault(d =>
            d.FriendlyName.Contains("Speaker", StringComparison.OrdinalIgnoreCase));

        _headphones = devices.FirstOrDefault(d =>
            d.FriendlyName.Contains("Head", StringComparison.OrdinalIgnoreCase));
    }

    private MMDevice GetDefaultDevice()
    {
        return _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
    }

    private void ToggleDevice()
    {
        var current = GetDefaultDevice();

        if (_speakers == null || _headphones == null)
        {
            RefreshDevices();
            return;
        }

        if (current.ID == _speakers.ID)
            SetDevice(_headphones);
        else
            SetDevice(_speakers);

        UpdateUI();
    }

    private void SetDevice(MMDevice device)
    {
        PolicyConfigClient.SetDefaultDevice(device.ID);

        _trayIcon.ShowBalloonTip(
            1000,
            "Audio Switch",
            $"Switched to {device.FriendlyName}",
            ToolTipIcon.Info
        );
    }

    private void UpdateUI()
    {
        var current = GetDefaultDevice();

        if (_headphones != null && current.ID == _headphones.ID)
            _trayIcon.Icon = _headphoneIcon;
        else
            _trayIcon.Icon = _speakerIcon;

        _trayIcon.Text = $"Audio: {current.FriendlyName}";
    }

    protected override void ExitThreadCore()
    {
        _trayIcon.Visible = false;
        _trayIcon.Dispose();

        _speakerIcon.Dispose();
        _headphoneIcon.Dispose();

        if (_hotkeyWindow != null)
        {
            UnregisterHotKey(_hotkeyWindow.Handle, HOTKEY_ID);
            _hotkeyWindow.Dispose();
        }

        base.ExitThreadCore();
    }
}