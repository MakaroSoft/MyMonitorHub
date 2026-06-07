using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using MyMonitorHub.Agent.Common;
using MyMonitorHub.Agent.Setup.UserControls;

namespace MyMonitorHub.Agent.Setup
{
    public partial class FrmMain : Form
    {

        private TreeNode _setup;
        private readonly string _licenseFile = Application.StartupPath + "/license.rtf";
        private readonly string _versionFile = Application.StartupPath + "/version.txt";

        private MonitorProfile _profile;

        private UserControl _lastPanel;

        private AboutControl _aboutControl;
        private ServicesControl _servicesControl;
        private SetupControl _setupControl;

        public FrmMain()
        {
            InitializeComponent();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            // read in monitor.json
            _profile = MonitorProfile.Current;

            const AnchorStyles panelAnchor = AnchorStyles.Top | AnchorStyles.Bottom
                                                                | AnchorStyles.Left | AnchorStyles.Right;

            int panelLeft   = treeView1.Left + treeView1.Width + 10;
            int panelTop    = treeView1.Top;
            int panelWidth  = ClientSize.Width - panelLeft - 8;
            int panelHeight = label7.Top - treeView1.Top - 4;

            _aboutControl = new AboutControl(_profile)
            {
                Location = new Point(panelLeft, panelTop),
                Size     = new Size(panelWidth, panelHeight),
                Anchor   = panelAnchor
            };
            Controls.Add(_aboutControl);

            _servicesControl = new ServicesControl(_profile)
            {
                Location = new Point(panelLeft, panelTop),
                Size     = new Size(panelWidth, panelHeight),
                Anchor   = panelAnchor,
                Visible  = false
            };
            Controls.Add(_servicesControl);

            _setupControl = new SetupControl(_profile)
            {
                Location = new Point(panelLeft, panelTop),
                Size     = new Size(panelWidth, panelHeight),
                Anchor   = panelAnchor,
                Visible  = false
            };
            Controls.Add(_setupControl);




            // --------- Hook up all the user control events ---------
            _aboutControl.Close += (s, eventArgs) =>
            {
                Close();
            };

            // --------- set up the category tree --------------

            var about = new TreeNode("About");
            var services = new TreeNode("Services");
            _setup = new TreeNode("Setup");

            treeView1.Nodes.Add(about);
            treeView1.Nodes.Add(services);
            treeView1.Nodes.Add(_setup);

            _aboutControl.Version = File.ReadAllLines(_versionFile)[0];


            try
            {
                _aboutControl.License = _licenseFile;
            }
            // ReSharper disable EmptyGeneralCatchClause
            catch (Exception)
            // ReSharper restore EmptyGeneralCatchClause
            {
            }

            if (string.IsNullOrEmpty(_profile.AccountName)
                || string.IsNullOrEmpty(_profile.GroupName)
                || string.IsNullOrEmpty(_profile.DeviceName))
            {
                _lastPanel = _aboutControl;
                treeView1.SelectedNode = _setup;
                treeView1.Focus();
            }
            else
            {
                _setupControl.FullDeviceName = _profile.GroupName + "> " + _profile.DeviceName;
            }

        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (_lastPanel != null)
            {
                _lastPanel.Visible = false;
            }
            switch (e.Node.Text)
            {
                case "About":
                    _lastPanel = _aboutControl;
                    break;
                case "Services":
                    _lastPanel = _servicesControl;
                    break;
                case "Setup":
                    _lastPanel = _setupControl;
                    break;
            }
            if (_lastPanel != null) _lastPanel.Visible = true;
        }




        private void bOK_Click(object sender, EventArgs e)
        {
            if (!DoSave()) return;
            Close();
        }

        private void bCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void bApply_Click(object sender, EventArgs e)
        {
            DoSave();
        }

        private bool DoSave()
        {
            _setupControl.CommitEdits();
            if (_profile.Changed)
            {
                var result = MessageBox.Show(@"Are you sure you want to save these changes?", @"Save Changes",
                    MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
                if (result != DialogResult.OK) return false;
                _profile.Save();
            }
            return true;
        }

    }
}
