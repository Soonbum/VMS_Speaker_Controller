using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using VMS_Speaker_Controller.Core;

namespace VMS_Speaker_Controller.Drivers;

public class OneCastClient : DeviceClientBase, ISpeakerClient
{
    public override string VendorName => "OneCast SBC-7200";

    public override Task<bool> ConnectAsync()
    {
        IsConnected = true;
        EmitLog("원캐스트 방송 서버 클라이언트 준비 완료");
        return Task.FromResult(true);
    }

    public override void Disconnect()
    {
        IsConnected = false;
    }

    public async Task<int> SendFileBroadcastAsync(int fileGroupId, int repeat, List<string> devNoList, List<string> groupNoList, string startChime = "Y", string endChime = "N")
    {
        string url = $"http://{Host}:{Port}/ext/bcast/file";
        try
        {
            JObject req = new()
            {
                ["access_id"] = UserId,
                ["access_pw"] = Password,
                ["start_chime"] = startChime,
                ["end_chime"] = endChime,
                ["play_file_group_id"] = fileGroupId,
                ["repeat"] = repeat
            };

            if (devNoList != null && devNoList.Count > 0)
                req["dev_no_list"] = JArray.FromObject(devNoList);
            if (groupNoList != null && groupNoList.Count > 0)
                req["group_no_list"] = JArray.FromObject(groupNoList);

            string json = req.ToString(Newtonsoft.Json.Formatting.None);
            EmitLog($"[TX OneCast FileBcast] {json}");

            string resBody = await PostJsonAsync(url, json);
            EmitLog($"[RX OneCast] {resBody}");

            JObject resObj = JObject.Parse(resBody);
            if (resObj["res_result"]?.ToString() == "SUCCESS")
            {
                return resObj["res_bcast_id"]?.Value<int>() ?? 0;
            }
            return -1;
        }
        catch (Exception ex)
        {
            EmitLog($"[Error OneCast] {ex.Message}");
            return -1;
        }
    }

    public async Task<int> SendTtsBroadcastAsync(string message, int repeat, List<string> devNoList, List<string> groupNoList)
    {
        string url = $"http://{Host}:{Port}/ext/bcast/tts";
        try
        {
            JObject req = new()
            {
                ["access_id"] = UserId,
                ["access_pw"] = Password,
                ["start_chime"] = "Y",
                ["end_chime"] = "N",
                ["bcast_contents"] = message,
                ["repeat"] = repeat
            };

            if (devNoList != null && devNoList.Count > 0)
                req["dev_no_list"] = JArray.FromObject(devNoList);
            if (groupNoList != null && groupNoList.Count > 0)
                req["group_no_list"] = JArray.FromObject(groupNoList);

            string json = req.ToString(Newtonsoft.Json.Formatting.None);
            EmitLog($"[TX OneCast TTS] {json}");

            string resBody = await PostJsonAsync(url, json);
            EmitLog($"[RX OneCast] {resBody}");

            JObject resObj = JObject.Parse(resBody);
            if (resObj["res_result"]?.ToString() == "SUCCESS")
            {
                return resObj["res_bcast_id"]?.Value<int>() ?? 0;
            }
            return -1;
        }
        catch (Exception ex)
        {
            EmitLog($"[Error OneCast TTS] {ex.Message}");
            return -1;
        }
    }

    public async Task<bool> StopBroadcastAsync(int bcastId)
    {
        string url = $"http://{Host}:{Port}/ext/bcast/stop";
        try
        {
            JObject req = new()
            {
                ["access_id"] = UserId,
                ["access_pw"] = Password,
                ["bcast_id"] = bcastId
            };

            string json = req.ToString(Newtonsoft.Json.Formatting.None);
            EmitLog($"[TX OneCast Stop] 방송 중지 요청 (ID: {bcastId})");

            string resBody = await PostJsonAsync(url, json);
            EmitLog($"[RX OneCast Stop] {resBody}");

            JObject resObj = JObject.Parse(resBody);
            return resObj["res_result"]?.ToString() == "SUCCESS";
        }
        catch (Exception ex)
        {
            EmitLog($"[Error OneCast Stop] {ex.Message}");
            return false;
        }
    }

    private async Task<string> PostJsonAsync(string url, string json)
    {
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
        request.Method = "POST";
        request.Timeout = 4000;
        request.ContentType = "application/json; charset=utf-8";

        byte[] sendBytes = Encoding.UTF8.GetBytes(json);
        using (Stream s = await request.GetRequestStreamAsync())
        {
            await s.WriteAsync(sendBytes, 0, sendBytes.Length);
        }

        using var response = (HttpWebResponse)await request.GetResponseAsync();
        using var reader = new StreamReader(response.GetResponseStream());
        return await reader.ReadToEndAsync();
    }
}