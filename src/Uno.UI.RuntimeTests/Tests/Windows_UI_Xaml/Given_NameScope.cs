using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml
{
	[TestClass]
	public partial class Given_NameScope
	{
#if !WINDOWS_UWP // NameScope isn't a public API in UWP
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Inside_ResourceDictionary()
		{
			var SUT = new Given_NameScope_When_Inside_ResourceDictionary();
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForIdle();
			var namescope = NameScope.GetNameScope(SUT);
			var brush = (SolidColorBrush)namescope.FindName("MyBrush");
			Assert.AreEqual(Colors.Blue, brush.Color);
		}
#endif
	}
}
