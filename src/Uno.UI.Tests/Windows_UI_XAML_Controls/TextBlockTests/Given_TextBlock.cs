using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Controls;
using System.Drawing;
using Microsoft.UI.Xaml.Media;

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
			Assert.IsInstanceOfType(tb.Foreground, typeof(SolidColorBrush));
			Assert.AreEqual(SolidColorBrushHelper.Red.Color, ((SolidColorBrush)tb.Foreground).Color);

			tb.Foreground = null;
			Assert.IsNull(tb.Foreground);

			tb.Foreground = SolidColorBrushHelper.AliceBlue;
			Assert.IsInstanceOfType(tb.Foreground, typeof(SolidColorBrush));
			Assert.AreEqual(SolidColorBrushHelper.AliceBlue.Color, ((SolidColorBrush)tb.Foreground).Color);
		}
#endif

		[TestMethod]
		public void When_LineBreak_SurroundingWhiteSpace()
		{
			var page = new LineBreak_Surrounding_WhiteSpace();
			var tbTrim = page.tbTrim;
			Assert.HasCount(5, tbTrim.Inlines);
			Assert.AreEqual("BeforeLineBreak", ((Run)tbTrim.Inlines[0]).Text);
			Assert.IsInstanceOfType(tbTrim.Inlines[1], typeof(LineBreak));
			Assert.AreEqual("You can construct URLs and access their parts. For URLs that represent local files, you can also manipulate properties of those files directly.", ((Run)tbTrim.Inlines[2]).Text);
			Assert.IsInstanceOfType(tbTrim.Inlines[3], typeof(LineBreak));
			Assert.AreEqual("AfterLineBreak", ((Run)tbTrim.Inlines[4]).Text);

			var tbPreserve = page.tbPreserve;
			Assert.HasCount(5, tbPreserve.Inlines);
			Assert.AreEqual("\n\t\t\t\t   BeforeLineBreak\n\t\t\t\t", ((Run)tbPreserve.Inlines[0]).Text);
			Assert.IsInstanceOfType(tbPreserve.Inlines[1], typeof(LineBreak));
			Assert.AreEqual("\n\t\t\t\t   You can construct URLs and access their parts. For URLs that represent local files, you can also manipulate properties of those files directly.\n\t\t\t\t", ((Run)tbPreserve.Inlines[2]).Text);
			Assert.IsInstanceOfType(tbPreserve.Inlines[3], typeof(LineBreak));
			Assert.AreEqual("\n\t\t\t\t   AfterLineBreak\n\t\t", ((Run)tbPreserve.Inlines[4]).Text);
		}

		[TestMethod]
		public void When_Space_In_TextBlock_Inlines()
		{
			var page = new WhiteSpace_In_TextBlock_Inlines();
			var tbDefault = page.tbDefault;
			Assert.AreEqual("Word1", tbDefault.Inlines[0].GetText());
			Assert.AreEqual(" ", tbDefault.Inlines[1].GetText());
			Assert.AreEqual("Word2", tbDefault.Inlines[2].GetText());
			Assert.AreEqual(" ", tbDefault.Inlines[3].GetText());
			Assert.AreEqual("Word3", tbDefault.Inlines[4].GetText());
			Assert.AreEqual(" LeftSpace", tbDefault.Inlines[5].GetText());
			Assert.AreEqual("Word4", tbDefault.Inlines[6].GetText());
			Assert.AreEqual("RightSpace ", tbDefault.Inlines[7].GetText());
			Assert.AreEqual("Word5", tbDefault.Inlines[8].GetText());
			Assert.AreEqual(" SurroundSpace ", tbDefault.Inlines[9].GetText());
			Assert.AreEqual("Word6", tbDefault.Inlines[10].GetText());
			Assert.AreEqual("Middle Space", tbDefault.Inlines[11].GetText());
			Assert.AreEqual("Word7", tbDefault.Inlines[12].GetText());
			Assert.AreEqual(" DoubleSpace", tbDefault.Inlines[13].GetText());

			var tbPreserve = page.tbPreserve;
			Assert.AreEqual("Word1", tbPreserve.Inlines[0].GetText());
			Assert.AreEqual(" ", tbPreserve.Inlines[1].GetText());
			Assert.AreEqual("Word2", tbPreserve.Inlines[2].GetText());
			Assert.AreEqual("  ", tbPreserve.Inlines[3].GetText());
			Assert.AreEqual("Word3", tbPreserve.Inlines[4].GetText());
			Assert.AreEqual(" LeftSpace", tbPreserve.Inlines[5].GetText());
			Assert.AreEqual("Word4", tbPreserve.Inlines[6].GetText());
			Assert.AreEqual("RightSpace ", tbPreserve.Inlines[7].GetText());
			Assert.AreEqual("Word5", tbPreserve.Inlines[8].GetText());
			Assert.AreEqual(" SurroundSpace ", tbPreserve.Inlines[9].GetText());
			Assert.AreEqual("Word6", tbPreserve.Inlines[10].GetText());
			Assert.AreEqual("Middle Space", tbPreserve.Inlines[11].GetText());
			Assert.AreEqual("Word7 ", tbPreserve.Inlines[12].GetText());
			Assert.AreEqual("  DoubleSpace  ", tbPreserve.Inlines[13].GetText());

			var tbFrontText = page.tbFrontText;
			Assert.AreEqual("Word0", tbFrontText.Inlines[0].GetText());
			Assert.AreEqual("Word1", tbFrontText.Inlines[1].GetText());
			Assert.AreEqual("Word2", tbFrontText.Inlines[2].GetText());
			Assert.AreEqual(" ", tbFrontText.Inlines[3].GetText());
			Assert.AreEqual("Word3", tbFrontText.Inlines[4].GetText());
			Assert.AreEqual(" LeftSpace", tbFrontText.Inlines[5].GetText());
			Assert.AreEqual("Word4", tbFrontText.Inlines[6].GetText());
			Assert.AreEqual("RightSpace ", tbFrontText.Inlines[7].GetText());
			Assert.AreEqual("Word5", tbFrontText.Inlines[8].GetText());
			Assert.AreEqual(" SurroundSpace ", tbFrontText.Inlines[9].GetText());
			Assert.AreEqual("Word6", tbFrontText.Inlines[10].GetText());
			Assert.AreEqual("Middle Space", tbFrontText.Inlines[11].GetText());
			Assert.AreEqual("Word7", tbFrontText.Inlines[12].GetText());
			Assert.AreEqual(" DoubleSpace", tbFrontText.Inlines[13].GetText());

			var tbFrontTextPreserve = page.tbFrontTextPreserve;
			Assert.AreEqual("Word0", tbFrontTextPreserve.Inlines[0].GetText());
			Assert.AreEqual("Word1", tbFrontTextPreserve.Inlines[1].GetText());
			Assert.AreEqual("Word2", tbFrontTextPreserve.Inlines[2].GetText());
			Assert.AreEqual("  ", tbFrontTextPreserve.Inlines[3].GetText());
			Assert.AreEqual("Word3", tbFrontTextPreserve.Inlines[4].GetText());
			Assert.AreEqual(" LeftSpace", tbFrontTextPreserve.Inlines[5].GetText());
			Assert.AreEqual("Word4", tbFrontTextPreserve.Inlines[6].GetText());
			Assert.AreEqual("RightSpace ", tbFrontTextPreserve.Inlines[7].GetText());
			Assert.AreEqual("Word5", tbFrontTextPreserve.Inlines[8].GetText());
			Assert.AreEqual(" SurroundSpace ", tbFrontTextPreserve.Inlines[9].GetText());
			Assert.AreEqual("Word6", tbFrontTextPreserve.Inlines[10].GetText());
			Assert.AreEqual("Middle Space", tbFrontTextPreserve.Inlines[11].GetText());
			Assert.AreEqual("Word7 ", tbFrontTextPreserve.Inlines[12].GetText());
			Assert.AreEqual("  DoubleSpace  ", tbFrontTextPreserve.Inlines[13].GetText());


			var tbInnerSpace = page.tbInnerSpace;
			Assert.AreEqual("Word0", tbInnerSpace.Inlines[0].GetText());
			Assert.AreEqual("", tbInnerSpace.Inlines[1].GetText());
			Assert.AreEqual("Word1", tbInnerSpace.Inlines[2].GetText());

			var tbInnerSpacePreserve = page.tbInnerSpacePreserve;
			Assert.AreEqual("Word0", tbInnerSpacePreserve.Inlines[0].GetText());
			Assert.AreEqual("   ", tbInnerSpacePreserve.Inlines[1].GetText());
			Assert.AreEqual("Word1", tbInnerSpacePreserve.Inlines[2].GetText());
		}
	}

	public partial class MyControl : Control { }
}
