using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using VMS_Speaker_Controller.Core;
using VMS_Speaker_Controller.Drivers;

namespace VMS_Speaker_Controller
{
    public partial class FormMain : Form
    {
        private readonly CatisVmsClient _catisClient = new();
        private readonly MStoneClient _mstoneClient = new();
        private readonly HanwhaVmsClient _hanwhaClient = new();
        private readonly InserveClient _inserveClient = new();
        private readonly VurixClient _vurixClient = new();
        private readonly IdisClient _idisClient = new();
        private readonly MgictClient _mgictClient = new();
        private readonly OneCastClient _onecastClient = new();

        // 탭별 동적 컨트롤 필드
        private ComboBox cbVendor;
        private TextBox txtIp;
        private TextBox txtPort;
        private TextBox txtUser;
        private TextBox txtPass;
        private TextBox txtDevNo;
        private TextBox txtSubNo;
        private TextBox txtGrpNo;
        private ComboBox cbIntName;
        private TextBox txtVal;

        private ComboBox cbCamVms;
        private ListView lvCameras;
        private TextBox txtTargetCamId;
        private NumericUpDown numPtzSpeed;
        private NumericUpDown numPresetVal;

        private TextBox txtSpkIp;
        private TextBox txtSpkPort;
        private TextBox txtSpkId;
        private TextBox txtSpkPw;
        private TextBox txtFileId;
        private TextBox txtRepeat;
        private TextBox txtDevList;
        private TextBox txtGrpList;
        private TextBox txtCurrentBcastId;
        private TextBox txtTtsText;

        public FormMain()
        {
            InitializeComponent();
            InitCustomControls();
            HookLogs();
        }

        private void HookLogs()
        {
            _catisClient.LogOccurred += AppendLog;
            _mstoneClient.LogOccurred += AppendLog;
            _hanwhaClient.LogOccurred += AppendLog;
            _inserveClient.LogOccurred += AppendLog;
            _vurixClient.LogOccurred += AppendLog;
            _idisClient.LogOccurred += AppendLog;
            _mgictClient.LogOccurred += AppendLog;
            _onecastClient.LogOccurred += AppendLog;
        }

