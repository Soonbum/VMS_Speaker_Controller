using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Nodes;

var builder = WebApplication.CreateBuilder(args);

// HTTP 서버 포트 설정 (예: 8080)
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080);
});

var app = builder.Build();

Console.Title = "CATS VMS & Speaker Integration Mock Server";
PrintLog("System", "=== CATS VMS / Speaker Mock Server 구동 시작 ===");

// -------------------------------------------------------------
// 1. Catis Standard VMS Mock
// -------------------------------------------------------------
app.MapPost("/api/events", async (HttpContext context) =>
{
    using var reader = new StreamReader(context.Request.Body);
    string body = await reader.ReadToEndAsync();
    PrintLog("CatisVMS", $"[알람 수신] Body: {body}");

    return Results.Ok(new { result = "success", code = 200 });
});

// -------------------------------------------------------------
// 2. Hanwha Vision CGI Mock
// -------------------------------------------------------------
app.MapGet("/stw-cgi/media.cgi", (string? msubmenu, string? action) =>
{
    PrintLog("Hanwha", $"[카메라 조회 요청] msubmenu={msubmenu}, action={action}");

    // Hanwha INI-style 카메라 목록 응답
    string responseText =
        "Channel.0.Name=정문_PTZ_01\r\nChannel.0.State=ON\r\n" +
        "Channel.1.Name=외곽_펜스_02\r\nChannel.1.State=ON\r\n" +
        "Channel.2.Name=후문_고정_03\r\nChannel.2.State=OFF\r\n";

    return Results.Text(responseText, "text/plain");
});

app.MapGet("/stw-cgi/ptzcontrol.cgi", (string? Channel, string? Preset) =>
{
    PrintLog("Hanwha", $"[PTZ 프리셋 이동] Channel: {Channel}, Preset: {Preset}");
    return Results.Text("OK", "text/plain");
});

// -------------------------------------------------------------
// 3. Inserve INSView Mock
// -------------------------------------------------------------
app.MapGet("/request/pids/sendevent", (string? outdoorracknum, string? pidssensornum, string? groupnum) =>
{
    PrintLog("Inserve", $"[PIDS 알람 수신] Rack: {outdoorracknum}, Sensor: {pidssensornum}, Group: {groupnum}");
    return Results.Ok("OK");
});

app.MapGet("/request/ptzcontrol", (string? edgedeviceid, string? cmd, int? speed, int? presetnum) =>
{
    PrintLog("Inserve", $"[PTZ 제어] Device: {edgedeviceid}, Cmd: {cmd}, Speed: {speed}, Preset: {presetnum}");
    return Results.Ok("SUCCESS");
});

app.MapGet("/info/camerainfo", () =>
{
    PrintLog("Inserve", "[카메라 목록 조회]");
    var res = new
    {
        CameraInfoList = new[]
        {
            new
            {
                EdgeDeviceID = "INS_CAM_01",
                EdgeDeviceName = "외곽 북측 1구역",
                DeviceAddress = "192.168.1.101",
                RTSPInfoList = new[]
                {
                    new { Width = 1920, Height = 1080, RtspUrl = "rtsp://127.0.0.1:8554/live/stream1" },
                    new { Width = 640, Height = 360, RtspUrl = "rtsp://127.0.0.1:8554/live/sub_stream1" } // 자동 서브스트림 선택 검증용
                }
            },
            new
            {
                EdgeDeviceID = "INS_CAM_02",
                EdgeDeviceName = "외곽 남측 2구역",
                DeviceAddress = "192.168.1.102",
                RTSPInfoList = new[]
                {
                    new { Width = 1280, Height = 720, RtspUrl = "rtsp://127.0.0.1:8554/live/stream2" }
                }
            }
        }
    };
    return Results.Json(res);
});

