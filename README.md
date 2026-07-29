# AudioToggleTray

A tiny Windows system tray utility that instantly switches your default audio
output device (e.g. speakers &harr; headphones) with one click or a global
hotkey.

## Features

- Lives quietly in the system tray — left-click the icon, or press **Ctrl+F12**,
  to switch between your two chosen audio devices.
- Icon changes to reflect which device is currently active.
- Pick exactly which two devices to toggle between from the tray menu
  (**Primary Device** / **Secondary Device**).
- Automatically notices when devices are plugged in, unplugged, or changed.
- Optional **Start with Windows**.
- Remembers your device choices between launches.

## Download & Install

1. Download the latest `AudioToggleTray-<version>-win-x64.zip` from the
   [Releases](../../releases) page.
2. **Right-click the downloaded zip → Properties → check "Unblock" → OK.**
   This step matters: Windows marks anything downloaded from the internet, and
   without unblocking it first you may see extraction or launch issues.
3. Extract the zip anywhere you like (e.g. `C:\Tools\AudioToggleTray`).
4. Run `AudioToggleTray.exe`.

The first time you run it, Windows SmartScreen will likely show a
"Windows protected your PC" warning, since this is an unsigned, independently
published app. Click **More info → Run anyway** to continue. This is normal
and only happens until the app builds up download reputation with Microsoft.

### Requirements

This build is **framework-dependent**, meaning it needs the
[.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0)
installed on your machine. If it isn't installed, Windows will show a prompt
with a direct download link the first time you try to run the app.

## Usage

- **Left-click** the tray icon, or press **Ctrl+F12**, to toggle between your
  two chosen devices.
- **Right-click** the tray icon for the full menu:
  - **Toggle Audio** — same as left-click.
  - **Primary Device / Secondary Device** — choose which two devices to
    switch between.
  - **Refresh Devices** — manually re-scan available audio devices.
  - **Start with Windows** — launch automatically at sign-in.
  - **Exit**

If something isn't switching correctly, check `%AppData%\AudioToggleTray\crash.log`
for details — the app logs errors there instead of failing silently.

## Building from source

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```
dotnet publish AudioToggleTray.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o publish
```

The published output is a single `AudioToggleTray.exe` — no other files
needed.

## License

Add your preferred license here (e.g. MIT).
