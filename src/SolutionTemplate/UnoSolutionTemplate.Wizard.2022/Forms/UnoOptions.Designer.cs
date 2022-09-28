namespace UnoSolutionTemplate.Wizard.Forms
{
	partial class UnoOptions
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
            this.labelDescription = new System.Windows.Forms.Label();
            this.checkWebAssembly = new System.Windows.Forms.CheckBox();
            this.checkGtk = new System.Windows.Forms.CheckBox();
            this.checkLinux = new System.Windows.Forms.CheckBox();
            this.checkWpf = new System.Windows.Forms.CheckBox();
            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.checkWinUI = new System.Windows.Forms.CheckBox();
            this.flowLayoutPanel3 = new System.Windows.Forms.FlowLayoutPanel();
            this.checkiOS = new System.Windows.Forms.CheckBox();
            this.checkAndroid = new System.Windows.Forms.CheckBox();
            this.flowLayoutPanel2 = new System.Windows.Forms.FlowLayoutPanel();
            this.checkCatalyst = new System.Windows.Forms.CheckBox();
            this.checkAppKit = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.label4 = new System.Windows.Forms.Label();
            this.flowLayoutPanel4 = new System.Windows.Forms.FlowLayoutPanel();
            this.label5 = new System.Windows.Forms.Label();
            this.BaseTargetFramework = new System.Windows.Forms.ComboBox();
            this.flowLayoutPanel3.SuspendLayout();
            this.flowLayoutPanel2.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.flowLayoutPanel4.SuspendLayout();
            this.SuspendLayout();
            // 
            // labelDescription
            // 
            this.labelDescription.AutoSize = true;
            this.labelDescription.Location = new System.Drawing.Point(12, 9);
            this.labelDescription.Name = "labelDescription";
            this.labelDescription.Size = new System.Drawing.Size(235, 13);
            this.labelDescription.TabIndex = 0;
            this.labelDescription.Text = "Select the following options for your new project.";
            // 
            // checkWebAssembly
            // 
            this.checkWebAssembly.AutoSize = true;
            this.checkWebAssembly.Checked = true;
            this.checkWebAssembly.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkWebAssembly.Location = new System.Drawing.Point(3, 3);
            this.checkWebAssembly.Name = "checkWebAssembly";
            this.checkWebAssembly.Size = new System.Drawing.Size(100, 21);
            this.checkWebAssembly.TabIndex = 1;
            this.checkWebAssembly.Text = "WebAssembly";
            this.checkWebAssembly.UseVisualStyleBackColor = true;
            this.checkWebAssembly.CheckedChanged += new System.EventHandler(this.checkWebAssembly_CheckedChanged);
            // 
            // checkGtk
            // 
            this.checkGtk.AutoSize = true;
            this.checkGtk.Checked = true;
            this.checkGtk.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkGtk.Location = new System.Drawing.Point(3, 30);
            this.checkGtk.Name = "checkGtk";
            this.checkGtk.Size = new System.Drawing.Size(219, 21);
            this.checkGtk.TabIndex = 1;
            this.checkGtk.Text = "Gtk (Linux, macOS, Windows 7 or later)";
            this.checkGtk.UseVisualStyleBackColor = true;
            this.checkGtk.CheckedChanged += new System.EventHandler(this.checkBox3_CheckedChanged);
            // 
            // checkLinux
            // 
            this.checkLinux.AutoSize = true;
            this.checkLinux.Location = new System.Drawing.Point(228, 3);
            this.checkLinux.Name = "checkLinux";
            this.checkLinux.Size = new System.Drawing.Size(117, 21);
            this.checkLinux.TabIndex = 1;
            this.checkLinux.Text = "Linux Framebuffer";
            this.checkLinux.UseVisualStyleBackColor = true;
            // 
            // checkWpf
            // 
            this.checkWpf.AutoSize = true;
            this.checkWpf.Location = new System.Drawing.Point(3, 111);
            this.checkWpf.Name = "checkWpf";
            this.checkWpf.Size = new System.Drawing.Size(154, 21);
            this.checkWpf.TabIndex = 1;
            this.checkWpf.Text = "WPF (Windows 7 or later)";
            this.checkWpf.UseVisualStyleBackColor = true;
            // 
            // buttonOK
            // 
            this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonOK.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonOK.Location = new System.Drawing.Point(218, 427);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(71, 23);
            this.buttonOK.TabIndex = 0;
            this.buttonOK.Text = "Create";
            this.buttonOK.UseVisualStyleBackColor = true;
            // 
            // buttonCancel
            // 
            this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonCancel.Location = new System.Drawing.Point(295, 427);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(71, 23);
            this.buttonCancel.TabIndex = 2;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            // 
            // checkWinUI
            // 
            this.checkWinUI.AutoSize = true;
            this.checkWinUI.Checked = true;
            this.checkWinUI.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkWinUI.Location = new System.Drawing.Point(3, 3);
            this.checkWinUI.Name = "checkWinUI";
            this.checkWinUI.Size = new System.Drawing.Size(163, 21);
            this.checkWinUI.TabIndex = 2;
            this.checkWinUI.Text = "WinUI (Windows App SDK)";
            this.checkWinUI.UseVisualStyleBackColor = true;
            this.checkWinUI.CheckedChanged += new System.EventHandler(this.checkWinUI_CheckedChanged);
            // 
            // flowLayoutPanel3
            // 
            this.flowLayoutPanel3.Controls.Add(this.checkiOS);
            this.flowLayoutPanel3.Controls.Add(this.checkAndroid);
            this.flowLayoutPanel3.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanel3.Location = new System.Drawing.Point(3, 135);
            this.flowLayoutPanel3.Name = "flowLayoutPanel3";
            this.flowLayoutPanel3.Size = new System.Drawing.Size(341, 50);
            this.flowLayoutPanel3.TabIndex = 1;
            // 
            // checkiOS
            // 
            this.checkiOS.AutoSize = true;
            this.checkiOS.Checked = true;
            this.checkiOS.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkiOS.Location = new System.Drawing.Point(3, 3);
            this.checkiOS.Name = "checkiOS";
            this.checkiOS.Size = new System.Drawing.Size(50, 21);
            this.checkiOS.TabIndex = 0;
            this.checkiOS.Text = "iOS";
            this.checkiOS.UseVisualStyleBackColor = true;
            // 
            // checkAndroid
            // 
            this.checkAndroid.AutoSize = true;
            this.checkAndroid.Checked = true;
            this.checkAndroid.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkAndroid.Location = new System.Drawing.Point(59, 3);
            this.checkAndroid.Name = "checkAndroid";
            this.checkAndroid.Size = new System.Drawing.Size(69, 21);
            this.checkAndroid.TabIndex = 1;
            this.checkAndroid.Text = "Android";
            this.checkAndroid.UseVisualStyleBackColor = true;
            // 
            // flowLayoutPanel2
            // 
            this.flowLayoutPanel2.Controls.Add(this.checkWinUI);
            this.flowLayoutPanel2.Controls.Add(this.checkGtk);
            this.flowLayoutPanel2.Controls.Add(this.checkCatalyst);
            this.flowLayoutPanel2.Controls.Add(this.checkAppKit);
            this.flowLayoutPanel2.Controls.Add(this.checkWpf);
            this.flowLayoutPanel2.Controls.Add(this.checkLinux);
            this.flowLayoutPanel2.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanel2.Location = new System.Drawing.Point(3, 211);
            this.flowLayoutPanel2.Name = "flowLayoutPanel2";
            this.flowLayoutPanel2.Size = new System.Drawing.Size(341, 139);
            this.flowLayoutPanel2.TabIndex = 2;
            // 
            // checkCatalyst
            // 
            this.checkCatalyst.AutoSize = true;
            this.checkCatalyst.Checked = true;
            this.checkCatalyst.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkCatalyst.Location = new System.Drawing.Point(3, 57);
            this.checkCatalyst.Name = "checkCatalyst";
            this.checkCatalyst.Size = new System.Drawing.Size(114, 21);
            this.checkCatalyst.TabIndex = 3;
            this.checkCatalyst.Text = "macOS (Catalyst)";
            this.checkCatalyst.UseVisualStyleBackColor = true;
            // 
            // checkAppKit
            // 
            this.checkAppKit.AutoSize = true;
            this.checkAppKit.Location = new System.Drawing.Point(3, 84);
            this.checkAppKit.Name = "checkAppKit";
            this.checkAppKit.Size = new System.Drawing.Size(108, 21);
            this.checkAppKit.TabIndex = 2;
            this.checkAppKit.Text = "macOS (AppKit)";
            this.checkAppKit.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.Location = new System.Drawing.Point(19, 389);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(341, 35);
            this.label1.TabIndex = 6;
            this.label1.Text = "If you do not select a platform at this time, you can add it back later by visiti" +
    "ng our documentation.";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(3, 108);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(57, 24);
            this.label2.TabIndex = 0;
            this.label2.Text = "Mobile";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.label2.UseCompatibleTextRendering = true;
            this.label2.Click += new System.EventHandler(this.label2_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(3, 188);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(76, 20);
            this.label3.TabIndex = 0;
            this.label3.Text = "Desktop";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.label3.Click += new System.EventHandler(this.label2_Click);
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.flowLayoutPanel1.Controls.Add(this.label5);
            this.flowLayoutPanel1.Controls.Add(this.BaseTargetFramework);
            this.flowLayoutPanel1.Controls.Add(this.label4);
            this.flowLayoutPanel1.Controls.Add(this.flowLayoutPanel4);
            this.flowLayoutPanel1.Controls.Add(this.label2);
            this.flowLayoutPanel1.Controls.Add(this.flowLayoutPanel3);
            this.flowLayoutPanel1.Controls.Add(this.label3);
            this.flowLayoutPanel1.Controls.Add(this.flowLayoutPanel2);
            this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(13, 25);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(347, 361);
            this.flowLayoutPanel1.TabIndex = 7;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(3, 51);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(41, 24);
            this.label4.TabIndex = 3;
            this.label4.Text = "Web";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.label4.UseCompatibleTextRendering = true;
            // 
            // flowLayoutPanel4
            // 
            this.flowLayoutPanel4.Controls.Add(this.checkWebAssembly);
            this.flowLayoutPanel4.Location = new System.Drawing.Point(3, 78);
            this.flowLayoutPanel4.Name = "flowLayoutPanel4";
            this.flowLayoutPanel4.Size = new System.Drawing.Size(341, 27);
            this.flowLayoutPanel4.TabIndex = 4;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(3, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(91, 24);
            this.label5.TabIndex = 5;
            this.label5.Text = "Framework";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.label5.UseCompatibleTextRendering = true;
            // 
            // BaseTargetFramework
            // 
            this.BaseTargetFramework.DisplayMember = "DisplayValue";
            this.BaseTargetFramework.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.BaseTargetFramework.FormattingEnabled = true;
            this.BaseTargetFramework.Location = new System.Drawing.Point(3, 27);
            this.BaseTargetFramework.Name = "BaseTargetFramework";
            this.BaseTargetFramework.Size = new System.Drawing.Size(121, 21);
            this.BaseTargetFramework.TabIndex = 6;
            this.BaseTargetFramework.ValueMember = "BaseValue";
            // 
            // UnoOptions
            // 
            this.AcceptButton = this.buttonOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.CancelButton = this.buttonCancel;
            this.ClientSize = new System.Drawing.Size(378, 462);
            this.Controls.Add(this.flowLayoutPanel1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.labelDescription);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(2);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(400, 200);
            this.Name = "UnoOptions";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "New Uno Platform App";
            this.Load += new System.EventHandler(this.UnoOptions_Load);
            this.flowLayoutPanel3.ResumeLayout(false);
            this.flowLayoutPanel3.PerformLayout();
            this.flowLayoutPanel2.ResumeLayout(false);
            this.flowLayoutPanel2.PerformLayout();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            this.flowLayoutPanel4.ResumeLayout(false);
            this.flowLayoutPanel4.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label labelDescription;
		private System.Windows.Forms.CheckBox checkWebAssembly;
		private System.Windows.Forms.CheckBox checkGtk;
		private System.Windows.Forms.CheckBox checkLinux;
		private System.Windows.Forms.CheckBox checkWpf;
		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.CheckBox checkWinUI;
		private System.Windows.Forms.CheckBox checkiOS;
		private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel2;
		private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel3;
		private System.Windows.Forms.CheckBox checkAndroid;
		private System.Windows.Forms.CheckBox checkCatalyst;
		private System.Windows.Forms.CheckBox checkAppKit;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox BaseTargetFramework;
    }
}
