using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using VMS_Speaker_Controller.Core;

namespace VMS_Speaker_Controller.Drivers;

public class HanwhaVmsClient : DeviceClientBase, IVmsClient, ICameraManageable
{
    public override string VendorName => "Hanwha Vision";

    public override Task<bool> ConnectAsync()
    {
        IsConnected = true;
        EmitLog("한화비전 VMS/NVR 클라이언트 준비 완료");
        return Task.FromResult(true);
    }

    public override void Disconnect()
    {
        IsConnected = false;
    }

    public Task<bool> SendAlarmAsync(AlarmEventData data)
    {
        EmitLog($"[Hanwha Event] 센서 {data.No}-{data.SubNo} 알람 수신 (한화 표준 CGI 프리셋 호출 연계)");
        return Task.FromResult(true);
    }

    // 1. 카메라 목록 조회 (Digest 인증 /stw-cgi/media.cgi)
    public async Task<List<CameraDeviceInfo>> SearchCamerasAsync()
    {
        var list = new List<CameraDeviceInfo>();
        string url = $"http://{Host}:{Port}/stw-cgi/media.cgi?msubmenu=videosource&action=view";
        EmitLog($"[TX Hanwha] {url}");

        try
        {
            string body = await SendDigestRequestAsync(url);
            string[] lines = body.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

            var tempDict = new Dictionary<string, CameraDeviceInfo>();

            foreach (var line in lines)
            {
                string[] parts = line.Split('.');
                if (parts.Length < 3 || !parts[0].Equals("Channel", StringComparison.OrdinalIgnoreCase)) continue;

                string ch = parts[1];
                string[] kv = parts[2].Split('=');
                if (kv.Length < 2) continue;

                if (!tempDict.ContainsKey(ch))
                {
                    tempDict[ch] = new CameraDeviceInfo
                    {
                        Id = ch,
                        IpAddress = Host,
                        RtspUrl = $"rtsp://{UserId}:{Password}@{Host}/profile2/media.smp" // 기본 스트림
                    };
                }

                if (kv[0].Equals("Name", StringComparison.OrdinalIgnoreCase))
                {
                    tempDict[ch].Name = kv[1];
                }
                else if (kv[0].Equals("State", StringComparison.OrdinalIgnoreCase))
                {
                    tempDict[ch].IsOnline = kv[1].StartsWith("ON", StringComparison.OrdinalIgnoreCase);
                }
            }

            list.AddRange(tempDict.Values);
            EmitLog($"[RX Hanwha] 카메라 {list.Count}대 정보 획득 완료");
        }
        catch (Exception ex)
        {
            EmitLog($"[Error Hanwha Search] {ex.Message}");
        }

        return list;
    }

    // 2. 프리셋 이동 제어 (/stw-cgi/ptzcontrol.cgi)
    public async Task<bool> SendPresetAsync(string cameraId, string presetNum)
    {
        string url = $"http://{Host}:{Port}/stw-cgi/ptzcontrol.cgi?msubmenu=preset&action=control&Channel={cameraId}&Preset={presetNum}";
        EmitLog($"[TX Hanwha Preset] {url}");

        try
        {
            string body = await SendDigestRequestAsync(url);
            EmitLog($"[RX Hanwha Preset Result] {body.Trim()}");
            return true;
        }
        catch (Exception ex)
        {
            EmitLog($"[Error Hanwha Preset] {ex.Message}");
            return false;
        }
    }

    private async Task<string> SendDigestRequestAsync(string url)
    {
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
        request.Method = "GET";
        request.Timeout = 3000;

        var cache = new CredentialCache
        {
            { new Uri(url), "Digest", new NetworkCredential(UserId, Password) }
        };
        request.Credentials = cache;

        using var response = (HttpWebResponse)await request.GetResponseAsync();
        using var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }
}