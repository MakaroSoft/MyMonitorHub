#nullable enable
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;
using MyMonitorHub.Agent.Common;

namespace MyMonitorHub.Agent.Setup.UserControls
{
    public partial class AboutControl: UserControl
    {
#pragma warning disable CS0067  // event is raised by designer-wired button via OnClose()
        public event EventHandler? Close;
#pragma warning restore CS0067

        private bool _centeringVersion;

        protected virtual void OnClose() => Close?.Invoke(this, EventArgs.Empty);
        public AboutControl(MonitorProfile profile)
        {
            InitializeComponent();
            bindingSource1.DataSource = profile;

            // Keep the "Version: x.y.z" pair centered horizontally as the control resizes.
            label6.Anchor = AnchorStyles.Top;
            lblVersion.Anchor = AnchorStyles.Top;
        }

        // Runs after every layout/DPI-scaling pass, so the labels stay centered
        // even after WinForms re-positions them when the form is shown or rescaled.
        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            if (_centeringVersion) return;
            _centeringVersion = true;
            try
            {
                CenterVersionLabels();
            }
            finally
            {
                _centeringVersion = false;
            }
        }

        private void CenterVersionLabels()
        {
            const int gap = 4;
            int totalWidth = label6.Width + gap + lblVersion.Width;
            // Center the "Version: x.y.z" pair under the title label (label10).
            int center = label10.Left + label10.Width / 2;
            int startX = center - totalWidth / 2;
            if (startX < 0) startX = 0;
            label6.Left = startX;
            lblVersion.Left = startX + label6.Width + gap;
            lblVersion.Top = label6.Top;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Version
        {
            set
            {
                lblVersion.Text = value;
                CenterVersionLabels();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string License
        {
            set { rtbLicense.LoadFile(value); }
        }

    }

}
