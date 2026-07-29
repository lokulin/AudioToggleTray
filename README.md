# AudioToggleTray

A lightweight Windows system tray application that toggles between two audio output devices (for example speakers and headphones) with a single click.

Built with .NET 8 WinForms and AudioSwitcher.CoreAudio.

---

# Features

* Single-click toggle between two audio output devices
* System tray icon reflects current audio mode
* Tooltip shows active device name
* Balloon notification when switching devices
* Right-click tray menu (Toggle / Refresh / Exit)
* Uses `.ico` files for stable Windows tray icons
* Minimal CPU and memory usage

---

# Requirements

* Windows 10 or Windows 11
* .NET 8 SDK
  [https://dotnet.microsoft.com/download](https://dotnet.microsoft.com/download)

---

# Setup

## Create project

```bash
dotnet new winforms -n AudioToggleTray
cd AudioToggleTray
```

---

## Install dependency

```bash
dotnet add package AudioSwitcher.AudioApi.CoreAudio
```

---

## Add icons

Create this folder structure:

AudioToggleTray/
Assets/
speaker.ico
headphones.ico

Icon requirements:

* Must be `.ico` format
* Should include multiple sizes (16x16, 32x32, 48x48, 256x256 recommended)

---

## Update project file (.csproj)

Replace with:

```xml
<Project Sdk="Microsoft.NET.Sdk.WindowsDesktop">

  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <Content Include="Assets\speaker.ico">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </Content>

    <Content Include="Assets\headphones.ico">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </Content>
  </ItemGroup>

</Project>
```

---

# Build

## Debug build

dotnet build

---

## Release build

dotnet build -c Release

---

# Run

dotnet run

Or run directly from output:

bin\Debug\net8.0-windows\AudioToggleTray.exe

---

# Publish (recommended)

Creates a standalone executable:

dotnet publish -c Release -r win-x64 --self-contained true

Output folder:

bin\Release\net8.0-windows\win-x64\publish\

---

# Add to Windows startup

## Method 1: Startup folder

1. Press Win + R
2. Enter:

shell:startup

3. Create shortcut to:

AudioToggleTray.exe

---

## Method 2: Task Scheduler

* Open Task Scheduler
* Create Task
* Trigger: At log on
* Action: Start program → path to exe
* Optional: run with highest privileges

---

# Usage

* Left-click tray icon → toggle audio device
* Right-click tray icon → menu options
* Hover tray icon → shows current device
* Balloon notification confirms switching

---

# Device detection

The app detects devices using simple name matching:

Speakers:

* "Speaker"
* "Realtek"

Headphones:

* "Head"
* "Headset"
* "USB"

If detection fails, adjust logic in RefreshDevices().

---

# Troubleshooting

## Audio does not switch

* Ensure both devices exist in Windows Sound settings
* Adjust device name matching rules

## Icons not showing

* Ensure .ico files are valid
* Ensure they are copied to output directory

## Build issues

dotnet clean
dotnet restore
dotnet build

---

# Notes

* Uses Windows Core Audio via AudioSwitcher library
* No administrator privileges required
* Runs safely in background
* Very low resource usage
* To trigger a release, tag a commit and push the tag:
   ```
   git tag v1.0.0
   git push origin v1.0.0
  ```

---

# License

## Personal / internal use
