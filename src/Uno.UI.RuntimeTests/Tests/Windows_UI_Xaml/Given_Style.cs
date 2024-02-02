using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.Extensions;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Microsoft.UI.Xaml;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml
{
	[TestClass]
	public class Given_Style
	{
#if !WINAPPSDK // Control template does not support lambda parameter
		[TestMethod]
		[RunsOnUIThread]
		public void When_StyleFailsToApply()
		{
			var style = new Style()
			{
				Setters =
				{
					new Setter(ContentControl.TemplateProperty, new ControlTemplate(() => throw new("Inner exception")))
				}
			};

			var e = Assert.ThrowsException<Exception>(() => new ContentControl() { Style = style });
			Assert.AreEqual("Inner exception", e.Message);
		}
#endif
	}
}
