using System;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace AudioToggleTray;

/// <summary>
/// Bridges NAudio's device-change callbacks (which can fire on a background COM thread)
/// to a single Action, so the caller can marshal back onto the UI thread.
/// </summary>
internal class DeviceNotificationClient : IMMNotificationClient
{
    private readonly Action _onChanged;

    public DeviceNotificationClient(Action onChanged)
    {
        _onChanged = onChanged;
    }

    public void OnDeviceStateChanged(string deviceId, DeviceState newState) => _onChanged();
    public void OnDeviceAdded(string pwstrDeviceId) => _onChanged();
    public void OnDeviceRemoved(string deviceId) => _onChanged();
    public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId) => _onChanged();
    public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key) { }
}
