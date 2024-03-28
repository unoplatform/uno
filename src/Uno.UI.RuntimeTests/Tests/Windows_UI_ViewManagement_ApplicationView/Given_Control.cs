using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.Extensions;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Windows.Foundation;
using Windows.UI.Xaml;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_ViewManagement_ApplicationView
{
	[TestClass]
	public class Given_ApplicationView
	{
		public static Rect StartupVisibleBounds { get; set; }

		public static string StartupTitle { get; set; }

#if __SKIA__
		[TestMethod]
		public void When_StartupTitle_Is_Defined()
		{
			if (TestServices.WindowHelper.IsXamlIsland)
			{
				return;
			}

			Assert.AreEqual(Windows.ApplicationModel.Package.Current.DisplayName, StartupTitle);
		}
#endif

#if __ANDROID__
		[TestMethod]
		public void When_StartupVisibleBounds_Has_Value()
		{
			Assert.IsFalse(RectHelper.GetIsEmpty(StartupVisibleBounds), $"VisibleBounds should not be empty");
		}
#endif
	}
}
