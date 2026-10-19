namespace MissionPlanner
{
    partial class Splash
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
            this.label1 = new System.Windows.Forms.Label();
            this.TXT_version = new System.Windows.Forms.Label();
            this.brandLogo = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.brandLogo)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.OliveDrab;
            this.label1.Location = new System.Drawing.Point(22, 358);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(364, 17);
            this.label1.TabIndex = 0;
            this.label1.Tag = "custom";
            this.label1.Text = "Starbound Labs | Built on ArduPilot Mission Planner";
            this.label1.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // TXT_version
            // 
            this.TXT_version.BackColor = System.Drawing.Color.Transparent;
            this.TXT_version.ForeColor = System.Drawing.Color.White;
            this.TXT_version.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.TXT_version.Location = new System.Drawing.Point(22, 288);
            this.TXT_version.Name = "TXT_version";
            this.TXT_version.Size = new System.Drawing.Size(616, 60);
            this.TXT_version.TabIndex = 1;
            this.TXT_version.Text = "Version: ";
            this.TXT_version.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // brandLogo
            // 
            this.brandLogo.BackColor = System.Drawing.Color.Transparent;
            this.brandLogo.Image = global::MissionPlanner.Properties.Resources.TD_MP;
            this.brandLogo.Location = new System.Drawing.Point(44, 100);
            this.brandLogo.Name = "brandLogo";
            this.brandLogo.Size = new System.Drawing.Size(572, 100);
            this.brandLogo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.brandLogo.TabIndex = 2;
            this.brandLogo.TabStop = false;
            // 
            // Splash
            // 
            this.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.ClientSize = new System.Drawing.Size(660, 400);
            this.ControlBox = false;
            this.Controls.Add(this.TXT_version);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.brandLogo);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = global::MissionPlanner.Properties.Resources.mpdesktop;
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(1000, 1000);
            this.MinimizeBox = false;
            this.Name = "Splash";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "StarboundPlanner";
            this.TopMost = true;
            ((System.ComponentModel.ISupportInitialize)(this.brandLogo)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label TXT_version;
        private System.Windows.Forms.PictureBox brandLogo;
    }
}