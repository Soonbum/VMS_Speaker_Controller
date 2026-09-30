using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
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

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            string auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{UserId}:{Password}"));
            request.Headers["Authorization"] = $"Basic {auth}";
            request.Method = "POST";
            request.Timeout = 3000;
            request.ContentType = "application/json; charset=utf-8";

            byte[] sendBytes = Encoding.UTF8.GetBytes(postJson);
            using (Stream stream = await request.GetRequestStreamAsync())
            {
                await stream.WriteAsync(sendBytes);
            }

            using var response = (HttpWebResponse)await request.GetResponseAsync();
            using var reader = new StreamReader(response.GetResponseStream());
            string body = await reader.ReadToEndAsync();
            EmitLog($"[RX MStone] Status: {response.StatusCode} | Body: {body}");
            return response.StatusCode == HttpStatusCode.OK;
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
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            string auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{UserId}:{Password}"));
            request.Headers["Authorization"] = $"Basic {auth}";
            request.Method = "GET";
            request.Timeout = 3000;

            using var response = (HttpWebResponse)await request.GetResponseAsync();
            using var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
            string body = await reader.ReadToEndAsync();

            JObject obj = JObject.Parse(body);

            if (obj["sources"] is JArray sources)
            {
                foreach (JObject item in sources)
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

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            string auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{UserId}:{Password}"));
            request.Headers["Authorization"] = $"Basic {auth}";
            request.Method = "GET";
            request.Timeout = 3000;

            using var response = (HttpWebResponse)await request.GetResponseAsync();
            bool ok = response.StatusCode == HttpStatusCode.OK;
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