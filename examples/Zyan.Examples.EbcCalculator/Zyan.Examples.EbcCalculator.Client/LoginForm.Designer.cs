namespace Zyan.Examples.EbcCalculator
{
    partial class LoginForm
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LoginForm));
			this.label1 = new System.Windows.Forms.Label();
			this._userBox = new System.Windows.Forms.TextBox();
			this._passwordBox = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this._acceptButton = new System.Windows.Forms.Button();
			this._cancelButton = new System.Windows.Forms.Button();
			this._messageLabel = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// label1
			// 
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			// 
			// _userBox
			// 
			resources.ApplyResources(this._userBox, "_userBox");
			this._userBox.Name = "_userBox";
			// 
			// _passwordBox
			// 
			resources.ApplyResources(this._passwordBox, "_passwordBox");
			this._passwordBox.Name = "_passwordBox";
			this._passwordBox.UseSystemPasswordChar = true;
			// 
			// label2
			// 
			resources.ApplyResources(this.label2, "label2");
			this.label2.Name = "label2";
			// 
			// _acceptButton
			// 
			this._acceptButton.DialogResult = System.Windows.Forms.DialogResult.OK;
			resources.ApplyResources(this._acceptButton, "_acceptButton");
			this._acceptButton.Name = "_acceptButton";
			this._acceptButton.UseVisualStyleBackColor = true;
			// 
			// _cancelButton
			// 
			this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(this._cancelButton, "_cancelButton");
			this._cancelButton.Name = "_cancelButton";
			this._cancelButton.UseVisualStyleBackColor = true;
			// 
			// _messageLabel
			// 
			resources.ApplyResources(this._messageLabel, "_messageLabel");
			this._messageLabel.Name = "_messageLabel";
			// 
			// LoginForm
			// 
			this.AcceptButton = this._acceptButton;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this._cancelButton;
			this.ControlBox = false;
			this.Controls.Add(this._messageLabel);
			this.Controls.Add(this._cancelButton);
			this.Controls.Add(this._acceptButton);
			this.Controls.Add(this._passwordBox);
			this.Controls.Add(this.label2);
			this.Controls.Add(this._userBox);
			this.Controls.Add(this.label1);
			this.DoubleBuffered = true;
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Name = "LoginForm";
			this.TopMost = true;
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox _userBox;
        private System.Windows.Forms.TextBox _passwordBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button _acceptButton;
        private System.Windows.Forms.Button _cancelButton;
        private System.Windows.Forms.Label _messageLabel;
    }
}