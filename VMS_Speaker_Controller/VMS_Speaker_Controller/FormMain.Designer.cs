namespace VMS_Speaker_Controller
{
    partial class FormMain
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.SplitContainer splitContainerMain;
        private System.Windows.Forms.TabControl tabControlMain;
        private System.Windows.Forms.TabPage tabVmsAlarm;
        private System.Windows.Forms.TabPage tabCameraControl;
        private System.Windows.Forms.TabPage tabSpeaker;
        private System.Windows.Forms.RichTextBox rtbLog;
        private System.Windows.Forms.Panel pnlLogHeader;
        private System.Windows.Forms.Label lblLogTitle;
        private System.Windows.Forms.Button btnClearLog;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.splitContainerMain = new System.Windows.Forms.SplitContainer();
            this.tabControlMain = new System.Windows.Forms.TabControl();
            this.tabVmsAlarm = new System.Windows.Forms.TabPage();
            this.tabCameraControl = new System.Windows.Forms.TabPage();
            this.tabSpeaker = new System.Windows.Forms.TabPage();
            this.pnlLogHeader = new System.Windows.Forms.Panel();
            this.lblLogTitle = new System.Windows.Forms.Label();
            this.btnClearLog = new System.Windows.Forms.Button();
            this.rtbLog = new System.Windows.Forms.RichTextBox();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerMain)).BeginInit();
            this.splitContainerMain.Panel1.SuspendLayout();
            this.splitContainerMain.Panel2.SuspendLayout();
            this.splitContainerMain.SuspendLayout();
            this.tabControlMain.SuspendLayout();
            this.pnlLogHeader.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainerMain
            // 
            this.splitContainerMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerMain.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitContainerMain.Location = new System.Drawing.Point(0, 0);
            this.splitContainerMain.Name = "splitContainerMain";
            this.splitContainerMain.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainerMain.Panel1
            // 
            this.splitContainerMain.Panel1.Controls.Add(this.tabControlMain);
            // 
            // splitContainerMain.Panel2
            // 
            this.splitContainerMain.Panel2.Controls.Add(this.rtbLog);
            this.splitContainerMain.Panel2.Controls.Add(this.pnlLogHeader);
            this.splitContainerMain.Size = new System.Drawing.Size(1164, 761);
            this.splitContainerMain.SplitterDistance = 470;
            this.splitContainerMain.TabIndex = 0;
            // 
            // tabControlMain
            // 
            this.tabControlMain.Controls.Add(this.tabVmsAlarm);
            this.tabControlMain.Controls.Add(this.tabCameraControl);
            this.tabControlMain.Controls.Add(this.tabSpeaker);
            this.tabControlMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControlMain.Font = new System.Drawing.Font("맑은 고딕", 9.5F);
            this.tabControlMain.Location = new System.Drawing.Point(0, 0);
            this.tabControlMain.Name = "tabControlMain";
            this.tabControlMain.SelectedIndex = 0;
            this.tabControlMain.Size = new System.Drawing.Size(1164, 470);
            this.tabControlMain.TabIndex = 0;
            // 
            // tabVmsAlarm
            // 
            this.tabVmsAlarm.Location = new System.Drawing.Point(4, 26);
            this.tabVmsAlarm.Name = "tabVmsAlarm";
            this.tabVmsAlarm.Padding = new System.Windows.Forms.Padding(3);
            this.tabVmsAlarm.Size = new System.Drawing.Size(1156, 440);
            this.tabVmsAlarm.TabIndex = 0;
            this.tabVmsAlarm.Text = "1. VMS 경보 발령";
            this.tabVmsAlarm.UseVisualStyleBackColor = true;
            // 
            // tabCameraControl
            // 
            this.tabCameraControl.Location = new System.Drawing.Point(4, 26);
            this.tabCameraControl.Name = "tabCameraControl";
            this.tabCameraControl.Padding = new System.Windows.Forms.Padding(3);
            this.tabCameraControl.Size = new System.Drawing.Size(1156, 440);
            this.tabCameraControl.TabIndex = 1;
            this.tabCameraControl.Text = "2. 카메라 목록 및 PTZ/프리셋 제어";
            this.tabCameraControl.UseVisualStyleBackColor = true;
            // 
            // tabSpeaker
            // 
            this.tabSpeaker.Location = new System.Drawing.Point(4, 26);
            this.tabSpeaker.Name = "tabSpeaker";
            this.tabSpeaker.Padding = new System.Windows.Forms.Padding(3);
            this.tabSpeaker.Size = new System.Drawing.Size(1156, 440);
            this.tabSpeaker.TabIndex = 2;
            this.tabSpeaker.Text = "3. 원캐스트 방송 제어";
            this.tabSpeaker.UseVisualStyleBackColor = true;
            // 
            // pnlLogHeader
            // 
            this.pnlLogHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(243)))), ((int)(((byte)(246)))));
            this.pnlLogHeader.Controls.Add(this.lblLogTitle);
            this.pnlLogHeader.Controls.Add(this.btnClearLog);
            this.pnlLogHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlLogHeader.Location = new System.Drawing.Point(0, 0);
            this.pnlLogHeader.Name = "pnlLogHeader";
            this.pnlLogHeader.Size = new System.Drawing.Size(1164, 32);
            this.pnlLogHeader.TabIndex = 0;
            // 
            // lblLogTitle
            // 
            this.lblLogTitle.AutoSize = true;
            this.lblLogTitle.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblLogTitle.Location = new System.Drawing.Point(10, 7);
            this.lblLogTitle.Name = "lblLogTitle";
            this.lblLogTitle.Size = new System.Drawing.Size(191, 15);
            this.lblLogTitle.TabIndex = 0;
            this.lblLogTitle.Text = "실시간 송수신 패킷 및 통신 로그";
            // 
            // btnClearLog
            // 
            this.btnClearLog.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClearLog.Location = new System.Drawing.Point(1060, 4);
            this.btnClearLog.Name = "btnClearLog";
            this.btnClearLog.Size = new System.Drawing.Size(85, 24);
            this.btnClearLog.TabIndex = 1;
            this.btnClearLog.Text = "로그 지우기";
            this.btnClearLog.UseVisualStyleBackColor = true;
            this.btnClearLog.Click += new System.EventHandler(this.BtnClearLog_Click);
            // 
            // rtbLog
            // 
            this.rtbLog.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(24)))), ((int)(((byte)(30)))));
            this.rtbLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rtbLog.Font = new System.Drawing.Font("Consolas", 9.5F);
            this.rtbLog.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(56)))), ((int)(((byte)(189)))), ((int)(((byte)(248)))));
            this.rtbLog.Location = new System.Drawing.Point(0, 32);
            this.rtbLog.Name = "rtbLog";
            this.rtbLog.ReadOnly = true;
            this.rtbLog.Size = new System.Drawing.Size(1164, 255);
            this.rtbLog.TabIndex = 1;
            this.rtbLog.Text = "";
            // 
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1164, 761);
            this.Controls.Add(this.splitContainerMain);
            this.Font = new System.Drawing.Font("맑은 고딕", 9.5F);
            this.Name = "FormMain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "AxiCos 통합 장비 연동 테스트 프로토타입 (VMS/카메라/스피커) v3.0";
            this.splitContainerMain.Panel1.ResumeLayout(false);
            this.splitContainerMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerMain)).EndInit();
            this.splitContainerMain.ResumeLayout(false);
            this.tabControlMain.ResumeLayout(false);
            this.pnlLogHeader.ResumeLayout(false);
            this.pnlLogHeader.PerformLayout();
            this.ResumeLayout(false);
        }
    }
}