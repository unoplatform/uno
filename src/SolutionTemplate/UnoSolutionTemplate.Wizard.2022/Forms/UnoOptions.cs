using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace UnoSolutionTemplate.Wizard.Forms
{
	public partial class UnoOptions : Form
	{
		private IServiceProvider _serviceProvider;

		public bool UseWebAssembly => checkWebAssembly.Checked;
		public bool UseiOS => checkiOS.Checked;
		public bool UseAndroid => checkAndroid.Checked;
		public bool UseCatalyst => checkCatalyst.Checked;
		public bool UseAppKit => checkAppKit.Checked;
		public bool UseGtk => checkGtk.Checked;
		public bool UseFramebuffer => checkLinux.Checked;
		public bool UseWpf => checkWpf.Checked;
		public bool UseWinUI => checkWinUI.Checked;
		public bool UseServer => checkServer.Checked;
		public string UseBaseTargetFramework => BaseTargetFramework.SelectedItem is TargetFrameworkOption option ? option.BaseValue : "invalid";

		public class TargetFrameworkOption
		{
			public string BaseValue { get; set; }
			public string DisplayValue { get; set; }
		}

		public UnoOptions(IServiceProvider serviceProvider)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			InitializeComponent();

			_serviceProvider = serviceProvider;

			if (_serviceProvider.GetService(typeof(IUIHostLocale)) is IUIHostLocale2 hostLocale)
			{
				UIDLGLOGFONT[] array = (UIDLGLOGFONT[])(object)new UIDLGLOGFONT[1];
				if (hostLocale.GetDialogFont(array) == 0)
				{
					Font = FontFromUIDLGLOGFONT(array[0]);
				}
			}

			BaseTargetFramework.Items.Add(new TargetFrameworkOption { BaseValue = "net6.0", DisplayValue = ".NET 6.0" });
			BaseTargetFramework.Items.Add(new TargetFrameworkOption { BaseValue = "net7.0", DisplayValue = ".NET 7.0" });
			BaseTargetFramework.SelectedIndex = 0;
		}

		private static Font FontFromUIDLGLOGFONT(UIDLGLOGFONT logFont)
		{
			var fonts = new char[logFont.lfFaceName.Length];

			var num = 0;
			ushort[] lfFaceName = logFont.lfFaceName;
			foreach (ushort num2 in lfFaceName)
			{
				fonts[num++] = (char)num2;
			}

			var familyName = new string(fonts);
			var emSize = -logFont.lfHeight;
			var fontStyle = FontStyle.Regular;

			if (logFont.lfItalic > 0)
			{
				fontStyle |= FontStyle.Italic;
			}
			if (logFont.lfUnderline > 0)
			{
				fontStyle |= FontStyle.Underline;
			}
			if (logFont.lfStrikeOut > 0)
			{
				fontStyle |= FontStyle.Strikeout;
			}
			if (logFont.lfWeight > 400)
			{
				fontStyle |= FontStyle.Bold;
			}

			var unit = GraphicsUnit.Pixel;
			var lfCharSet = logFont.lfCharSet;

			return new Font(familyName, emSize, fontStyle, unit, lfCharSet);
		}

		private void UnoOptions_Load(object sender, EventArgs e)
		{
			using Graphics graphics = CreateGraphics();
			var sizeF = graphics.MeasureString(labelDescription.Text, Font);

			int widthRatio = (int)(sizeF.Width / (float)labelDescription.Width);
			if (widthRatio != 0)
			{
				int heightCeil = (int)Math.Ceiling(sizeF.Height);
				SuspendLayout();
				labelDescription.Height = heightCeil + widthRatio * heightCeil;
				ResumeLayout(performLayout: true);
			}

			CenterToParent();
		}

		private void checkWebAssembly_CheckedChanged(object sender, EventArgs e)
		{
		}
	}
}
