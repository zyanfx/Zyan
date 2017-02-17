namespace WindowsFormsApplication2
{
	partial class ClientForm
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
			this.FirstServiceTestMethodBtn = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// FirstServiceTestMethodBtn
			// 
			this.FirstServiceTestMethodBtn.Location = new System.Drawing.Point(38, 49);
			this.FirstServiceTestMethodBtn.Name = "FirstServiceTestMethodBtn";
			this.FirstServiceTestMethodBtn.Size = new System.Drawing.Size(142, 23);
			this.FirstServiceTestMethodBtn.TabIndex = 0;
			this.FirstServiceTestMethodBtn.Text = "First service test method";
			this.FirstServiceTestMethodBtn.UseVisualStyleBackColor = true;
			this.FirstServiceTestMethodBtn.Click += new System.EventHandler(this.FirstServiceTestMethodBtn_Click);
			// 
			// ClientForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(284, 261);
			this.Controls.Add(this.FirstServiceTestMethodBtn);
			this.Name = "ClientForm";
			this.Text = "ClientForm";
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button FirstServiceTestMethodBtn;
	}
}

