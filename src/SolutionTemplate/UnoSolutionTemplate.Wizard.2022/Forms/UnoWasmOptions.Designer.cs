namespace UnoSolutionTemplate.Wizard.Forms
{
	partial class UnoWasmOptions
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
            this.buttonCancel = new System.Windows.Forms.Button();
            this.buttonOK = new System.Windows.Forms.Button();
            this.labelDescription = new System.Windows.Forms.Label();
            this.flowLayoutPanel4 = new System.Windows.Forms.FlowLayoutPanel();
            this.checkManifestJson = new System.Windows.Forms.CheckBox();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.flowLayoutPanel4.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // buttonCancel
            // 
            this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonCancel.Location = new System.Drawing.Point(442, 659);
            this.buttonCancel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(106, 35);
            this.buttonCancel.TabIndex = 10;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            // 
            // buttonOK
            // 
            this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonOK.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonOK.Location = new System.Drawing.Point(327, 659);
            this.buttonOK.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(106, 35);
            this.buttonOK.TabIndex = 8;
            this.buttonOK.Text = "Create";
            this.buttonOK.UseVisualStyleBackColor = true;
            // 
            // labelDescription
            // 
            this.labelDescription.AutoSize = true;
            this.labelDescription.Location = new System.Drawing.Point(18, 16);
            this.labelDescription.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelDescription.Name = "labelDescription";
            this.labelDescription.Size = new System.Drawing.Size(420, 20);
            this.labelDescription.TabIndex = 9;
            this.labelDescription.Text = "Select the following options for your WebAssembly project.";
            // 
            // flowLayoutPanel4
            // 
            this.flowLayoutPanel4.Controls.Add(this.checkManifestJson);
            this.flowLayoutPanel4.Location = new System.Drawing.Point(4, 5);
            this.flowLayoutPanel4.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.flowLayoutPanel4.Name = "flowLayoutPanel4";
            this.flowLayoutPanel4.Size = new System.Drawing.Size(512, 42);
            this.flowLayoutPanel4.TabIndex = 4;
            // 
            // checkManifestJson
            // 
            this.checkManifestJson.AutoSize = true;
            this.checkManifestJson.Checked = true;
            this.checkManifestJson.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkManifestJson.Location = new System.Drawing.Point(4, 5);
            this.checkManifestJson.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.checkManifestJson.Name = "checkManifestJson";
            this.checkManifestJson.Size = new System.Drawing.Size(334, 24);
            this.checkManifestJson.TabIndex = 1;
            this.checkManifestJson.Text = "Include manifest.json file for PWA support.";
            this.checkManifestJson.UseVisualStyleBackColor = true;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.flowLayoutPanel1.Controls.Add(this.flowLayoutPanel4);
            this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(20, 40);
            this.flowLayoutPanel1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(520, 555);
            this.flowLayoutPanel1.TabIndex = 12;
            // 
            // UnoWasmOptions
            // 
            this.AcceptButton = this.buttonOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.CancelButton = this.buttonCancel;
            this.ClientSize = new System.Drawing.Size(567, 711);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.labelDescription);
            this.Controls.Add(this.flowLayoutPanel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(400, 200);
			this.Name = "UnoWasmOptions";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "New Uno Platform App";
			this.Load += new System.EventHandler(this.UnoWasmOptions_Load);
			this.flowLayoutPanel4.ResumeLayout(false);
            this.flowLayoutPanel4.PerformLayout();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion
		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.Label labelDescription;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel4;
        private System.Windows.Forms.CheckBox checkManifestJson;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
    }
}
