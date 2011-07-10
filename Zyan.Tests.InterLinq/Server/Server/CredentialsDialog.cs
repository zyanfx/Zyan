using System.Windows.Forms;

namespace InterLinq.UnitTests.Server
{
    public class CredentialsDialog : Form
    {
        public static DialogResult ShowDialog(IWin32Window parent, ref string server, ref string username, ref string password)
        {
            CredentialsDialog frmInit = new CredentialsDialog
                                         {
                                             txtDbServer = { Text = server != string.Empty ? server : "localhost" },
                                             txtUsername = { Text = username },
                                             txtPassword = { Text = password }
                                         };

            DialogResult result = frmInit.ShowDialog(parent);
            if (result == DialogResult.OK)
            {
                username = frmInit.txtUsername.Text;
                password = frmInit.txtPassword.Text;
                server = frmInit.txtDbServer.Text;
            }
            return result;
        }

        private Button btnRunTest;
        private Button btnCancel;
        private GroupBox groupDbConfig;
        private Label labDbUser;
        private Label labPassword;
        private Label labelUsername;
        private TextBox txtUsername;
        private TextBox txtPassword;
        private Label labDbServer;
        private TextBox txtDbServer;

        private CredentialsDialog()
        {
            InitializeComponent();
        }

        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btnRunTest = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.groupDbConfig = new System.Windows.Forms.GroupBox();
            this.txtDbServer = new System.Windows.Forms.TextBox();
            this.labDbServer = new System.Windows.Forms.Label();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.txtUsername = new System.Windows.Forms.TextBox();
            this.labelUsername = new System.Windows.Forms.Label();
            this.labPassword = new System.Windows.Forms.Label();
            this.labDbUser = new System.Windows.Forms.Label();
            this.groupDbConfig.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnRunTest
            // 
            this.btnRunTest.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnRunTest.Location = new System.Drawing.Point(32, 248);
            this.btnRunTest.Name = "btnRunTest";
            this.btnRunTest.Size = new System.Drawing.Size(75, 23);
            this.btnRunTest.TabIndex = 1;
            this.btnRunTest.Text = "Run test";
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(120, 248);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.Text = "Cancel";
            // 
            // groupDbConfig
            // 
            this.groupDbConfig.Controls.Add(this.txtDbServer);
            this.groupDbConfig.Controls.Add(this.labDbServer);
            this.groupDbConfig.Controls.Add(this.txtPassword);
            this.groupDbConfig.Controls.Add(this.txtUsername);
            this.groupDbConfig.Controls.Add(this.labelUsername);
            this.groupDbConfig.Controls.Add(this.labPassword);
            this.groupDbConfig.Controls.Add(this.labDbUser);
            this.groupDbConfig.Location = new System.Drawing.Point(8, 8);
            this.groupDbConfig.Name = "groupDbConfig";
            this.groupDbConfig.Size = new System.Drawing.Size(192, 232);
            this.groupDbConfig.TabIndex = 0;
            this.groupDbConfig.TabStop = false;
            this.groupDbConfig.Text = "DB connection setup";
            // 
            // txtDbServer
            // 
            this.txtDbServer.Location = new System.Drawing.Point(8, 104);
            this.txtDbServer.Name = "txtDbServer";
            this.txtDbServer.Size = new System.Drawing.Size(176, 20);
            this.txtDbServer.TabIndex = 1;
            this.txtDbServer.Text = "localhost";
            // 
            // labDbServer
            // 
            this.labDbServer.AutoSize = true;
            this.labDbServer.Location = new System.Drawing.Point(8, 88);
            this.labDbServer.Name = "labDbServer";
            this.labDbServer.Size = new System.Drawing.Size(88, 13);
            this.labDbServer.TabIndex = 12;
            this.labDbServer.Text = "Database server:";
            // 
            // txtPassword
            // 
            this.txtPassword.Location = new System.Drawing.Point(8, 200);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.PasswordChar = '*';
            this.txtPassword.Size = new System.Drawing.Size(176, 20);
            this.txtPassword.TabIndex = 0;
            // 
            // txtUsername
            // 
            this.txtUsername.Location = new System.Drawing.Point(8, 152);
            this.txtUsername.Name = "txtUsername";
            this.txtUsername.Size = new System.Drawing.Size(176, 20);
            this.txtUsername.TabIndex = 2;
            // 
            // labelUsername
            // 
            this.labelUsername.AutoSize = true;
            this.labelUsername.Location = new System.Drawing.Point(8, 136);
            this.labelUsername.Name = "labelUsername";
            this.labelUsername.Size = new System.Drawing.Size(61, 13);
            this.labelUsername.TabIndex = 13;
            this.labelUsername.Text = "User name:";
            // 
            // labPassword
            // 
            this.labPassword.AutoSize = true;
            this.labPassword.Location = new System.Drawing.Point(8, 184);
            this.labPassword.Name = "labPassword";
            this.labPassword.Size = new System.Drawing.Size(56, 13);
            this.labPassword.TabIndex = 14;
            this.labPassword.Text = "Password:";
            // 
            // labDbUser
            // 
            this.labDbUser.Location = new System.Drawing.Point(8, 24);
            this.labDbUser.Name = "labDbUser";
            this.labDbUser.Size = new System.Drawing.Size(176, 48);
            this.labDbUser.TabIndex = 11;
            this.labDbUser.Text = "Enter the username and password for a database user which has the rights to creat" +
                "e and drop databases and tables.";
            // 
            // CredentialsDialog
            // 
            this.AcceptButton = this.btnRunTest;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(210, 286);
            this.Controls.Add(this.groupDbConfig);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnRunTest);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "CredentialsDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Credentials Dialog";
            this.TopMost = true;
            this.groupDbConfig.ResumeLayout(false);
            this.groupDbConfig.PerformLayout();
            this.ResumeLayout(false);

        }
        #endregion
    }
}
