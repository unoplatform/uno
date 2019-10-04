using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Tests.Windows_UI_XAML_Controls.TextBlockTests
{
	[TestClass]
	public class Given_TextBlock
	{
		private const double DefaultFontSize = 15.0; //TODO: This should change to 14.0 when Uno is aligned to a newer version of the UWP SDK

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
	}

	public partial class MyControl : Control { }
}
