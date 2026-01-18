using System;
using System.Runtime.InteropServices;

namespace Gamix.Core.Audio
{
    /// <summary>
    /// Windows のデフォルトオーディオデバイスを変更するための COM インターフェース。
    /// 公式にはドキュメント化されていないが、広く使用されている。
    /// </summary>
    [ComImport]
    [Guid("F8679F50-850A-41CF-9C72-430F290290C8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IPolicyConfig
    {
        [PreserveSig]
        int GetMixFormat(string pszDeviceName, IntPtr ppFormat);

        [PreserveSig]
        int GetDeviceFormat(string pszDeviceName, bool bDefault, IntPtr ppFormat);

        [PreserveSig]
        int ResetDeviceFormat(string pszDeviceName);

        [PreserveSig]
        int SetDeviceFormat(string pszDeviceName, IntPtr pEndpointFormat, IntPtr MixFormat);

        [PreserveSig]
        int GetProcessingPeriod(string pszDeviceName, bool bDefault, IntPtr pmftDefaultPeriod, IntPtr pmftMinimumPeriod);

        [PreserveSig]
        int SetProcessingPeriod(string pszDeviceName, IntPtr pmftPeriod);

        [PreserveSig]
        int GetShareMode(string pszDeviceName, IntPtr pMode);

        [PreserveSig]
        int SetShareMode(string pszDeviceName, IntPtr mode);

        [PreserveSig]
        int GetPropertyValue(string pszDeviceName, bool bFxStore, IntPtr key, IntPtr pv);

        [PreserveSig]
        int SetPropertyValue(string pszDeviceName, bool bFxStore, IntPtr key, IntPtr pv);

        [PreserveSig]
        int SetDefaultEndpoint(string pszDeviceName, int eRole);

        [PreserveSig]
        int SetEndpointVisibility(string pszDeviceName, bool bVisible);
    }

    /// <summary>
    /// PolicyConfigClient COM クラス。
    /// </summary>
    [ComImport]
    [Guid("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9")]
    internal class PolicyConfigClient
    {
    }

    /// <summary>
    /// デフォルトオーディオデバイスを設定するためのヘルパークラス。
    /// </summary>
    public static class DefaultAudioDeviceSwitcher
    {
        /// <summary>
        /// 指定したデバイスをデフォルトの出力デバイスとして設定します。
        /// </summary>
        /// <param name="deviceId">デバイスID。</param>
        public static void SetDefaultDevice(string deviceId)
        {
            try
            {
                var policyConfig = (IPolicyConfig)new PolicyConfigClient();
                // eRole: 0 = Console, 1 = Multimedia, 2 = Communications
                // すべてのロールに設定
                policyConfig.SetDefaultEndpoint(deviceId, 0); // Console
                policyConfig.SetDefaultEndpoint(deviceId, 1); // Multimedia
                policyConfig.SetDefaultEndpoint(deviceId, 2); // Communications
            }
            catch (Exception)
            {
                // COM エラーは無視（権限不足など）
            }
        }
    }
}
