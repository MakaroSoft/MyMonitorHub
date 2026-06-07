namespace MyMonitorHub.Agent.Setup
{
    partial class FrmMain
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmMain));
            treeView1 = new System.Windows.Forms.TreeView();
            label1 = new System.Windows.Forms.Label();
            bApply = new System.Windows.Forms.Button();
            bCancel = new System.Windows.Forms.Button();
            bOK = new System.Windows.Forms.Button();
            label7 = new System.Windows.Forms.Label();
            SuspendLayout();
            // 
            // treeView1
            // 
            treeView1.Location = new System.Drawing.Point(15, 45);
            treeView1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            treeView1.Name = "treeView1";
            treeView1.ShowLines = false;
            treeView1.Size = new System.Drawing.Size(193, 724);
            treeView1.TabIndex = 0;
            treeView1.AfterSelect += treeView1_AfterSelect;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(15, 14);
            label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(88, 25);
            label1.TabIndex = 2;
            label1.Text = "Category:";
            // 
            // bApply
            // 
            bApply.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            bApply.Location = new System.Drawing.Point(840, 803);
            bApply.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            bApply.Name = "bApply";
            bApply.Size = new System.Drawing.Size(125, 44);
            bApply.TabIndex = 7;
            bApply.Text = "Apply";
            bApply.UseVisualStyleBackColor = true;
            bApply.Click += bApply_Click;
            // 
            // bCancel
            // 
            bCancel.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            bCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            bCancel.Location = new System.Drawing.Point(705, 803);
            bCancel.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            bCancel.Name = "bCancel";
            bCancel.Size = new System.Drawing.Size(125, 44);
            bCancel.TabIndex = 6;
            bCancel.Text = "Cancel";
            bCancel.UseVisualStyleBackColor = true;
            bCancel.Click += bCancel_Click;
            // 
            // bOK
            // 
            bOK.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            bOK.Location = new System.Drawing.Point(570, 803);
            bOK.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            bOK.Name = "bOK";
            bOK.Size = new System.Drawing.Size(125, 44);
            bOK.TabIndex = 5;
            bOK.Text = "OK";
            bOK.UseVisualStyleBackColor = true;
            bOK.Click += bOK_Click;
            // 
            // label7
            // 
            label7.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            label7.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            label7.Location = new System.Drawing.Point(19, 788);
            label7.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label7.Name = "label7";
            label7.Size = new System.Drawing.Size(940, 2);
            label7.TabIndex = 8;
            // 
            // FrmMain
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(975, 869);
            Controls.Add(label7);
            Controls.Add(bApply);
            Controls.Add(bCancel);
            Controls.Add(bOK);
            Controls.Add(label1);
            Controls.Add(treeView1);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            MaximizeBox = false;
            Name = "FrmMain";
            SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            Text = "MyMonitorHub Agent Setup";
            Load += frmMain_Load;
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TreeView treeView1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button bApply;
        private System.Windows.Forms.Button bCancel;
        private System.Windows.Forms.Button bOK;
        private System.Windows.Forms.Label label7;
    }
}

