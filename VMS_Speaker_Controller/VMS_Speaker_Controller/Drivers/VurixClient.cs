using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using VMS_Speaker_Controller.Core;

namespace VMS_Speaker_Controller.Drivers;

public class VurixClient : DeviceClientBase, IVmsClient, ICameraManageable
{
    public override string VendorName => "Innodep VURIX";

    private string _authToken = "";
    private int _apiSerial = 10;
    private int _userSerial = 0;

    public override async Task<bool> ConnectAsync()
    {
        try
        {
            // 로그인: GET /api/login?force-login=true
            string url = $"http://{Host}:{Port}/api/login?force-login=true";
            EmitLog($"[Vurix Login] {url}");

            using var client = new HttpClient { Timeout = TimeSpan.FromMilliseconds(3000) };

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("x-account-id", UserId);
            request.Headers.Add("x-account-pass", Password);
            request.Headers.Add("x-account-group", "group1");
            request.Headers.Add("x-license", "licNormalClient");

            using var response = await client.SendAsync(request);
            string body = await response.Content.ReadAsStringAsync();

            JObject obj = JObject.Parse(body);
            if (obj["code"]?.ToString() == "200")
            {
                JObject results = (JObject)obj["results"]!;
                _authToken = results["auth_token"]?.ToString() ?? "";
                _apiSerial = results["api_serial"]?.Value<int>() ?? 10;
                _userSerial = results["user_serial"]?.Value<int>() ?? 0;
                IsConnected = true;
                EmitLog($"[Vurix Login 성공] AuthToken: {_authToken[..Math.Min(12, _authToken.Length)]}...");
                return true;
            }
            EmitLog($"[Vurix Login 실패] {body}");
            return false;
        }
        catch (Exception ex)
        {
            EmitLog($"[Vurix Login 오류] {ex.Message}");
            return false;
        }
    }

    public override void Disconnect()
    {
        if (IsConnected)
        {
            string token = _authToken;
            string host = Host;
            int port = Port;

            _ = Task.Run(async () =>
            {
                try
                {
                    string url = $"http://{host}:{port}/api/logout";
                    using var client = new HttpClient { Timeout = TimeSpan.FromMilliseconds(3000) };
                    using var req = new HttpRequestMessage(HttpMethod.Delete, url);
                    req.Headers.Add("x-auth-token", token);

                    using var response = await client.SendAsync(req);
                }
                catch { }
            });
        }
        IsConnected = false;
        _authToken = "";
        EmitLog("Vurix 세션 로그아웃");
    }

    public Task<bool> SendAlarmAsync(AlarmEventData data)
    {
        EmitLog($"[Vurix Event] 센서 {data.No}-{data.SubNo} 발생");
        return Task.FromResult(true);
    }

    // 1. 카메라 장비 목록 조회 (/api/device/list/{userserial}/0/0)
    public async Task<List<CameraDeviceInfo>> SearchCamerasAsync()
    {
        var list = new List<CameraDeviceInfo>();
        if (!IsConnected)
        {
            EmitLog("Vurix 서버에 먼저 로그인(연결)해야 합니다.");
            return list;
        }

        try
        {
            string url = $"http://{Host}:{Port}/api/device/list/{_userSerial}/0/0";
            EmitLog($"[TX Vurix DevList] {url}");

            string body = await SendAuthGetAsync(url);
            JObject obj = JObject.Parse(body);

            if (obj["code"]?.ToString() == "200")
            {
                JArray tree = (JArray)obj["results"]!["tree"]!;
                foreach (JObject item in tree.Cast<JObject>())
                {
                    int devSerial = item["dev_serial"]?.Value<int>() ?? 0;
                    string devName = item["dev_name"]?.ToString() ?? $"CAM_{devSerial}";
                    string devAddr = item["dev_addr"]?.ToString() ?? "";

                    string rtsp = await GetRtspUrlAsync(devSerial);

                    list.Add(new CameraDeviceInfo
                    {
                        Id = devSerial.ToString(),
                        Name = devName,
                        IpAddress = devAddr,
                        RtspUrl = rtsp
                    });
                }
                EmitLog($"[RX Vurix DevList] {list.Count}대 카메라 조회 완료");
            }
        }
        catch (Exception ex)
        {
            EmitLog($"[Error Vurix DevList] {ex.Message}");
        }

        return list;
    }

    private async Task<string> GetRtspUrlAsync(int devSerial)
    {
        try
        {
            string url = $"http://{Host}:{Port}/api/video/rtsp-url/{devSerial}/0/0";
            string body = await SendAuthGetAsync(url);
            JObject obj = JObject.Parse(body);
            if (obj["code"]?.ToString() == "200")
            {
                return obj["results"]?["url"]?.ToString() ?? "";
            }
        }
        catch { }
        return "";
    }

    // 2. 프리셋 이동 제어 (cmd=32)
    public async Task<bool> SendPresetAsync(string cameraId, string presetNum)
    {
        if (!IsConnected) return false;
        try
        {
            int devSerial = int.TryParse(cameraId, out var d) ? d : 0;
            int pNum = int.TryParse(presetNum, out var p) ? p : 1;

            string url = $"http://{Host}:{Port}/api/device/function/ptz/{devSerial}/0?cmd=32&preset_no={pNum}";
            EmitLog($"[TX Vurix Preset] {url}");

            string body = await SendAuthGetAsync(url);
            EmitLog($"[RX Vurix Preset] {body}");
            return true;
        }
        catch (Exception ex)
        {
            EmitLog($"[Error Vurix Preset] {ex.Message}");
            return false;
        }
    }

    private async Task<string> SendAuthGetAsync(string url)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromMilliseconds(4000) };
        using var request = new HttpRequestMessage(HttpMethod.Get, url);

        request.Headers.Add("x-auth-token", _authToken);
        request.Headers.Add("x-api-serial", (++_apiSerial).ToString());

        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }
}