// -------------------------------------------------------------
// 4. MStone VMS Mock
// -------------------------------------------------------------
app.MapGet("/api/sources", () =>
{
    PrintLog("MStone", "[카메라 목록 조회 (/api/sources)]");
    var res = new
    {
        sources = new[]
        {
            new { id = "1", name = "엠스톤 1채널 펜스", address = "192.168.1.201" },
            new { id = "2", name = "엠스톤 2채널 외곽", address = "192.168.1.202" }
        }
    };
    return Results.Json(res);
});

app.MapGet("/ptz/{callId}", (int callId, string? action, string? preset) =>
{
    PrintLog("MStone", $"[PTZ 프리셋 이동] CallId: {callId}, Action: {action}, Preset: {preset}");
    return Results.Ok();
});

// -------------------------------------------------------------
// 5. Innodep VURIX Mock
// -------------------------------------------------------------
app.MapGet("/api/login", (HttpContext ctx) =>
{
    string user = ctx.Request.Headers["x-account-id"].ToString();
    PrintLog("VURIX", $"[로그인 요청] User: {user}");

    return Results.Json(new
    {
        code = "200",
        results = new
        {
            auth_token = "VURIX_TEST_TOKEN_99999",
            api_serial = 10,
            user_serial = 1001
        }
    });
});

app.MapDelete("/api/logout", (HttpContext ctx) =>
{
    string token = ctx.Request.Headers["x-auth-token"].ToString();
    PrintLog("VURIX", $"[로그아웃 요청] Token: {token}");
    return Results.Ok();
});

app.MapGet("/api/device/list/{userSerial}/0/0", (int userSerial) =>
{
    PrintLog("VURIX", $"[장치 목록 트리 조회] UserSerial: {userSerial}");
    return Results.Json(new
    {
        code = "200",
        results = new
        {
            tree = new[]
            {
                new { dev_serial = 101, dev_name = "VURIX 센서연동 PTZ 1", dev_addr = "192.168.10.51" },
                new { dev_serial = 102, dev_name = "VURIX 고정 감시카메라 2", dev_addr = "192.168.10.52" }
            }
        }
    });
});

app.MapGet("/api/video/rtsp-url/{devSerial}/0/0", (int devSerial) =>
{
    PrintLog("VURIX", $"[RTSP URL 요청] DevSerial: {devSerial}");
    return Results.Json(new
    {
        code = "200",
        results = new
        {
            url = $"rtsp://127.0.0.1:8554/vurix_{devSerial}"
        }
    });
});

app.MapGet("/api/device/function/ptz/{devSerial}/0", (int devSerial, int? cmd, int? preset_no) =>
{
    PrintLog("VURIX", $"[PTZ 제어] DevSerial: {devSerial}, Cmd: {cmd}, Preset: {preset_no}");
    return Results.Text("OK");
});

// -------------------------------------------------------------
// 6. OneCast SBC-7200 Speaker Mock
// -------------------------------------------------------------
int bcastCounter = 100;

app.MapPost("/ext/bcast/file", async (HttpContext ctx) =>
{
    using var reader = new StreamReader(ctx.Request.Body);
    string body = await reader.ReadToEndAsync();
    int newBcastId = Interlocked.Increment(ref bcastCounter);
    PrintLog("OneCast", $"[파일 방송 시작 요청] ID 발급: {newBcastId} | Body: {body}");

    return Results.Json(new { res_result = "SUCCESS", res_bcast_id = newBcastId });
});

app.MapPost("/ext/bcast/tts", async (HttpContext ctx) =>
{
    using var reader = new StreamReader(ctx.Request.Body);
    string body = await reader.ReadToEndAsync();
    int newBcastId = Interlocked.Increment(ref bcastCounter);
    PrintLog("OneCast", $"[TTS 방송 시작 요청] ID 발급: {newBcastId} | Body: {body}");

    return Results.Json(new { res_result = "SUCCESS", res_bcast_id = newBcastId });
});

app.MapPost("/ext/bcast/stop", async (HttpContext ctx) =>
{
    using var reader = new StreamReader(ctx.Request.Body);
    string body = await reader.ReadToEndAsync();
    PrintLog("OneCast", $"[방송 중지 요청] Body: {body}");

    return Results.Json(new { res_result = "SUCCESS" });
});

