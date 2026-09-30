using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
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

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.Timeout = 3000;
            request.ContentType = "application/json; charset=utf-8";

            if (UseBasicAuth)
            {
                string auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{UserId}:{Password}"));
                request.Headers["Authorization"] = $"Basic {auth}";
            }

            byte[] sendBytes = Encoding.UTF8.GetBytes(postJson);
            using (Stream reqStream = await request.GetRequestStreamAsync())
            {
                await reqStream.WriteAsync(sendBytes, 0, sendBytes.Length);
            }

            using var response = (HttpWebResponse)await request.GetResponseAsync();
            using var reader = new StreamReader(response.GetResponseStream());
            string resBody = await reader.ReadToEndAsync();
            EmitLog($"[RX Response] Code: {response.StatusCode} | Body: {resBody}");
            return response.StatusCode == HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            EmitLog($"[Error] Catis VMS 전송 실패: {ex.Message}");
            return false;
        }
    }
}