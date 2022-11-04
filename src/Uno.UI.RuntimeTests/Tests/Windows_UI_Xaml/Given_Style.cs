using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.Extensions;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Windows.UI.Xaml;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml
{
	[TestClass]
	public class Given_Style
	{
#if !NETFX_CORE // Control template does not support lambda parameter
		[TestMethod]
		[RunsOnUIThread]
		public void When_StyleFailsToApply()
		{
			var style = new Style()
			{
				Setters =
				{
					new Setter(ContentControl.TemplateProperty, new ControlTemplate(() => throw new Exception("Inner exception")))
				}
			};

			var e = Assert.ThrowsException<Exception>(() => new ContentControl() { Style = style });
			Assert.AreEqual("Inner exception", e.Message);
		}
#endif
	}
}
