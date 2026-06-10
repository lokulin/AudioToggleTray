using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
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

public class TrayAppContext : ApplicationContext
{
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

        base.ExitThreadCore();
    }
}