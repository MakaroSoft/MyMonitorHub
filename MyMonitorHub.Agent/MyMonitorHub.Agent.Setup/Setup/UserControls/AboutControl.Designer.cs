namespace MyMonitorHub.Agent.Setup.UserControls
{
    partial class AboutControl
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

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AboutControl));
            lbDeviceName = new System.Windows.Forms.Label();
            bindingSource1 = new System.Windows.Forms.BindingSource(components);
            lbGroupName = new System.Windows.Forms.Label();
            lbAccountName = new System.Windows.Forms.Label();
            pictureBox1 = new System.Windows.Forms.PictureBox();
            lblVersion = new System.Windows.Forms.Label();
            label6 = new System.Windows.Forms.Label();
            label10 = new System.Windows.Forms.Label();
            groupBox4 = new System.Windows.Forms.GroupBox();
            rtbLicense = new System.Windows.Forms.RichTextBox();
            label2 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)bindingSource1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            groupBox4.SuspendLayout();
            SuspendLayout();
            // 
            // lbDeviceName
            // 
            lbDeviceName.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            lbDeviceName.DataBindings.Add(new System.Windows.Forms.Binding("Text", bindingSource1, "DeviceName", true));
            lbDeviceName.Location = new System.Drawing.Point(155, 154);
            lbDeviceName.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lbDeviceName.Name = "lbDeviceName";
            lbDeviceName.Size = new System.Drawing.Size(415, 30);
            lbDeviceName.TabIndex = 30;
            lbDeviceName.Text = "Device Name";
            lbDeviceName.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // bindingSource1
            // 
            bindingSource1.DataSource = typeof(Common.MonitorProfile);
            // 
            // lbGroupName
            // 
            lbGroupName.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            lbGroupName.DataBindings.Add(new System.Windows.Forms.Binding("Text", bindingSource1, "GroupName", true));
            lbGroupName.Location = new System.Drawing.Point(155, 122);
            lbGroupName.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lbGroupName.Name = "lbGroupName";
            lbGroupName.Size = new System.Drawing.Size(415, 30);
            lbGroupName.TabIndex = 29;
            lbGroupName.Text = "Customer";
            lbGroupName.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // lbAccountName
            // 
            lbAccountName.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            lbAccountName.DataBindings.Add(new System.Windows.Forms.Binding("Text", bindingSource1, "AccountName", true));
            lbAccountName.Location = new System.Drawing.Point(155, 90);
            lbAccountName.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lbAccountName.Name = "lbAccountName";
            lbAccountName.Size = new System.Drawing.Size(415, 30);
            lbAccountName.TabIndex = 28;
            lbAccountName.Text = "Organization";
            lbAccountName.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // pictureBox1
            // 
            pictureBox1.Image = (System.Drawing.Image)resources.GetObject("pictureBox1.Image");
            pictureBox1.InitialImage = (System.Drawing.Image)resources.GetObject("pictureBox1.InitialImage");
            pictureBox1.Location = new System.Drawing.Point(20, 55);
            pictureBox1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new System.Drawing.Size(120, 135);
            pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            pictureBox1.TabIndex = 27;
            pictureBox1.TabStop = false;
            // 
            // lblVersion
            // 
            lblVersion.AutoSize = true;
            lblVersion.Location = new System.Drawing.Point(233, 192);
            lblVersion.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lblVersion.Name = "lblVersion";
            lblVersion.Size = new System.Drawing.Size(59, 25);
            lblVersion.TabIndex = 25;
            lblVersion.Text = "label2";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new System.Drawing.Point(155, 192);
            label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label6.Name = "label6";
            label6.Size = new System.Drawing.Size(74, 25);
            label6.TabIndex = 24;
            label6.Text = "Version:";
            // 
            // label10
            // 
            label10.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            label10.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            label10.Location = new System.Drawing.Point(155, 55);
            label10.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            label10.Name = "label10";
            label10.Size = new System.Drawing.Size(410, 29);
            label10.TabIndex = 23;
            label10.Text = "MyMonitorHub Monitoring Agent";
            label10.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // groupBox4
            // 
            groupBox4.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            groupBox4.Controls.Add(rtbLicense);
            groupBox4.Location = new System.Drawing.Point(10, 222);
            groupBox4.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            groupBox4.Name = "groupBox4";
            groupBox4.Padding = new System.Windows.Forms.Padding(5, 6, 5, 6);
            groupBox4.Size = new System.Drawing.Size(565, 238);
            groupBox4.TabIndex = 22;
            groupBox4.TabStop = false;
            groupBox4.Text = "License";
            // 
            // rtbLicense
            // 
            rtbLicense.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            rtbLicense.BackColor = System.Drawing.SystemColors.Window;
            rtbLicense.Location = new System.Drawing.Point(10, 36);
            rtbLicense.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            rtbLicense.Name = "rtbLicense";
            rtbLicense.ReadOnly = true;
            rtbLicense.Size = new System.Drawing.Size(545, 188);
            rtbLicense.TabIndex = 0;
            rtbLicense.TabStop = false;
            rtbLicense.Text = "";
            // 
            // label2
            // 
            label2.BackColor = System.Drawing.SystemColors.ActiveBorder;
            label2.Dock = System.Windows.Forms.DockStyle.Top;
            label2.Location = new System.Drawing.Point(0, 0);
            label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label2.Name = "label2";
            label2.Padding = new System.Windows.Forms.Padding(6, 8, 6, 8);
            label2.Size = new System.Drawing.Size(590, 42);
            label2.TabIndex = 21;
            label2.Text = "About";
            // 
            // AboutControl
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(lbDeviceName);
            Controls.Add(lbGroupName);
            Controls.Add(lbAccountName);
            Controls.Add(pictureBox1);
            Controls.Add(lblVersion);
            Controls.Add(label6);
            Controls.Add(label10);
            Controls.Add(groupBox4);
            Controls.Add(label2);
            Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            Name = "AboutControl";
            Size = new System.Drawing.Size(590, 470);
            ((System.ComponentModel.ISupportInitialize)bindingSource1).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            groupBox4.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.Label label2;
        protected System.Windows.Forms.Label lblVersion;
        protected System.Windows.Forms.RichTextBox rtbLicense;
        protected System.Windows.Forms.Label lbAccountName;
        protected System.Windows.Forms.Label lbDeviceName;
        protected System.Windows.Forms.Label lbGroupName;
        private System.Windows.Forms.BindingSource bindingSource1;
    }
}
