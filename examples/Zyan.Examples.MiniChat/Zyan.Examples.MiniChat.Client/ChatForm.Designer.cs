namespace Zyan.Examples.MiniChat.Client
{
    partial class ChatForm
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
            this._sayBox = new System.Windows.Forms.TextBox();
            this._sendButton = new System.Windows.Forms.Button();
            this._chatList = new System.Windows.Forms.ListBox();
            this._nickName = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // _sayBox
            // 
            this._sayBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._sayBox.Location = new System.Drawing.Point(12, 38);
            this._sayBox.Name = "_sayBox";
            this._sayBox.Size = new System.Drawing.Size(351, 20);
            this._sayBox.TabIndex = 0;
            // 
            // _sendButton
            // 
            this._sendButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._sendButton.Location = new System.Drawing.Point(369, 38);
            this._sendButton.Name = "_sendButton";
            this._sendButton.Size = new System.Drawing.Size(75, 20);
            this._sendButton.TabIndex = 1;
            this._sendButton.Text = "Send";
            this._sendButton.UseVisualStyleBackColor = true;
            this._sendButton.Click += new System.EventHandler(this._sendButton_Click);
            // 
            // _chatList
            // 
            this._chatList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._chatList.FormattingEnabled = true;
            this._chatList.Location = new System.Drawing.Point(12, 77);
            this._chatList.Name = "_chatList";
            this._chatList.Size = new System.Drawing.Size(432, 199);
            this._chatList.TabIndex = 2;
            // 
            // _nickName
            // 
            this._nickName.Location = new System.Drawing.Point(12, 12);
            this._nickName.Name = "_nickName";
            this._nickName.Size = new System.Drawing.Size(136, 20);
            this._nickName.TabIndex = 3;
            this._nickName.Text = "Rainbird";
            // 
            // ChatForm
            // 
            this.AcceptButton = this._sendButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(456, 289);
            this.Controls.Add(this._nickName);
            this.Controls.Add(this._chatList);
            this.Controls.Add(this._sendButton);
            this.Controls.Add(this._sayBox);
            this.Name = "ChatForm";
            this.Text = "Mini Chat";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.ChatForm_FormClosed);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox _sayBox;
        private System.Windows.Forms.Button _sendButton;
        private System.Windows.Forms.ListBox _chatList;
        private System.Windows.Forms.TextBox _nickName;
    }
}

