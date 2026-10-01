using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace VMS_Speaker_Controller.Core;

public enum DeviceVendorType
{
    CatisStandard,
    MStone,
    Hanwha,
    Inserve,
    Vurix,
    IDIS,
    MGICT,
    OneCast
}

public class CameraDeviceInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string RtspUrl { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public bool IsOnline { get; set; } = true;
}

public class AlarmEventData
{
    public string No { get; set; } = "1001";
    public string SubNo { get; set; } = "0001";
    public string GroupSeq { get; set; } = "1";
    public string IntName { get; set; } = "acc"; // acc, pir, input, offline
    public string EventTime { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    public string ZoneName { get; set; } = "고리1-지역1-존1";
    public string Value { get; set; } = "150";
}

public interface IDeviceClient : IDisposable
{
    string VendorName { get; }
    bool IsConnected { get; }
    Task<bool> ConnectAsync();
    void Disconnect();
    event Action<string>? LogOccurred;
}

public interface IVmsClient : IDeviceClient
{
    Task<bool> SendAlarmAsync(AlarmEventData data);
}

public interface ICameraManageable
{
    Task<List<CameraDeviceInfo>> SearchCamerasAsync();
    Task<bool> SendPresetAsync(string cameraId, string presetNum);
}

public interface IPtzControllable
{
    Task<bool> SendPtzCommandAsync(string deviceId, string command, int speed = 5, int presetNum = 0);
}

public interface ISpeakerClient : IDeviceClient
{
    Task<int> SendFileBroadcastAsync(int fileGroupId, int repeat, List<string> devNoList, List<string> groupNoList, string startChime = "Y", string endChime = "N");
    Task<int> SendTtsBroadcastAsync(string message, int repeat, List<string> devNoList, List<string> groupNoList);
    Task<bool> StopBroadcastAsync(int bcastId);
}

public abstract class DeviceClientBase : IDeviceClient
{
    public abstract string VendorName { get; }
    public virtual bool IsConnected { get; protected set; }
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 80;
    public string UserId { get; set; } = "admin";
    public string Password { get; set; } = "1234";

    public event Action<string>? LogOccurred;

    protected void EmitLog(string message)
    {
        LogOccurred?.Invoke($"[{DateTime.Now:HH:mm:ss.fff}][{VendorName}] {message}");
    }

    public abstract Task<bool> ConnectAsync();
    public abstract void Disconnect();
    public virtual void Dispose()
    {
        Disconnect();
        GC.SuppressFinalize(this);
    }
}