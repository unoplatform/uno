using System.Drawing;
using System;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;

namespace UnoSolutionTemplate.Wizard.Forms
{
	public class UnoOptionsBaseForm : Form
	{
		private readonly IServiceProvider _serviceProvider;

		public UnoOptionsBaseForm(IServiceProvider serviceProvider)
		{
			_serviceProvider = serviceProvider;
		}

		public UnoOptionsBaseForm()
		{

		}

		protected void InitializeFont()
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (_serviceProvider.GetService(typeof(IUIHostLocale)) is IUIHostLocale2 hostLocale)
			{
				UIDLGLOGFONT[] array = (UIDLGLOGFONT[])(object)new UIDLGLOGFONT[1];
				if (hostLocale.GetDialogFont(array) == 0)
				{
					Font = FontFromUIDLGLOGFONT(array[0]);
				}
			}
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

		protected void ResizeLabelDescription(Label labelDescription)
		{
			if (labelDescription == null)
			{
				throw new ArgumentNullException(nameof(labelDescription));
			}

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
		}
	}
}