// -------------------------------------------------------------
// 7. TCP 소켓 Mock 서버 (IDIS & MGICT) 구동
// -------------------------------------------------------------
_ = Task.Run(() => StartIdisTcpMock(9001));
_ = Task.Run(() => StartMgictTcpMock(9002));

// 웹 서버 구동
app.Run();


// =============================================================
// TCP 소켓 Mock 핸들러 함수
// =============================================================
static async Task StartIdisTcpMock(int port)
{
    var listener = new TcpListener(IPAddress.Any, port);
    listener.Start();
    PrintLog("IDIS TCP", $"IDIS 소켓 서버 수신 대기 중 (Port: {port})");

    while (true)
    {
        var client = await listener.AcceptTcpClientAsync();
        _ = Task.Run(async () =>
        {
            PrintLog("IDIS TCP", $"클라이언트 접속됨: {client.Client.RemoteEndPoint}");
            using var stream = client.GetStream();
            byte[] buffer = new byte[1024];

            while (true)
            {
                int read = await stream.ReadAsync(buffer);
                if (read == 0) break;

                string text = Encoding.UTF8.GetString(buffer, 0, read);
                PrintLog("IDIS TCP", $"[패킷 수신] Raw: {text.TrimEnd('\0')}");

                // 필요 시 에코/ACK 응답
                byte[] ack = Encoding.UTF8.GetBytes("ACK\0");
                await stream.WriteAsync(ack);
            }
            PrintLog("IDIS TCP", "클라이언트 접속 종료");
        });
    }
}

static async Task StartMgictTcpMock(int port)
{
    var listener = new TcpListener(IPAddress.Any, port);
    listener.Start();
    PrintLog("MGICT TCP", $"명광(MGICT) 소켓 서버 수신 대기 중 (Port: {port})");

    while (true)
    {
        var client = await listener.AcceptTcpClientAsync();
        _ = Task.Run(async () =>
        {
            PrintLog("MGICT TCP", $"클라이언트 접속됨: {client.Client.RemoteEndPoint}");
            using var stream = client.GetStream();
            byte[] buffer = new byte[200];

            while (true)
            {
                int read = await stream.ReadAsync(buffer);
                if (read == 0) break;

                // 200바이트 고정 패킷 분석
                if (buffer[0] == 0xFF && buffer[1] == 0xFF && buffer[2] == 0xFF && buffer[3] == 0xFF)
                {
                    byte code = buffer[4];
                    if (code == 0x31)
                    {
                        PrintLog("MGICT TCP", "[Heartbeat 수신] 40초 주기 0x31 Alive 패킷 확인");
                    }
                    else
                    {
                        string group = Encoding.ASCII.GetString(buffer, 25, 10).Trim('\0');
                        string sensor = Encoding.ASCII.GetString(buffer, 35, 165).Trim('\0');
                        PrintLog("MGICT TCP", $"[경보 패킷 수신] 코드: 0x{code:X2}, 그룹: {group}, 센서: {sensor}");
                    }

                    // 에코 응답 (200바이트 그대로 반환)
                    await stream.WriteAsync(buffer.AsMemory(0, read));
                }
            }
            PrintLog("MGICT TCP", "클라이언트 접속 종료");
        });
    }
}

static void PrintLog(string source, string message)
{
    var prevColor = Console.ForegroundColor;
    Console.ForegroundColor = source switch
    {
        "CatisVMS" => ConsoleColor.Cyan,
        "Hanwha" => ConsoleColor.Yellow,
        "Inserve" => ConsoleColor.Green,
        "MStone" => ConsoleColor.Magenta,
        "VURIX" => ConsoleColor.Blue,
        "OneCast" => ConsoleColor.DarkYellow,
        "IDIS TCP" => ConsoleColor.DarkCyan,
        "MGICT TCP" => ConsoleColor.DarkGreen,
        _ => ConsoleColor.White
    };
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}][{source}] {message}");
    Console.ForegroundColor = prevColor;
}