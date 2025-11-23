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
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
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
            this.groupBox1.Location = new System.Drawing.Point(2, 3);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(451, 152);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "KIỂM TRA PHẦN MỀM ANTIVIRUS";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(98, 94);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(52, 13);
            this.label1.TabIndex = 5;
            this.label1.Text = "Directory:";
            // 
            // txtfoundav
            // 
            this.txtfoundav.Location = new System.Drawing.Point(156, 36);
            this.txtfoundav.Name = "txtfoundav";
            this.txtfoundav.ReadOnly = true;
            this.txtfoundav.Size = new System.Drawing.Size(272, 20);
            this.txtfoundav.TabIndex = 4;
            // 
            // txtDirectory
            // 
            this.txtDirectory.Location = new System.Drawing.Point(156, 91);
            this.txtDirectory.Name = "txtDirectory";
            this.txtDirectory.ReadOnly = true;
            this.txtDirectory.Size = new System.Drawing.Size(272, 20);
            this.txtDirectory.TabIndex = 3;
            // 
            // lblavname
            // 
            this.lblavname.AutoSize = true;
            this.lblavname.Location = new System.Drawing.Point(95, 39);
            this.lblavname.Name = "lblavname";
            this.lblavname.Size = new System.Drawing.Size(40, 13);
            this.lblavname.TabIndex = 2;
            this.lblavname.Text = "Found:";
            // 
            // btnavcheck
            // 
            this.btnavcheck.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold);
            this.btnavcheck.Location = new System.Drawing.Point(10, 56);
            this.btnavcheck.Name = "btnavcheck";
            this.btnavcheck.Size = new System.Drawing.Size(82, 55);
            this.btnavcheck.TabIndex = 1;
            this.btnavcheck.Text = "ANTI-VIRUS CHECK";
            this.btnavcheck.UseVisualStyleBackColor = true;
            this.btnavcheck.Click += new System.EventHandler(this.btnavcheck_Click_1);
            // 
            // lblavstatus
            // 
            this.lblavstatus.AutoSize = true;
            this.lblavstatus.Location = new System.Drawing.Point(22, 36);
            this.lblavstatus.Name = "lblavstatus";
            this.lblavstatus.Size = new System.Drawing.Size(35, 13);
            this.lblavstatus.TabIndex = 0;
            this.lblavstatus.Text = "label1";
            this.lblavstatus.Visible = false;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.lblstatus);
            this.groupBox2.Controls.Add(this.lblspeed);
            this.groupBox2.Controls.Add(this.progressBarDownload);
            this.groupBox2.Controls.Add(this.btndownload);
            this.groupBox2.Location = new System.Drawing.Point(2, 161);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(451, 152);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "DOWNLOAD MANAGER";
            // 
            // lblstatus
            // 
            this.lblstatus.AutoSize = true;
            this.lblstatus.Location = new System.Drawing.Point(22, 127);
            this.lblstatus.Name = "lblstatus";
            this.lblstatus.Size = new System.Drawing.Size(120, 13);
            this.lblstatus.TabIndex = 8;
            this.lblstatus.Text = "STATUS DOWNLOAD:";
            // 
            // lblspeed
            // 
            this.lblspeed.AutoSize = true;
            this.lblspeed.Location = new System.Drawing.Point(22, 91);
            this.lblspeed.Name = "lblspeed";
            this.lblspeed.Size = new System.Drawing.Size(46, 13);
            this.lblspeed.TabIndex = 6;
            this.lblspeed.Text = "SPEED:";
            // 
            // progressBarDownload
            // 
            this.progressBarDownload.Location = new System.Drawing.Point(105, 37);
            this.progressBarDownload.Name = "progressBarDownload";
            this.progressBarDownload.Size = new System.Drawing.Size(323, 23);
            this.progressBarDownload.TabIndex = 7;
            // 
            // btndownload
            // 
            this.btndownload.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold);
            this.btndownload.Location = new System.Drawing.Point(10, 19);
            this.btndownload.Name = "btndownload";
            this.btndownload.Size = new System.Drawing.Size(89, 55);
            this.btndownload.TabIndex = 6;
            this.btndownload.Text = "DOWNLOAD";
            this.btndownload.UseVisualStyleBackColor = true;
            this.btndownload.Click += new System.EventHandler(this.btndownload_Click);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.lblos);
            this.groupBox3.Location = new System.Drawing.Point(2, 319);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(451, 110);
            this.groupBox3.TabIndex = 2;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "OS:";
            // 
            // lblos
            // 
            this.lblos.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblos.AutoSize = true;
            this.lblos.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblos.Location = new System.Drawing.Point(31, 58);
            this.lblos.Name = "lblos";
            this.lblos.Size = new System.Drawing.Size(50, 16);
            this.lblos.TabIndex = 1;
            this.lblos.Text = "label1";
            this.lblos.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(454, 431);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(470, 470);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "ANTI-VIRUS FINDER TOOL V1.1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.GroupBox groupBox3;
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
    }
}

