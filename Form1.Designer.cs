namespace frmavfinnder
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.txtfoundav = new System.Windows.Forms.TextBox();
            this.txtDirectory = new System.Windows.Forms.TextBox();
            this.lblavname = new System.Windows.Forms.Label();
            this.btnavcheck = new System.Windows.Forms.Button();
            this.lblavstatus = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.lblstatus = new System.Windows.Forms.Label();
            this.lblspeed = new System.Windows.Forms.Label();
            this.progressBarDownload = new System.Windows.Forms.ProgressBar();
            this.btndownload = new System.Windows.Forms.Button();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.lblos = new System.Windows.Forms.Label();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.lbAvSupport = new System.Windows.Forms.ListBox();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.SuspendLayout();
            //
            // timer1
            //
            this.timer1.Enabled = true;
            this.timer1.Interval = 1000;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            //
            // groupBox1
            //
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.txtfoundav);
            this.groupBox1.Controls.Add(this.txtDirectory);
            this.groupBox1.Controls.Add(this.lblavname);
            this.groupBox1.Controls.Add(this.btnavcheck);
            this.groupBox1.Controls.Add(this.lblavstatus);
            this.groupBox1.Location = new System.Drawing.Point(10, 10);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(15);
            this.groupBox1.Size = new System.Drawing.Size(430, 160);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "ANTIVIRUS EXISTENCE CHECK";
            this.groupBox1.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            //
            // label1
            //
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(110, 100);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(68, 19);
            this.label1.TabIndex = 5;
            this.label1.Text = "Directory:";
            this.label1.Font = new System.Drawing.Font("Segoe UI", 9F);
            //
            // txtfoundav
            //
            this.txtfoundav.Location = new System.Drawing.Point(175, 35);
            this.txtfoundav.Name = "txtfoundav";
            this.txtfoundav.ReadOnly = true;
            this.txtfoundav.Size = new System.Drawing.Size(238, 25);
            this.txtfoundav.TabIndex = 4;
            this.txtfoundav.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.txtfoundav.BackColor = System.Drawing.Color.FromArgb(245, 245, 245);
            //
            // txtDirectory
            //
            this.txtDirectory.Location = new System.Drawing.Point(175, 97);
            this.txtDirectory.Name = "txtDirectory";
            this.txtDirectory.ReadOnly = true;
            this.txtDirectory.Size = new System.Drawing.Size(238, 25);
            this.txtDirectory.TabIndex = 3;
            this.txtDirectory.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.txtDirectory.BackColor = System.Drawing.Color.FromArgb(245, 245, 245);
            //
            // lblavname
            //
            this.lblavname.AutoSize = true;
            this.lblavname.Location = new System.Drawing.Point(110, 38);
            this.lblavname.Name = "lblavname";
            this.lblavname.Size = new System.Drawing.Size(61, 19);
            this.lblavname.TabIndex = 2;
            this.lblavname.Text = "AV Name:";
            this.lblavname.Font = new System.Drawing.Font("Segoe UI", 9F);
            //
            // btnavcheck
            //
            this.btnavcheck.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnavcheck.Location = new System.Drawing.Point(20, 35);
            this.btnavcheck.Name = "btnavcheck";
            this.btnavcheck.Size = new System.Drawing.Size(85, 87);
            this.btnavcheck.TabIndex = 1;
            this.btnavcheck.Text = "CHECK";
            this.btnavcheck.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnavcheck.FlatAppearance.BorderSize = 0;
            this.btnavcheck.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(0, 100, 195);
            this.btnavcheck.BackColor = System.Drawing.Color.FromArgb(0, 120, 215);
            this.btnavcheck.ForeColor = System.Drawing.Color.White;
            this.btnavcheck.Click += new System.EventHandler(this.btnavcheck_Click_1);
            this.btnavcheck.Cursor = System.Windows.Forms.Cursors.Hand;
            //
            // lblavstatus
            //
            this.lblavstatus.AutoSize = true;
            this.lblavstatus.Location = new System.Drawing.Point(110, 65);
            this.lblavstatus.Name = "lblavstatus";
            this.lblavstatus.Size = new System.Drawing.Size(35, 19);
            this.lblavstatus.TabIndex = 0;
            this.lblavstatus.Text = "";
            this.lblavstatus.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblavstatus.ForeColor = System.Drawing.Color.FromArgb(100, 100, 100);
            //
            // groupBox2
            //
            this.groupBox2.Controls.Add(this.lblstatus);
            this.groupBox2.Controls.Add(this.lblspeed);
            this.groupBox2.Controls.Add(this.progressBarDownload);
            this.groupBox2.Controls.Add(this.btndownload);
            this.groupBox2.Location = new System.Drawing.Point(10, 180);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Padding = new System.Windows.Forms.Padding(15);
            this.groupBox2.Size = new System.Drawing.Size(430, 160);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "DOWNLOAD MANAGER";
            this.groupBox2.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            //
            // lblstatus
            //
            this.lblstatus.AutoSize = true;
            this.lblstatus.Location = new System.Drawing.Point(110, 130);
            this.lblstatus.Name = "lblstatus";
            this.lblstatus.Size = new System.Drawing.Size(153, 19);
            this.lblstatus.TabIndex = 8;
            this.lblstatus.Text = "STATUS: Waiting...";
            this.lblstatus.Font = new System.Drawing.Font("Segoe UI", 9F);
            //
            // lblspeed
            //
            this.lblspeed.AutoSize = true;
            this.lblspeed.Location = new System.Drawing.Point(110, 100);
            this.lblspeed.Name = "lblspeed";
            this.lblspeed.Size = new System.Drawing.Size(87, 19);
            this.lblspeed.TabIndex = 6;
            this.lblspeed.Text = "Speed: 0 KB/s";
            this.lblspeed.Font = new System.Drawing.Font("Segoe UI", 9F);
            //
            // progressBarDownload
            //
            this.progressBarDownload.Location = new System.Drawing.Point(110, 55);
            this.progressBarDownload.Name = "progressBarDownload";
            this.progressBarDownload.Size = new System.Drawing.Size(303, 30);
            this.progressBarDownload.TabIndex = 7;
            this.progressBarDownload.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            //
            // btndownload
            //
            this.btndownload.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btndownload.Location = new System.Drawing.Point(20, 35);
            this.btndownload.Name = "btndownload";
            this.btndownload.Size = new System.Drawing.Size(85, 87);
            this.btndownload.TabIndex = 6;
            this.btndownload.Text = "DOWNLOAD";
            this.btndownload.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btndownload.FlatAppearance.BorderSize = 0;
            this.btndownload.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(0, 100, 195);
            this.btndownload.BackColor = System.Drawing.Color.FromArgb(0, 120, 215);
            this.btndownload.ForeColor = System.Drawing.Color.White;
            this.btndownload.Click += new System.EventHandler(this.btndownload_Click);
            this.btndownload.Cursor = System.Windows.Forms.Cursors.Hand;
            //
            // groupBox3
            //
            this.groupBox3.Controls.Add(this.lblos);
            this.groupBox3.Location = new System.Drawing.Point(10, 350);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Padding = new System.Windows.Forms.Padding(15);
            this.groupBox3.Size = new System.Drawing.Size(430, 70);
            this.groupBox3.TabIndex = 2;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "OPERATING SYSTEM";
            this.groupBox3.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            //
            // lblos
            //
            this.lblos.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblos.AutoSize = true;
            this.lblos.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblos.Location = new System.Drawing.Point(20, 35);
            this.lblos.Name = "lblos";
            this.lblos.Size = new System.Drawing.Size(50, 19);
            this.lblos.TabIndex = 1;
            this.lblos.Text = "";
            this.lblos.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblos.ForeColor = System.Drawing.Color.FromArgb(0, 120, 215);
            //
            // groupBox4
            //
            this.groupBox4.Controls.Add(this.lbAvSupport);
            this.groupBox4.Location = new System.Drawing.Point(10, 430);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Padding = new System.Windows.Forms.Padding(15);
            this.groupBox4.Size = new System.Drawing.Size(430, 155);
            this.groupBox4.TabIndex = 3;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "AV SUPPORT LIST";
            this.groupBox4.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            //
            // lbAvSupport
            //
            this.lbAvSupport.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lbAvSupport.BackColor = System.Drawing.Color.FromArgb(245, 245, 245);
            this.lbAvSupport.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lbAvSupport.FormattingEnabled = true;
            this.lbAvSupport.Location = new System.Drawing.Point(20, 30);
            this.lbAvSupport.Name = "lbAvSupport";
            this.lbAvSupport.Size = new System.Drawing.Size(390, 108);
            this.lbAvSupport.TabIndex = 0;
            //
            // Form1
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(450, 595);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximumSize = new System.Drawing.Size(466, 635);
            this.MinimumSize = new System.Drawing.Size(466, 635);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "ANTI-VIRUS FINDER TOOL V1.1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.BackColor = System.Drawing.Color.FromArgb(240, 245, 250);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtfoundav;
        private System.Windows.Forms.TextBox txtDirectory;
        private System.Windows.Forms.Label lblavname;
        private System.Windows.Forms.Button btnavcheck;
        private System.Windows.Forms.Label lblavstatus;
        private System.Windows.Forms.Label lblstatus;
        private System.Windows.Forms.Label lblspeed;
        private System.Windows.Forms.ProgressBar progressBarDownload;
        private System.Windows.Forms.Button btndownload;
        private System.Windows.Forms.Label lblos;
        private System.Windows.Forms.ListBox lbAvSupport;
    }
}
