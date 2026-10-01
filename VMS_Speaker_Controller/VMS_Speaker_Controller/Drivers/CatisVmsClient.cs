using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using VMS_Speaker_Controller.Core;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace VMS_Speaker_Controller.Drivers;

public class CatisVmsClient : DeviceClientBase, IVmsClient
{
    public override string VendorName => "Catis Standard VMS";
    public bool UseBasicAuth { get; set; } = true;
    public bool SendSensorDetail { get; set; } = true;

    public override Task<bool> ConnectAsync()
    {
        IsConnected = true;
        EmitLog("Catis VMS 클라이언트 활성화됨");
        return Task.FromResult(true);
    }

    public override void Disconnect()
    {
        IsConnected = false;
        EmitLog("Catis VMS 클라이언트 연결 해제됨");
    }

    public async Task<bool> SendAlarmAsync(AlarmEventData data)
    {
        string url = $"http://{Host}:{Port}/api/events";
        try
        {
            int groupNo = int.TryParse(data.GroupSeq, out var g) ? g : 1;
            JObject packet = new()
            {
                ["type"] = 1,
                ["devices"] = new JArray { groupNo }
            };

            if (SendSensorDetail)
            {
                packet["sensor"] = new JArray { $"{data.No}{data.SubNo}" };
            }

            string postJson = packet.ToString(Newtonsoft.Json.Formatting.None);
            EmitLog($"[TX Request] URL: {url} | Body: {postJson}");

            using var client = new HttpClient { Timeout = TimeSpan.FromMilliseconds(3000) };

            using var request = new HttpRequestMessage(HttpMethod.Post, url);

            if (UseBasicAuth)
            {
                string auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{UserId}:{Password}"));
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);
            }

            request.Content = new StringContent(postJson, Encoding.UTF8, "application/json");

            using var response = await client.SendAsync(request);
            string resBody = await response.Content.ReadAsStringAsync();

            EmitLog($"[RX Response] Code: {response.StatusCode} | Body: {resBody}");
            return response.StatusCode == HttpStatusCode.OK; // 또는 response.IsSuccessStatusCode
        }
        catch (Exception ex)
        {
            EmitLog($"[Error] Catis VMS 전송 실패: {ex.Message}");
            return false;
        }
    }
}