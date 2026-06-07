using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using MyMonitorHub.Agent.Common;

namespace MyMonitorHub.Agent.Setup.UserControls
{
    public partial class SetupControl: UserControl
    {
        private const string MaskedPlaceholder   = "*****************";
        private const string DecryptFailedText   = "Unable to Decrypt";

        private readonly MonitorProfile _profile;
        private bool   _apiKeyEditing    = false;
        private string _apiKeyPlaceholder = "";

        public SetupControl(MonitorProfile profile)
        {
            InitializeComponent();
            _profile = profile;
            bindingSource1.DataSource = profile;
            tbAccountId.Text = profile.AccountId.ToString();
            tbDeviceId.Text  = profile.DeviceId.ToString();
            InitApiKeyDisplay();
        }

        private void InitApiKeyDisplay()
        {
            switch (_profile.ApiKeyLoadState)
            {
                case ApiKeyState.DecryptionFailed:
                    _apiKeyPlaceholder   = DecryptFailedText;
                    tbApiKey.ForeColor   = Color.DarkRed;
                    break;
                case ApiKeyState.Loaded:
                    _apiKeyPlaceholder   = MaskedPlaceholder;
                    tbApiKey.ForeColor   = SystemColors.WindowText;
                    break;
                default:
                    _apiKeyPlaceholder   = "";
                    tbApiKey.ForeColor   = SystemColors.WindowText;
                    break;
            }
            tbApiKey.Text = _apiKeyPlaceholder;
        }

        private void tbApiKey_Enter(object sender, EventArgs e)
        {
            if (_apiKeyEditing) return;
            _apiKeyEditing     = true;
            tbApiKey.ForeColor = SystemColors.WindowText;
            tbApiKey.Text      = "";
        }

        private void tbApiKey_Leave(object sender, EventArgs e)
        {
            _apiKeyEditing = false;
            if (!string.IsNullOrEmpty(tbApiKey.Text))
            {
                _profile.ApiKey    = tbApiKey.Text;
                _apiKeyPlaceholder = MaskedPlaceholder;
            }
            tbApiKey.ForeColor = _apiKeyPlaceholder == DecryptFailedText
                ? Color.DarkRed
                : SystemColors.WindowText;
            tbApiKey.Text = _apiKeyPlaceholder;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string FullDeviceName { set { lbFullDeviceName.Text = value; } }

        public void CommitEdits()
        {
            if (int.TryParse(tbAccountId.Text, out int accountId))
                _profile.AccountId = accountId;
            if (int.TryParse(tbDeviceId.Text, out int deviceId))
                _profile.DeviceId = deviceId;
        }

        private void tbAccountId_Leave(object sender, EventArgs e)
        {
            if (int.TryParse(tbAccountId.Text, out int accountId))
                _profile.AccountId = accountId;
        }

        private void tbDeviceId_Leave(object sender, EventArgs e)
        {
            if (int.TryParse(tbDeviceId.Text, out int deviceId))
                _profile.DeviceId = deviceId;
        }

        private void tbAccountId_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) &&
                (e.KeyChar != '.'))
            {
                e.Handled = true;
            }
        }

        private void tbDeviceId_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) &&
                (e.KeyChar != '.'))
            {
                e.Handled = true;
            }
        }
    }
}
