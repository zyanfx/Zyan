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
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 50);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(49, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Benutzer";
            // 
            // _userBox
            // 
            this._userBox.Location = new System.Drawing.Point(67, 47);
            this._userBox.Name = "_userBox";
            this._userBox.Size = new System.Drawing.Size(196, 20);
            this._userBox.TabIndex = 1;
            // 
            // _passwordBox
            // 
            this._passwordBox.Location = new System.Drawing.Point(67, 73);
            this._passwordBox.Name = "_passwordBox";
            this._passwordBox.Size = new System.Drawing.Size(196, 20);
            this._passwordBox.TabIndex = 3;
            this._passwordBox.UseSystemPasswordChar = true;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 76);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(50, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Passwort";
            // 
            // _acceptButton
            // 
            this._acceptButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._acceptButton.Location = new System.Drawing.Point(188, 115);
            this._acceptButton.Name = "_acceptButton";
            this._acceptButton.Size = new System.Drawing.Size(75, 23);
            this._acceptButton.TabIndex = 4;
            this._acceptButton.Text = "OK";
            this._acceptButton.UseVisualStyleBackColor = true;
            // 
            // _cancelButton
            // 
            this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._cancelButton.Location = new System.Drawing.Point(107, 115);
            this._cancelButton.Name = "_cancelButton";
            this._cancelButton.Size = new System.Drawing.Size(75, 23);
            this._cancelButton.TabIndex = 5;
            this._cancelButton.Text = "Abbrechen";
            this._cancelButton.UseVisualStyleBackColor = true;
            // 
            // _messageLabel
            // 
            this._messageLabel.Location = new System.Drawing.Point(12, 9);
            this._messageLabel.Name = "_messageLabel";
            this._messageLabel.Size = new System.Drawing.Size(251, 35);
            this._messageLabel.TabIndex = 6;
            this._messageLabel.Text = "Bitte melden Sie sich mit Ihrem lokalen Windows-Benutzer an.";
            // 
            // LoginForm
            // 
            this.AcceptButton = this._acceptButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this._cancelButton;
            this.ClientSize = new System.Drawing.Size(275, 150);
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
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Anmelden";
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