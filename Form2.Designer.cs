namespace frmavfinnder
{
    partial class Form2
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form2));
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.lbldetails = new System.Windows.Forms.Label();
            this.lblmode = new System.Windows.Forms.Label();
            this.lblstatus = new System.Windows.Forms.Label();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.lblstep = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.lbldetails);
            this.groupBox1.Controls.Add(this.lblmode);
            this.groupBox1.Controls.Add(this.lblstatus);
            this.groupBox1.Controls.Add(this.progressBar1);
            this.groupBox1.Controls.Add(this.lblstep);
            this.groupBox1.Location = new System.Drawing.Point(15, 15);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(20);
            this.groupBox1.Size = new System.Drawing.Size(470, 250);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "SAFE MODE MANAGER";
            this.groupBox1.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.groupBox1.ForeColor = System.Drawing.Color.FromArgb(0, 120, 215);
            // 
            // lblstatus
            // 
            this.lblstatus.AutoSize = true;
            this.lblstatus.Location = new System.Drawing.Point(25, 35);
            this.lblstatus.Name = "lblstatus";
            this.lblstatus.Size = new System.Drawing.Size(50, 20);
            this.lblstatus.TabIndex = 0;
            this.lblstatus.Text = "";
            this.lblstatus.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.lblstatus.ForeColor = System.Drawing.Color.FromArgb(0, 120, 215);
            // 
            // lblmode
            // 
            this.lblmode.AutoSize = true;
            this.lblmode.Location = new System.Drawing.Point(25, 65);
            this.lblmode.Name = "lblmode";
            this.lblmode.Size = new System.Drawing.Size(50, 19);
            this.lblmode.TabIndex = 1;
            this.lblmode.Text = "";
            this.lblmode.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.lblmode.ForeColor = System.Drawing.Color.FromArgb(100, 100, 100);
            // 
            // lbldetails
            // 
            this.lbldetails.AutoSize = true;
            this.lbldetails.Location = new System.Drawing.Point(25, 90);
            this.lbldetails.Name = "lbldetails";
            this.lbldetails.Size = new System.Drawing.Size(50, 19);
            this.lbldetails.TabIndex = 2;
            this.lbldetails.Text = "";
            this.lbldetails.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lbldetails.ForeColor = System.Drawing.Color.FromArgb(64, 64, 64);
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(25, 130);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(420, 35);
            this.progressBar1.TabIndex = 3;
            this.progressBar1.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progressBar1.MarqueeAnimationSpeed = 30;
            // 
            // lblstep
            // 
            this.lblstep.AutoSize = true;
            this.lblstep.Location = new System.Drawing.Point(25, 175);
            this.lblstep.Name = "lblstep";
            this.lblstep.Size = new System.Drawing.Size(50, 19);
            this.lblstep.TabIndex = 4;
            this.lblstep.Text = "";
            this.lblstep.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Italic);
            this.lblstep.ForeColor = System.Drawing.Color.FromArgb(0, 120, 215);
            // 
            // Form2
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(500, 280);
            this.Controls.Add(this.groupBox1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Antivirus Uninstaller - Safe Mode";
            this.Load += new System.EventHandler(this.Form2_Load);
            this.BackColor = System.Drawing.Color.FromArgb(240, 245, 250);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label lblstatus;
        private System.Windows.Forms.Label lblmode;
        private System.Windows.Forms.Label lbldetails;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Label lblstep;
    }
}
