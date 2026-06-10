using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using AudioSwitcher.AudioApi.CoreAudio;

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
    private readonly CoreAudioController _audio;

    private CoreAudioDevice? _speakers;
    private CoreAudioDevice? _headphones;

    private readonly Icon _speakerIcon;
    private readonly Icon _headphoneIcon;

    public TrayAppContext()
    {
        _audio = new CoreAudioController();

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
        var devices = _audio.GetPlaybackDevices().ToList();

        _speakers = devices.FirstOrDefault(d =>
            d.FullName.Contains("Speaker", StringComparison.OrdinalIgnoreCase));

        _headphones = devices.FirstOrDefault(d =>
            d.FullName.Contains("Head", StringComparison.OrdinalIgnoreCase));
    }

    private void ToggleDevice()
    {
        var current = _audio.DefaultPlaybackDevice;

        if (_speakers == null || _headphones == null)
        {
            RefreshDevices();
            return;
        }

        if (current?.Id == _speakers.Id)
            SetDevice(_headphones);
        else
            SetDevice(_speakers);

        UpdateUI();
    }

    private void SetDevice(CoreAudioDevice device)
    {
        device.SetAsDefault();

        _trayIcon.ShowBalloonTip(
            1000,
            "Audio Switch",
            $"Switched to {device.FullName}",
            ToolTipIcon.Info
        );
    }

    private void UpdateUI()
    {
        var current = _audio.DefaultPlaybackDevice;

        if (current == null)
            return;

        bool isHeadphones =
            _headphones != null &&
            current.Id == _headphones.Id;

        _trayIcon.Icon = isHeadphones ? _headphoneIcon : _speakerIcon;

        _trayIcon.Text = $"Audio: {current.FullName}";
    }

    protected override void ExitThreadCore()
    {
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        _audio.Dispose();

        _speakerIcon.Dispose();
        _headphoneIcon.Dispose();

        base.ExitThreadCore();
    }
}