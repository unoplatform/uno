using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions;
using Uno.UI.Tests.Windows_UI_XAML_Controls.PopupTests.Controls;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace Uno.UI.Tests.PivotTests
{
	[TestClass]
	public class Given_Popup
	{
		[TestMethod]
		public void When_Popup()
		{
			var SUT = new When_Popup();

			Assert.IsTrue(SUT.FindName("myPopup")?.GetType() == typeof(Windows.UI.Xaml.Controls.Primitives.Popup));
		}
	}
}
