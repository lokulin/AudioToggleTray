using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using NAudio.CoreAudioApi;

namespace AudioToggleTray;

public class TrayAppContext : ApplicationContext
{
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    private const uint MOD_CONTROL = 0x0002;
    private const int HOTKEY_ID = 1;

    private HotkeyWindow? _hotkeyWindow;
    private readonly NotifyIcon _trayIcon;
    private readonly MMDeviceEnumerator _enumerator = new();
    private DeviceNotificationClient? _notificationClient;

    private readonly ToolStripMenuItem _primaryMenu = new("Primary Device");
    private readonly ToolStripMenuItem _secondaryMenu = new("Secondary Device");
    private readonly ToolStripMenuItem _startupMenuItem;

    private System.Collections.Generic.List<MMDevice> _devices = new();
    private MMDevice? _primary;
    private MMDevice? _secondary;

    private readonly Icon _speakerIcon;
    private readonly Icon _headphoneIcon;

    public TrayAppContext()
    {
        _speakerIcon = LoadIcon("speaker.ico");
        _headphoneIcon = LoadIcon("headphones.ico");

        RefreshDevices();
        RebuildDeviceMenus();

        _startupMenuItem = new ToolStripMenuItem("Start with Windows")
        {
            CheckOnClick = true,
            Checked = StartupManager.IsEnabled()
        };
        _startupMenuItem.CheckedChanged += (_, _) => StartupManager.SetEnabled(_startupMenuItem.Checked);

        _trayIcon = new NotifyIcon
        {
            Visible = true,
            ContextMenuStrip = BuildMenu(),
            Icon = _speakerIcon
        };

        _trayIcon.MouseClick += TrayIcon_MouseClick;

        _hotkeyWindow = new HotkeyWindow();
        _hotkeyWindow.HotkeyPressed += (_, _) => ToggleDevice();
        _hotkeyWindow.DevicesChanged += (_, _) =>
        {
            RefreshDevices();
            RebuildDeviceMenus();
            UpdateUI();
        };

        if (!RegisterHotKey(_hotkeyWindow.Handle, HOTKEY_ID, MOD_CONTROL, (uint)Keys.F12))
        {
            _trayIcon.ShowBalloonTip(
                3000,
                "AudioToggleTray",
                "Couldn't register the Ctrl+F12 hotkey - it may already be in use by another application.",
                ToolTipIcon.Warning);
        }

        _notificationClient = new DeviceNotificationClient(() =>
            PostMessage(_hotkeyWindow.Handle, HotkeyWindow.WM_DEVICES_CHANGED, IntPtr.Zero, IntPtr.Zero));
        _enumerator.RegisterEndpointNotificationCallback(_notificationClient);

        UpdateUI();
    }

