#nullable enable

using System;
using System.IO;
using System.Text;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Storage.Streams;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	public partial class Given_RichEditBox
	{

		[TestMethod]
		public void When_Rtf_Color_Table_Auto_And_Omitted_Components_Are_Structural()
		{
			const string rtf = @"{\rtf1{\colortbl;\red255;;\green128\blue64;}"
				+ @"\cf1 R\cf2 A\cf3 G}";

			var fragment = RichTextRtfCodec.Read(rtf);

			Assert.AreEqual(Windows.UI.Color.FromArgb(255, 255, 0, 0), fragment.GetCharacterFormatAt(0).Foreground);
			Assert.IsNull(fragment.GetCharacterFormatAt(1).Foreground);
			Assert.AreEqual(Windows.UI.Color.FromArgb(255, 0, 128, 64), fragment.GetCharacterFormatAt(2).Foreground);
		}
	}
}
