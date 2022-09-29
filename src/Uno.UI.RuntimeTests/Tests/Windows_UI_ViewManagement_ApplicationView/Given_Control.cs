#nullable disable

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.Extensions;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_ViewManagement_ApplicationView
{
	[TestClass]
	public class Given_ApplicationView
	{
		public static string StartupTitle { get; set; }

#if __SKIA__
		[TestMethod]
		public async Task When_StartupTitle_Is_Defined()
		{
			Assert.AreEqual(Windows.ApplicationModel.Package.Current.DisplayName, StartupTitle);
		}
#endif
	}
}
