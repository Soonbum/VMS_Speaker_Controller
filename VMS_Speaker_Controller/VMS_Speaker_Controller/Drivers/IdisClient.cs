using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using VMS_Speaker_Controller.Core;

namespace VMS_Speaker_Controller.Drivers;

public class IdisClient : DeviceClientBase, IVmsClient
{
    public override string VendorName => "IDIS VMS";
    private TcpClient? _tcpClient;
    private NetworkStream? _stream;
    public bool UseAlarmGroup { get; set; } = true;

    public override async Task<bool> ConnectAsync()
    {
        try
        {
            _tcpClient = new TcpClient();
            await _tcpClient.ConnectAsync(Host, Port);
            _stream = _tcpClient.GetStream();
            IsConnected = true;
            EmitLog($"IDIS 소켓 서버 연결 완료 ({Host}:{Port})");

            _ = Task.Run(ReceiveLoop);
            return true;
        }
        catch (Exception ex)
        {
            EmitLog($"IDIS 연결 실패: {ex.Message}");
            IsConnected = false;
            return false;
        }
    }

    private async Task ReceiveLoop()
    {
        byte[] buf = new byte[1024];
        while (IsConnected && _stream != null)
        {
            try
            {
                int read = await _stream.ReadAsync(buf, 0, buf.Length);
                if (read == 0) break;
                string res = Encoding.UTF8.GetString(buf, 0, read);
                EmitLog($"[RX IDIS Packet] {res.TrimEnd('\0')}");
            }
            catch { break; }
        }
        Disconnect();
    }

    public override void Disconnect()
    {
        IsConnected = false;
        _stream?.Close();
        _tcpClient?.Close();
        _stream = null;
        _tcpClient = null;
        EmitLog("IDIS 소켓 연결 종료");
    }

    public async Task<bool> SendAlarmAsync(AlarmEventData data)
    {
        if (!IsConnected || _stream == null)
        {
            EmitLog("서버 미접속 상태로 전송 불가");
            return false;
        }

        try
        {
            string payload = UseAlarmGroup
                ? data.GroupSeq.PadLeft(8, '0')
                : $"{data.No.PadLeft(4, '0')}{data.SubNo.PadLeft(4, '0')}";
            string packetStr = $"G{payload}@";

            byte[] sendBytes = Encoding.UTF8.GetBytes(packetStr);
            await _stream.WriteAsync(sendBytes, 0, sendBytes.Length);
            EmitLog($"[TX IDIS] {packetStr} (Hex: {BitConverter.ToString(sendBytes)})");
            return true;
        }
        catch (Exception ex)
        {
            EmitLog($"[Error IDIS] {ex.Message}");
            return false;
        }
    }
}