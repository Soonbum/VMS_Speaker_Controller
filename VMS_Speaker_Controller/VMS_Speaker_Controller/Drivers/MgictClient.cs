using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VMS_Speaker_Controller.Core;

namespace VMS_Speaker_Controller.Drivers;

public class MgictClient : DeviceClientBase, IVmsClient
{
    public override string VendorName => "MGICT AnyKeeper";
    private TcpClient? _tcpClient;
    private NetworkStream? _stream;
    private CancellationTokenSource? _aliveCts;
    private DateTime _lastAlarmSent = DateTime.MinValue;

    public override async Task<bool> ConnectAsync()
    {
        try
        {
            _tcpClient = new TcpClient();
            await _tcpClient.ConnectAsync(Host, Port);
            _stream = _tcpClient.GetStream();
            IsConnected = true;
            EmitLog($"명광 통합관제 소켓 연결 성공 ({Host}:{Port})");

            _aliveCts = new CancellationTokenSource();
            _ = Task.Run(() => AliveLoopAsync(_aliveCts.Token));
            _ = Task.Run(ReceiveLoop);
            return true;
        }
        catch (Exception ex)
        {
            EmitLog($"명광 소켓 연결 실패: {ex.Message}");
            IsConnected = false;
            return false;
        }
    }

    private async Task AliveLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested && IsConnected && _stream != null)
        {
            try
            {
                await Task.Delay(40000, token); // 40초 주기 Heartbeat
                byte[] alivePacket = new byte[200];
                alivePacket[0] = 0xFF; alivePacket[1] = 0xFF; alivePacket[2] = 0xFF; alivePacket[3] = 0xFF;
                alivePacket[4] = 0x31; // Alive Code
                alivePacket[5] = 0x31; // Constant '1'

                await _stream.WriteAsync(alivePacket, token);
                EmitLog("[TX MGICT Alive] 200 Byte Heartbeat 패킷 송신");
            }
            catch { break; }
        }
    }

    private async Task ReceiveLoop()
    {
        byte[] buf = new byte[200];
        while (IsConnected && _stream != null)
        {
            try
            {
                int read = await _stream.ReadAsync(buf);
                if (read == 0) break;
                EmitLog($"[RX MGICT Echo] 수신 바이트: {read}B | Hex: {BitConverter.ToString(buf, 0, Math.Min(read, 20))}...");
            }
            catch { break; }
        }
        Disconnect();
    }

    public override void Disconnect()
    {
        IsConnected = false;
        _aliveCts?.Cancel();
        _stream?.Close();
        _tcpClient?.Close();
        _stream = null;
        _tcpClient = null;
        EmitLog("명광 소켓 연결 해제");
    }

    public async Task<bool> SendAlarmAsync(AlarmEventData data)
    {
        if (!IsConnected || _stream == null)
        {
            EmitLog("서버 미접속으로 패킷 전송 불가");
            return false;
        }

        // 10초 이내 중복 전송 방지 로직
        if (data.IntName != "input" && (DateTime.Now - _lastAlarmSent).TotalSeconds < 10)
        {
            EmitLog($"[MGICT 중복 억제] 이전 경보 후 10초 미만으로 패킷 전송을 스킵합니다. (경과: {(DateTime.Now - _lastAlarmSent).TotalSeconds:F1}초)");
            return false;
        }

        try
        {
            byte[] packet = new byte[200];
            // 1. 헤더 (4B)
            packet[0] = 0xFF; packet[1] = 0xFF; packet[2] = 0xFF; packet[3] = 0xFF;

            // 2. 알람 구분 코드 (1B)
            byte code = data.IntName.ToLower() switch
            {
                "acc" => 0x33,     // 진동
                "pir" => 0x32,     // 적외선
                "input" => 0x34,   // 접점
                "offline" => 0x35, // 통신이상
                _ => 0x33
            };
            packet[4] = code;
            packet[5] = 0x31; // Type = '1'

            // 3. 소그룹 ID (10B, 25번 오프셋)
            byte[] groupBytes = Encoding.ASCII.GetBytes(data.GroupSeq.Trim());
            Buffer.BlockCopy(groupBytes, 0, packet, 25, Math.Min(groupBytes.Length, 10));

            // 4. 센서 ID 목록 (165B, 35번 오프셋)
            string sensorStr = $"{data.No}{data.SubNo}";
            byte[] sensorBytes = Encoding.ASCII.GetBytes(sensorStr);
            Buffer.BlockCopy(sensorBytes, 0, packet, 35, Math.Min(sensorBytes.Length, 165));

            await _stream.WriteAsync(packet);
            _lastAlarmSent = DateTime.Now;

            EmitLog($"[TX MGICT Alarm] 200B 송신 (종류: 0x{code:X2}, 그룹: {data.GroupSeq}, 센서: {sensorStr})");
            return true;
        }
        catch (Exception ex)
        {
            EmitLog($"[Error MGICT] {ex.Message}");
            return false;
        }
    }
}