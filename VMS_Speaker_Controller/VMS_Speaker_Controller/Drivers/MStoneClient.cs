using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using VMS_Speaker_Controller.Core;

namespace VMS_Speaker_Controller.Drivers;

public class MStoneClient : DeviceClientBase, IVmsClient, ICameraManageable
{
    public override string VendorName => "MStone";

    public override Task<bool> ConnectAsync()
    {
        IsConnected = true;
        EmitLog("엠스톤 HTTP 클라이언트 준비 완료");
        return Task.FromResult(true);
    }

    public override void Disconnect()
    {
        IsConnected = false;
    }

    public async Task<bool> SendAlarmAsync(AlarmEventData data)
    {
        try
        {
            int groupNo = int.TryParse(data.GroupSeq, out var g) ? g : 1;
            int v = groupNo / 128;
            int u = groupNo % 128;
            int calcPort = v * 1000 + 80;
            int targetDevice = u <= 0 ? 128 : u;

            string url = $"http://{Host}:{calcPort}/api/events";

            JObject payload = new()
            {
                ["type"] = 1,
                ["devices"] = new JArray { targetDevice - 1 }
            };

            string postJson = payload.ToString(Newtonsoft.Json.Formatting.None);
            EmitLog($"[TX MStone] URL: {url} (그룹:{groupNo} -> 가변포트:{calcPort}, 디바이스인덱스:{targetDevice - 1}) | Data: {postJson}");

            using var client = new HttpClient { Timeout = TimeSpan.FromMilliseconds(3000) };

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            string auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{UserId}:{Password}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);

            request.Content = new StringContent(postJson, Encoding.UTF8, "application/json");

            using var response = await client.SendAsync(request);
            string body = await response.Content.ReadAsStringAsync();

            EmitLog($"[RX MStone] Status: {response.StatusCode} | Body: {body}");
            return response.IsSuccessStatusCode; // 또는 response.StatusCode == HttpStatusCode.OK
        }
        catch (Exception ex)
        {
            EmitLog($"[Error MStone] {ex.Message}");
            return false;
        }
    }

    // ICameraManageable 구현: 카메라 목록 조회 (/api/sources)
    public async Task<List<CameraDeviceInfo>> SearchCamerasAsync()
    {
        var list = new List<CameraDeviceInfo>();
        string url = $"http://{Host}:{Port}/api/sources";
        EmitLog($"[TX MStone Sources] {url}");

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromMilliseconds(3000) };

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            string auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{UserId}:{Password}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);

            using var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            string body = await response.Content.ReadAsStringAsync();
            JObject obj = JObject.Parse(body);

            if (obj["sources"] is JArray sources)
            {
                foreach (JObject item in sources.Cast<JObject>())
                {
                    string id = item["id"]?.ToString() ?? "";
                    string name = item["name"]?.ToString() ?? "";
                    string addr = item["address"]?.ToString() ?? "";

                    list.Add(new CameraDeviceInfo
                    {
                        Id = id,
                        Name = $"{name} ({addr})",
                        IpAddress = addr,
                        RtspUrl = $"rtsp://{UserId}:{Password}@{Host}/video{id}"
                    });
                }
            }
            EmitLog($"[RX MStone Sources] 카메라 {list.Count}대 조회 완료");
        }
        catch (Exception ex)
        {
            EmitLog($"[Error MStone Sources] {ex.Message}");
        }
        return list;
    }

    // ICameraManageable 구현: 프리셋 이동 (/ptz/{callid}?action=preset-move&preset={preset})
    public async Task<bool> SendPresetAsync(string cameraId, string presetNum)
    {
        try
        {
            int callId = (int.TryParse(cameraId, out var id) ? id : 1) - 1; // 0-based
            string url = $"http://{Host}:{Port}/ptz/{callId}?action=preset-move&preset={presetNum}";
            EmitLog($"[TX MStone Preset] {url}");

            using var client = new HttpClient { Timeout = TimeSpan.FromMilliseconds(3000) };

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            string auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{UserId}:{Password}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);

            using var response = await client.SendAsync(request);
            bool ok = response.StatusCode == HttpStatusCode.OK; // 또는 response.IsSuccessStatusCode
            EmitLog($"[RX MStone Preset Result] Status: {response.StatusCode}");
            return ok;
        }
        catch (Exception ex)
        {
            EmitLog($"[Error MStone Preset] {ex.Message}");
            return false;
        }
    }
}