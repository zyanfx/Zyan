namespace Zyan.Examples.WhisperChat.Client
{
    partial class MainForm
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
            this.label1 = new System.Windows.Forms.Label();
            this._textboxName = new System.Windows.Forms.TextBox();
            this._buttonRegister = new System.Windows.Forms.Button();
            this._buttonUnregister = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this._textboxTo = new System.Windows.Forms.TextBox();
            this._textboxText = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this._textboxChat = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this._buttonWhisper = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(35, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(35, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Name";
            // 
            // _textboxName
            // 
            this._textboxName.Location = new System.Drawing.Point(76, 6);
            this._textboxName.Name = "_textboxName";
            this._textboxName.Size = new System.Drawing.Size(262, 20);
            this._textboxName.TabIndex = 1;
            // 
            // _buttonRegister
            // 
            this._buttonRegister.Location = new System.Drawing.Point(344, 6);
            this._buttonRegister.Name = "_buttonRegister";
            this._buttonRegister.Size = new System.Drawing.Size(69, 20);
            this._buttonRegister.TabIndex = 2;
            this._buttonRegister.Text = "Register";
            this._buttonRegister.UseVisualStyleBackColor = true;
            this._buttonRegister.Click += new System.EventHandler(this._buttonRegister_Click);
            // 
            // _buttonUnregister
            // 
            this._buttonUnregister.Enabled = false;
            this._buttonUnregister.Location = new System.Drawing.Point(419, 6);
            this._buttonUnregister.Name = "_buttonUnregister";
            this._buttonUnregister.Size = new System.Drawing.Size(69, 20);
            this._buttonUnregister.TabIndex = 3;
            this._buttonUnregister.Text = "Unregister";
            this._buttonUnregister.UseVisualStyleBackColor = true;
            this._buttonUnregister.Click += new System.EventHandler(this._buttonUnregister_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 35);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(58, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Whisper to";
            // 
            // _textboxTo
            // 
            this._textboxTo.Enabled = false;
            this._textboxTo.Location = new System.Drawing.Point(76, 32);
            this._textboxTo.Name = "_textboxTo";
            this._textboxTo.Size = new System.Drawing.Size(262, 20);
            this._textboxTo.TabIndex = 5;
            // 
            // _textboxText
            // 
            this._textboxText.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this._textboxText.Enabled = false;
            this._textboxText.Location = new System.Drawing.Point(76, 58);
            this._textboxText.Multiline = true;
            this._textboxText.Name = "_textboxText";
            this._textboxText.Size = new System.Drawing.Size(549, 57);
            this._textboxText.TabIndex = 6;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(42, 61);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(28, 13);
            this.label3.TabIndex = 7;
            this.label3.Text = "Text";
            // 
            // _textboxChat
            // 
            this._textboxChat.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this._textboxChat.Location = new System.Drawing.Point(76, 162);
            this._textboxChat.Multiline = true;
            this._textboxChat.Name = "_textboxChat";
            this._textboxChat.ReadOnly = true;
            this._textboxChat.ScrollBars = System.Windows.Forms.ScrollBars.Horizontal;
            this._textboxChat.Size = new System.Drawing.Size(549, 200);
            this._textboxChat.TabIndex = 8;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(42, 162);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(29, 13);
            this.label4.TabIndex = 9;
            this.label4.Text = "Chat";
            // 
            // _buttonWhisper
            // 
            this._buttonWhisper.Enabled = false;
            this._buttonWhisper.Location = new System.Drawing.Point(76, 121);
            this._buttonWhisper.Name = "_buttonWhisper";
            this._buttonWhisper.Size = new System.Drawing.Size(159, 35);
            this._buttonWhisper.TabIndex = 10;
            this._buttonWhisper.Text = "Whisper";
            this._buttonWhisper.UseVisualStyleBackColor = true;
            this._buttonWhisper.Click += new System.EventHandler(this._buttonWhisper_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(637, 374);
            this.Controls.Add(this._buttonWhisper);
            this.Controls.Add(this.label4);
            this.Controls.Add(this._textboxChat);
            this.Controls.Add(this.label3);
            this.Controls.Add(this._textboxText);
            this.Controls.Add(this._textboxTo);
            this.Controls.Add(this.label2);
            this.Controls.Add(this._buttonUnregister);
            this.Controls.Add(this._buttonRegister);
            this.Controls.Add(this._textboxName);
            this.Controls.Add(this.label1);
            this.Name = "MainForm";
            this.Text = "Form1";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainForm_FormClosed);
            this.Shown += new System.EventHandler(this.MainForm_Shown);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox _textboxName;
        private System.Windows.Forms.Button _buttonRegister;
        private System.Windows.Forms.Button _buttonUnregister;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox _textboxTo;
        private System.Windows.Forms.TextBox _textboxText;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox _textboxChat;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button _buttonWhisper;
    }
}

