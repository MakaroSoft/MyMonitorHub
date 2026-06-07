namespace MyMonitorHub.Agent.Setup.UserControls
{
    partial class SetupControl
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
            this.components = new System.ComponentModel.Container();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.lbFullDeviceName = new System.Windows.Forms.Label();
            this.lbAccountName = new System.Windows.Forms.Label();
            this.tbApiKey = new System.Windows.Forms.TextBox();
            this.label12 = new System.Windows.Forms.Label();
            this.tbAccountId = new System.Windows.Forms.TextBox();
            this.tbDeviceId = new System.Windows.Forms.TextBox();
            this.label15 = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.bindingSource1 = new System.Windows.Forms.BindingSource(this.components);
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.bindingSource1)).BeginInit();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.lbFullDeviceName);
            this.groupBox1.Controls.Add(this.lbAccountName);
            this.groupBox1.Controls.Add(this.tbApiKey);
            this.groupBox1.Controls.Add(this.label12);
            this.groupBox1.Controls.Add(this.tbAccountId);
            this.groupBox1.Controls.Add(this.tbDeviceId);
            this.groupBox1.Controls.Add(this.label15);
            this.groupBox1.Controls.Add(this.label14);
            this.groupBox1.Location = new System.Drawing.Point(10, 43);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(4);
            this.groupBox1.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right | System.Windows.Forms.AnchorStyles.Top;
            this.groupBox1.Size = new System.Drawing.Size(565, 162);
            this.groupBox1.TabIndex = 4;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Connection Information";
            // 
            // lbFullDeviceName
            // 
            this.lbFullDeviceName.Location = new System.Drawing.Point(286, 89);
            this.lbFullDeviceName.Name = "lbFullDeviceName";
            this.lbFullDeviceName.Size = new System.Drawing.Size(260, 17);
            this.lbFullDeviceName.TabIndex = 9;
            // 
            // lbAccountName
            // 
            this.lbAccountName.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.bindingSource1, "AccountName", true));
            this.lbAccountName.Location = new System.Drawing.Point(286, 59);
            this.lbAccountName.Name = "lbAccountName";
            this.lbAccountName.Size = new System.Drawing.Size(260, 17);
            this.lbAccountName.TabIndex = 8;
            // 
            // tbApiKey
            //
            this.tbApiKey.Location = new System.Drawing.Point(159, 118);
            this.tbApiKey.Margin = new System.Windows.Forms.Padding(4);
            this.tbApiKey.Name = "tbApiKey";
            this.tbApiKey.Size = new System.Drawing.Size(386, 22);
            this.tbApiKey.TabIndex = 5;
            this.tbApiKey.Enter += new System.EventHandler(this.tbApiKey_Enter);
            this.tbApiKey.Leave += new System.EventHandler(this.tbApiKey_Leave);
            //
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(91, 121);
            this.label12.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(38, 17);
            this.label12.TabIndex = 4;
            this.label12.Text = "API Key";
            // 
            // tbAccountId
            // 
            this.tbAccountId.Location = new System.Drawing.Point(159, 56);
            this.tbAccountId.Margin = new System.Windows.Forms.Padding(4);
            this.tbAccountId.Name = "tbAccountId";
            this.tbAccountId.Size = new System.Drawing.Size(120, 22);
            this.tbAccountId.TabIndex = 3;
            this.tbAccountId.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.tbAccountId_KeyPress);
            this.tbAccountId.Leave += new System.EventHandler(this.tbAccountId_Leave);
            // 
            // tbDeviceId
            // 
            this.tbDeviceId.Location = new System.Drawing.Point(159, 86);
            this.tbDeviceId.Margin = new System.Windows.Forms.Padding(4);
            this.tbDeviceId.Name = "tbDeviceId";
            this.tbDeviceId.Size = new System.Drawing.Size(120, 22);
            this.tbDeviceId.TabIndex = 1;
            this.tbDeviceId.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.tbDeviceId_KeyPress);
            this.tbDeviceId.Leave += new System.EventHandler(this.tbDeviceId_Leave);
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(83, 91);
            this.label15.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(68, 17);
            this.label15.TabIndex = 0;
            this.label15.Text = "Device ID";
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(75, 59);
            this.label14.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(76, 17);
            this.label14.TabIndex = 2;
            this.label14.Text = "Account ID";
            // 
            // label5
            // 
            this.label5.BackColor = System.Drawing.SystemColors.ActiveBorder;
            this.label5.Dock = System.Windows.Forms.DockStyle.Top;
            this.label5.Location = new System.Drawing.Point(0, 0);
            this.label5.Name = "label5";
            this.label5.Padding = new System.Windows.Forms.Padding(5);
            this.label5.Size = new System.Drawing.Size(590, 27);
            this.label5.TabIndex = 3;
            this.label5.Text = "Setup";
            // 
            // bindingSource1
            // 
            this.bindingSource1.DataSource = typeof(MyMonitorHub.Agent.Common.MonitorProfile);
            // 
            // SetupControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.label5);
            this.Name = "SetupControl";
            this.Size = new System.Drawing.Size(590, 470);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.bindingSource1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Label label5;
        protected System.Windows.Forms.TextBox tbApiKey;
        protected System.Windows.Forms.TextBox tbAccountId;
        protected System.Windows.Forms.TextBox tbDeviceId;
        protected System.Windows.Forms.Label lbFullDeviceName;
        protected System.Windows.Forms.Label lbAccountName;
        private System.Windows.Forms.BindingSource bindingSource1;
    }
}
