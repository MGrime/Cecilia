namespace CeciliaWrapper
{
    partial class FrmMainWin
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
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.lblClientVer = new System.Windows.Forms.Label();
            this.lblCeciliaVer = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::CeciliaWrapper.Properties.Resources.Big_Brand;
            this.pictureBox1.Location = new System.Drawing.Point(12, 12);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(298, 217);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // lblClientVer
            // 
            this.lblClientVer.AutoSize = true;
            this.lblClientVer.Location = new System.Drawing.Point(123, 243);
            this.lblClientVer.Name = "lblClientVer";
            this.lblClientVer.Size = new System.Drawing.Size(77, 13);
            this.lblClientVer.TabIndex = 1;
            this.lblClientVer.Text = "Client Version: ";
            // 
            // lblCeciliaVer
            // 
            this.lblCeciliaVer.AutoSize = true;
            this.lblCeciliaVer.Location = new System.Drawing.Point(121, 258);
            this.lblCeciliaVer.Name = "lblCeciliaVer";
            this.lblCeciliaVer.Size = new System.Drawing.Size(82, 13);
            this.lblCeciliaVer.TabIndex = 2;
            this.lblCeciliaVer.Text = "Cecilia Version: ";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(100, 335);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(127, 23);
            this.button1.TabIndex = 3;
            this.button1.Text = "Check for Updates";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // FrmMainWin
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.WhiteSmoke;
            this.ClientSize = new System.Drawing.Size(322, 370);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.lblCeciliaVer);
            this.Controls.Add(this.lblClientVer);
            this.Controls.Add(this.pictureBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "FrmMainWin";
            this.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.Text = "Cecilia Client for Windows";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label lblClientVer;
        private System.Windows.Forms.Label lblCeciliaVer;
        private System.Windows.Forms.Button button1;
    }
}

