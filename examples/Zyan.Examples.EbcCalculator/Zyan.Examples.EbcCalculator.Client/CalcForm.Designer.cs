namespace Zyan.Examples.EbcCalculator
{
    partial class CalcForm
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
            this._textNumber1 = new System.Windows.Forms.TextBox();
            this._textNumber2 = new System.Windows.Forms.TextBox();
            this._buttonCalc = new System.Windows.Forms.Button();
            this._textResult = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // _textNumber1
            // 
            this._textNumber1.Location = new System.Drawing.Point(12, 12);
            this._textNumber1.Name = "_textNumber1";
            this._textNumber1.Size = new System.Drawing.Size(198, 20);
            this._textNumber1.TabIndex = 0;
            // 
            // _textNumber2
            // 
            this._textNumber2.Location = new System.Drawing.Point(12, 38);
            this._textNumber2.Name = "_textNumber2";
            this._textNumber2.Size = new System.Drawing.Size(198, 20);
            this._textNumber2.TabIndex = 1;
            // 
            // _buttonCalc
            // 
            this._buttonCalc.Location = new System.Drawing.Point(12, 64);
            this._buttonCalc.Name = "_buttonCalc";
            this._buttonCalc.Size = new System.Drawing.Size(198, 23);
            this._buttonCalc.TabIndex = 2;
            this._buttonCalc.Text = "button1";
            this._buttonCalc.UseVisualStyleBackColor = true;
            this._buttonCalc.Click += new System.EventHandler(this._buttonCalc_Click);
            // 
            // _textResult
            // 
            this._textResult.Location = new System.Drawing.Point(12, 93);
            this._textResult.Name = "_textResult";
            this._textResult.Size = new System.Drawing.Size(198, 20);
            this._textResult.TabIndex = 3;
            // 
            // CalcForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(222, 144);
            this.Controls.Add(this._textResult);
            this.Controls.Add(this._buttonCalc);
            this.Controls.Add(this._textNumber2);
            this.Controls.Add(this._textNumber1);
            this.Name = "CalcForm";
            this.Text = "Zyan EBC Rechner";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox _textNumber1;
        private System.Windows.Forms.TextBox _textNumber2;
        private System.Windows.Forms.Button _buttonCalc;
        private System.Windows.Forms.TextBox _textResult;
    }
}

