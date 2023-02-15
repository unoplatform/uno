using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Controls;
using System.Drawing;
using Windows.UI.Xaml.Media;

namespace Uno.UI.Tests.Windows_UI_XAML_Controls.TextBlockTests
{
	[TestClass]
	public class Given_TextBlock
	{
		private const double DefaultFontSize = 14.0;

		[TestMethod]
		public void When_Default_FontSize()
		{
			var tb = new TextBlock();
			Assert.AreEqual(DefaultFontSize, tb.FontSize);

			var ctrl = new MyControl();
			Assert.AreEqual(DefaultFontSize, ctrl.FontSize);

			var sp = new Span();
			Assert.AreEqual(DefaultFontSize, sp.FontSize);

			var cp = new ContentPresenter();
			Assert.AreEqual(DefaultFontSize, cp.FontSize);
		}

#if !NETFX_CORE
		[TestMethod]
		public void When_Changing_Foreground_Property()
		{
			var tb = new TextBlock();
			tb.Foreground = SolidColorBrushHelper.Red;
			Assert.AreEqual(SolidColorBrushHelper.Red.Color, (tb.Foreground as SolidColorBrush)?.Color);

			tb.Foreground = null;
			Assert.AreEqual(null, tb.Foreground);

			tb.Foreground = SolidColorBrushHelper.AliceBlue;
			Assert.AreEqual(SolidColorBrushHelper.AliceBlue.Color, (tb.Foreground as SolidColorBrush)?.Color);
		}
#endif

		[TestMethod]
		public void When_LineBreak_SurroundingWhiteSpace()
		{
			var page = new LineBreak_Surrounding_WhiteSpace();
			var tbTrim = page.tbTrim;
			Assert.AreEqual(5, tbTrim.Inlines.Count);
			Assert.AreEqual("BeforeLineBreak", ((Run)tbTrim.Inlines[0]).Text);
			Assert.IsInstanceOfType(tbTrim.Inlines[1], typeof(LineBreak));
			Assert.AreEqual("You can construct URLs and access their parts. For URLs that represent local files, you can also manipulate properties of those files directly.", ((Run)tbTrim.Inlines[2]).Text);
			Assert.IsInstanceOfType(tbTrim.Inlines[3], typeof(LineBreak));
			Assert.AreEqual("AfterLineBreak", ((Run)tbTrim.Inlines[4]).Text);

			var tbPreserve = page.tbPreserve;
			Assert.AreEqual(5, tbPreserve.Inlines.Count);
			Assert.AreEqual("\n\t\t\t\t   BeforeLineBreak\n\t\t\t\t", ((Run)tbPreserve.Inlines[0]).Text);
			Assert.IsInstanceOfType(tbPreserve.Inlines[1], typeof(LineBreak));
			Assert.AreEqual("\n\t\t\t\t   You can construct URLs and access their parts. For URLs that represent local files, you can also manipulate properties of those files directly.\n\t\t\t\t", ((Run)tbPreserve.Inlines[2]).Text);
			Assert.IsInstanceOfType(tbPreserve.Inlines[3], typeof(LineBreak));
			Assert.AreEqual("\n\t\t\t\t   AfterLineBreak\n\t\t", ((Run)tbPreserve.Inlines[4]).Text);
		}
	}

	public partial class MyControl : Control { }
}