    private Icon LoadIcon(string file)
    {
        using var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(file)
            ?? throw new FileNotFoundException($"Embedded resource '{file}' not found.");
        return new Icon(stream);
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();

        menu.Items.Add("Toggle Audio", null, (_, _) => ToggleDevice());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_primaryMenu);
        menu.Items.Add(_secondaryMenu);
        menu.Items.Add("Refresh Devices", null, (_, _) =>
        {
            RefreshDevices();
            RebuildDeviceMenus();
            UpdateUI();
        });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_startupMenuItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitThread());

        return menu;
    }

    private void RebuildDeviceMenus()
    {
        _primaryMenu.DropDownItems.Clear();
        _secondaryMenu.DropDownItems.Clear();

        foreach (var device in _devices)
        {
            var deviceId = device.ID;

            var primaryItem = new ToolStripMenuItem(device.FriendlyName) { Checked = _primary?.ID == deviceId };
            primaryItem.Click += (_, _) =>
            {
                _primary = _devices.FirstOrDefault(d => d.ID == deviceId);
                SaveDeviceSelection();
                RebuildDeviceMenus();
                UpdateUI();
            };
            _primaryMenu.DropDownItems.Add(primaryItem);

            var secondaryItem = new ToolStripMenuItem(device.FriendlyName) { Checked = _secondary?.ID == deviceId };
            secondaryItem.Click += (_, _) =>
            {
                _secondary = _devices.FirstOrDefault(d => d.ID == deviceId);
                SaveDeviceSelection();
                RebuildDeviceMenus();
                UpdateUI();
            };
            _secondaryMenu.DropDownItems.Add(secondaryItem);
        }

        if (_devices.Count == 0)
        {
            _primaryMenu.DropDownItems.Add(new ToolStripMenuItem("(no devices found)") { Enabled = false });
            _secondaryMenu.DropDownItems.Add(new ToolStripMenuItem("(no devices found)") { Enabled = false });
        }
    }

    private void TrayIcon_MouseClick(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
            ToggleDevice();
    }

    private void RefreshDevices()
    {
        try
        {
            _devices = _enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active).ToList();
        }
        catch (Exception ex)
        {
            Program.LogCrash(ex);
            _devices = new System.Collections.Generic.List<MMDevice>();
            return;
        }

        var settings = SettingsStore.Load();

        _primary = _devices.FirstOrDefault(d => d.ID == settings.PrimaryDeviceId);
        _secondary = _devices.FirstOrDefault(d => d.ID == settings.SecondaryDeviceId);

        // First run, or a previously-selected device is no longer present: fall back to a
        // reasonable guess by name so the app is usable out of the box. The user can always
        // override this from the Primary/Secondary Device menus.
        _primary ??= _devices.FirstOrDefault(d => d.FriendlyName.Contains("Speaker", StringComparison.OrdinalIgnoreCase));
        _secondary ??= _devices.FirstOrDefault(d => d.FriendlyName.Contains("Head", StringComparison.OrdinalIgnoreCase));

        SaveDeviceSelection();
    }

    private void SaveDeviceSelection()
    {
        SettingsStore.Save(new AppSettings
        {
            PrimaryDeviceId = _primary?.ID,
            SecondaryDeviceId = _secondary?.ID
        });
    }

    private MMDevice GetDefaultDevice()
    {
        return _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
    }

    private void ToggleDevice()
    {
        try
        {
            if (_primary == null || _secondary == null)
            {
                RefreshDevices();
                RebuildDeviceMenus();
            }

            if (_primary == null || _secondary == null)
            {
                _trayIcon.ShowBalloonTip(
                    3000,
                    "AudioToggleTray",
                    "Couldn't find two audio devices to toggle between. Pick them from the tray menu.",
                    ToolTipIcon.Warning);
                return;
            }

            var current = GetDefaultDevice();
            var target = current.ID == _primary.ID ? _secondary : _primary;

            SetDevice(target);
            UpdateUI();
        }
        catch (Exception ex)
        {
            Program.LogCrash(ex);
            _trayIcon.ShowBalloonTip(
                3000,
                "AudioToggleTray",
                "Failed to switch the audio device. Details were written to crash.log.",
                ToolTipIcon.Error);
        }
    }

    private void SetDevice(MMDevice device)
    {
        PolicyConfigClient.SetDefaultDevice(device.ID);

        _trayIcon.ShowBalloonTip(1000, "Audio Switch", $"Switched to {device.FriendlyName}", ToolTipIcon.Info);
    }

    private void UpdateUI()
    {
        MMDevice current;

        try
        {
            current = GetDefaultDevice();
        }
        catch (Exception ex)
        {
            Program.LogCrash(ex);
            return;
        }

        _trayIcon.Icon = _secondary != null && current.ID == _secondary.ID ? _headphoneIcon : _speakerIcon;
        _trayIcon.Text = Truncate($"Audio: {current.FriendlyName}", 63);
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..(maxLength - 1)] + "\u2026";

    protected override void ExitThreadCore()
    {
        _trayIcon.Visible = false;
        _trayIcon.Dispose();

        _speakerIcon.Dispose();
        _headphoneIcon.Dispose();

        if (_notificationClient != null)
            _enumerator.UnregisterEndpointNotificationCallback(_notificationClient);

        _enumerator.Dispose();

        if (_hotkeyWindow != null)
        {
            UnregisterHotKey(_hotkeyWindow.Handle, HOTKEY_ID);
            _hotkeyWindow.Dispose();
        }

        base.ExitThreadCore();
    }
}
