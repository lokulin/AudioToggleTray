using System;
using System.Runtime.InteropServices;

internal static class PolicyConfigClient
{
    private static readonly Guid CLSID_PolicyConfigClient =
        new("870af99c-171d-4f9e-af0d-e63df40c2bc9");

    private static readonly Guid IID_IPolicyConfig =
        new("F8679F50-850A-41CF-9C72-430F290290C8");

    [ComImport]
    [Guid("870af99c-171d-4f9e-af0d-e63df40c2bc9")]
    private class PolicyConfigClientCom { }

    public static void SetDefaultDevice(string deviceId)
    {
        var policyConfig = (IPolicyConfig)new PolicyConfigClientCom();

        Marshal.ThrowExceptionForHR(
            policyConfig.SetDefaultEndpoint(deviceId, 0)
        );
        Marshal.ThrowExceptionForHR(
            policyConfig.SetDefaultEndpoint(deviceId, 1)
        );
        Marshal.ThrowExceptionForHR(
            policyConfig.SetDefaultEndpoint(deviceId, 2)
        );
    }

    [ComImport]
    [Guid("F8679F50-850A-41CF-9C72-430F290290C8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPolicyConfig
    {
        int GetMixFormat();
        int GetDeviceFormat();
        int ResetDeviceFormat();
        int SetDeviceFormat();
        int GetProcessingPeriod();
        int SetProcessingPeriod();
        int GetShareMode();
        int SetShareMode();
        int GetPropertyValue();
        int SetPropertyValue();
        int SetDefaultEndpoint([MarshalAs(UnmanagedType.LPWStr)] string deviceId, int role);
    }
}