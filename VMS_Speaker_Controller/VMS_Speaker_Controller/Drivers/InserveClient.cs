using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using VMS_Speaker_Controller.Core;

namespace VMS_Speaker_Controller.Drivers;

public class InserveClient : DeviceClientBase, IVmsClient, IPtzControllable, ICameraManageable
{
    public override string VendorName => "Inserve INSView";

    public override Task<bool> ConnectAsync()
    {
        IsConnected = true;
        EmitLog("Inserve HTTP 클라이언트 준비 완료");
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
            string rackNum = data.No;
            string sensorNum = data.SubNo.PadLeft(4, '0');
            string groupNum = string.IsNullOrEmpty(data.GroupSeq) ? "" : data.GroupSeq.PadLeft(4, '0');

            string url = string.IsNullOrEmpty(groupNum)
                ? $"http://{Host}:{Port}/request/pids/sendevent?outdoorracknum={rackNum}&pidssensornum={sensorNum}"
                : $"http://{Host}:{Port}/request/pids/sendevent?outdoorracknum={rackNum}&pidssensornum={sensorNum}&groupnum={groupNum}";

            EmitLog($"[TX Inserve CGI] {url}");

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            string auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{UserId}:{Password}"));
            request.Headers["Authorization"] = $"Basic {auth}";
            request.Method = "GET";
            request.Timeout = 3000;

            using var response = (HttpWebResponse)await request.GetResponseAsync();
            using var reader = new StreamReader(response.GetResponseStream());
            string body = await reader.ReadToEndAsync();
            EmitLog($"[RX Inserve] Code: {response.StatusCode} | Body: {body.Trim()}");
            return response.StatusCode == HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            EmitLog($"[Error Inserve] {ex.Message}");
            return false;
        }
    }

    public async Task<bool> SendPtzCommandAsync(string deviceId, string command, int speed = 5, int presetNum = 0)
    {
        try
        {
            string url = presetNum > 0
                ? $"http://{Host}:{Port}/request/ptzcontrol?edgedeviceid={deviceId}&cmd={command}&presetnum={presetNum}&speed={speed}"
                : $"http://{Host}:{Port}/request/ptzcontrol?edgedeviceid={deviceId}&cmd={command}&speed={speed}";

            EmitLog($"[TX Inserve PTZ] {url}");
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            string auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{UserId}:{Password}"));
            request.Headers["Authorization"] = $"Basic {auth}";
            request.Method = "GET";
            request.Timeout = 3000;

            using var response = (HttpWebResponse)await request.GetResponseAsync();
            using var reader = new StreamReader(response.GetResponseStream());
            string body = await reader.ReadToEndAsync();
            EmitLog($"[RX Inserve PTZ] Result: {body.Trim()}");
            return true;
        }
        catch (Exception ex)
        {
            EmitLog($"[Error Inserve PTZ] {ex.Message}");
            return false;
        }
    }

    // ICameraManageable 구현: 카메라 목록 조회 (/info/camerainfo)
    public async Task<List<CameraDeviceInfo>> SearchCamerasAsync()
    {
        var list = new List<CameraDeviceInfo>();
        string url = $"http://{Host}:{Port}/info/camerainfo";
        EmitLog($"[TX Inserve Camerainfo] {url}");

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

            if (obj["CameraInfoList"] is JArray camList)
            {
                foreach (JObject item in camList)
                {
                    string edgeId = item["EdgeDeviceID"]?.ToString() ?? "";
                    string name = item["EdgeDeviceName"]?.ToString() ?? "";
                    string addr = item["DeviceAddress"]?.ToString() ?? "";

                    string rtspUrl = "";
                    if (item["RTSPInfoList"] is JArray rtspList && rtspList.Count > 0)
                    {
                        // 서브스트림 우선 선택 (Width + Height 합이 가장 작은 것)
                        var minStream = rtspList.OrderBy(x => (x["Width"]?.Value<int>() ?? 0) + (x["Height"]?.Value<int>() ?? 0)).FirstOrDefault();
                        rtspUrl = minStream?["RtspUrl"]?.ToString() ?? "";
                    }

                    list.Add(new CameraDeviceInfo
                    {
                        Id = edgeId,
                        Name = name,
                        IpAddress = addr,
                        RtspUrl = rtspUrl
                    });
                }
            }
            EmitLog($"[RX Inserve Camerainfo] {list.Count}대 카메라 조회 완료");
        }
        catch (Exception ex)
        {
            EmitLog($"[Error Inserve Camerainfo] {ex.Message}");
        }
        return list;
    }

    // ICameraManageable 구현: 프리셋 이동 (/request/ptzcontrol?cmd=presetmov)
    public async Task<bool> SendPresetAsync(string cameraId, string presetNum)
    {
        int pNum = int.TryParse(presetNum, out var p) ? p : 1;
        return await SendPtzCommandAsync(cameraId, "presetmov", 5, pNum);
    }
}