using System;
using System.ServiceProcess;
using System.Windows.Forms;
using MyMonitorHub.Agent.Common;

namespace MyMonitorHub.Agent.Setup.UserControls
{
    public partial class ServicesControl : UserControl
    {
        private readonly Timer _timer = new Timer();

        public ServicesControl(MonitorProfile profile)
        {
            InitializeComponent();
            _timer.Interval = 1000;
            _timer.Tick += timer_onTick;
            _timer.Enabled = true;
        }

        private void timer_onTick(object sender, EventArgs e)
        {
            UpdateServiceScreen();
        }

        private ServiceController GetServiceController()
        {
            var controller = new ServiceController("MyMonitorHubAgent", ".");
            return controller;
        }

        private void UpdateServiceScreen()
        {
            try
            {
                var controller = GetServiceController();
                var status = controller.Status.ToString();
                lblStatus.Text = status;
                if (controller.Status == ServiceControllerStatus.Stopped)
                {
                    bStart.Enabled = true;
                    bStop.Enabled = false;
                }
                else if (controller.Status == ServiceControllerStatus.Running)
                {
                    bStart.Enabled = false;
                    bStop.Enabled = true;
                }
                else
                {
                    bStart.Enabled = false;
                    bStop.Enabled = false;
                }
            }
            catch (InvalidOperationException)
            {
            }
        }

        private void bStart_Click(object sender, EventArgs e)
        {
            try
            {
                var controller = GetServiceController();
                controller.Start();
                UpdateServiceScreen();
            }
            catch (InvalidOperationException)
            {
            }
        }

        private void bStop_Click(object sender, EventArgs e)
        {
            try
            {
                var controller = GetServiceController();
                controller.Stop();
                UpdateServiceScreen();
            }
            catch (InvalidOperationException)
            {
            }
        }

    }
}