        private void AppendLog(string msg)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(AppendLog), msg);
                return;
            }
            rtbLog.AppendText(msg + Environment.NewLine);
            rtbLog.SelectionStart = rtbLog.Text.Length;
            rtbLog.ScrollToCaret();
        }

        private void BtnClearLog_Click(object sender, EventArgs e)
        {
            rtbLog.Clear();
        }

        // =========================================================================
        // 디자이너 에러를 방지하기 위해 탭 내부 세부 컨트롤을 코드로 구성
        // =========================================================================
        private void InitCustomControls()
        {
            // --- TAB 1: VMS 경보 발령 ---
            var pnlVms = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15) };
            var grpServer = new GroupBox { Text = "VMS 서버 접속 설정", Location = new Point(15, 10), Size = new Size(1110, 85) };

            var lblVendor = new Label { Text = "VMS 벤더:", Location = new Point(15, 25), AutoSize = true };
            cbVendor = new ComboBox { Location = new Point(85, 22), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            cbVendor.Items.AddRange(["카티스 표준 (HTTP)", "엠스톤 (MStone)", "한화비전 (Hanwha)", "인서브 (INSView)", "이노뎁 (VURIX)", "아이디스 (IDIS TCP)", "명광 AnyKeeper (TCP)"]);
            cbVendor.SelectedIndex = 0;
            cbVendor.SelectedIndexChanged += CbVendor_SelectedIndexChanged;

            var lblIp = new Label { Text = "IP:", Location = new Point(245, 25), AutoSize = true };
            txtIp = new TextBox { Text = "127.0.0.1", Location = new Point(275, 22), Width = 110 };

            var lblPort = new Label { Text = "포트:", Location = new Point(395, 25), AutoSize = true };
            txtPort = new TextBox { Text = "80", Location = new Point(435, 22), Width = 55 };

            var lblAuth = new Label { Text = "ID/PW:", Location = new Point(505, 25), AutoSize = true };
            txtUser = new TextBox { Text = "admin", Location = new Point(555, 22), Width = 80 };
            txtPass = new TextBox { Text = "1234", Location = new Point(640, 22), Width = 90, PasswordChar = '*' };

            var btnConnect = new Button { Text = "로그인/연결", Location = new Point(745, 21), Size = new Size(95, 27) };
            var btnDisconnect = new Button { Text = "연결 해제", Location = new Point(845, 21), Size = new Size(95, 27) };
            btnConnect.Click += BtnConnect_Click;
            btnDisconnect.Click += BtnDisconnect_Click;

            grpServer.Controls.AddRange([lblVendor, cbVendor, lblIp, txtIp, lblPort, txtPort, lblAuth, txtUser, txtPass, btnConnect, btnDisconnect]);

            var grpAlarm = new GroupBox { Text = "경보(Alarm) 이벤트 데이터 주입", Location = new Point(15, 105), Size = new Size(1110, 110) };
            var lblDevNo = new Label { Text = "주장치 번호:", Location = new Point(20, 30), AutoSize = true };
            txtDevNo = new TextBox { Text = "2001", Location = new Point(100, 27), Width = 70 };

            var lblSubNo = new Label { Text = "하위 번호:", Location = new Point(190, 30), AutoSize = true };
            txtSubNo = new TextBox { Text = "0251", Location = new Point(260, 27), Width = 70 };

            var lblGrpNo = new Label { Text = "경보그룹 번호:", Location = new Point(350, 30), AutoSize = true };
            txtGrpNo = new TextBox { Text = "1", Location = new Point(440, 27), Width = 60 };

            var lblType = new Label { Text = "경보 종류:", Location = new Point(520, 30), AutoSize = true };
            cbIntName = new ComboBox { Location = new Point(590, 27), Width = 90, DropDownStyle = ComboBoxStyle.DropDownList };
            cbIntName.Items.AddRange(["acc", "pir", "input", "offline"]);
            cbIntName.SelectedIndex = 0;

            var lblVal = new Label { Text = "감지값:", Location = new Point(700, 30), AutoSize = true };
            txtVal = new TextBox { Text = "165", Location = new Point(750, 27), Width = 50 };

            var btnSendAlarm = new Button
            {
                Text = "🚨 경보 이벤트 전송",
                Location = new Point(830, 22),
                Size = new Size(180, 68),
                BackColor = Color.FromArgb(239, 68, 68),
                ForeColor = Color.White,
                Font = new Font("맑은 고딕", 10.5f, FontStyle.Bold)
            };
            btnSendAlarm.Click += BtnSendAlarm_Click;

            grpAlarm.Controls.AddRange([lblDevNo, txtDevNo, lblSubNo, txtSubNo, lblGrpNo, txtGrpNo, lblType, cbIntName, lblVal, txtVal, btnSendAlarm]);
            pnlVms.Controls.AddRange([grpServer, grpAlarm]);
            tabVmsAlarm.Controls.Add(pnlVms);

            // --- TAB 2: 카메라 목록 및 PTZ/프리셋 제어 ---
            var pnlCam = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15) };
            var grpCamList = new GroupBox { Text = "VMS 카메라 목록 조회", Location = new Point(15, 10), Size = new Size(570, 420) };

            var lblSelectVms = new Label { Text = "대상 VMS:", Location = new Point(15, 25), AutoSize = true };
            cbCamVms = new ComboBox { Location = new Point(85, 22), Width = 140, DropDownStyle = ComboBoxStyle.DropDownList };
            cbCamVms.Items.AddRange(["한화비전 (Hanwha)", "엠스톤 (MStone)", "인서브 (INSView)", "이노뎁 (VURIX)"]);
            cbCamVms.SelectedIndex = 0;

            var btnFetchCams = new Button { Text = "🔍 카메라 목록 가져오기", Location = new Point(235, 21), Size = new Size(160, 26) };
            btnFetchCams.Click += BtnFetchCams_Click;

            lvCameras = new ListView
            {
                Location = new Point(15, 55),
                Size = new Size(540, 350),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };
            lvCameras.Columns.Add("채널/ID", 80);
            lvCameras.Columns.Add("카메라명", 150);
            lvCameras.Columns.Add("IP 주소", 100);
            lvCameras.Columns.Add("RTSP URL", 190);
            lvCameras.SelectedIndexChanged += LvCameras_SelectedIndexChanged;

            grpCamList.Controls.AddRange([lblSelectVms, cbCamVms, btnFetchCams, lvCameras]);

            var grpPtz = new GroupBox { Text = "PTZ 및 프리셋 이동 제어", Location = new Point(600, 10), Size = new Size(520, 420) };
            var lblTargetCam = new Label { Text = "선택된 카메라 ID:", Location = new Point(20, 30), AutoSize = true };
            txtTargetCamId = new TextBox { Text = "0", Location = new Point(140, 27), Width = 150 };

            var lblSpeed = new Label { Text = "속도(1~10):", Location = new Point(310, 30), AutoSize = true };
            numPtzSpeed = new NumericUpDown { Value = 5, Minimum = 1, Maximum = 10, Location = new Point(390, 28), Width = 45 };

            var pnlPad = new Panel { Location = new Point(30, 80), Size = new Size(180, 180) };
            var btnUp = new Button { Text = "▲", Location = new Point(60, 10), Size = new Size(50, 40) };
            var btnLeft = new Button { Text = "◀", Location = new Point(10, 60), Size = new Size(50, 40) };
            var btnRight = new Button { Text = "▶", Location = new Point(110, 60), Size = new Size(50, 40) };
            var btnDown = new Button { Text = "▼", Location = new Point(60, 110), Size = new Size(50, 40) };
            var btnStop = new Button { Text = "STOP", Location = new Point(60, 60), Size = new Size(50, 40) };

            btnUp.Click += (s, e) => PtzDirection_Click("up");
            btnDown.Click += (s, e) => PtzDirection_Click("down");
            btnLeft.Click += (s, e) => PtzDirection_Click("left");
            btnRight.Click += (s, e) => PtzDirection_Click("right");
            btnStop.Click += (s, e) => PtzDirection_Click("stop");
            pnlPad.Controls.AddRange([btnUp, btnLeft, btnRight, btnDown, btnStop]);

            var btnZoomIn = new Button { Text = "줌 인 (+)", Location = new Point(230, 85), Size = new Size(95, 35) };
            var btnZoomOut = new Button { Text = "줌 아웃 (-)", Location = new Point(230, 130), Size = new Size(95, 35) };
            btnZoomIn.Click += (s, e) => PtzDirection_Click("zoomin");
            btnZoomOut.Click += (s, e) => PtzDirection_Click("zoomout");

            var grpPresetBox = new GroupBox { Text = "프리셋(Preset) 이동", Location = new Point(30, 275), Size = new Size(460, 110) };
            var lblPresetNum = new Label { Text = "프리셋 번호(1~255):", Location = new Point(15, 35), AutoSize = true };
            numPresetVal = new NumericUpDown { Value = 1, Minimum = 1, Maximum = 255, Location = new Point(145, 33), Width = 60 };

            var btnCallPreset = new Button
            {
                Text = "🎯 프리셋 호출 이동 (Preset Move)",
                Location = new Point(220, 25),
                Size = new Size(220, 45),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                Font = new Font("맑은 고딕", 9.5f, FontStyle.Bold)
            };
            btnCallPreset.Click += BtnCallPreset_Click;

            grpPresetBox.Controls.AddRange([lblPresetNum, numPresetVal, btnCallPreset]);
            grpPtz.Controls.AddRange([lblTargetCam, txtTargetCamId, lblSpeed, numPtzSpeed, pnlPad, btnZoomIn, btnZoomOut, grpPresetBox]);

            pnlCam.Controls.AddRange([grpCamList, grpPtz]);
            tabCameraControl.Controls.Add(pnlCam);

            // --- TAB 3: 원캐스트 방송 제어 ---
            var pnlSpk = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15) };
            var grpSpkServer = new GroupBox { Text = "OneCast SIP 방송 서버 (SBC-7200)", Location = new Point(15, 10), Size = new Size(1110, 75) };

            var lblSpkIp = new Label { Text = "서버 IP/Port:", Location = new Point(15, 25), AutoSize = true };
            txtSpkIp = new TextBox { Text = "221.138.17.234", Location = new Point(105, 22), Width = 120 };
            txtSpkPort = new TextBox { Text = "8080", Location = new Point(230, 22), Width = 50 };

            var lblSpkAuth = new Label { Text = "Access ID/PW:", Location = new Point(310, 25), AutoSize = true };
            txtSpkId = new TextBox { Text = "sk-test", Location = new Point(410, 22), Width = 90 };
            txtSpkPw = new TextBox { Text = "sk-test#123", Location = new Point(505, 22), Width = 110 };

            grpSpkServer.Controls.AddRange([lblSpkIp, txtSpkIp, txtSpkPort, lblSpkAuth, txtSpkId, txtSpkPw]);

            var grpFileBcast = new GroupBox { Text = "음원 파일 / 비상벨 방송 (/ext/bcast/file)", Location = new Point(15, 95), Size = new Size(540, 220) };
            var lblFileId = new Label { Text = "재생 파일그룹 ID:", Location = new Point(15, 30), AutoSize = true };
            txtFileId = new TextBox { Text = "2", Location = new Point(135, 27), Width = 60 };

            var lblRepeat = new Label { Text = "반복횟수:", Location = new Point(220, 30), AutoSize = true };
            txtRepeat = new TextBox { Text = "3", Location = new Point(285, 27), Width = 40 };

            var lblDevList = new Label { Text = "단말 목록(,구분):", Location = new Point(15, 65), AutoSize = true };
            txtDevList = new TextBox { Text = "3003, 3020, 3025", Location = new Point(135, 62), Width = 200 };

            var lblGrpList = new Label { Text = "그룹 목록(,구분):", Location = new Point(15, 100), AutoSize = true };
            txtGrpList = new TextBox { Text = "9001", Location = new Point(135, 97), Width = 200 };

            var btnSendFile = new Button { Text = "📢 음원 파일 방송 시작", Location = new Point(15, 140), Size = new Size(200, 45), BackColor = Color.FromArgb(37, 99, 235), ForeColor = Color.White, Font = new Font("맑은 고딕", 9.5f, FontStyle.Bold) };
            btnSendFile.Click += BtnSendFile_Click;

            var lblBcastId = new Label { Text = "발령 방송ID:", Location = new Point(230, 150), AutoSize = true };
            txtCurrentBcastId = new TextBox { Text = "0", Location = new Point(310, 147), Width = 60, ReadOnly = true };

            var btnStopBcast = new Button { Text = "⏹ 방송 즉시 중지", Location = new Point(380, 140), Size = new Size(130, 45), BackColor = Color.FromArgb(220, 38, 38), ForeColor = Color.White, Font = new Font("맑은 고딕", 9.5f, FontStyle.Bold) };
            btnStopBcast.Click += BtnStopBcast_Click;

            grpFileBcast.Controls.AddRange([lblFileId, txtFileId, lblRepeat, txtRepeat, lblDevList, txtDevList, lblGrpList, txtGrpList, btnSendFile, lblBcastId, txtCurrentBcastId, btnStopBcast]);

            var grpTtsBcast = new GroupBox { Text = "TTS 문자 음성합성 방송 (/ext/bcast/tts)", Location = new Point(570, 95), Size = new Size(555, 220) };
            var lblTtsText = new Label { Text = "방송 내용 문구:", Location = new Point(15, 30), AutoSize = true };
            txtTtsText = new TextBox { Text = "경계선 1구역에 침입 감지 경보가 발생하였습니다. 즉시 확인 바랍니다.", Location = new Point(15, 55), Size = new Size(525, 60), Multiline = true };

            var btnSendTts = new Button { Text = "🗣️ TTS 음성 방송 시작", Location = new Point(15, 140), Size = new Size(200, 45), BackColor = Color.FromArgb(16, 185, 129), ForeColor = Color.White, Font = new Font("맑은 고딕", 9.5f, FontStyle.Bold) };
            btnSendTts.Click += BtnSendTts_Click;

            grpTtsBcast.Controls.AddRange([lblTtsText, txtTtsText, btnSendTts]);

            pnlSpk.Controls.AddRange([grpSpkServer, grpFileBcast, grpTtsBcast]);
            tabSpeaker.Controls.Add(pnlSpk);
        }

        // =========================================================================
        // 이벤트 핸들러 로직
        // =========================================================================
        private void CbVendor_SelectedIndexChanged(object sender, EventArgs e)
        {
            txtPort.Text = cbVendor.SelectedIndex switch
            {
                0 => "80",
                1 => "80",
                2 => "80",
                3 => "8080",
                4 => "8080",
                5 => "125",
                6 => "2201",
                _ => "80"
            };
        }

        private async void BtnConnect_Click(object sender, EventArgs e)
        {
            string host = txtIp.Text.Trim();
            int port = int.TryParse(txtPort.Text.Trim(), out var p) ? p : 80;
            string u = txtUser.Text.Trim();
            string pw = txtPass.Text.Trim();

            switch (cbVendor.SelectedIndex)
            {
                case 0:
                    _catisClient.Host = host; _catisClient.Port = port; _catisClient.UserId = u; _catisClient.Password = pw;
                    await _catisClient.ConnectAsync();
                    break;
                case 1:
                    _mstoneClient.Host = host; _mstoneClient.Port = port; _mstoneClient.UserId = u; _mstoneClient.Password = pw;
                    await _mstoneClient.ConnectAsync();
                    break;
                case 2:
                    _hanwhaClient.Host = host; _hanwhaClient.Port = port; _hanwhaClient.UserId = u; _hanwhaClient.Password = pw;
                    await _hanwhaClient.ConnectAsync();
                    break;
                case 3:
                    _inserveClient.Host = host; _inserveClient.Port = port; _inserveClient.UserId = u; _inserveClient.Password = pw;
                    await _inserveClient.ConnectAsync();
                    break;
                case 4:
                    _vurixClient.Host = host; _vurixClient.Port = port; _vurixClient.UserId = u; _vurixClient.Password = pw;
                    await _vurixClient.ConnectAsync();
                    break;
                case 5:
                    _idisClient.Host = host; _idisClient.Port = port;
                    await _idisClient.ConnectAsync();
                    break;
                case 6:
                    _mgictClient.Host = host; _mgictClient.Port = port;
                    await _mgictClient.ConnectAsync();
                    break;
            }
        }

        private void BtnDisconnect_Click(object sender, EventArgs e)
        {
            _idisClient.Disconnect();
            _mgictClient.Disconnect();
            _vurixClient.Disconnect();
        }

        private async void BtnSendAlarm_Click(object sender, EventArgs e)
        {
            var data = new AlarmEventData
            {
                No = txtDevNo.Text.Trim(),
                SubNo = txtSubNo.Text.Trim(),
                GroupSeq = txtGrpNo.Text.Trim(),
                IntName = cbIntName.Text.Trim(),
                Value = txtVal.Text.Trim()
            };

            switch (cbVendor.SelectedIndex)
            {
                case 0: await _catisClient.SendAlarmAsync(data); break;
                case 1: await _mstoneClient.SendAlarmAsync(data); break;
                case 2: await _hanwhaClient.SendAlarmAsync(data); break;
                case 3: await _inserveClient.SendAlarmAsync(data); break;
                case 4: await _vurixClient.SendAlarmAsync(data); break;
                case 5: await _idisClient.SendAlarmAsync(data); break;
                case 6: await _mgictClient.SendAlarmAsync(data); break;
            }
        }

        private async void BtnFetchCams_Click(object sender, EventArgs e)
        {
            string host = txtIp.Text.Trim();
            int port = int.TryParse(txtPort.Text.Trim(), out var p) ? p : 80;
            string u = txtUser.Text.Trim();
            string pw = txtPass.Text.Trim();

            ICameraManageable? client = cbCamVms.SelectedIndex switch
            {
                0 => _hanwhaClient,
                1 => _mstoneClient,
                2 => _inserveClient,
                3 => _vurixClient,
                _ => null
            };

            if (client is DeviceClientBase baseClient)
            {
                baseClient.Host = host;
                baseClient.Port = port;
                baseClient.UserId = u;
                baseClient.Password = pw;
            }

            if (client == null) return;

            lvCameras.Items.Clear();
            var cameras = await client.SearchCamerasAsync();

            foreach (var cam in cameras)
            {
                var lvi = new ListViewItem(cam.Id);
                lvi.SubItems.Add(cam.Name);
                lvi.SubItems.Add(cam.IpAddress);
                lvi.SubItems.Add(cam.RtspUrl);
                lvi.Tag = cam;
                lvCameras.Items.Add(lvi);
            }
        }

        private void LvCameras_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvCameras.SelectedItems.Count > 0)
            {
                if (lvCameras.SelectedItems[0].Tag is CameraDeviceInfo cam)
                {
                    txtTargetCamId.Text = cam.Id;
                }
            }
        }

        private async void PtzDirection_Click(string cmd)
        {
            string camId = txtTargetCamId.Text.Trim();
            int spd = (int)numPtzSpeed.Value;

            if (cbCamVms.SelectedIndex == 2)
            {
                await _inserveClient.SendPtzCommandAsync(camId, cmd, spd);
            }
            else
            {
                AppendLog($"[{cbCamVms.Text}] {cmd} PTZ 명령 송출");
            }
        }

        private async void BtnCallPreset_Click(object sender, EventArgs e)
        {
            string camId = txtTargetCamId.Text.Trim();
            string pNum = numPresetVal.Value.ToString();

            ICameraManageable? client = cbCamVms.SelectedIndex switch
            {
                0 => _hanwhaClient,
                1 => _mstoneClient,
                2 => _inserveClient,
                3 => _vurixClient,
                _ => null
            };

            if (client != null)
            {
                await client.SendPresetAsync(camId, pNum);
            }
        }

        private async void BtnSendFile_Click(object sender, EventArgs e)
        {
            _onecastClient.Host = txtSpkIp.Text.Trim();
            _onecastClient.Port = int.TryParse(txtSpkPort.Text.Trim(), out var p) ? p : 8080;
            _onecastClient.UserId = txtSpkId.Text.Trim();
            _onecastClient.Password = txtSpkPw.Text.Trim();

            int fId = int.TryParse(txtFileId.Text.Trim(), out var f) ? f : 1;
            int rep = int.TryParse(txtRepeat.Text.Trim(), out var r) ? r : 1;
            var devs = txtDevList.Text.Split([','], StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();
            var grps = txtGrpList.Text.Split([','], StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();

            int bId = await _onecastClient.SendFileBroadcastAsync(fId, rep, devs, grps);
            if (bId > 0) txtCurrentBcastId.Text = bId.ToString();
        }

        private async void BtnSendTts_Click(object sender, EventArgs e)
        {
            _onecastClient.Host = txtSpkIp.Text.Trim();
            _onecastClient.Port = int.TryParse(txtSpkPort.Text.Trim(), out var p) ? p : 8080;
            _onecastClient.UserId = txtSpkId.Text.Trim();
            _onecastClient.Password = txtSpkPw.Text.Trim();

            int rep = int.TryParse(txtRepeat.Text.Trim(), out var r) ? r : 1;
            var devs = txtDevList.Text.Split([','], StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();
            var grps = txtGrpList.Text.Split([','], StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();

            int bId = await _onecastClient.SendTtsBroadcastAsync(txtTtsText.Text.Trim(), rep, devs, grps);
            if (bId > 0) txtCurrentBcastId.Text = bId.ToString();
        }

        private async void BtnStopBcast_Click(object sender, EventArgs e)
        {
            if (int.TryParse(txtCurrentBcastId.Text.Trim(), out var bId) && bId > 0)
            {
                await _onecastClient.StopBroadcastAsync(bId);
            }
        }

        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FormMain());
        }
    }
}